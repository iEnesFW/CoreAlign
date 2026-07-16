import { useRef, useState } from 'react';
import { useDrag3D } from '@/shared/three-engine';
import type { Group } from 'three';
import type { SceneSurfacePoint } from '../../model/project.types';

const MM = 1000;
const GRID_MM = 10;
const HANDLE_RADIUS_M = 0.045;
const HANDLE_COLOR = '#0891b2';
const HANDLE_HOVER_COLOR = '#f97316';
const MIN_EDGE_MM = 200;

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

interface SurfaceEdgeBowHandlesProps {
  points: SceneSurfacePoint[];
  edgeArcs?: (number | null)[] | null;
  centroidXMm: number;
  centroidYMm: number;
  topM: number;
  onPreview: (edgeIndex: number, sagittaMm: number) => void;
  onCommit: (edgeIndex: number, sagittaMm: number) => void;
}

export function SurfaceEdgeBowHandles({
  points,
  edgeArcs,
  centroidXMm,
  centroidYMm,
  topM,
  onPreview,
  onCommit,
}: SurfaceEdgeBowHandlesProps) {
  if (points.length < 3) return null;
  return (
    <>
      {points.map((a, index) => {
        const b = points[(index + 1) % points.length];
        if (Math.hypot(b.x - a.x, b.y - a.y) < MIN_EDGE_MM) return null;
        return (
          <EdgeBowHandle
            key={index}
            index={index}
            a={a}
            b={b}
            sagittaMm={edgeArcs?.[index] ?? 0}
            centroidXMm={centroidXMm}
            centroidYMm={centroidYMm}
            topM={topM}
            onPreview={onPreview}
            onCommit={onCommit}
          />
        );
      })}
    </>
  );
}

interface EdgeBowHandleProps {
  index: number;
  a: SceneSurfacePoint;
  b: SceneSurfacePoint;
  sagittaMm: number;
  centroidXMm: number;
  centroidYMm: number;
  topM: number;
  onPreview: (edgeIndex: number, sagittaMm: number) => void;
  onCommit: (edgeIndex: number, sagittaMm: number) => void;
}

function EdgeBowHandle({
  index,
  a,
  b,
  sagittaMm,
  centroidXMm,
  centroidYMm,
  topM,
  onPreview,
  onCommit,
}: EdgeBowHandleProps) {
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const midX = (a.x + b.x) / 2;
  const midY = (a.y + b.y) / 2;
  const len = Math.hypot(b.x - a.x, b.y - a.y) || 1;
  const acrossX = -(b.y - a.y) / len;
  const acrossY = (b.x - a.x) / len;
  const apexX = midX + acrossX * sagittaMm;
  const apexY = midY + acrossY * sagittaMm;

  const resolve = (dxMm: number, dzMm: number) =>
    snapGrid(sagittaMm + dxMm * acrossX + dzMm * acrossY);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const next = resolve(delta.x, delta.z);
      anchorRef.current?.position.set(
        (midX + acrossX * next - centroidXMm) / MM,
        topM,
        (midY + acrossY * next - centroidYMm) / MM,
      );
      onPreview(index, next);
    },
    onCommit: (delta) => {
      const next = resolve(delta.x, delta.z);
      anchorRef.current?.position.set((apexX - centroidXMm) / MM, topM, (apexY - centroidYMm) / MM);
      onCommit(index, next);
    },
  });

  return (
    <group
      ref={anchorRef}
      position={[(apexX - centroidXMm) / MM, topM, (apexY - centroidYMm) / MM]}
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
