import { useMemo, useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';
import { bowArcPlanPoints } from '../../model/arcGeometry';

const MM = 1000;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#16a34a';
const HANDLE_HOVER = '#f97316';

interface ArcSweepHandleProps {
  // The two chord endpoints (plan mm) — they stay FIXED while bowing.
  startX: number;
  startY: number;
  endX: number;
  endY: number;
  // Signed bow of the current arc (0 = straight); positions the rest apex.
  currentSagittaMm: number;
  topYM: number;
  // Reports the new signed sagitta (perpendicular bow, mm); the parent maps it via arcFromBow.
  onCommit: (sagittaMm: number) => void;
}

// Green apex handle for a CHORD-INVARIANT curve. The two chord endpoints stay FIXED; the glass bows
// between them. Dragging the apex perpendicular to the chord sets the signed sagitta 1:1 in world mm
// (exactly like the resize handles, so the apex tracks the cursor) — the curve deepens smoothly from
// straight, through a half-circle, into a major (>180°) arc, all without the ends moving. A dashed
// line previews the exact arc (bowArcPlanPoints = the same arc the commit produces).
export function ArcSweepHandle({
  startX,
  startY,
  endX,
  endY,
  currentSagittaMm,
  topYM,
  onCommit,
}: ArcSweepHandleProps) {
  const dx = endX - startX;
  const dy = endY - startY;
  const chord = Math.hypot(dx, dy) || 1;
  // +across is the +sagitta bulge side (left of the start→end chord).
  const acrossX = -dy / chord;
  const acrossY = dx / chord;
  const midX = (startX + endX) / 2;
  const midY = (startY + endY) / 2;

  const apexFor = (sagittaMm: number) => ({
    x: midX + acrossX * sagittaMm,
    y: midY + acrossY * sagittaMm,
  });

  const rest = apexFor(currentSagittaMm);
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const [dragSag, setDragSag] = useState<number | null>(null);

  // useDrag3D returns the delta in MILLIMETRES already — do NOT multiply by MM (that 1000× double
  // conversion pinned the curve to a full circle on any drag). The perpendicular component of the
  // world drag IS the sagitta delta, so the apex follows the cursor 1:1.
  const sagittaAt = (delta: { x: number; z: number }) =>
    currentSagittaMm + delta.x * acrossX + delta.z * acrossY;

  const previewPoints = useMemo<[number, number, number][] | null>(() => {
    if (dragSag === null) return null;
    return bowArcPlanPoints(startX, startY, endX, endY, dragSag).map(
      (p) => [p.x / MM, topYM, p.y / MM] as [number, number, number],
    );
  }, [dragSag, startX, startY, endX, endY, topYM]);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const s = sagittaAt(delta);
      setDragSag(s);
      const apex = apexFor(s);
      anchorRef.current?.position.set(apex.x / MM, topYM, apex.y / MM);
    },
    onCommit: (delta) => {
      const s = sagittaAt(delta);
      setDragSag(null);
      anchorRef.current?.position.set(rest.x / MM, topYM, rest.y / MM);
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
      <group ref={anchorRef} position={[rest.x / MM, topYM, rest.y / MM]}>
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
