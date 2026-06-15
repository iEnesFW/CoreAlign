import { useEffect, useMemo } from 'react';
import { Billboard, Edges, Text } from '@react-three/drei';
import { ExtrudeGeometry, Shape } from 'three';
import type { ThreeEvent } from '@react-three/fiber';
import { useGlassMaterial } from '../materials/glassMaterial';
import { HardwareObject, type HardwareDragDelta } from './HardwareObject';
import { PanelFittings } from './PanelFittings';
import { arcPointAt, type ArcChord } from '../../model/arcGeometry';
import type { QualityPreset } from '@/shared/three-engine';
import type { GlassOpeningType, GlassStructure } from '../../model/glassEnclosure.types';
import type { SceneHardwareItem } from '../../model/project.types';

interface CurvedPanelMeshProps {
  panelId: string;
  radiusM: number;
  direction: 1 | -1;
  phiStart: number;
  phiEnd: number;
  chord: ArcChord;
  baseY: number;
  heightM: number;
  thicknessMm: number;
  glassStructure?: GlassStructure;
  openingType: GlassOpeningType;
  hasHandle: boolean;
  hasLock: boolean;
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

const CURVE_STEP_RAD = 0.08;

const buildCurvedGlassGeometry = (
  radiusM: number,
  direction: 1 | -1,
  phiStart: number,
  phiEnd: number,
  thicknessM: number,
  heightM: number,
) => {
  // Guard degenerate inputs (0/NaN radius or zero-width span) that would feed
  // NaN/empty geometry into the extruder.
  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, phiEnd - phiStart);
  const endPhi = phiStart + span;
  const centerY = -direction * radius;
  const outer = radius + thicknessM / 2;
  const inner = Math.max(0.001, radius - thicknessM / 2);
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const outerClockwise = direction === 1;
  const shape = new Shape();
  shape.absarc(0, centerY, outer, toAngle(phiStart), toAngle(endPhi), outerClockwise);
  shape.absarc(0, centerY, inner, toAngle(endPhi), toAngle(phiStart), !outerClockwise);
  shape.closePath();
  const curveSegments = Math.max(8, Math.ceil(span / CURVE_STEP_RAD));
  return new ExtrudeGeometry(shape, { depth: heightM, bevelEnabled: false, curveSegments });
};

export function CurvedPanelMesh({
  radiusM,
  direction,
  phiStart,
  phiEnd,
  chord,
  baseY,
  heightM,
  thicknessMm,
  glassStructure,
  openingType,
  hasHandle,
  hasLock,
  hardware,
  selectedHardwareId,
  onSelectHardware,
  onDragHardware,
  quality,
  showAnnotations,
  panelIndex,
  isSelected,
  onSelect,
}: CurvedPanelMeshProps) {
  const material = useGlassMaterial({
    quality,
    thicknessMm,
    structure: glassStructure,
    isSelected,
  });
  const thicknessM = thicknessMm / 1000;

  const geometry = useMemo(
    () => buildCurvedGlassGeometry(radiusM, direction, phiStart, phiEnd, thicknessM, heightM),
    [radiusM, direction, phiStart, phiEnd, thicknessM, heightM],
  );
  useEffect(() => () => geometry.dispose(), [geometry]);

  const annotationAnchor = useMemo(
    () => arcPointAt(radiusM, direction, (phiStart + phiEnd) / 2),
    [radiusM, direction, phiStart, phiEnd],
  );

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    onSelect();
  };

  return (
    <group>
      <mesh
        geometry={geometry}
        material={material}
        rotation={[-Math.PI / 2, 0, 0]}
        position={[0, baseY, 0]}
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
        <Edges color={isSelected ? '#2563eb' : '#9aacb5'} threshold={15} />
      </mesh>

      <group
        position={[chord.midX, baseY + heightM / 2, chord.midZ]}
        rotation={[0, -chord.yawRad, 0]}
      >
        <PanelFittings
          widthM={chord.chordM}
          thicknessM={thicknessM}
          openingType={openingType}
          hasHandle={hasHandle}
          hasLock={hasLock}
          onSelect={onSelect}
        />
        {hardware.map((hw) => (
          <HardwareObject
            key={hw.id}
            item={hw}
            isSelected={selectedHardwareId === hw.id}
            onSelect={() => onSelectHardware(hw.id)}
            onCommitDrag={onDragHardware ? (delta) => onDragHardware(hw.id, delta) : undefined}
          />
        ))}
      </group>

      {showAnnotations && chord.chordM > 0.18 && (
        <Billboard
          position={[
            annotationAnchor.x,
            baseY + heightM + 0.1 + (panelIndex % 2 === 0 ? 0 : 0.12),
            annotationAnchor.z,
          ]}
          follow
        >
          <Text
            fontSize={0.07}
            color={isSelected ? '#1d4ed8' : '#475569'}
            anchorX="center"
            anchorY="bottom"
            outlineWidth={0.003}
            outlineColor="#ffffff"
          >
            {`${panelIndex + 1} · ${Math.round(radiusM * (phiEnd - phiStart) * 1000)} mm ${OPENING_SYMBOL[openingType]}`}
          </Text>
        </Billboard>
      )}
    </group>
  );
}
