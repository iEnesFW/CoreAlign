import { useEffect, useMemo, useRef, useState } from 'react';
import { Edges } from '@react-three/drei';
import { ExtrudeGeometry, Shape } from 'three';
import { toCreasedNormals } from 'three/examples/jsm/utils/BufferGeometryUtils.js';
import type { ThreeEvent } from '@react-three/fiber';
import type { BufferGeometry, Group, Texture } from 'three';
import {
  getProceduralTexture,
  isAltPressed,
  isProceduralMaterialKey,
  stickyDimensionMm,
  useDrag3D,
} from '@/shared/three-engine';
import { StretchFaces } from '../interaction/StretchFaces';
import { SurfaceVertexHandles } from '../interaction/SurfaceVertexHandles';
import { collectHeightLevels, snapToLevels } from '../interaction/levelSnap';
import { buildSurfaceFootprint, restElevationMm } from '../interaction/planCollision';
import { polygonSelfIntersects } from '../../model/polygonValidation';
import { useDesignerStore } from '../../model/designerStore';
import type { PlanFootprint } from '../interaction/planCollision';
import type { StretchFaceDef } from '../interaction/StretchFaces';
import type { SceneSurfacePoint, SceneSurfaceState } from '../../model/project.types';

interface PolygonSurfaceObjectProps {
  surface: SceneSurfaceState;
  isSelected: boolean;
  interactive: boolean;
  penActive?: boolean;
  supports?: PlanFootprint[];
  onSelect: (surfaceId: string) => void;
}

const EMPTY_SUPPORTS: PlanFootprint[] = [];

const IGNORE_RAYCAST = () => null;

const FLOOR_COLOR = '#b7bfc7';
const ROOF_COLOR = '#8c98a4';
const SELECTED_EDGE = '#1d4ed8';
const EDGE_COLOR = '#64748b';
const MM = 1000;
const FACE_LIFT_M = 0.002;
const MIN_THICKNESS_MM = 20;
const SMOOTH_CREASE_RAD = Math.PI / 6;
const EDGE_THRESHOLD_DEG = 25;

const boundsOf = (points: SceneSurfacePoint[]) => {
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;
  for (const p of points) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.y > maxY) maxY = p.y;
  }
  return { minX, maxX, minY, maxY };
};

const signedAreaMm = (points: SceneSurfacePoint[]): number => {
  let area = 0;
  for (let i = 0; i < points.length; i += 1) {
    const a = points[i];
    const b = points[(i + 1) % points.length];
    area += a.x * b.y - b.x * a.y;
  }
  return area / 2;
};

const buildGeometry = (
  points: SceneSurfacePoint[],
  thicknessMm: number,
  cx: number,
  cy: number,
): BufferGeometry | null => {
  if (points.length < 3) return null;
  const ccw = signedAreaMm(points) < 0 ? [...points].reverse() : points;
  const shape = new Shape();
  ccw.forEach((p, i) => {
    const x = (p.x - cx) / MM;
    const y = (p.y - cy) / MM;
    if (i === 0) shape.moveTo(x, y);
    else shape.lineTo(x, y);
  });
  shape.closePath();
  const thicknessM = thicknessMm / MM;
  const extrude = new ExtrudeGeometry(shape, { depth: thicknessM, bevelEnabled: false });
  extrude.rotateX(Math.PI / 2);
  extrude.translate(0, thicknessM, 0);
  const geometry = toCreasedNormals(extrude, SMOOTH_CREASE_RAD);
  extrude.dispose();
  return geometry;
};

export function PolygonSurfaceObject({
  surface,
  isSelected,
  interactive,
  penActive = false,
  supports,
  onSelect,
}: PolygonSurfaceObjectProps) {
  const activeTool = useDesignerStore((s) => s.activeTool);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const presentation = useDesignerStore((s) => s.presentationMode);
  const scene = useDesignerStore((s) => s.scene);
  const updateSurface = useDesignerStore((s) => s.updateSurface);
  const removeSurface = useDesignerStore((s) => s.removeSurface);

  const centroid = useMemo(() => {
    const cx = surface.points.reduce((sum, p) => sum + p.x, 0) / surface.points.length;
    const cy = surface.points.reduce((sum, p) => sum + p.y, 0) / surface.points.length;
    return { cx, cy };
  }, [surface.points]);

  const [previewPoints, setPreviewPoints] = useState<SceneSurfacePoint[] | null>(null);
  const livePoints = previewPoints ?? surface.points;

  const geometry = useMemo(
    () => buildGeometry(livePoints, surface.thicknessMm, centroid.cx, centroid.cy),
    [livePoints, surface.thicknessMm, centroid],
  );
  useEffect(() => () => geometry?.dispose(), [geometry]);

  const groupRef = useRef<Group>(null);
  const lastDeltaRef = useRef({ x: 0, y: 0 });
  const altLatchRef = useRef(false);
  const supportFootprints = supports ?? EMPTY_SUPPORTS;
  // Alt-drag rests the surface on whatever it overlaps (ground fallback); a plain
  // drag keeps its current elevation and just slides in plan. restElevationMm skips
  // this surface's own footprint by ownerId, so it never rests on itself.
  const restElevationAt = (dxMm: number, dyMm: number) =>
    restElevationMm(buildSurfaceFootprint(surface, dxMm, dyMm), supportFootprints, 0);

  const moveEnabled = interactive && activeTool === 'move' && !surface.locked;
  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: moveEnabled,
    onMove: (delta) => {
      const dx = Math.round(delta.x);
      const dy = Math.round(delta.z);
      lastDeltaRef.current = { x: dx, y: dy };
      const alt = isAltPressed();
      altLatchRef.current = alt;
      const elevMm = alt ? restElevationAt(dx, dy) : surface.elevationMm;
      groupRef.current?.position.set(
        (centroid.cx + delta.x) / MM,
        elevMm / MM,
        (centroid.cy + delta.z) / MM,
      );
    },
    onCommit: () => {
      const d = lastDeltaRef.current;
      const alt = altLatchRef.current;
      lastDeltaRef.current = { x: 0, y: 0 };
      altLatchRef.current = false;
      if (d.x !== 0 || d.y !== 0) {
        updateSurface(surface.id, {
          points: surface.points.map((p) => ({ x: p.x + d.x, y: p.y + d.y })),
          ...(alt ? { elevationMm: Math.round(restElevationAt(d.x, d.y)) } : {}),
        });
      } else {
        groupRef.current?.position.set(
          centroid.cx / MM,
          surface.elevationMm / MM,
          centroid.cy / MM,
        );
      }
    },
  });

  const texture: Texture | null =
    surface.materialKey && isProceduralMaterialKey(surface.materialKey)
      ? getProceduralTexture(surface.materialKey)
      : null;

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    if (drag.consumeClick()) return;
    if (!interactive) return;
    if (activeTool === 'erase') {
      removeSurface(surface.id);
      return;
    }
    if (activeTool === 'paint') {
      if (paintMaterial) updateSurface(surface.id, { materialKey: paintMaterial, colorHex: null });
      else if (paintColor)
        updateSurface(surface.id, { colorHex: paintColor.hex, materialKey: null });
      return;
    }
    onSelect(surface.id);
  };

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (moveEnabled && e.nativeEvent.button === 0) onSelect(surface.id);
    drag.handlers.onPointerDown(e);
  };

  const bounds = useMemo(() => boundsOf(livePoints), [livePoints]);
  const widthMm = bounds.maxX - bounds.minX;
  const depthMm = bounds.maxY - bounds.minY;
  const thicknessM = surface.thicknessMm / MM;
  const minXL = (bounds.minX - centroid.cx) / MM;
  const maxXL = (bounds.maxX - centroid.cx) / MM;
  const minYL = (bounds.minY - centroid.cy) / MM;
  const maxYL = (bounds.maxY - centroid.cy) / MM;

  const previewVertex = (index: number, xMm: number, yMm: number) =>
    setPreviewPoints(surface.points.map((p, i) => (i === index ? { x: xMm, y: yMm } : p)));
  const commitVertex = (index: number, xMm: number, yMm: number) => {
    setPreviewPoints(null);
    const cur = surface.points[index];
    if (!cur || (cur.x === xMm && cur.y === yMm)) return;
    const nextPoints = surface.points.map((p, i) => (i === index ? { x: xMm, y: yMm } : p));
    if (polygonSelfIntersects(nextPoints)) return;
    updateSurface(surface.id, { points: nextPoints });
  };

  const commitThickness = (deltaMm: number) => {
    const next = Math.max(MIN_THICKNESS_MM, stickyDimensionMm(surface.thicknessMm + deltaMm));
    if (next !== surface.thicknessMm) updateSurface(surface.id, { thicknessMm: next });
  };
  const heightLevels = collectHeightLevels(scene, surface.id);
  const commitElevation = (deltaMm: number) => {
    const next = snapToLevels(surface.elevationMm - deltaMm, heightLevels);
    if (next !== surface.elevationMm) updateSurface(surface.id, { elevationMm: next });
  };

  const stretchActive =
    interactive && activeTool === 'stretch' && Boolean(geometry) && !surface.locked;
  const stretchFaces: StretchFaceDef[] = stretchActive
    ? [
        {
          id: 'top',
          centerM: [(minXL + maxXL) / 2, thicknessM + FACE_LIFT_M, (minYL + maxYL) / 2],
          rotation: [-Math.PI / 2, 0, 0],
          widthM: widthMm / MM,
          heightM: depthMm / MM,
          axis: [0, 1, 0],
          label: (d) =>
            `${Math.max(MIN_THICKNESS_MM, stickyDimensionMm(surface.thicknessMm + d))} mm`,
          onPreview: () => {},
          onCommit: commitThickness,
        },
        {
          id: 'bottom',
          centerM: [(minXL + maxXL) / 2, -FACE_LIFT_M, (minYL + maxYL) / 2],
          rotation: [Math.PI / 2, 0, 0],
          widthM: widthMm / MM,
          heightM: depthMm / MM,
          axis: [0, -1, 0],
          label: (d) => `${Math.round(surface.elevationMm - d)} mm`,
          onPreview: () => {},
          onCommit: commitElevation,
        },
      ]
    : [];

  if (!geometry) return null;
  const baseColor = surface.kind === 'roof' ? ROOF_COLOR : FLOOR_COLOR;

  return (
    <group ref={groupRef} position={[centroid.cx / MM, surface.elevationMm / MM, centroid.cy / MM]}>
      <mesh
        geometry={geometry}
        castShadow
        receiveShadow
        raycast={penActive ? IGNORE_RAYCAST : undefined}
        {...drag.handlers}
        onPointerDown={handlePointerDown}
        onClick={handleClick}
        onPointerOver={(e) => {
          e.stopPropagation();
          document.body.style.cursor = moveEnabled ? 'grab' : 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
      >
        <meshStandardMaterial
          key={texture ? (surface.materialKey ?? 'plain') : 'plain'}
          color={texture ? '#ffffff' : (surface.colorHex ?? baseColor)}
          map={texture ?? undefined}
          roughness={0.85}
          metalness={0.05}
          emissive={isSelected ? SELECTED_EDGE : '#000000'}
          emissiveIntensity={isSelected ? 0.12 : 0}
        />
        {!presentation && (
          <Edges color={isSelected ? SELECTED_EDGE : EDGE_COLOR} threshold={EDGE_THRESHOLD_DEG} />
        )}
      </mesh>
      {stretchActive && <StretchFaces faces={stretchFaces} />}
      {stretchActive && (
        <SurfaceVertexHandles
          points={surface.points}
          centroidXMm={centroid.cx}
          centroidYMm={centroid.cy}
          topM={thicknessM}
          onPreview={previewVertex}
          onCommit={commitVertex}
        />
      )}
    </group>
  );
}
