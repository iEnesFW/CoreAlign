import { useEffect, useMemo, useRef } from 'react';
import { DoubleSide, Raycaster } from 'three';
import { useThree } from '@react-three/fiber';
import { clearSnapGuides, setSnapGuides } from '@/shared/three-engine';
import { applyPlanMoveSnap } from './planSnap';
import {
  RUN_PLAN_THICKNESS_MM,
  buildPlanFootprint,
  clampPlanMove,
  penetratesAny,
} from './planCollision';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group, Mesh, MeshBasicMaterial, Object3D } from 'three';
import type { PlanFootprint } from './planCollision';
import type { PlanPoint, PlanSnapTargets } from './planSnap';
import type { PlacementKind } from '../../model/designerStore';
import type { SceneRunState, SceneWallState } from '../../model/project.types';

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
  walls: SceneWallState[];
  snapTargets: PlanSnapTargets;
  obstacles: PlanFootprint[];
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
const PLANE_SIZE_M = 400;
const DEG2RAD = Math.PI / 180;
const STRUCTURE_MIN_Y_MM = 200;

const snapToPlaceGrid = (valueMm: number) => Math.round(valueMm / PLACE_GRID_MM) * PLACE_GRID_MM;

const nearestRunHeightMm = (runs: SceneRunState[], xMm: number, yMm: number): number | null => {
  let bestHeight: number | null = null;
  let bestDist = Number.POSITIVE_INFINITY;
  for (const run of runs) {
    const rad = run.rotationDeg * DEG2RAD;
    const cx = run.originX + (run.lengthMm / 2) * Math.cos(rad);
    const cy = run.originY + (run.lengthMm / 2) * Math.sin(rad);
    const dist = Math.hypot(cx - xMm, cy - yMm);
    if (dist < bestDist) {
      bestDist = dist;
      bestHeight = run.heightMm;
    }
  }
  return bestHeight;
};

const distanceToSpineMm = (
  originX: number,
  originY: number,
  lengthMm: number,
  rotationDeg: number,
  xMm: number,
  yMm: number,
): number => {
  const rad = rotationDeg * DEG2RAD;
  const ex = originX + lengthMm * Math.cos(rad);
  const ey = originY + lengthMm * Math.sin(rad);
  const vx = ex - originX;
  const vy = ey - originY;
  const lenSq = vx * vx + vy * vy;
  const t =
    lenSq === 0
      ? 0
      : Math.min(1, Math.max(0, ((xMm - originX) * vx + (yMm - originY) * vy) / lenSq));
  return Math.hypot(originX + t * vx - xMm, originY + t * vy - yMm);
};

const roofElevationAt = (
  runs: SceneRunState[],
  walls: SceneWallState[],
  xMm: number,
  yMm: number,
): number => {
  let bestTop: number | null = null;
  let bestDist = Number.POSITIVE_INFINITY;
  for (const run of runs) {
    const dist = distanceToSpineMm(
      run.originX,
      run.originY,
      run.lengthMm,
      run.rotationDeg,
      xMm,
      yMm,
    );
    if (dist < bestDist) {
      bestDist = dist;
      bestTop = (run.geomZ ?? 0) + run.heightMm;
    }
  }
  for (const wall of walls) {
    const dist = distanceToSpineMm(
      wall.originX,
      wall.originY,
      wall.lengthMm,
      wall.rotationDeg,
      xMm,
      yMm,
    );
    if (dist < bestDist) {
      bestDist = dist;
      bestTop = Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm);
    }
  }
  return bestTop === null ? ROOF_FALLBACK_ELEVATION_MM : bestTop;
};

export function PlacementController({
  placement,
  runs,
  walls,
  snapTargets,
  obstacles,
  onPlaceWall,
  onPlaceRun,
  onPlaceSlab,
}: PlacementControllerProps) {
  const scene = useThree((s) => s.scene);
  const raycaster = useMemo(() => new Raycaster(), []);
  const ghostRef = useRef<Group>(null);
  const planeMeshRef = useRef<Mesh>(null);
  const meshRef = useRef<Mesh>(null);
  const matRef = useRef<MeshBasicMaterial>(null);
  const freeRef = useRef<PlanPoint | null>(null);
  const posRef = useRef<PlanPoint | null>(null);
  const blockedRef = useRef(false);
  const downRef = useRef({ x: 0, y: 0 });
  const rotationRef = useRef(0);

  const isLine = placement === 'wall' || placement === 'run';

  useEffect(() => {
    freeRef.current = null;
    posRef.current = null;
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

  const probes: PlanPoint[] = isLine
    ? [
        { x: -LINE_LENGTH_MM / 2, y: 0 },
        { x: LINE_LENGTH_MM / 2, y: 0 },
      ]
    : [
        { x: -SLAB_LENGTH_MM / 2, y: -SLAB_DEPTH_MM / 2 },
        { x: SLAB_LENGTH_MM / 2, y: -SLAB_DEPTH_MM / 2 },
        { x: SLAB_LENGTH_MM / 2, y: SLAB_DEPTH_MM / 2 },
        { x: -SLAB_LENGTH_MM / 2, y: SLAB_DEPTH_MM / 2 },
      ];

  const lineStart = (xMm: number, yMm: number) => {
    const rad = rotationRef.current * DEG2RAD;
    return {
      x: xMm - (LINE_LENGTH_MM / 2) * Math.cos(rad),
      y: yMm - (LINE_LENGTH_MM / 2) * Math.sin(rad),
    };
  };

  const ghostFootprintAt = (xMm: number, yMm: number, heightMm: number): PlanFootprint => {
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
    const elevationMm =
      placement === 'floor' ? FLOOR_ELEVATION_MM : roofElevationAt(runs, walls, xMm, yMm);
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

  const applyGhost = (xMm: number, yMm: number, heightMm: number, blocked: boolean) => {
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
      const elevationM =
        (placement === 'floor' ? FLOOR_ELEVATION_MM : roofElevationAt(runs, walls, xMm, yMm)) / MM;
      mesh.scale.set(SLAB_LENGTH_MM / MM, SLAB_THICKNESS_MM / MM, SLAB_DEPTH_MM / MM);
      mesh.position.set(0, elevationM + SLAB_THICKNESS_MM / MM / 2, 0);
    }
    mat.color.set(blocked ? BLOCKED_COLOR : GHOST_COLOR);
  };

  const applyAt = (
    targetX: number,
    targetY: number,
    guides: ReturnType<typeof applyPlanMoveSnap>['guides'],
  ) => {
    let x = targetX;
    let y = targetY;
    const heightMm = isLine ? lineHeightAt(x, y) : 0;
    const blocked = penetratesAny(ghostFootprintAt(x, y, heightMm), obstacles);
    if (blocked && freeRef.current) {
      const from = freeRef.current;
      const clamped = clampPlanMove(
        (dx, dy) => ghostFootprintAt(from.x + dx, from.y + dy, heightMm),
        obstacles,
        x - from.x,
        y - from.y,
      );
      x = from.x + clamped.dxMm;
      y = from.y + clamped.dyMm;
    }
    const stillBlocked = penetratesAny(ghostFootprintAt(x, y, heightMm), obstacles);
    if (!stillBlocked) freeRef.current = { x, y };
    posRef.current = { x, y };
    blockedRef.current = stillBlocked;
    setSnapGuides(stillBlocked ? [] : guides);
    applyGhost(x, y, heightMm, stillBlocked);
  };

  // The XZ of the structure the cursor points at (run/wall/slab top), so a roof lands
  // where you POINT — not on the cursor's ground projection, which parallaxes away in a
  // perspective view ("mouse treated as ground"). Ground disk / grid (y≈0) are ignored.
  const pickStructureXZ = (e: ThreeEvent<PointerEvent>): PlanPoint | null => {
    raycaster.set(e.ray.origin, e.ray.direction);
    for (const hit of raycaster.intersectObjects(scene.children, true)) {
      if (hit.point.y * MM <= STRUCTURE_MIN_Y_MM) continue;
      // Skip floating dimension labels (troika <Text> carries a string `text` prop) so a
      // label hovering above a structure can't hijack the placement XZ.
      if (typeof (hit.object as { text?: unknown }).text === 'string') continue;
      let o: Object3D | null = hit.object;
      let owned = false;
      while (o) {
        if (o === ghostRef.current || o === planeMeshRef.current) {
          owned = true;
          break;
        }
        o = o.parent;
      }
      if (owned) continue;
      return { x: hit.point.x * MM, y: hit.point.z * MM };
    }
    return null;
  };

  const followPointer = (e: ThreeEvent<PointerEvent>) => {
    const gridX = snapToPlaceGrid(e.point.x * MM);
    const gridY = snapToPlaceGrid(e.point.z * MM);
    // A slab (roof/floor) is large; snapping its footprint probes to every wall/run
    // corner yanks it far off the cursor. Slabs follow the cursor on the grid; a roof
    // additionally lands on the structure under the cursor so it's easy to position.
    if (!isLine) {
      const hit = placement === 'roof' ? pickStructureXZ(e) : null;
      const x = hit ? snapToPlaceGrid(hit.x) : gridX;
      const y = hit ? snapToPlaceGrid(hit.y) : gridY;
      applyAt(x, y, []);
      return;
    }
    const stuck = applyPlanMoveSnap(probes, gridX, gridY, snapTargets);
    applyAt(stuck.dxMm, stuck.dyMm, stuck.guides);
  };

  const applyAtRef = useRef(applyAt);
  useEffect(() => {
    applyAtRef.current = applyAt;
  });

  useEffect(() => {
    if (!placement || !isLine) return;
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      e.stopPropagation();
      const step = e.deltaY > 0 ? 90 : -90;
      rotationRef.current = (((rotationRef.current + step) % 360) + 360) % 360;
      const pos = posRef.current;
      if (pos) applyAtRef.current(pos.x, pos.y, []);
    };
    window.addEventListener('wheel', onWheel, { passive: false, capture: true });
    return () => window.removeEventListener('wheel', onWheel, true);
  }, [placement, isLine]);

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    downRef.current = { x: e.nativeEvent.clientX, y: e.nativeEvent.clientY };
  };

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const dx = e.nativeEvent.clientX - downRef.current.x;
    const dy = e.nativeEvent.clientY - downRef.current.y;
    if (dx * dx + dy * dy > CLICK_SLOP_PX * CLICK_SLOP_PX) return;
    const pos = posRef.current;
    if (!pos || blockedRef.current) return;
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
        placement === 'floor' ? FLOOR_ELEVATION_MM : roofElevationAt(runs, walls, pos.x, pos.y),
    });
  };

  return (
    <>
      <mesh
        ref={planeMeshRef}
        rotation={[-Math.PI / 2, 0, 0]}
        onPointerDown={handlePointerDown}
        onPointerMove={followPointer}
        onClick={handleClick}
      >
        <planeGeometry args={[PLANE_SIZE_M, PLANE_SIZE_M]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
      </mesh>
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
    </>
  );
}
