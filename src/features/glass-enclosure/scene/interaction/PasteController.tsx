import { useEffect, useRef } from 'react';
import { DoubleSide } from 'three';
import { clearSnapGuides, setSnapGuides } from '@/shared/three-engine';
import { applyPlanMoveSnap } from './planSnap';
import { buildPlanFootprint, clampPlanMove, penetratesAny } from './planCollision';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group, Mesh, MeshBasicMaterial } from 'three';
import type { PlanFootprint } from './planCollision';
import type { PlanPoint, PlanSnapTargets } from './planSnap';

export interface PasteGhostSpec {
  lengthMm: number;
  halfWidthMm: number;
  zMinMm: number;
  zMaxMm: number;
  rotationDeg: number;
}

interface PasteControllerProps {
  spec: PasteGhostSpec;
  snapTargets: PlanSnapTargets;
  obstacles: PlanFootprint[];
  onPlace: (centerXMm: number, centerYMm: number) => void;
}

const MM = 1000;
const DEG2RAD = Math.PI / 180;
const GHOST_ID = 'paste-ghost';
const GHOST_COLOR = '#7c3aed';
const BLOCKED_COLOR = '#dc2626';
const GHOST_OPACITY = 0.35;
const PASTE_GRID_MM = 50;
const CLICK_SLOP_PX = 5;
const PLANE_SIZE_M = 400;

const snapToGrid = (valueMm: number) => Math.round(valueMm / PASTE_GRID_MM) * PASTE_GRID_MM;

export function PasteController({ spec, snapTargets, obstacles, onPlace }: PasteControllerProps) {
  const ghostRef = useRef<Group>(null);
  const meshRef = useRef<Mesh>(null);
  const matRef = useRef<MeshBasicMaterial>(null);
  const freeRef = useRef<PlanPoint | null>(null);
  const posRef = useRef<PlanPoint | null>(null);
  const blockedRef = useRef(false);
  const downRef = useRef({ x: 0, y: 0 });

  useEffect(() => {
    freeRef.current = null;
    posRef.current = null;
    blockedRef.current = false;
    const ghost = ghostRef.current;
    if (ghost) ghost.visible = false;
    clearSnapGuides();
    return () => clearSnapGuides();
  }, [spec]);

  const rad = spec.rotationDeg * DEG2RAD;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);
  const halfL = spec.lengthMm / 2;
  const halfW = spec.halfWidthMm;

  const probes: PlanPoint[] = [
    { x: -halfL * dirX + halfW * dirY, y: -halfL * dirY - halfW * dirX },
    { x: -halfL * dirX - halfW * dirY, y: -halfL * dirY + halfW * dirX },
    { x: halfL * dirX + halfW * dirY, y: halfL * dirY - halfW * dirX },
    { x: halfL * dirX - halfW * dirY, y: halfL * dirY + halfW * dirX },
  ];

  const footprintAt = (centerX: number, centerY: number): PlanFootprint =>
    buildPlanFootprint(
      GHOST_ID,
      centerX - halfL * dirX,
      centerY - halfL * dirY,
      spec.lengthMm,
      spec.rotationDeg,
      spec.halfWidthMm,
      spec.zMinMm,
      spec.zMaxMm,
    );

  const applyGhost = (xMm: number, yMm: number, blocked: boolean) => {
    const ghost = ghostRef.current;
    const mesh = meshRef.current;
    const mat = matRef.current;
    if (!ghost || !mesh || !mat) return;
    ghost.visible = true;
    ghost.position.set(xMm / MM, 0, yMm / MM);
    ghost.rotation.y = -spec.rotationDeg * DEG2RAD;
    const heightM = (spec.zMaxMm - spec.zMinMm) / MM;
    mesh.scale.set(spec.lengthMm / MM, Math.max(0.01, heightM), (spec.halfWidthMm * 2) / MM);
    mesh.position.set(0, spec.zMinMm / MM + heightM / 2, 0);
    mat.color.set(blocked ? BLOCKED_COLOR : GHOST_COLOR);
  };

  const followPointer = (e: ThreeEvent<PointerEvent>) => {
    const gridX = snapToGrid(e.point.x * MM);
    const gridY = snapToGrid(e.point.z * MM);
    const stuck = applyPlanMoveSnap(probes, gridX, gridY, snapTargets);
    let x = stuck.dxMm;
    let y = stuck.dyMm;
    const blocked = penetratesAny(footprintAt(x, y), obstacles);
    if (blocked && freeRef.current) {
      const from = freeRef.current;
      const clamped = clampPlanMove(
        (dx, dy) => footprintAt(from.x + dx, from.y + dy),
        obstacles,
        x - from.x,
        y - from.y,
      );
      x = from.x + clamped.dxMm;
      y = from.y + clamped.dyMm;
    }
    const stillBlocked = penetratesAny(footprintAt(x, y), obstacles);
    if (!stillBlocked) freeRef.current = { x, y };
    posRef.current = { x, y };
    blockedRef.current = stillBlocked;
    setSnapGuides(stillBlocked ? [] : stuck.guides);
    applyGhost(x, y, stillBlocked);
  };

  const handlePointerDown = (e: ThreeEvent<PointerEvent>) => {
    downRef.current = { x: e.nativeEvent.clientX, y: e.nativeEvent.clientY };
  };

  const handleClick = (e: ThreeEvent<MouseEvent>) => {
    e.stopPropagation();
    const dx = e.nativeEvent.clientX - downRef.current.x;
    const dy = e.nativeEvent.clientY - downRef.current.y;
    if (dx * dx + dy * dy > CLICK_SLOP_PX * CLICK_SLOP_PX) return;
    const pos = posRef.current;
    if (!pos || blockedRef.current) return;
    clearSnapGuides();
    onPlace(Math.round(pos.x), Math.round(pos.y));
  };

  return (
    <>
      <mesh
        rotation={[-Math.PI / 2, 0, 0]}
        onPointerDown={handlePointerDown}
        onPointerMove={followPointer}
        onClick={handleClick}
      >
        <planeGeometry args={[PLANE_SIZE_M, PLANE_SIZE_M]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
      </mesh>
      <group ref={ghostRef} visible={false}>
        <mesh ref={meshRef} raycast={() => null}>
          <boxGeometry args={[1, 1, 1]} />
          <meshBasicMaterial
            ref={matRef}
            color={GHOST_COLOR}
            transparent
            opacity={GHOST_OPACITY}
            depthWrite={false}
          />
        </mesh>
      </group>
    </>
  );
}
