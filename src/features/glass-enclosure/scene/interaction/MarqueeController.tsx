import { useRef, useState } from 'react';
import { DoubleSide } from 'three';
import { Line } from '@react-three/drei';
import { useDrag3D } from './useDrag3D';
import type { ThreeEvent } from '@react-three/fiber';
import type { PlanPoint } from './planSnap';

interface MarqueeControllerProps {
  onSelect: (polygonMm: PlanPoint[]) => void;
}

const MM = 1000;
const MIN_SIZE_MM = 80;
const MARQUEE_COLOR = '#2563eb';
const FILL_COLOR = '#3b82f6';
const RECT_Y_M = 0.03;
const PLANE_SIZE_M = 400;

export function MarqueeController({ onSelect }: MarqueeControllerProps) {
  const startRef = useRef<PlanPoint | null>(null);
  const rectRef = useRef<{ minX: number; minY: number; maxX: number; maxY: number } | null>(null);
  const [rect, setRect] = useState<{
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
  } | null>(null);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const start = startRef.current;
      if (!start) return;
      const cx = start.x + delta.x;
      const cy = start.y + delta.z;
      const next = {
        minX: Math.min(start.x, cx),
        minY: Math.min(start.y, cy),
        maxX: Math.max(start.x, cx),
        maxY: Math.max(start.y, cy),
      };
      rectRef.current = next;
      setRect(next);
    },
    onCommit: () => {
      const r = rectRef.current;
      startRef.current = null;
      rectRef.current = null;
      setRect(null);
      if (!r || r.maxX - r.minX < MIN_SIZE_MM || r.maxY - r.minY < MIN_SIZE_MM) return;
      onSelect([
        { x: r.minX, y: r.minY },
        { x: r.maxX, y: r.minY },
        { x: r.maxX, y: r.maxY },
        { x: r.minX, y: r.maxY },
      ]);
    },
  });

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (e.nativeEvent.button === 0) {
      startRef.current = { x: e.point.x * MM, y: e.point.z * MM };
    }
    drag.handlers.onPointerDown(e);
  };

  const outline = rect
    ? ([
        [rect.minX / MM, RECT_Y_M, rect.minY / MM],
        [rect.maxX / MM, RECT_Y_M, rect.minY / MM],
        [rect.maxX / MM, RECT_Y_M, rect.maxY / MM],
        [rect.minX / MM, RECT_Y_M, rect.maxY / MM],
        [rect.minX / MM, RECT_Y_M, rect.minY / MM],
      ] as [number, number, number][])
    : null;

  return (
    <>
      <mesh
        rotation={[-Math.PI / 2, 0, 0]}
        onPointerDown={handlePointerDown}
        onPointerMove={drag.handlers.onPointerMove}
        onPointerUp={drag.handlers.onPointerUp}
        onPointerCancel={drag.handlers.onPointerCancel}
        onClick={(e) => {
          e.stopPropagation();
          drag.consumeClick();
        }}
      >
        <planeGeometry args={[PLANE_SIZE_M, PLANE_SIZE_M]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
      </mesh>
      {rect && (
        <mesh
          position={[(rect.minX + rect.maxX) / 2 / MM, RECT_Y_M, (rect.minY + rect.maxY) / 2 / MM]}
          rotation={[-Math.PI / 2, 0, 0]}
          raycast={() => null}
        >
          <planeGeometry args={[(rect.maxX - rect.minX) / MM, (rect.maxY - rect.minY) / MM]} />
          <meshBasicMaterial
            color={FILL_COLOR}
            transparent
            opacity={0.12}
            depthWrite={false}
            side={DoubleSide}
          />
        </mesh>
      )}
      {outline && (
        <Line
          points={outline}
          color={MARQUEE_COLOR}
          dashed
          dashSize={0.1}
          gapSize={0.06}
          lineWidth={1.5}
          raycast={() => null}
        />
      )}
    </>
  );
}
