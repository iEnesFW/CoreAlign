import { useMemo, useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import { Vector3 } from 'three';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';
import { sampleArcPlan } from '../../model/arcGeometry';

const MM = 1000;
const DEG = Math.PI / 180;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#16a34a';
const HANDLE_HOVER = '#f97316';
const MAX_SWEEP_DEG = 360;
// Degrees of sweep per SCREEN pixel of perpendicular drag — zoom/perspective independent, so a full
// circle takes a deliberate ~900px pull and a small drag makes a small curve (no runaway).
const DEG_PER_PIXEL = 0.4;
const PROBE_M = 0.1;
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
  const camera = useThree((s) => s.camera);
  const screenSize = useThree((s) => s.size);
  const probeRef = useRef(new Vector3());

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

  // Project a world point (metres) to screen pixels.
  const toPixels = (x: number, y: number, z: number) => {
    const v = probeRef.current.set(x, y, z).project(camera);
    return { px: (v.x * 0.5 + 0.5) * screenSize.width, py: (-v.y * 0.5 + 0.5) * screenSize.height };
  };

  // How many world-mm equal one screen pixel along the across direction, at the apex's depth. Used
  // to convert the (perspective-distorted) world drag into a stable screen-pixel pull.
  const mmPerPixelAcross = () => {
    const apex = apexFor(currentSweepDeg);
    const a = toPixels(apex.x / MM, topYM, apex.y / MM);
    const b = toPixels(apex.x / MM + acrossX * PROBE_M, topYM, apex.y / MM + acrossY * PROBE_M);
    const dist = Math.hypot(b.px - a.px, b.py - a.py);
    return dist > 0.001 ? (PROBE_M * MM) / dist : 1;
  };

  const sweepAt = (delta: { x: number; z: number }) => {
    const perpMm = (delta.x * acrossX + delta.z * acrossY) * MM;
    const pixelPull = perpMm / mmPerPixelAcross();
    const next = currentSweepDeg + pixelPull * DEG_PER_PIXEL;
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
