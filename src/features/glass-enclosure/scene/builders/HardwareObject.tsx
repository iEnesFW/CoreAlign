import { useRef, type ReactNode } from 'react';
import { Edges } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group } from 'three';
import { useDrag3D } from '../interaction/useDrag3D';
import type { SceneHardwareItem, SceneHardwareKind } from '../../model/project.types';

export interface HardwareDragDelta {
  dx: number;
  dy: number;
  dz: number;
}

interface HardwareObjectProps {
  item: SceneHardwareItem;
  isSelected: boolean;
  onSelect: () => void;
  onCommitDrag?: (delta: HardwareDragDelta) => void;
}

const MM = 1000;
const MIN_M = 0.004;
const SELECTED_EDGE = '#2563eb';
const SLAT_COUNT = 4;
const CORNER_LEG_RATIO = 0.35;

const KIND_ROTATIONS: Partial<Record<SceneHardwareKind, [number, number, number]>> = {
  Lock: [Math.PI / 2, 0, 0],
  Roller: [0, 0, Math.PI / 2],
  Stopper: [-Math.PI / 2, 0, 0],
};

const METAL_KINDS = new Set<SceneHardwareKind>(['Lock', 'Roller', 'PullHandle']);
const RUBBER_KINDS = new Set<SceneHardwareKind>(['Stopper', 'GasketStrip']);

const toMeters = (mm: number) => Math.max(MIN_M, mm / MM);

const surfaceFor = (kind: SceneHardwareKind) => {
  if (RUBBER_KINDS.has(kind)) return { metalness: 0.1, roughness: 0.85 };
  if (METAL_KINDS.has(kind)) return { metalness: 0.85, roughness: 0.35 };
  return { metalness: 0.6, roughness: 0.35 };
};

interface KindGeometryProps {
  kind: SceneHardwareKind;
  w: number;
  h: number;
  d: number;
}

function KindGeometry({ kind, w, h, d }: KindGeometryProps) {
  if (kind === 'Lock') {
    const r = Math.min(w, h) / 2;
    return <cylinderGeometry args={[r, r, d, 24]} />;
  }
  if (kind === 'Roller') {
    const r = Math.min(h, d) / 2;
    return <cylinderGeometry args={[r, r, w, 24]} />;
  }
  if (kind === 'PullHandle') {
    const r = Math.min(w, d) / 2;
    return <cylinderGeometry args={[r, r, h, 24]} />;
  }
  if (kind === 'Stopper') {
    const r = Math.min(w, h) / 2;
    return <cylinderGeometry args={[r, r, d, 3]} />;
  }
  if (kind === 'CornerJoint') {
    return <boxGeometry args={[w * CORNER_LEG_RATIO, h, d]} />;
  }
  return <boxGeometry args={[w, h, d]} />;
}

export function HardwareObject({ item, isSelected, onSelect, onCommitDrag }: HardwareObjectProps) {
  const w = toMeters(item.widthMm);
  const h = toMeters(item.heightMm);
  const d = toMeters(item.depthMm);
  const groupRef = useRef<Group>(null);
  const baseX = item.offsetXmm / MM;
  const baseY = item.offsetYmm / MM;
  const baseZ = item.offsetZmm / MM;
  const draggable = isSelected && Boolean(onCommitDrag);

  const drag = useDrag3D({
    constraint: { mode: 'panelPlane', targetRef: groupRef },
    enabled: draggable,
    onMove: (delta) => {
      groupRef.current?.position.set(baseX + delta.x / MM, baseY + delta.y / MM, baseZ);
    },
    onCommit: (delta) => {
      groupRef.current?.position.set(baseX, baseY, baseZ);
      if (Math.round(delta.x) !== 0 || Math.round(delta.y) !== 0) {
        onCommitDrag?.({ dx: delta.x, dy: delta.y, dz: 0 });
      }
    },
  });

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    if (drag.consumeClick()) return;
    onSelect();
  };

  const isCornerJoint = item.kind === 'CornerJoint';
  const hasSlats = item.kind === 'Vent' || item.kind === 'Louver';
  const surface = surfaceFor(item.kind);

  const body: ReactNode = (
    <>
      <mesh
        castShadow
        onClick={handleClick}
        onPointerOver={(e) => {
          e.stopPropagation();
          document.body.style.cursor = draggable ? 'grab' : 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
        position={isCornerJoint ? [(-w * (1 - CORNER_LEG_RATIO)) / 2, 0, 0] : [0, 0, 0]}
        rotation={KIND_ROTATIONS[item.kind] ?? [0, 0, 0]}
      >
        <KindGeometry kind={item.kind} w={w} h={h} d={d} />
        <meshStandardMaterial
          color={item.colorHex}
          metalness={surface.metalness}
          roughness={surface.roughness}
          envMapIntensity={0.6}
        />
        {isSelected && <Edges color={SELECTED_EDGE} threshold={15} />}
      </mesh>

      {isCornerJoint && (
        <mesh castShadow onClick={handleClick} position={[0, (-h * (1 - CORNER_LEG_RATIO)) / 2, 0]}>
          <boxGeometry args={[w, h * CORNER_LEG_RATIO, d]} />
          <meshStandardMaterial
            color={item.colorHex}
            metalness={surface.metalness}
            roughness={surface.roughness}
            envMapIntensity={0.6}
          />
          {isSelected && <Edges color={SELECTED_EDGE} threshold={15} />}
        </mesh>
      )}

      {hasSlats && (
        <HardwareSlats
          width={w}
          height={h}
          depth={d}
          colorHex={item.colorHex}
          angled={item.kind === 'Louver'}
        />
      )}
    </>
  );

  return (
    <group ref={groupRef} position={[baseX, baseY, baseZ]} {...drag.handlers}>
      {body}
    </group>
  );
}

interface HardwareSlatsProps {
  width: number;
  height: number;
  depth: number;
  colorHex: string;
  angled: boolean;
}

function HardwareSlats({ width, height, depth, colorHex, angled }: HardwareSlatsProps) {
  const gap = height / (SLAT_COUNT + 1);
  const slatHeight = gap * 0.6;
  return (
    <>
      {Array.from({ length: SLAT_COUNT }, (_, i) => {
        const y = -height / 2 + gap * (i + 1);
        return (
          <mesh
            key={i}
            position={[0, y, depth / 2 + 0.001]}
            rotation={[angled ? -0.5 : 0, 0, 0]}
            castShadow
          >
            <boxGeometry args={[width * 0.86, slatHeight, depth * 0.5]} />
            <meshStandardMaterial color={colorHex} metalness={0.55} roughness={0.4} />
          </mesh>
        );
      })}
    </>
  );
}
