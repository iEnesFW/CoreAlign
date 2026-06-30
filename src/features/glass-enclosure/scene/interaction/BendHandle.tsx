import { useMemo, useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';

const MM = 1000;
const DEG = 180 / Math.PI;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#7c3aed';
const HANDLE_HOVER = '#f97316';
const MAX_BEND_DEG = 175;
const UNBEND_THRESHOLD_DEG = 3;
const SNAP_TARGETS_DEG = [45, 90, 135];
const SNAP_TOLERANCE_DEG = 6;

interface BendHandleProps {
  // Wall origin + segment-1 direction (plan mm / degrees) and the developed lengths.
  startX: number;
  startY: number;
  dirDeg: number;
  lengthMm: number;
  bendAtMm: number;
  currentBendDeg: number;
  topYM: number;
  // Reports the new bend angle (deg); 0 means the wall should return to straight.
  onCommit: (bendDeg: number) => void;
}

const normalizeDeg = (deg: number) => {
  let d = deg % 360;
  if (d > 180) d -= 360;
  if (d < -180) d += 360;
  return d;
};

const snapBend = (bendDeg: number) => {
  if (Math.abs(bendDeg) < UNBEND_THRESHOLD_DEG) return 0;
  const sign = bendDeg < 0 ? -1 : 1;
  const mag = Math.min(Math.abs(bendDeg), MAX_BEND_DEG);
  for (const target of SNAP_TARGETS_DEG) {
    if (Math.abs(mag - target) <= SNAP_TOLERANCE_DEG) return sign * target;
  }
  return sign * Math.round(mag);
};

// A violet handle at the free end of an L-wall's second segment. Dragging it swings that segment
// about the bend point on a fixed-radius arc (the segment length never changes); the reported angle
// is the wall's bend (plan-turn α = −bendAngle, so the parent negates). Dragging back to nearly
// straight reports 0 so the bend is removed. A dashed two-segment line previews the L live.
export function BendHandle({
  startX,
  startY,
  dirDeg,
  lengthMm,
  bendAtMm,
  currentBendDeg,
  topYM,
  onCommit,
}: BendHandleProps) {
  const dirRad = dirDeg / DEG;
  const dir = { x: Math.cos(dirRad), y: Math.sin(dirRad) };
  const bendAt = Math.min(Math.max(bendAtMm, 0), lengthMm);
  const seg2 = Math.max(0, lengthMm - bendAt);
  const px = startX + bendAt * dir.x;
  const py = startY + bendAt * dir.y;

  const endFor = (bendDeg: number) => {
    const a = (dirDeg - bendDeg) / DEG;
    return { x: px + seg2 * Math.cos(a), y: py + seg2 * Math.sin(a) };
  };
  const rest = endFor(currentBendDeg);

  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const [dragBend, setDragBend] = useState<number | null>(null);

  const bendAt2 = (delta: { x: number; z: number }) => {
    // useDrag3D already returns the delta in MILLIMETRES (rest is plan-mm too) — do NOT multiply by
    // MM again, that made the bend 1000× too sensitive.
    const ex = rest.x + delta.x;
    const ey = rest.y + delta.z;
    const alpha = normalizeDeg(Math.atan2(ey - py, ex - px) * DEG - dirDeg);
    return snapBend(-alpha);
  };

  const previewPoints = useMemo<[number, number, number][] | null>(() => {
    if (dragBend === null) return null;
    const a = (dirDeg - dragBend) / DEG;
    const endX = px + seg2 * Math.cos(a);
    const endY = py + seg2 * Math.sin(a);
    return [
      [startX / MM, topYM, startY / MM],
      [px / MM, topYM, py / MM],
      [endX / MM, topYM, endY / MM],
    ];
  }, [dragBend, startX, startY, px, py, topYM, seg2, dirDeg]);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const b = bendAt2(delta);
      setDragBend(b);
      const end = endFor(b);
      anchorRef.current?.position.set(end.x / MM, topYM, end.y / MM);
    },
    onCommit: (delta) => {
      const b = bendAt2(delta);
      setDragBend(null);
      anchorRef.current?.position.set(rest.x / MM, topYM, rest.y / MM);
      onCommit(b);
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
