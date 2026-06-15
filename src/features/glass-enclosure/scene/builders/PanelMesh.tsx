import { useMemo } from 'react';
import { Billboard, Edges, Text } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import { useGlassMaterial } from '../materials/glassMaterial';
import { HardwareObject, type HardwareDragDelta } from './HardwareObject';
import { PanelFittings } from './PanelFittings';
import type { QualityPreset } from '@/shared/three-engine';
import type { GlassOpeningType, GlassStructure } from '../../model/glassEnclosure.types';
import type { SceneHardwareItem } from '../../model/project.types';

interface PanelMeshProps {
  panelId: string;
  centerX: number;
  baseY: number;
  widthM: number;
  heightM: number;
  thicknessMm: number;
  glassStructure?: GlassStructure;
  openingType: GlassOpeningType;
  hasHandle: boolean;
  hasLock: boolean;
  hasBrushSeal: boolean;
  hardware: SceneHardwareItem[];
  selectedHardwareId: string | null;
  onSelectHardware: (hardwareId: string) => void;
  onDragHardware?: (hardwareId: string, delta: HardwareDragDelta) => void;
  quality: QualityPreset;
  showAnnotations: boolean;
  panelIndex: number;
  isSelected: boolean;
  onSelect: () => void;
}

const OPENING_SYMBOL: Record<GlassOpeningType, string> = {
  Fixed: '■',
  SlidingLeft: '◀',
  SlidingRight: '▶',
  Folding: '◆',
  Hinged: '◯',
  Guillotine: '▲',
};

export function PanelMesh({
  centerX,
  baseY,
  widthM,
  heightM,
  thicknessMm,
  glassStructure,
  openingType,
  hasHandle,
  hasLock,
  hasBrushSeal,
  hardware,
  selectedHardwareId,
  onSelectHardware,
  onDragHardware,
  quality,
  showAnnotations,
  panelIndex,
  isSelected,
  onSelect,
}: PanelMeshProps) {
  const material = useGlassMaterial({
    quality,
    thicknessMm,
    structure: glassStructure,
    isSelected,
  });
  const thicknessM = useMemo(() => thicknessMm / 1000, [thicknessMm]);

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    onSelect();
  };

  return (
    <group position={[centerX, baseY + heightM / 2, 0]}>
      <mesh
        material={material}
        castShadow
        receiveShadow
        onClick={handleClick}
        onPointerOver={(e) => {
          e.stopPropagation();
          document.body.style.cursor = 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
      >
        <boxGeometry args={[widthM, heightM, thicknessM]} />
        <Edges color={isSelected ? '#2563eb' : '#9aacb5'} threshold={15} />
      </mesh>

      <PanelFittings
        widthM={widthM}
        thicknessM={thicknessM}
        openingType={openingType}
        hasHandle={hasHandle}
        hasLock={hasLock}
        onSelect={onSelect}
      />

      {hasBrushSeal && (
        <>
          <mesh position={[-widthM / 2 + 0.006, 0, 0]}>
            <boxGeometry args={[0.012, heightM, thicknessM + 0.004]} />
            <meshStandardMaterial
              color="#1f2937"
              roughness={0.95}
              metalness={0}
              transparent
              opacity={0.92}
            />
          </mesh>
          <mesh position={[widthM / 2 - 0.006, 0, 0]}>
            <boxGeometry args={[0.012, heightM, thicknessM + 0.004]} />
            <meshStandardMaterial
              color="#1f2937"
              roughness={0.95}
              metalness={0}
              transparent
              opacity={0.92}
            />
          </mesh>
        </>
      )}

      {hardware.map((hw) => (
        <HardwareObject
          key={hw.id}
          item={hw}
          isSelected={selectedHardwareId === hw.id}
          onSelect={() => onSelectHardware(hw.id)}
          onCommitDrag={onDragHardware ? (delta) => onDragHardware(hw.id, delta) : undefined}
        />
      ))}

      {showAnnotations && widthM > 0.18 && (
        <Billboard position={[0, heightM / 2 + 0.1 + (panelIndex % 2 === 0 ? 0 : 0.12), 0]} follow>
          <Text
            fontSize={0.07}
            color={isSelected ? '#1d4ed8' : '#475569'}
            anchorX="center"
            anchorY="bottom"
            outlineWidth={0.003}
            outlineColor="#ffffff"
          >
            {`${panelIndex + 1} · ${Math.round(widthM * 1000)} mm ${OPENING_SYMBOL[openingType]}`}
          </Text>
        </Billboard>
      )}
    </group>
  );
}
