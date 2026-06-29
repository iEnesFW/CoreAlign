import { useMemo, useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';

const MM = 1000;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#16a34a';
const HANDLE_HOVER = '#f97316';

interface CurveBowHandleProps {
  // Chord endpoints in plan mm (start = the object's origin). Dragging the midpoint handle
  // perpendicular bows the object between these fixed endpoints.
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  // Current signed bow (sagitta in the chord's +90° across direction); 0 for a straight object.
  currentSagittaMm: number;
  topYM: number;
  onCommit: (sagittaMm: number) => void;
}

// A green handle at a chord's midpoint (rendered in the WORLD frame, so it sits OUTSIDE the
// object's rotated group). Dragging it perpendicular to the chord reports a signed sagitta; the
// parent turns that into its own arc model (run/wall arc, or slab rise) while keeping the chord
// fixed. A dashed parabola previews the bow live.
export function CurveBowHandle({
  startX,
  startY,
  endX,
  endY,
  currentSagittaMm,
  topYM,
  onCommit,
}: CurveBowHandleProps) {
  const dx = endX - startX;
  const dy = endY - startY;
  const chordMm = Math.hypot(dx, dy) || 1;
  const acrossX = -dy / chordMm;
  const acrossY = dx / chordMm;
  const midX = (startX + endX) / 2;
  const midY = (startY + endY) / 2;
  // Allow well past a half-circle (sagitta > chord/2 → major arc) so the curve can be driven as
  // deep as the user wants; the arc maths caps the rendered sweep before a degenerate full circle.
  const maxSagittaMm = chordMm * 2;
  const restX = midX + acrossX * currentSagittaMm;
  const restY = midY + acrossY * currentSagittaMm;

  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const [dragSagitta, setDragSagitta] = useState<number | null>(null);

  const sagittaAt = (delta: { x: number; z: number }) => {
    const s = currentSagittaMm + (delta.x * MM * acrossX + delta.z * MM * acrossY);
    return Math.max(-maxSagittaMm, Math.min(maxSagittaMm, s));
  };

  const previewPoints = useMemo<[number, number, number][] | null>(() => {
    if (dragSagitta === null) return null;
    const pts: [number, number, number][] = [];
    for (let i = 0; i <= 16; i += 1) {
      const t = i / 16;
      const bow = dragSagitta * (1 - (2 * t - 1) ** 2);
      pts.push([
        (startX + dx * t + acrossX * bow) / MM,
        topYM,
        (startY + dy * t + acrossY * bow) / MM,
      ]);
    }
    return pts;
  }, [dragSagitta, startX, startY, dx, dy, acrossX, acrossY, topYM]);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const s = sagittaAt(delta);
      setDragSagitta(s);
      anchorRef.current?.position.set((midX + acrossX * s) / MM, topYM, (midY + acrossY * s) / MM);
    },
    onCommit: (delta) => {
      const s = sagittaAt(delta);
      setDragSagitta(null);
      anchorRef.current?.position.set(restX / MM, topYM, restY / MM);
      onCommit(s);
    },
  });

  return (
    <>
      {previewPoints && (
        <Line
          points={previewPoints}
          color={HANDLE_HOVER}
          lineWidth={2}
          dashed
          dashSize={0.05}
          gapSize={0.03}
        />
      )}
      <group ref={anchorRef} position={[restX / MM, topYM, restY / MM]}>
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
          renderOrder={999}
        >
          <sphereGeometry args={[HANDLE_RADIUS_M, 16, 16]} />
          <meshBasicMaterial
            color={hovered ? HANDLE_HOVER : HANDLE_COLOR}
            depthTest={false}
            depthWrite={false}
            transparent
          />
        </mesh>
      </group>
    </>
  );
}
