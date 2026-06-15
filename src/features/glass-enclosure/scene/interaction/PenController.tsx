import { useEffect, useRef, useState } from 'react';
import { DoubleSide } from 'three';
import { Line } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import { isShiftPressed, snapAngleDeg } from '@/shared/three-engine';
import { clearSnapGuides, setSnapGuides } from '@/shared/three-engine';
import { chordBulgeMm, tessellateArc } from './penArc';
import type { ThreeEvent } from '@react-three/fiber';
import type { PlanPoint, PlanSnapTargets } from './planSnap';

interface PenControllerProps {
  snapTargets: PlanSnapTargets;
  onFinish: (pointsMm: PlanPoint[]) => void;
}

const MM = 1000;
const PLANE_SIZE_M = 400;
const PEN_Y_M = 0.02;
const CORNER_SNAP_MM = 200;
const CLOSE_SNAP_MM = 250;
const GRID_MM = 50;
const PEN_COLOR = '#2563eb';
const FILL_COLOR = '#3b82f6';
const MIN_POINTS = 3;

const snapToGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

const nearestCorner = (x: number, y: number, targets: PlanSnapTargets): PlanPoint | null => {
  let best: PlanPoint | null = null;
  let bestDist = CORNER_SNAP_MM;
  for (const point of targets.points) {
    const dist = Math.hypot(point.x - x, point.y - y);
    if (dist <= bestDist) {
      bestDist = dist;
      best = { x: point.x, y: point.y };
    }
  }
  return best;
};

const applyShiftConstraint = (from: PlanPoint, x: number, y: number): PlanPoint => {
  const dx = x - from.x;
  const dy = y - from.y;
  const len = Math.hypot(dx, dy);
  if (len < 1) return { x, y };
  const angle = snapAngleDeg((Math.atan2(dy, dx) * 180) / Math.PI);
  const rad = (angle * Math.PI) / 180;
  return { x: from.x + len * Math.cos(rad), y: from.y + len * Math.sin(rad) };
};

export function PenController({ snapTargets, onFinish }: PenControllerProps) {
  const pointsRef = useRef<PlanPoint[]>([]);
  const [points, setPoints] = useState<PlanPoint[]>([]);
  const [cursor, setCursor] = useState<PlanPoint | null>(null);
  const arcRef = useRef<{ active: boolean; end: PlanPoint } | null>(null);
  const suppressClickRef = useRef(false);
  const [arcPreview, setArcPreview] = useState<PlanPoint[] | null>(null);
  const getThree = useThree((s) => s.get);

  const setOrbitEnabled = (value: boolean) => {
    const controls = getThree().controls as unknown as { enabled: boolean } | null;
    if (controls) controls.enabled = value;
  };

  const resolve = (rawX: number, rawY: number): { point: PlanPoint; onCorner: boolean } => {
    const corner = nearestCorner(rawX, rawY, snapTargets);
    if (corner) return { point: corner, onCorner: true };
    const pts = pointsRef.current;
    const last = pts[pts.length - 1];
    if (last && isShiftPressed()) {
      return {
        point: applyShiftConstraint(last, snapToGrid(rawX), snapToGrid(rawY)),
        onCorner: false,
      };
    }
    return { point: { x: snapToGrid(rawX), y: snapToGrid(rawY) }, onCorner: false };
  };

  const finish = () => {
    const raw = pointsRef.current;
    pointsRef.current = [];
    setPoints([]);
    setCursor(null);
    clearSnapGuides();
    const cleaned: PlanPoint[] = [];
    for (const p of raw) {
      const prev = cleaned[cleaned.length - 1];
      if (!prev || Math.hypot(p.x - prev.x, p.y - prev.y) >= 1) cleaned.push(p);
    }
    const first = cleaned[0];
    const last = cleaned[cleaned.length - 1];
    if (cleaned.length > 1 && first && last && Math.hypot(first.x - last.x, first.y - last.y) < 1) {
      cleaned.pop();
    }
    if (cleaned.length >= MIN_POINTS) onFinish(cleaned);
  };

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      if (
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target instanceof HTMLSelectElement ||
        target?.isContentEditable
      )
        return;
      if (e.key === 'Enter') {
        e.preventDefault();
        finish();
      } else if (e.key === 'Escape') {
        cancelArc();
        pointsRef.current = [];
        setPoints([]);
        setCursor(null);
        clearSnapGuides();
      } else if (e.key === 'Backspace') {
        e.preventDefault();
        pointsRef.current = pointsRef.current.slice(0, -1);
        setPoints([...pointsRef.current]);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => {
      window.removeEventListener('keydown', onKey);
      clearSnapGuides();
      if (arcRef.current?.active) setOrbitEnabled(true);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (e.nativeEvent.button !== 0) return;
    if (isShiftPressed() && pointsRef.current.length >= 1) {
      const { point } = resolve(e.point.x * MM, e.point.z * MM);
      arcRef.current = { active: true, end: point };
      setOrbitEnabled(false);
      (e.target as Element | null)?.setPointerCapture?.(e.pointerId);
    }
  };

  const handleMove = (e: ThreeEvent<PointerEvent>) => {
    const { point, onCorner } = resolve(e.point.x * MM, e.point.z * MM);
    const arc = arcRef.current;
    if (arc?.active) {
      const anchor = pointsRef.current[pointsRef.current.length - 1];
      const bulge = chordBulgeMm(anchor, arc.end, point);
      setArcPreview([anchor, ...tessellateArc(anchor, arc.end, bulge)]);
      setCursor(arc.end);
      return;
    }
    setCursor(point);
    setSnapGuides(
      onCorner ? [{ kind: 'corner', x1: point.x, y1: point.y, x2: point.x, y2: point.y }] : [],
    );
  };

  const cancelArc = () => {
    arcRef.current = null;
    setArcPreview(null);
    setOrbitEnabled(true);
  };

  const handlePointerUp = (e: ThreeEvent<PointerEvent>) => {
    const arc = arcRef.current;
    arcRef.current = null;
    setArcPreview(null);
    setOrbitEnabled(true);
    if (!arc?.active) return;
    (e.target as Element | null)?.releasePointerCapture?.(e.pointerId);
    suppressClickRef.current = true;
    const { point } = resolve(e.point.x * MM, e.point.z * MM);
    const anchor = pointsRef.current[pointsRef.current.length - 1];
    const bulge = chordBulgeMm(anchor, arc.end, point);
    pointsRef.current = [...pointsRef.current, ...tessellateArc(anchor, arc.end, bulge)];
    setPoints(pointsRef.current);
  };

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    if (suppressClickRef.current) {
      suppressClickRef.current = false;
      return;
    }
    // Ignore the 2nd click of a double-click (handled by onDoubleClick).
    if (e.nativeEvent.detail > 1) return;
    const { point } = resolve(e.point.x * MM, e.point.z * MM);
    const pts = pointsRef.current;
    const first = pts[0];
    if (
      first &&
      pts.length >= MIN_POINTS &&
      Math.hypot(point.x - first.x, point.y - first.y) <= CLOSE_SNAP_MM
    ) {
      finish();
      return;
    }
    const prev = pts[pts.length - 1];
    if (prev && Math.hypot(point.x - prev.x, point.y - prev.y) < 1) return;
    pointsRef.current = [...pts, point];
    setPoints(pointsRef.current);
  };

  const handleDoubleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    finish();
  };

  const drawn = arcPreview ?? (cursor ? [...points, cursor] : points);
  const linePoints =
    drawn.length >= 2
      ? drawn.map((p): [number, number, number] => [p.x / MM, PEN_Y_M, p.y / MM])
      : null;
  const closedHint =
    points.length >= MIN_POINTS
      ? ([
          [points[0].x / MM, PEN_Y_M, points[0].y / MM],
          drawn.length > 0
            ? [drawn[drawn.length - 1].x / MM, PEN_Y_M, drawn[drawn.length - 1].y / MM]
            : [points[0].x / MM, PEN_Y_M, points[0].y / MM],
        ] as [number, number, number][])
      : null;

  return (
    <>
      <mesh
        rotation={[-Math.PI / 2, 0, 0]}
        onPointerDown={handlePointerDown}
        onPointerMove={handleMove}
        onPointerUp={handlePointerUp}
        onPointerCancel={cancelArc}
        onPointerLeave={cancelArc}
        onClick={handleClick}
        onDoubleClick={handleDoubleClick}
      >
        <planeGeometry args={[PLANE_SIZE_M, PLANE_SIZE_M]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
      </mesh>
      {linePoints && (
        <Line points={linePoints} color={PEN_COLOR} lineWidth={2} raycast={() => null} />
      )}
      {closedHint && (
        <Line
          points={closedHint}
          color={FILL_COLOR}
          dashed
          dashSize={0.1}
          gapSize={0.08}
          lineWidth={1}
          raycast={() => null}
        />
      )}
      {points.map((p, i) => (
        <mesh key={i} position={[p.x / MM, PEN_Y_M, p.y / MM]} raycast={() => null}>
          <sphereGeometry args={[0.03, 8, 8]} />
          <meshBasicMaterial color={PEN_COLOR} />
        </mesh>
      ))}
    </>
  );
}
