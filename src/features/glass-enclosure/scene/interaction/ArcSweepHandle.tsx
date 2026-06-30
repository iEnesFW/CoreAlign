import { useMemo, useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';
import { sampleArcPlan } from '../../model/arcGeometry';

const MM = 1000;
const DEG = Math.PI / 180;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#16a34a';
const HANDLE_HOVER = '#f97316';
const MAX_SWEEP_DEG = 360;
const STRAIGHTEN_DEG = 2;

interface ArcSweepHandleProps {
  // The run/wall start (plan mm) + its start-tangent direction (deg) and FIXED developed glass
  // length. The arc is an overlay on top: dragging this handle sets only the sweep angle.
  startX: number;
  startY: number;
  dirDeg: number;
  arcLengthMm: number;
  currentSweepDeg: number;
  topYM: number;
  // Reports the new signed sweep (deg); 0 means the run should return to straight.
  onCommit: (sweepDeg: number) => void;
}

// A green handle at the arc's apex (the mid-point of the developed length). Dragging it
// perpendicular to the start tangent sets the SWEEP ANGLE continuously from 0° up to a full circle
// (360°), bulging to whichever side it is pulled. The glass length stays fixed (radius =
// arcLength/sweep), so the ends draw together as the curve tightens. A dashed line previews the
// exact arc that will result.
export function ArcSweepHandle({
  startX,
  startY,
  dirDeg,
  arcLengthMm,
  currentSweepDeg,
  topYM,
  onCommit,
}: ArcSweepHandleProps) {
  const dirRad = dirDeg * DEG;
  const cos = Math.cos(dirRad);
  const sin = Math.sin(dirRad);
  // Perpendicular to the start tangent (+90°); +across is the +sweep bulge side.
  const acrossX = -sin;
  const acrossY = cos;
  // Sensitivity: dragging across by the semicircle apex distance (≈ arcLength/π) reaches ~180°, so
  // the handle roughly tracks the cursor for the common case; deeper angles need a little more pull.
  const degPerMm = (180 * Math.PI) / Math.max(1, arcLengthMm);

  const apexFor = (sweepDeg: number) => {
    const sweepRad = Math.abs(sweepDeg) * DEG;
    if (sweepRad < 0.0005) {
      return { x: startX + (arcLengthMm / 2) * cos, y: startY + (arcLengthMm / 2) * sin };
    }
    const dir = sweepDeg < 0 ? -1 : 1;
    const sweep = Math.min(Math.PI * 2, sweepRad);
    const radius = arcLengthMm / sweep;
    const phi = sweep / 2;
    const lx = radius * Math.sin(phi);
    const ly = dir * radius * (1 - Math.cos(phi));
    return { x: startX + lx * cos - ly * sin, y: startY + lx * sin + ly * cos };
  };

  const rest = apexFor(currentSweepDeg);
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const [dragSweep, setDragSweep] = useState<number | null>(null);

  const sweepAt = (delta: { x: number; z: number }) => {
    const perpMm = (delta.x * acrossX + delta.z * acrossY) * MM;
    const next = currentSweepDeg + perpMm * degPerMm;
    return Math.max(-MAX_SWEEP_DEG, Math.min(MAX_SWEEP_DEG, next));
  };

  const previewPoints = useMemo<[number, number, number][] | null>(() => {
    if (dragSweep === null) return null;
    return sampleArcPlan(startX, startY, dirDeg, arcLengthMm, dragSweep).map(
      (p) => [p.x / MM, topYM, p.y / MM] as [number, number, number],
    );
  }, [dragSweep, startX, startY, dirDeg, arcLengthMm, topYM]);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const s = sweepAt(delta);
      setDragSweep(s);
      const apex = apexFor(s);
      anchorRef.current?.position.set(apex.x / MM, topYM, apex.y / MM);
    },
    onCommit: (delta) => {
      const s = sweepAt(delta);
      setDragSweep(null);
      anchorRef.current?.position.set(rest.x / MM, topYM, rest.y / MM);
      onCommit(Math.abs(s) < STRAIGHTEN_DEG ? 0 : Math.round(s * 10) / 10);
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
