import { useRef } from 'react';
import { useDrag3D } from './useDrag3D';
import { applyPlanMoveSnap } from './planSnap';
import { clampPlanRotation, normalizePlanAngleDeg, slidePlanMove } from './planCollision';
import { rotatePlanPointDeg } from './planTransform';
import { snapAngleDeg } from './angleSnap';
import { clearSnapGuides, setSnapGuides } from './snapGuides';
import { setDragReadout } from './dragReadout';
import { isAltPressed, isCtrlPressed } from './modifierKeys';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group } from 'three';
import type { RefObject } from 'react';
import type { DragDeltaMm, Drag3DHandlers } from './useDrag3D';
import type { PlanFootprint, PlanFootprintSet } from './planCollision';
import type { PlanMoveDelta, PlanPoint, PlanSnapTargets } from './planSnap';

export interface PlanGestureAdapter {
  originXMm: number;
  originYMm: number;
  rotationDeg: number;
  baseYM: number;
  centerXMm: number;
  centerYMm: number;
  moveProbes: PlanPoint[];
  footprintAt: (dxMm: number, dyMm: number, rotationDeg: number) => PlanFootprintSet;
  liftYMAt?: (dxMm: number, dyMm: number) => number;
  altLiftYMAt?: (dxMm: number, dyMm: number) => number;
}

export interface PlanRotationCommit {
  rotationDeg: number;
  originX: number;
  originY: number;
  sweepDeg: number;
}

export type ObjectGestureMode = 'move' | 'rotate' | null;

export interface UseObjectGesturesOptions {
  adapter: PlanGestureAdapter;
  groupRef: RefObject<Group | null>;
  enabled: boolean;
  mode: ObjectGestureMode;
  snapTargets: PlanSnapTargets;
  obstacles: PlanFootprint[];
  onPick: () => void;
  onMoveCommit: (delta: PlanMoveDelta, meta: { alt: boolean }) => void;
  onRotateCommit: (commit: PlanRotationCommit) => void;
  onMovePreview?: (delta: PlanMoveDelta) => void;
  onRotatePreview?: (sweepDeg: number) => void;
  onGestureStart?: () => void;
}

export interface ObjectGestures {
  handlers: Drag3DHandlers;
  consumeClick: () => boolean;
}

const MM = 1000;
const DEG2RAD = Math.PI / 180;
const RAD2DEG = 180 / Math.PI;
const ZERO_MOVE: PlanMoveDelta = { dxMm: 0, dyMm: 0 };
const SNAP_CONTACT_KEEP_MM = 2;

export function useObjectGestures({
  adapter,
  groupRef,
  enabled,
  mode,
  snapTargets,
  obstacles,
  onPick,
  onMoveCommit,
  onRotateCommit,
  onMovePreview,
  onRotatePreview,
  onGestureStart,
}: UseObjectGesturesOptions): ObjectGestures {
  const gestureEnabled = enabled && mode !== null;

  const startRef = useRef({ xMm: 0, yMm: 0 });
  const lastMoveRef = useRef<PlanMoveDelta>(ZERO_MOVE);
  const lastAngleRef = useRef(0);
  const startedRef = useRef(false);
  const altLatchRef = useRef(false);

  const resetTransform = () => {
    const group = groupRef.current;
    if (!group) return;
    group.position.set(adapter.originXMm / MM, adapter.baseYM, adapter.originYMm / MM);
    group.rotation.y = -adapter.rotationDeg * DEG2RAD;
  };

  const applyMovePreview = (delta: DragDeltaMm) => {
    if (delta.x === 0 && delta.z === 0) {
      lastMoveRef.current = ZERO_MOVE;
      resetTransform();
      clearSnapGuides();
      setDragReadout(null);
      onMovePreview?.(ZERO_MOVE);
      return;
    }
    const snapped = applyPlanMoveSnap(adapter.moveProbes, delta.x, delta.z, snapTargets);
    const altStack = isAltPressed();
    altLatchRef.current = altStack;
    // WHY: Alt = stack-on-top, which deliberately needs plan overlap (the body rests on the
    // other in Z); the no-deepen collision gate must NOT run here or stacking is impossible.
    // Plain moves go through slidePlanMove, which now forbids deepening any overlap.
    const slid = altStack
      ? snapped
      : slidePlanMove(
          (dx, dy) => adapter.footprintAt(dx, dy, adapter.rotationDeg),
          obstacles,
          snapped.dxMm,
          snapped.dyMm,
        );
    const divergenceMm = Math.hypot(slid.dxMm - snapped.dxMm, slid.dyMm - snapped.dyMm);
    setSnapGuides(divergenceMm <= SNAP_CONTACT_KEEP_MM ? snapped.guides : []);
    lastMoveRef.current = slid;
    const group = groupRef.current;
    if (group) {
      const liftM =
        altStack && adapter.altLiftYMAt
          ? adapter.altLiftYMAt(slid.dxMm, slid.dyMm)
          : (adapter.liftYMAt?.(slid.dxMm, slid.dyMm) ?? adapter.baseYM);
      group.position.set(
        (adapter.originXMm + slid.dxMm) / MM,
        liftM,
        (adapter.originYMm + slid.dyMm) / MM,
      );
    }
    const dist = Math.round(Math.hypot(slid.dxMm, slid.dyMm));
    setDragReadout(
      `X ${Math.round(adapter.originXMm + slid.dxMm)} · Y ${Math.round(
        adapter.originYMm + slid.dyMm,
      )} mm  ·  Δ ${dist} mm`,
    );
    onMovePreview?.(slid);
  };

  const resolveAngle = (delta: DragDeltaMm) => {
    const start = startRef.current;
    const fromDeg =
      Math.atan2(start.yMm - adapter.centerYMm, start.xMm - adapter.centerXMm) * RAD2DEG;
    const toDeg =
      Math.atan2(start.yMm + delta.z - adapter.centerYMm, start.xMm + delta.x - adapter.centerXMm) *
      RAD2DEG;
    const sweep = ((((toDeg - fromDeg) % 360) + 540) % 360) - 180;
    const target = adapter.rotationDeg + sweep;
    return isCtrlPressed() ? Math.round(target) : snapAngleDeg(target);
  };

  const applyRotatePreview = (delta: DragDeltaMm) => {
    const nextDeg = delta.x === 0 && delta.z === 0 ? adapter.rotationDeg : resolveAngle(delta);
    lastAngleRef.current = nextDeg;
    const sweepDeg = nextDeg - adapter.rotationDeg;
    const origin = rotatePlanPointDeg(
      adapter.originXMm,
      adapter.originYMm,
      adapter.centerXMm,
      adapter.centerYMm,
      sweepDeg,
    );
    const group = groupRef.current;
    if (group) {
      group.position.set(origin.x / MM, adapter.baseYM, origin.y / MM);
      group.rotation.y = -nextDeg * DEG2RAD;
    }
    setDragReadout(
      delta.x === 0 && delta.z === 0 ? null : `${Math.round(normalizePlanAngleDeg(nextDeg))}°`,
    );
    onRotatePreview?.(sweepDeg);
  };

  const commitMove = () => {
    const delta = lastMoveRef.current;
    lastMoveRef.current = ZERO_MOVE;
    clearSnapGuides();
    setDragReadout(null);
    if (delta.dxMm === 0 && delta.dyMm === 0) {
      onMovePreview?.(ZERO_MOVE);
      resetTransform();
      return;
    }
    onMoveCommit(delta, { alt: altLatchRef.current });
  };

  const commitRotate = () => {
    const targetDeg = lastAngleRef.current;
    setDragReadout(null);
    const resetIdle = () => {
      onRotatePreview?.(0);
      resetTransform();
    };
    if (targetDeg === adapter.rotationDeg) {
      resetIdle();
      return;
    }
    const clamped = clampPlanRotation(
      (deg) => {
        const origin = rotatePlanPointDeg(
          adapter.originXMm,
          adapter.originYMm,
          adapter.centerXMm,
          adapter.centerYMm,
          deg - adapter.rotationDeg,
        );
        return adapter.footprintAt(origin.x - adapter.originXMm, origin.y - adapter.originYMm, deg);
      },
      obstacles,
      adapter.rotationDeg,
      targetDeg,
    );
    const sweepDeg = clamped - adapter.rotationDeg;
    if (sweepDeg === 0) {
      resetIdle();
      return;
    }
    const origin = rotatePlanPointDeg(
      adapter.originXMm,
      adapter.originYMm,
      adapter.centerXMm,
      adapter.centerYMm,
      sweepDeg,
    );
    const group = groupRef.current;
    if (group) {
      group.position.set(origin.x / MM, adapter.baseYM, origin.y / MM);
      group.rotation.y = -clamped * DEG2RAD;
    }
    onRotatePreview?.(sweepDeg);
    onRotateCommit({
      rotationDeg: normalizePlanAngleDeg(clamped),
      originX: Math.round(origin.x),
      originY: Math.round(origin.y),
      sweepDeg,
    });
  };

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: gestureEnabled,
    onMove: (delta) => {
      if (!startedRef.current) {
        startedRef.current = true;
        onPick();
        onGestureStart?.();
      }
      if (mode === 'rotate') applyRotatePreview(delta);
      else applyMovePreview(delta);
    },
    onCommit: () => {
      if (mode === 'rotate') commitRotate();
      else commitMove();
    },
  });

  const onPointerDown = (e: ThreeEvent<PointerEvent>) => {
    if (gestureEnabled && e.nativeEvent.button === 0) {
      startRef.current.xMm = e.point.x * MM;
      startRef.current.yMm = e.point.z * MM;
      lastMoveRef.current = ZERO_MOVE;
      lastAngleRef.current = adapter.rotationDeg;
      startedRef.current = false;
    }
    drag.handlers.onPointerDown(e);
  };

  return {
    handlers: { ...drag.handlers, onPointerDown },
    consumeClick: drag.consumeClick,
  };
}
