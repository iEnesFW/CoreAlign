import { useEffect, useMemo, useRef } from 'react';
import { Box3, Plane, Raycaster, Vector2, Vector3 } from 'three';
import { useThree } from '@react-three/fiber';
import {
  clearSnapGuides,
  DESIGNER_ROOT_NAME,
  setSnapGuides,
  supportTopBelowMm,
  SUPPORT_TOLERANCE_MM,
} from '@/shared/three-engine';
import { applyPlanMoveSnap } from './planSnap';
import { bodyEndLocalMm } from '../../geometry/curvature';
import {
  RUN_PLAN_THICKNESS_MM,
  buildPlanFootprint,
  clampPlanMove,
  penetratesAny,
} from './planCollision';
import type { Group, Mesh, MeshBasicMaterial, Object3D } from 'three';
import type { PlanFootprint } from './planCollision';
import { useTranslation } from 'react-i18next';
import { notifyPlacementBlocked } from './stackFeedback';
import type { PlanPoint, PlanSnapTargets } from './planSnap';
import type { PlacementKind } from '../../model/designerStore';
import type { SceneRunState } from '../../model/project.types';

export interface PlacementWallDraft {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm: number;
  thicknessMm: number;
}

export interface PlacementRunDraft {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm: number;
}

export interface PlacementSlabDraft {
  originX: number;
  originY: number;
  lengthMm: number;
  depthMm: number;
  thicknessMm: number;
  elevationMm: number;
}

interface PlacementControllerProps {
  placement: PlacementKind;
  runs: SceneRunState[];
  snapTargets: PlanSnapTargets;
  obstacles: PlanFootprint[];
  roofSupports: PlanFootprint[];
  onPlaceWall: (draft: PlacementWallDraft) => void;
  onPlaceRun: (draft: PlacementRunDraft) => void;
  onPlaceSlab: (kind: 'floor' | 'roof', draft: PlacementSlabDraft) => void;
}

const MM = 1000;
const GHOST_ID = 'placement-ghost';
const GHOST_COLOR = '#2563eb';
const BLOCKED_COLOR = '#dc2626';
const GHOST_OPACITY = 0.35;
const PLACE_GRID_MM = 50;
const LINE_LENGTH_MM = 3000;
const WALL_THICKNESS_MM = 200;
const WALL_FALLBACK_HEIGHT_MM = 2600;
const WALL_HEIGHT_MARGIN_MM = 200;
const RUN_HEIGHT_MM = 2400;
const SLAB_LENGTH_MM = 3000;
const SLAB_DEPTH_MM = 2000;
const SLAB_THICKNESS_MM = 150;
const FLOOR_ELEVATION_MM = -150;
const ROOF_FALLBACK_ELEVATION_MM = 2450;
const CLICK_SLOP_PX = 5;
const DEG2RAD = Math.PI / 180;
const STRUCTURE_MIN_Y_MM = 200;

const snapToPlaceGrid = (valueMm: number) => Math.round(valueMm / PLACE_GRID_MM) * PLACE_GRID_MM;

const nearestRunHeightMm = (runs: SceneRunState[], xMm: number, yMm: number): number | null => {
  let bestHeight: number | null = null;
  let bestDist = Number.POSITIVE_INFINITY;
  for (const run of runs) {
    // WHY bodyEndLocalMm: on an ARC run rotationDeg is the ROLLED start tangent, so
    // origin + length/2 · dir(rotationDeg) is a phantom point that sits ~0.3·R off the real band.
    // The plan canvas already measures the same midpoint this way (planBodyCenterMm).
    const rad = run.rotationDeg * DEG2RAD;
    const end = bodyEndLocalMm(run);
    const cos = Math.cos(rad);
    const sin = Math.sin(rad);
    const midX = end.xMm / 2;
    const midY = end.yMm / 2;
    const cx = run.originX + midX * cos - midY * sin;
    const cy = run.originY + midX * sin + midY * cos;
    const dist = Math.hypot(cx - xMm, cy - yMm);
    if (dist < bestDist) {
      bestDist = dist;
      bestHeight = run.heightMm;
    }
  }
  return bestHeight;
};

/**
 * The top of whatever actually sits under this footprint.
 *
 * WHY this replaced a nearest-spine guess: the old resolver measured the distance to each run's and
 * wall's CENTRELINE and took the closest one — so a roof dropped beside a building landed at the
 * height of a wall it was not over at all, and a wall's top was read as `heightMm` with its
 * `geomZ` IGNORED (a wall standing on a deck reported the height it would have had on the ground).
 * Slabs and surfaces were not considered at all, so a roof could not be placed on another roof.
 * This uses the same overlap-based support resolver the gravity model uses, over the same footprint
 * set, so placement and settling can never disagree.
 */
const NO_SUPPORT_MM = Number.NEGATIVE_INFINITY;

// How far a roof may reach to find the structure it spans. A roof BRIDGES walls: dropped inside a
// room it touches none of them in plan, so an overlap-only lookup finds nothing but the floor.
const BRIDGE_REACH_MM = 4000;

const aabbOf = (f: PlanFootprint) => {
  if (f.polygon && f.polygon.length > 0) {
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    for (const v of f.polygon) {
      minX = Math.min(minX, v.x);
      maxX = Math.max(maxX, v.x);
      minY = Math.min(minY, v.y);
      maxY = Math.max(maxY, v.y);
    }
    return { minX, maxX, minY, maxY };
  }
  const h = f.halfWidthMm;
  return {
    minX: Math.min(f.x1, f.x2) - h,
    maxX: Math.max(f.x1, f.x2) + h,
    minY: Math.min(f.y1, f.y2) - h,
    maxY: Math.max(f.y1, f.y2) + h,
  };
};

const aabbGapMm = (a: PlanFootprint, b: PlanFootprint) => {
  const p = aabbOf(a);
  const q = aabbOf(b);
  const dx = Math.max(0, Math.max(p.minX - q.maxX, q.minX - p.maxX));
  const dy = Math.max(0, Math.max(p.minY - q.maxY, q.minY - p.maxY));
  return Math.hypot(dx, dy);
};

/**
 * The height a ROOF should land at.
 *
 * WHY this is not the plain overlap resolver: a roof rests on the structure it SPANS, and a slab
 * dropped inside a room overlaps none of the perimeter walls in plan — only the floor. Feeding it
 * the generic support set therefore parked the roof at floor level ("çatı direkt yere yapışıyor").
 * `roofSupports` deliberately excludes floors; when nothing is directly under the ghost it reaches
 * out to the tallest structure within BRIDGE_REACH_MM, which is what "put it on the walls" means.
 */
const roofSupportTopMm = (ghost: PlanFootprint, roofSupports: PlanFootprint[]): number | null => {
  const overlapping = supportTopBelowMm(
    ghost,
    roofSupports,
    Number.POSITIVE_INFINITY,
    NO_SUPPORT_MM,
    SUPPORT_TOLERANCE_MM,
  );
  if (overlapping !== NO_SUPPORT_MM) return overlapping;

  let best = NO_SUPPORT_MM;
  for (const s of roofSupports) {
    if (s.ownerId === ghost.ownerId) continue;
    if (s.zMaxMm <= best) continue;
    if (aabbGapMm(ghost, s) > BRIDGE_REACH_MM) continue;
    best = s.zMaxMm;
  }
  return best === NO_SUPPORT_MM ? null : best;
};

export function PlacementController({
  placement,
  runs,
  snapTargets,
  obstacles,
  roofSupports,
  onPlaceWall,
  onPlaceRun,
  onPlaceSlab,
}: PlacementControllerProps) {
  const { t } = useTranslation();
  const scene = useThree((s) => s.scene);
  const camera = useThree((s) => s.camera);
  const gl = useThree((s) => s.gl);
  const raycaster = useMemo(() => new Raycaster(), []);
  // A mathematical ground plane (y=0). The cursor's XZ comes from intersecting the pointer ray
  // with THIS, not a mesh — so the preview tracks the cursor even when it is over another object
  // (object meshes call stopPropagation and would otherwise swallow the ground mesh's events).
  const groundPlane = useMemo(() => new Plane(new Vector3(0, 1, 0), 0), []);
  const ndc = useMemo(() => new Vector2(), []);
  const hitPoint = useMemo(() => new Vector3(), []);
  const objectBox = useMemo(() => new Box3(), []);
  const ghostRef = useRef<Group>(null);
  const meshRef = useRef<Mesh>(null);
  const matRef = useRef<MeshBasicMaterial>(null);
  const freeRef = useRef<PlanPoint | null>(null);
  const posRef = useRef<PlanPoint | null>(null);
  const elevationRef = useRef<number | null>(null);
  const blockedRef = useRef(false);
  const downRef = useRef({ x: 0, y: 0, button: 0 });
  const rotationRef = useRef(0);

  const isLine = placement === 'wall' || placement === 'run';

  useEffect(() => {
    freeRef.current = null;
    posRef.current = null;
    elevationRef.current = null;
    blockedRef.current = false;
    rotationRef.current = 0;
    const ghost = ghostRef.current;
    if (ghost) ghost.visible = false;
    clearSnapGuides();
    return () => clearSnapGuides();
  }, [placement]);

  const lineHeightAt = (xMm: number, yMm: number) => {
    if (placement === 'run') return RUN_HEIGHT_MM;
    const nearest = nearestRunHeightMm(runs, xMm, yMm);
    return nearest === null ? WALL_FALLBACK_HEIGHT_MM : nearest + WALL_HEIGHT_MARGIN_MM;
  };

  /**
   * The ghost's two ENDS in plan, rotated to its current heading.
   *
   * WHY a function and not an array built at render: the wheel turns the ghost through
   * `rotationRef` WITHOUT re-rendering, so a captured array keeps the original heading — the snap
   * then measured the UNROTATED ends and pulled the body toward a corner it was not near. The
   * footprint (`ghostFootprintAt`) already rotates, so the two disagreed.
   *
   * Only the line path uses probes; a slab returns before the snap (it follows the cursor).
   */
  const lineProbes = (): PlanPoint[] => {
    const rad = rotationRef.current * DEG2RAD;
    const half = LINE_LENGTH_MM / 2;
    const cos = Math.cos(rad);
    const sin = Math.sin(rad);
    return [
      { x: -half * cos, y: -half * sin },
      { x: half * cos, y: half * sin },
    ];
  };

  const lineStart = (xMm: number, yMm: number) => {
    const rad = rotationRef.current * DEG2RAD;
    return {
      x: xMm - (LINE_LENGTH_MM / 2) * Math.cos(rad),
      y: yMm - (LINE_LENGTH_MM / 2) * Math.sin(rad),
    };
  };

  const slabElevationAt = (xMm: number, yMm: number, restOnTopMm?: number): number => {
    if (placement === 'floor') return FLOOR_ELEVATION_MM;
    // A roof rests on the surface directly under the cursor (the hovered object's top) when the
    // ray hits one; otherwise it rests on whatever its own footprint OVERLAPS. The ghost's own z
    // is irrelevant to that lookup (the resolver only tests plan overlap), so it is safe to probe
    // with a ground-level ghost before the elevation is known.
    if (restOnTopMm !== undefined) return restOnTopMm;
    return (
      roofSupportTopMm(ghostFootprintAt(xMm, yMm, 0, 0), roofSupports) ?? ROOF_FALLBACK_ELEVATION_MM
    );
  };

  const ghostFootprintAt = (
    xMm: number,
    yMm: number,
    heightMm: number,
    restOnTopMm?: number,
  ): PlanFootprint => {
    if (isLine) {
      const halfWidthMm = placement === 'wall' ? WALL_THICKNESS_MM / 2 : RUN_PLAN_THICKNESS_MM / 2;
      const start = lineStart(xMm, yMm);
      return buildPlanFootprint(
        GHOST_ID,
        start.x,
        start.y,
        LINE_LENGTH_MM,
        rotationRef.current,
        halfWidthMm,
        0,
        heightMm,
      );
    }
    const elevationMm = slabElevationAt(xMm, yMm, restOnTopMm);
    return buildPlanFootprint(
      GHOST_ID,
      xMm - SLAB_LENGTH_MM / 2,
      yMm,
      SLAB_LENGTH_MM,
      0,
      SLAB_DEPTH_MM / 2,
      elevationMm,
      elevationMm + SLAB_THICKNESS_MM,
    );
  };

  const applyGhost = (
    xMm: number,
    yMm: number,
    heightMm: number,
    blocked: boolean,
    restOnTopMm?: number,
  ) => {
    const ghost = ghostRef.current;
    const mesh = meshRef.current;
    const mat = matRef.current;
    if (!ghost || !mesh || !mat) return;
    ghost.visible = true;
    ghost.position.set(xMm / MM, 0, yMm / MM);
    ghost.rotation.y = isLine ? -rotationRef.current * DEG2RAD : 0;
    if (isLine) {
      const heightM = heightMm / MM;
      const thickM = (placement === 'wall' ? WALL_THICKNESS_MM : RUN_PLAN_THICKNESS_MM) / MM;
      mesh.scale.set(LINE_LENGTH_MM / MM, heightM, thickM);
      mesh.position.set(0, heightM / 2, 0);
    } else {
      const elevationM = slabElevationAt(xMm, yMm, restOnTopMm) / MM;
      mesh.scale.set(SLAB_LENGTH_MM / MM, SLAB_THICKNESS_MM / MM, SLAB_DEPTH_MM / MM);
      mesh.position.set(0, elevationM + SLAB_THICKNESS_MM / MM / 2, 0);
    }
    mat.color.set(blocked ? BLOCKED_COLOR : GHOST_COLOR);
  };

  const applyAt = (
    targetX: number,
    targetY: number,
    guides: ReturnType<typeof applyPlanMoveSnap>['guides'],
    restOnTopMm?: number,
  ) => {
    let x = targetX;
    let y = targetY;
    const heightMm = isLine ? lineHeightAt(x, y) : 0;
    const blocked = penetratesAny(ghostFootprintAt(x, y, heightMm, restOnTopMm), obstacles);
    if (blocked && freeRef.current) {
      const from = freeRef.current;
      const clamped = clampPlanMove(
        (dx, dy) => ghostFootprintAt(from.x + dx, from.y + dy, heightMm, restOnTopMm),
        obstacles,
        x - from.x,
        y - from.y,
      );
      x = from.x + clamped.dxMm;
      y = from.y + clamped.dyMm;
    }
    const stillBlocked = penetratesAny(ghostFootprintAt(x, y, heightMm, restOnTopMm), obstacles);
    if (!stillBlocked) freeRef.current = { x, y };
    posRef.current = { x, y };
    // Remember the resolved roof elevation so the placed slab lands exactly where the ghost
    // previewed (on the hovered object's top), not on the nearest-spine fallback.
    elevationRef.current = isLine ? null : slabElevationAt(x, y, restOnTopMm);
    blockedRef.current = stillBlocked;
    setSnapGuides(stillBlocked ? [] : guides);
    applyGhost(x, y, heightMm, stillBlocked, restOnTopMm);
  };

  // The XZ of the structure the cursor points at (run/wall/slab top), so a roof lands
  // where you POINT — not on the cursor's ground projection, which parallaxes away in a
  // perspective view ("mouse treated as ground"). Uses the raycaster already set from the pointer.
  const pickStructureXZ = (): { x: number; y: number; elevationMm: number } | null => {
    // Only the designer geometry can be a placement target, so raycast that subtree instead of the
    // whole graph — the ground disc, the grid, the helpers and the lights were all being walked and
    // sorted on every pointer event before being discarded by the filters below.
    const root = scene.getObjectByName(DESIGNER_ROOT_NAME);
    for (const hit of raycaster.intersectObjects(root ? root.children : scene.children, true)) {
      if (hit.point.y * MM <= STRUCTURE_MIN_Y_MM) continue;
      // Skip floating dimension labels (troika <Text> carries a string `text` prop) so a
      // label hovering above a structure can't hijack the placement XZ.
      if (typeof (hit.object as { text?: unknown }).text === 'string') continue;
      let o: Object3D | null = hit.object;
      let owned = false;
      while (o) {
        if (o === ghostRef.current) {
          owned = true;
          break;
        }
        o = o.parent;
      }
      if (owned) continue;
      // Rest on the TOP of the hit object (its bounding-box max Y), not the hit face — otherwise
      // hovering a side face would seat the roof at mid-height and bury it INSIDE the wall (#2).
      objectBox.setFromObject(hit.object);
      const topMm = (Number.isFinite(objectBox.max.y) ? objectBox.max.y : hit.point.y) * MM;
      return { x: hit.point.x * MM, y: hit.point.z * MM, elevationMm: topMm };
    }
    return null;
  };

  // Drive the preview from raw client coords against a MATH plane (not a mesh), so it tracks the
  // cursor even over objects that stopPropagation. Returns the ground XZ in mm, or null off-plane.
  const groundFromClient = (clientX: number, clientY: number): PlanPoint | null => {
    const rect = gl.domElement.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return null;
    ndc.set(
      ((clientX - rect.left) / rect.width) * 2 - 1,
      -((clientY - rect.top) / rect.height) * 2 + 1,
    );
    raycaster.setFromCamera(ndc, camera);
    if (!raycaster.ray.intersectPlane(groundPlane, hitPoint)) return null;
    return { x: hitPoint.x * MM, y: hitPoint.z * MM };
  };

  const followPointer = (clientX: number, clientY: number) => {
    const ground = groundFromClient(clientX, clientY);
    if (!ground) return;
    const gridX = snapToPlaceGrid(ground.x);
    const gridY = snapToPlaceGrid(ground.y);
    // A slab (roof/floor) is large; snapping its footprint probes to every wall/run
    // corner yanks it far off the cursor. Slabs follow the cursor on the grid; a roof
    // additionally lands on the structure under the cursor so it's easy to position.
    if (!isLine) {
      // raycaster is already set from this pointer (above) → pickStructureXZ reuses it.
      const hit = placement === 'roof' ? pickStructureXZ() : null;
      const x = hit ? snapToPlaceGrid(hit.x) : gridX;
      const y = hit ? snapToPlaceGrid(hit.y) : gridY;
      applyAt(x, y, [], hit?.elevationMm);
      return;
    }
    const stuck = applyPlanMoveSnap(lineProbes(), gridX, gridY, snapTargets);
    applyAt(stuck.dxMm, stuck.dyMm, stuck.guides);
  };

  const commitPlacement = () => {
    const pos = posRef.current;
    if (!pos) return;
    // A blocked click used to do NOTHING — no object, no reason, no sound. Users read that as the
    // tool being broken rather than the spot being occupied.
    if (blockedRef.current) {
      notifyPlacementBlocked(t);
      return;
    }
    clearSnapGuides();
    const start = lineStart(pos.x, pos.y);
    if (placement === 'wall') {
      onPlaceWall({
        originX: Math.round(start.x),
        originY: Math.round(start.y),
        rotationDeg: rotationRef.current,
        lengthMm: LINE_LENGTH_MM,
        heightMm: lineHeightAt(pos.x, pos.y),
        thicknessMm: WALL_THICKNESS_MM,
      });
      return;
    }
    if (placement === 'run') {
      onPlaceRun({
        originX: Math.round(start.x),
        originY: Math.round(start.y),
        rotationDeg: rotationRef.current,
        lengthMm: LINE_LENGTH_MM,
        heightMm: RUN_HEIGHT_MM,
      });
      return;
    }
    onPlaceSlab(placement === 'floor' ? 'floor' : 'roof', {
      originX: Math.round(pos.x - SLAB_LENGTH_MM / 2),
      originY: Math.round(pos.y - SLAB_DEPTH_MM / 2),
      lengthMm: SLAB_LENGTH_MM,
      depthMm: SLAB_DEPTH_MM,
      thicknessMm: SLAB_THICKNESS_MM,
      elevationMm:
        placement === 'floor'
          ? FLOOR_ELEVATION_MM
          : (elevationRef.current ??
            roofSupportTopMm(ghostFootprintAt(pos.x, pos.y, 0, 0), roofSupports) ??
            ROOF_FALLBACK_ELEVATION_MM),
    });
  };

  // Keep the latest closures in refs so the DOM listeners (registered once per placement kind)
  // never read stale obstacles / snapTargets / rotation.
  const followPointerRef = useRef(followPointer);
  const commitPlacementRef = useRef(commitPlacement);
  const reapplyRef = useRef<() => void>(() => {});
  useEffect(() => {
    followPointerRef.current = followPointer;
    commitPlacementRef.current = commitPlacement;
    reapplyRef.current = () => {
      const pos = posRef.current;
      if (pos) applyAt(pos.x, pos.y, []);
    };
  });

  useEffect(() => {
    if (!placement) return;
    const el = gl.domElement;
    // WHY coalesce to one call per frame: followPointer is not cheap — in roof mode it recursively
    // raycasts the WHOLE scene graph and, when the ray misses, resolves the bridged support height
    // several times over. A pointer stream fires far faster than the display refreshes, so every
    // extra call is work whose result is thrown away before it can be seen. useDrag3D already
    // rAF-throttles for exactly this reason (scheduleMove); this is the same guard.
    let pending: { x: number; y: number } | null = null;
    let frame = 0;
    const flush = () => {
      frame = 0;
      if (!pending) return;
      const { x, y } = pending;
      pending = null;
      followPointerRef.current(x, y);
    };
    const onMove = (e: PointerEvent) => {
      pending = { x: e.clientX, y: e.clientY };
      if (frame === 0) frame = requestAnimationFrame(flush);
    };
    const onDown = (e: PointerEvent) => {
      downRef.current = { x: e.clientX, y: e.clientY, button: e.button };
    };
    const onUp = (e: PointerEvent) => {
      // Placement is a LEFT click. Without this, panning the camera with the right button (or a
      // middle-button nudge) dropped a wall wherever the pointer happened to rest.
      if (e.button !== 0 || downRef.current.button !== 0) return;
      const dx = e.clientX - downRef.current.x;
      const dy = e.clientY - downRef.current.y;
      // Distinguish a placement click from an orbit drag (which moves the pointer further).
      if (dx * dx + dy * dy > CLICK_SLOP_PX * CLICK_SLOP_PX) return;
      commitPlacementRef.current();
    };
    const onWheel = (e: WheelEvent) => {
      if (!isLine) return;
      e.preventDefault();
      e.stopPropagation();
      const step = e.deltaY > 0 ? 90 : -90;
      rotationRef.current = (((rotationRef.current + step) % 360) + 360) % 360;
      reapplyRef.current();
    };
    el.addEventListener('pointermove', onMove);
    el.addEventListener('pointerdown', onDown);
    el.addEventListener('pointerup', onUp);
    // WHY the CANVAS and not window: on window this swallowed EVERY wheel event while a line
    // placement was armed, so the inspector and layer panels could not be scrolled at all — the
    // ghost just spun instead. Capture phase on the canvas still beats OrbitControls' own handler.
    el.addEventListener('wheel', onWheel, { passive: false, capture: true });
    return () => {
      if (frame !== 0) cancelAnimationFrame(frame);
      el.removeEventListener('pointermove', onMove);
      el.removeEventListener('pointerdown', onDown);
      el.removeEventListener('pointerup', onUp);
      el.removeEventListener('wheel', onWheel, true);
    };
  }, [placement, isLine, gl]);

  return (
    <group ref={ghostRef} visible={false}>
      <mesh ref={meshRef} raycast={() => null}>
        <boxGeometry args={[1, 1, 1]} />
        <meshBasicMaterial
          ref={matRef}
          color={GHOST_COLOR}
          transparent
          opacity={GHOST_OPACITY}
          depthWrite={false}
        />
      </mesh>
    </group>
  );
}
