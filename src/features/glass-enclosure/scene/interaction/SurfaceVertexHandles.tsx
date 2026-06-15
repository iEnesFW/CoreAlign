import { useRef, useState } from 'react';
import { useDrag3D } from '@/shared/three-engine';
import type { Group } from 'three';
import type { SceneSurfacePoint } from '../../model/project.types';

interface SurfaceVertexHandlesProps {
  points: SceneSurfacePoint[];
  centroidXMm: number;
  centroidYMm: number;
  topM: number;
  onPreview: (index: number, xMm: number, yMm: number) => void;
  onCommit: (index: number, xMm: number, yMm: number) => void;
}

const MM = 1000;
const GRID_MM = 10;
const CORNER_MIN_TURN_DEG = 18;
const HANDLE_RADIUS_M = 0.05;
const HANDLE_COLOR = '#1d4ed8';
const HANDLE_HOVER_COLOR = '#f97316';

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

const turnDeg = (
  prev: SceneSurfacePoint,
  cur: SceneSurfacePoint,
  next: SceneSurfacePoint,
): number => {
  const a1 = Math.atan2(cur.y - prev.y, cur.x - prev.x);
  const a2 = Math.atan2(next.y - cur.y, next.x - cur.x);
  return Math.abs(((((a2 - a1) * 180) / Math.PI + 540) % 360) - 180);
};

// Indices of the true corners (sharp turns); smooth arc samples are skipped so
// a curved span keeps only its endpoints as handles.
const cornerIndices = (points: SceneSurfacePoint[]): number[] => {
  if (points.length < 3) return points.map((_, i) => i);
  const result: number[] = [];
  for (let i = 0; i < points.length; i += 1) {
    const prev = points[(i - 1 + points.length) % points.length];
    const next = points[(i + 1) % points.length];
    if (turnDeg(prev, points[i], next) >= CORNER_MIN_TURN_DEG) result.push(i);
  }
  return result.length >= 3 ? result : points.map((_, i) => i);
};

export function SurfaceVertexHandles({
  points,
  centroidXMm,
  centroidYMm,
  topM,
  onPreview,
  onCommit,
}: SurfaceVertexHandlesProps) {
  const indices = cornerIndices(points);
  return (
    <>
      {indices.map((index) => (
        <VertexHandle
          key={index}
          index={index}
          point={points[index]}
          centroidXMm={centroidXMm}
          centroidYMm={centroidYMm}
          topM={topM}
          onPreview={onPreview}
          onCommit={onCommit}
        />
      ))}
    </>
  );
}

interface VertexHandleProps {
  index: number;
  point: SceneSurfacePoint;
  centroidXMm: number;
  centroidYMm: number;
  topM: number;
  onPreview: (index: number, xMm: number, yMm: number) => void;
  onCommit: (index: number, xMm: number, yMm: number) => void;
}

function VertexHandle({
  index,
  point,
  centroidXMm,
  centroidYMm,
  topM,
  onPreview,
  onCommit,
}: VertexHandleProps) {
  const anchorRef = useRef<Group>(null);
  const lastRef = useRef({ x: point.x, y: point.y });
  const [hovered, setHovered] = useState(false);

  const resolve = (dxMm: number, dzMm: number) => ({
    x: snapGrid(point.x + dxMm),
    y: snapGrid(point.y + dzMm),
  });

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const next = resolve(delta.x, delta.z);
      anchorRef.current?.position.set(
        (next.x - centroidXMm) / MM,
        topM,
        (next.y - centroidYMm) / MM,
      );
      // Only rebuild the surface when the snapped vertex actually crosses to a
      // new grid cell, so a steady drag does not rebuild geometry every frame.
      if (next.x === lastRef.current.x && next.y === lastRef.current.y) return;
      lastRef.current = next;
      onPreview(index, next.x, next.y);
    },
    onCommit: (delta) => {
      const next = resolve(delta.x, delta.z);
      anchorRef.current?.position.set(
        (point.x - centroidXMm) / MM,
        topM,
        (point.y - centroidYMm) / MM,
      );
      // Always notify so the parent clears the preview, even on a no-move click.
      onCommit(index, next.x, next.y);
    },
  });

  return (
    <group
      ref={anchorRef}
      position={[(point.x - centroidXMm) / MM, topM, (point.y - centroidYMm) / MM]}
    >
      <mesh
        {...drag.handlers}
        onClick={(e) => {
          e.stopPropagation();
          drag.consumeClick();
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          setHovered(true);
          document.body.style.cursor = 'grab';
        }}
        onPointerOut={() => {
          setHovered(false);
          document.body.style.cursor = 'auto';
        }}
      >
        <sphereGeometry args={[HANDLE_RADIUS_M, 16, 16]} />
        <meshBasicMaterial color={hovered ? HANDLE_HOVER_COLOR : HANDLE_COLOR} />
      </mesh>
    </group>
  );
}
