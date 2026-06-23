import { useEffect, useMemo, useRef, useState } from 'react';
import { DoubleSide, Raycaster } from 'three';
import { Html, Line } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import type { Mesh, Object3D } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import type { PlanPoint, PlanSnapTargets } from './planSnap';

interface MeasureControllerProps {
  snapTargets: PlanSnapTargets;
}

interface MeasurePoint {
  x: number;
  y: number;
  z: number;
}

const MM = 1000;
const PLANE_SIZE_M = 400;
const GRID_MM = 10;
const CORNER_SNAP_MM = 200;
const LINE_COLOR = '#0ea5e9';
const POINT_COLOR = '#0284c7';

const LABEL_STYLE = {
  pointerEvents: 'none',
  background: 'rgba(2, 132, 199, 0.95)',
  color: '#ffffff',
  padding: '2px 8px',
  borderRadius: 6,
  fontSize: 11,
  fontWeight: 700,
  whiteSpace: 'nowrap',
} as const;

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

const groundCornerSnap = (x: number, z: number, targets: PlanSnapTargets): PlanPoint => {
  let best: PlanPoint | null = null;
  let bestDist = CORNER_SNAP_MM;
  for (const p of targets.points) {
    const d = Math.hypot(p.x - x, p.y - z);
    if (d <= bestDist) {
      bestDist = d;
      best = { x: p.x, y: p.y };
    }
  }
  return best ?? { x: snapGrid(x), y: snapGrid(z) };
};

export function MeasureController({ snapTargets }: MeasureControllerProps) {
  const scene = useThree((s) => s.scene);
  const [points, setPoints] = useState<MeasurePoint[]>([]);
  const [cursor, setCursor] = useState<MeasurePoint | null>(null);
  const planeRef = useRef<Mesh>(null);
  const raycaster = useMemo(() => new Raycaster(), []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setPoints([]);
        setCursor(null);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  // Pick a real 3D point off whatever scene surface the ray hits (wall top, panel,
  // roof, floor at its true elevation); fall back to the ground plane (y=0, plan-corner
  // snapped) over empty space. This makes vertical / diagonal / free measurement work,
  // not just flat ground distances.
  const resolve = (e: ThreeEvent<MouseEvent>): MeasurePoint => {
    raycaster.set(e.ray.origin, e.ray.direction);
    const hits = raycaster.intersectObjects(scene.children, true);
    const surface = hits.find((h) => {
      let o: Object3D | null = h.object;
      while (o) {
        if (o === planeRef.current) return false;
        o = o.parent;
      }
      return true;
    });
    if (surface) {
      return {
        x: Math.round(surface.point.x * MM),
        y: Math.round(surface.point.y * MM),
        z: Math.round(surface.point.z * MM),
      };
    }
    const ground = groundCornerSnap(e.point.x * MM, e.point.z * MM, snapTargets);
    return { x: ground.x, y: 0, z: ground.y };
  };

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const point = resolve(e);
    setPoints((prev) => (prev.length >= 2 ? [point] : [...prev, point]));
  };

  const handleMove = (e: ThreeEvent<PointerEvent>) => {
    setCursor(resolve(e));
  };

  const segment = useMemo<MeasurePoint[] | null>(() => {
    if (points.length === 2) return [points[0], points[1]];
    if (points.length === 1 && cursor) return [points[0], cursor];
    return null;
  }, [points, cursor]);

  const dimension = useMemo(() => {
    if (!segment) return null;
    const [a, b] = segment;
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const dz = b.z - a.z;
    return {
      distMm: Math.round(Math.hypot(dx, dy, dz)),
      verticalMm: Math.round(Math.abs(dy)),
      horizontalMm: Math.round(Math.hypot(dx, dz)),
      mid: { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2, z: (a.z + b.z) / 2 },
    };
  }, [segment]);

  return (
    <>
      <mesh
        ref={planeRef}
        rotation={[-Math.PI / 2, 0, 0]}
        onPointerMove={handleMove}
        onClick={handleClick}
        onPointerLeave={() => setCursor(null)}
      >
        <planeGeometry args={[PLANE_SIZE_M, PLANE_SIZE_M]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
      </mesh>
      {segment && (
        <Line
          points={segment.map((p): [number, number, number] => [p.x / MM, p.y / MM, p.z / MM])}
          color={LINE_COLOR}
          lineWidth={2}
          raycast={() => null}
        />
      )}
      {points.map((p, i) => (
        <mesh key={i} position={[p.x / MM, p.y / MM, p.z / MM]} raycast={() => null}>
          <sphereGeometry args={[0.035, 12, 12]} />
          <meshBasicMaterial color={POINT_COLOR} />
        </mesh>
      ))}
      {dimension && dimension.distMm > 0 && (
        <Html
          center
          position={[dimension.mid.x / MM, dimension.mid.y / MM + 0.06, dimension.mid.z / MM]}
        >
          <div style={LABEL_STYLE}>
            {dimension.distMm} mm
            {dimension.verticalMm > 0 && dimension.horizontalMm > 0
              ? ` · ↕ ${dimension.verticalMm} · ↔ ${dimension.horizontalMm}`
              : ''}
          </div>
        </Html>
      )}
    </>
  );
}
