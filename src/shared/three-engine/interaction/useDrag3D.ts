import { useEffect, useRef } from 'react';
import { useThree } from '@react-three/fiber';
import { Plane, Quaternion, Vector3 } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import type { Object3D } from 'three';
import type { RefObject } from 'react';

export interface DragDeltaMm {
  x: number;
  y: number;
  z: number;
}

export type DragConstraint =
  | { mode: 'ground' }
  | { mode: 'panelPlane'; targetRef: RefObject<Object3D | null> }
  | { mode: 'axis'; targetRef: RefObject<Object3D | null>; localAxis: [number, number, number] };

export interface UseDrag3DOptions {
  constraint: DragConstraint;
  enabled: boolean;
  thresholdPx?: number;
  onMove: (delta: DragDeltaMm) => void;
  onCommit: (delta: DragDeltaMm) => void;
}

export interface Drag3DHandlers {
  onPointerDown: (e: ThreeEvent<PointerEvent>) => void;
  onPointerMove: (e: ThreeEvent<PointerEvent>) => void;
  onPointerUp: (e: ThreeEvent<PointerEvent>) => void;
  onPointerCancel: (e: ThreeEvent<PointerEvent>) => void;
}

export interface Drag3D {
  handlers: Drag3DHandlers;
  consumeClick: () => boolean;
}

const MM = 1000;
const ZERO_DELTA: DragDeltaMm = { x: 0, y: 0, z: 0 };
const GRAZE_DIR_Y = 0.25;
const MIN_GRAZE_DIR_Y = 0.05;
const MAX_GRAZE_GAIN = 4;
const MAX_PLANE_DELTA_M = 250;

const createWorkspace = () => ({
  plane: new Plane(),
  start: new Vector3(),
  hit: new Vector3(),
  axis: new Vector3(),
  axisX: new Vector3(),
  axisY: new Vector3(),
  normal: new Vector3(),
  quat: new Quaternion(),
  fwd: new Vector3(),
  right: new Vector3(),
  shallow: false,
  gain: 1,
});

type Workspace = ReturnType<typeof createWorkspace>;

interface DragSession {
  pointerId: number | null;
  active: boolean;
  screenX: number;
  screenY: number;
  delta: DragDeltaMm;
  suppressClick: boolean;
  frame: number | null;
}

export function useDrag3D({
  constraint,
  enabled,
  thresholdPx = 3,
  onMove,
  onCommit,
}: UseDrag3DOptions): Drag3D {
  const getThree = useThree((s) => s.get);
  const workspaceRef = useRef<Workspace | null>(null);
  const sessionRef = useRef<DragSession>({
    pointerId: null,
    active: false,
    screenX: 0,
    screenY: 0,
    delta: { x: 0, y: 0, z: 0 },
    suppressClick: false,
    frame: null,
  });

  const setControlsEnabled = (value: boolean) => {
    const controls = getThree().controls as unknown as { enabled: boolean } | null;
    if (controls) controls.enabled = value;
  };

  useEffect(() => {
    const session = sessionRef.current;
    return () => {
      if (session.frame !== null) cancelAnimationFrame(session.frame);
      if (session.pointerId === null) return;
      const controls = getThree().controls as unknown as { enabled: boolean } | null;
      if (controls) controls.enabled = true;
    };
  }, [getThree]);

  const beginPlane = (e: ThreeEvent<PointerEvent>): boolean => {
    const v = (workspaceRef.current ??= createWorkspace());
    v.start.copy(e.point);
    if (constraint.mode === 'ground') {
      const dirY = e.ray.direction.y;
      v.shallow = Math.abs(dirY) < GRAZE_DIR_Y;
      if (!v.shallow) {
        v.plane.normal.set(0, 1, 0);
        v.plane.constant = -e.point.y;
        return true;
      }
      v.fwd.set(e.ray.direction.x, 0, e.ray.direction.z);
      if (v.fwd.lengthSq() < 1e-8) return false;
      v.fwd.normalize();
      v.right.set(-v.fwd.z, 0, v.fwd.x);
      v.gain = Math.min(1 / Math.max(Math.abs(dirY), MIN_GRAZE_DIR_Y), MAX_GRAZE_GAIN);
      v.plane.setFromNormalAndCoplanarPoint(v.fwd, v.start);
      return true;
    }
    const target = constraint.targetRef.current;
    if (!target) return false;
    target.updateWorldMatrix(true, false);
    target.getWorldQuaternion(v.quat);
    if (constraint.mode === 'panelPlane') {
      v.axisX.set(1, 0, 0).applyQuaternion(v.quat);
      v.axisY.set(0, 1, 0).applyQuaternion(v.quat);
      v.normal.set(0, 0, 1).applyQuaternion(v.quat);
      v.plane.setFromNormalAndCoplanarPoint(v.normal, v.start);
      return true;
    }
    const [ax, ay, az] = constraint.localAxis;
    v.axis.set(ax, ay, az).applyQuaternion(v.quat).normalize();
    v.normal.copy(e.ray.direction).addScaledVector(v.axis, -e.ray.direction.dot(v.axis));
    if (v.normal.lengthSq() < 1e-6) {
      v.normal.set(0, 1, 0).addScaledVector(v.axis, -v.axis.y);
    }
    if (v.normal.lengthSq() < 1e-6) return false;
    v.normal.normalize();
    v.plane.setFromNormalAndCoplanarPoint(v.normal, v.start);
    return true;
  };

  const computeDelta = (e: ThreeEvent<PointerEvent>, out: DragDeltaMm): boolean => {
    const v = workspaceRef.current;
    if (!v || !e.ray.intersectPlane(v.plane, v.hit)) return false;
    v.hit.sub(v.start);
    if (v.hit.lengthSq() > MAX_PLANE_DELTA_M * MAX_PLANE_DELTA_M) return false;
    if (constraint.mode === 'ground') {
      if (v.shallow) {
        const rightAmt = v.hit.dot(v.right);
        const fwdAmt = v.hit.y * v.gain;
        out.x = (v.right.x * rightAmt + v.fwd.x * fwdAmt) * MM;
        out.y = 0;
        out.z = (v.right.z * rightAmt + v.fwd.z * fwdAmt) * MM;
        return true;
      }
      out.x = v.hit.x * MM;
      out.y = 0;
      out.z = v.hit.z * MM;
      return true;
    }
    if (constraint.mode === 'panelPlane') {
      out.x = v.hit.dot(v.axisX) * MM;
      out.y = v.hit.dot(v.axisY) * MM;
      out.z = 0;
      return true;
    }
    out.x = v.hit.dot(v.axis) * MM;
    out.y = 0;
    out.z = 0;
    return true;
  };

  // WHY: pointermove fires at the POINTING DEVICE's polling rate (125 Hz on a plain mouse, up to
  // 1000 Hz on a gaming one), not the display rate. Solving the move and rebuilding the preview on
  // every event throws away most of that work unseen — and on a heavy body (CSG'd wall, free-drawn
  // surface) that surplus is what makes the drag stutter. The delta is still computed from EVERY
  // event (the event object is only valid synchronously and we want the newest position), but the
  // downstream onMove runs at most once per displayed frame.
  const cancelFrame = (session: DragSession) => {
    if (session.frame === null) return;
    cancelAnimationFrame(session.frame);
    session.frame = null;
  };

  const scheduleMove = (session: DragSession) => {
    if (session.frame !== null) return;
    session.frame = requestAnimationFrame(() => {
      session.frame = null;
      if (session.pointerId === null || !session.active) return;
      onMove(session.delta);
    });
  };

  const endDrag = (e: ThreeEvent<PointerEvent>, commit: boolean) => {
    const session = sessionRef.current;
    if (session.pointerId !== e.pointerId) return;
    (e.target as Element | null)?.releasePointerCapture(e.pointerId);
    cancelFrame(session);
    session.pointerId = null;
    setControlsEnabled(true);
    document.body.style.cursor = 'auto';
    if (!session.active) return;
    session.active = false;
    session.suppressClick = true;
    e.stopPropagation();
    // The last pointermove may still be sitting in a cancelled frame — settle the preview on the
    // real final delta before committing, so commit and preview never disagree by one frame.
    if (commit) {
      onMove(session.delta);
      onCommit(session.delta);
    } else {
      onMove(ZERO_DELTA);
    }
  };

  const onPointerDown = (e: ThreeEvent<PointerEvent>) => {
    const session = sessionRef.current;
    session.suppressClick = false;
    if (!enabled || session.pointerId !== null) return;
    if (e.nativeEvent.button !== 0) return;
    if (!beginPlane(e)) return;
    e.stopPropagation();
    (e.target as Element | null)?.setPointerCapture(e.pointerId);
    session.pointerId = e.pointerId;
    session.active = thresholdPx <= 0;
    session.screenX = e.nativeEvent.clientX;
    session.screenY = e.nativeEvent.clientY;
    session.delta.x = 0;
    session.delta.y = 0;
    session.delta.z = 0;
    setControlsEnabled(false);
    if (session.active) document.body.style.cursor = 'grabbing';
  };

  const onPointerMove = (e: ThreeEvent<PointerEvent>) => {
    const session = sessionRef.current;
    if (session.pointerId !== e.pointerId) return;
    if (!session.active) {
      const dx = e.nativeEvent.clientX - session.screenX;
      const dy = e.nativeEvent.clientY - session.screenY;
      if (dx * dx + dy * dy < thresholdPx * thresholdPx) return;
      session.active = true;
      document.body.style.cursor = 'grabbing';
    }
    e.stopPropagation();
    if (computeDelta(e, session.delta)) scheduleMove(session);
  };

  const consumeClick = () => {
    const session = sessionRef.current;
    if (!session.suppressClick) return false;
    session.suppressClick = false;
    return true;
  };

  return {
    handlers: {
      onPointerDown,
      onPointerMove,
      onPointerUp: (e) => endDrag(e, true),
      onPointerCancel: (e) => endDrag(e, false),
    },
    consumeClick,
  };
}
