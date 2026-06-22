import { useEffect, useState } from 'react';
import { DoubleSide } from 'three';
import { Html, Line } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import type { CSSProperties } from 'react';
import type { PlanPoint, PlanSnapTargets } from './planSnap';

interface MeasureControllerProps {
  snapTargets: PlanSnapTargets;
}

const MM = 1000;
const PLANE_SIZE_M = 400;
const LINE_Y_M = 0.04;
const GRID_MM = 10;
const CORNER_SNAP_MM = 200;
const LINE_COLOR = '#0ea5e9';
const POINT_COLOR = '#0284c7';

const LABEL_STYLE: CSSProperties = {
  pointerEvents: 'none',
  background: 'rgba(2, 132, 199, 0.95)',
  color: '#ffffff',
  padding: '2px 8px',
  borderRadius: 6,
  fontSize: 11,
  fontWeight: 700,
  whiteSpace: 'nowrap',
};

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

const snapToCorner = (x: number, y: number, targets: PlanSnapTargets): PlanPoint => {
  let best: PlanPoint | null = null;
  let bestDist = CORNER_SNAP_MM;
  for (const p of targets.points) {
    const d = Math.hypot(p.x - x, p.y - y);
    if (d <= bestDist) {
      bestDist = d;
      best = { x: p.x, y: p.y };
    }
  }
  return best ?? { x: snapGrid(x), y: snapGrid(y) };
};

export function MeasureController({ snapTargets }: MeasureControllerProps) {
  const [points, setPoints] = useState<PlanPoint[]>([]);
  const [cursor, setCursor] = useState<PlanPoint | null>(null);

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

  const resolve = (e: ThreeEvent<MouseEvent>) =>
    snapToCorner(e.point.x * MM, e.point.z * MM, snapTargets);

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const point = resolve(e);
    setPoints((prev) => (prev.length >= 2 ? [point] : [...prev, point]));
  };

  const handleMove = (e: ThreeEvent<PointerEvent>) => {
    setCursor(snapToCorner(e.point.x * MM, e.point.z * MM, snapTargets));
  };

  const segment =
    points.length === 2
      ? [points[0], points[1]]
      : points.length === 1 && cursor
        ? [points[0], cursor]
        : null;

  const dimension = segment
    ? {
        distMm: Math.round(Math.hypot(segment[1].x - segment[0].x, segment[1].y - segment[0].y)),
        angleDeg: Math.round(
          (Math.atan2(segment[1].y - segment[0].y, segment[1].x - segment[0].x) * 180) / Math.PI,
        ),
        midX: (segment[0].x + segment[1].x) / 2,
        midY: (segment[0].y + segment[1].y) / 2,
      }
    : null;

  return (
    <>
      <mesh
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
          points={segment.map((p): [number, number, number] => [p.x / MM, LINE_Y_M, p.y / MM])}
          color={LINE_COLOR}
          lineWidth={2}
          raycast={() => null}
        />
      )}
      {points.map((p, i) => (
        <mesh key={i} position={[p.x / MM, LINE_Y_M, p.y / MM]} raycast={() => null}>
          <sphereGeometry args={[0.035, 12, 12]} />
          <meshBasicMaterial color={POINT_COLOR} />
        </mesh>
      ))}
      {dimension && dimension.distMm > 0 && (
        <Html center position={[dimension.midX / MM, LINE_Y_M + 0.06, dimension.midY / MM]}>
          <div style={LABEL_STYLE}>
            {dimension.distMm} mm · {dimension.angleDeg}°
          </div>
        </Html>
      )}
    </>
  );
}
