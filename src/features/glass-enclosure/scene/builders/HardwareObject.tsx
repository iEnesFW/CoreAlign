import { useRef, useState, type ReactNode } from 'react';
import { Edges } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import type { Group } from 'three';
import { useDrag3D } from '../interaction/useDrag3D';
import { useDesignerStore } from '../../model/designerStore';
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
  onResize?: (widthMm: number, heightMm: number) => void;
}

const MM = 1000;
const MIN_M = 0.004;
const SELECTED_EDGE = '#2563eb';
const SLAT_COUNT = 4;
const CORNER_LEG_RATIO = 0.35;
const HANDLE_RADIUS_M = 0.018;
const HANDLE_COLOR = '#1d4ed8';
const HANDLE_HOVER_COLOR = '#f97316';
const MIN_HARDWARE_MM = 8;
const RESIZE_GRID_MM = 2;
const CORNER_SIGNS: readonly [number, number][] = [
  [-1, -1],
  [1, -1],
  [1, 1],
  [-1, 1],
];

const KIND_ROTATIONS: Partial<Record<SceneHardwareKind, [number, number, number]>> = {
  Lock: [Math.PI / 2, 0, 0],
  Roller: [0, 0, Math.PI / 2],
  Stopper: [-Math.PI / 2, 0, 0],
};

const METAL_KINDS = new Set<SceneHardwareKind>(['Lock', 'Roller', 'PullHandle']);
const RUBBER_KINDS = new Set<SceneHardwareKind>(['Stopper', 'GasketStrip']);

const toMeters = (mm: number) => Math.max(MIN_M, mm / MM);

const surfaceFor = (kind: SceneHardwareKind) => {
  if (RUBBER_KINDS.has(kind))
    return { metalness: 0, roughness: 0.9, envMapIntensity: 0.4, clearcoat: 0 };
  if (METAL_KINDS.has(kind))
    return { metalness: 0.92, roughness: 0.22, envMapIntensity: 1.25, clearcoat: 0.35 };
  return { metalness: 0.75, roughness: 0.3, envMapIntensity: 1, clearcoat: 0.15 };
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

export function HardwareObject({
  item,
  isSelected,
  onSelect,
  onCommitDrag,
  onResize,
}: HardwareObjectProps) {
  const w = toMeters(item.widthMm);
  const h = toMeters(item.heightMm);
  const d = toMeters(item.depthMm);
  const groupRef = useRef<Group>(null);
  const baseX = item.offsetXmm / MM;
  const baseY = item.offsetYmm / MM;
  const baseZ = item.offsetZmm / MM;
  const draggable = isSelected && Boolean(onCommitDrag);
  const transformHandlesActive = useDesignerStore((s) => s.transformHandlesActive);
  const showResizeHandles = isSelected && transformHandlesActive && Boolean(onResize);

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
        <meshPhysicalMaterial
          color={item.colorHex}
          metalness={surface.metalness}
          roughness={surface.roughness}
          envMapIntensity={surface.envMapIntensity}
          clearcoat={surface.clearcoat}
          clearcoatRoughness={0.15}
        />
        {isSelected && <Edges color={SELECTED_EDGE} threshold={15} />}
      </mesh>

      {isCornerJoint && (
        <mesh castShadow onClick={handleClick} position={[0, (-h * (1 - CORNER_LEG_RATIO)) / 2, 0]}>
          <boxGeometry args={[w, h * CORNER_LEG_RATIO, d]} />
          <meshPhysicalMaterial
            color={item.colorHex}
            metalness={surface.metalness}
            roughness={surface.roughness}
            envMapIntensity={surface.envMapIntensity}
            clearcoat={surface.clearcoat}
            clearcoatRoughness={0.15}
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
    <>
      <group ref={groupRef} position={[baseX, baseY, baseZ]} {...drag.handlers}>
        {body}
      </group>
      {showResizeHandles && onResize && (
        <group position={[baseX, baseY, baseZ]}>
          {CORNER_SIGNS.map(([sx, sy], i) => (
            <HardwareCornerHandle key={i} item={item} sx={sx} sy={sy} onResize={onResize} />
          ))}
        </group>
      )}
    </>
  );
}

// Q-mode corner handles for a selected hardware item: drag a corner to resize width/height
// symmetrically about its centre (so the offset stays put, matching the inspector size fields).
function HardwareCornerHandle({
  item,
  sx,
  sy,
  onResize,
}: {
  item: SceneHardwareItem;
  sx: number;
  sy: number;
  onResize: (widthMm: number, heightMm: number) => void;
}) {
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const baseX = (sx * item.widthMm) / 2 / MM;
  const baseY = (sy * item.heightMm) / 2 / MM;
  const z = item.depthMm / 2 / MM + 0.01;

  const drag = useDrag3D({
    constraint: { mode: 'panelPlane', targetRef: anchorRef },
    enabled: true,
    onMove: (delta) => {
      anchorRef.current?.position.set(baseX + delta.x / MM, baseY + delta.y / MM, z);
    },
    onCommit: (delta) => {
      anchorRef.current?.position.set(baseX, baseY, z);
      const snap = (v: number) => Math.round(v / RESIZE_GRID_MM) * RESIZE_GRID_MM;
      const nextW = Math.max(MIN_HARDWARE_MM, snap(item.widthMm + 2 * sx * delta.x));
      const nextH = Math.max(MIN_HARDWARE_MM, snap(item.heightMm + 2 * sy * delta.y));
      if (nextW !== item.widthMm || nextH !== item.heightMm) onResize(nextW, nextH);
    },
  });

  return (
    <group ref={anchorRef} position={[baseX, baseY, z]}>
      <mesh
        {...drag.handlers}
        onClick={(e) => {
          e.stopPropagation();
          drag.consumeClick();
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          setHovered(true);
          document.body.style.cursor = 'nwse-resize';
        }}
        onPointerOut={() => {
          setHovered(false);
          document.body.style.cursor = 'auto';
        }}
        renderOrder={999}
      >
        <sphereGeometry args={[HANDLE_RADIUS_M, 14, 14]} />
        {/* WHY: depthTest off so corner handles stay visible in front of the hardware body. */}
        <meshBasicMaterial
          color={hovered ? HANDLE_HOVER_COLOR : HANDLE_COLOR}
          depthTest={false}
          depthWrite={false}
          transparent
        />
      </mesh>
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
            <meshPhysicalMaterial
              color={colorHex}
              metalness={0.85}
              roughness={0.3}
              envMapIntensity={1}
              clearcoat={0.2}
              clearcoatRoughness={0.15}
            />
          </mesh>
        );
      })}
    </>
  );
}
