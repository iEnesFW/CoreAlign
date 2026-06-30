import { useEffect, useMemo } from 'react';
import { Billboard, Edges, Text } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import { useGlassMaterial } from '../materials/glassMaterial';
import { HardwareObject, type HardwareDragDelta } from './HardwareObject';
import { PanelFittings } from './PanelFittings';
import { buildPanelGlassGeometry, type PanelGlassSpec } from './panelGeometry';
import { buildPanelFrameGeometry } from './panelFrameGeometry';
import { panelIsShaped } from '../../model/panelOutline';
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
  onResizeHardware?: (hardwareId: string, widthMm: number, heightMm: number) => void;
  quality: QualityPreset;
  showAnnotations: boolean;
  panelIndex: number;
  isSelected: boolean;
  onSelect: () => void;
  shapeSpec?: PanelGlassSpec | null;
  frameColor?: string;
  showFrameBand?: boolean;
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
  onResizeHardware,
  quality,
  showAnnotations,
  panelIndex,
  isSelected,
  onSelect,
  shapeSpec,
  frameColor,
  showFrameBand = false,
}: PanelMeshProps) {
  const material = useGlassMaterial({
    quality,
    thicknessMm,
    structure: glassStructure,
    isSelected,
  });
  const thicknessM = useMemo(() => thicknessMm / 1000, [thicknessMm]);

  const shaped = Boolean(shapeSpec && panelIsShaped(shapeSpec));
  const sw = shapeSpec?.widthMm;
  const sh = shapeSpec?.heightMm;
  const st = shapeSpec?.topShape;
  const str = shapeSpec?.topRightHeightMm;
  const sa = shapeSpec?.archRiseMm;
  const sc = shapeSpec?.cornerRadiiMm;
  const sn = shapeSpec?.cornerNotchMm;
  const sk = shapeSpec?.shapeKind;
  const sp = shapeSpec?.points;
  const shapedGeometry = useMemo(() => {
    if (!shaped || sw === undefined || sh === undefined) return null;
    return buildPanelGlassGeometry(
      {
        widthMm: sw,
        heightMm: sh,
        topShape: st,
        topRightHeightMm: str,
        archRiseMm: sa,
        cornerRadiiMm: sc,
        cornerNotchMm: sn,
        shapeKind: sk,
        points: sp,
      },
      thicknessM,
    );
  }, [shaped, sw, sh, st, str, sa, sc, sn, sk, sp, thicknessM]);
  useEffect(() => () => shapedGeometry?.dispose(), [shapedGeometry]);
  const frameGeometry = useMemo(() => {
    // The wrapping band is only the panel's OWN frame when the run's rectangular rails are
    // suppressed (a single shaped pane). In a multi-panel run the cell rails already frame the
    // pane, so adding the band on top reads as a "panel inside a panel" — skip it there.
    if (!showFrameBand) return null;
    // Must stay a SUBSET of panelIsShaped (which gates the glass mesh): an arched top
    // only counts once it has a positive rise, else a 0-rise arched pane would suppress
    // the rect frame yet render no band (panelIsShaped=false → no shaped glass) = bare glass.
    const anyCorner = (c?: typeof sc) =>
      Boolean(c && ((c.tl ?? 0) > 0 || (c.tr ?? 0) > 0 || (c.bl ?? 0) > 0 || (c.br ?? 0) > 0));
    const frameShaped =
      sk === 'ellipse' ||
      sk === 'polygon' ||
      st === 'raked' ||
      (st === 'arched' && (sa ?? 0) > 0) ||
      anyCorner(sc) ||
      anyCorner(sn);
    if (!frameShaped || sw === undefined || sh === undefined) return null;
    return buildPanelFrameGeometry(
      {
        widthMm: sw,
        heightMm: sh,
        topShape: st,
        topRightHeightMm: str,
        archRiseMm: sa,
        cornerRadiiMm: sc,
        cornerNotchMm: sn,
        shapeKind: sk,
        points: sp,
      },
      35,
      Math.max(thicknessM * 1.6, 0.02),
    );
  }, [showFrameBand, sw, sh, st, str, sa, sc, sn, sk, sp, thicknessM]);
  useEffect(() => () => frameGeometry?.dispose(), [frameGeometry]);

  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    onSelect();
  };
  const onPointerOver = (e: ThreeEvent<PointerEvent>) => {
    e.stopPropagation();
    document.body.style.cursor = 'pointer';
  };
  const onPointerOut = () => {
    document.body.style.cursor = 'auto';
  };

  return (
    <group position={[centerX, baseY + heightM / 2, 0]}>
      {shapedGeometry ? (
        <>
          <mesh
            geometry={shapedGeometry}
            position={[0, -heightM / 2, 0]}
            material={material}
            castShadow
            receiveShadow
            onClick={handleClick}
            onPointerOver={onPointerOver}
            onPointerOut={onPointerOut}
          >
            <Edges color={isSelected ? '#2563eb' : '#cbd5e1'} threshold={15} />
          </mesh>
          {frameGeometry && (
            <mesh geometry={frameGeometry} position={[0, -heightM / 2, 0]} castShadow receiveShadow>
              <meshStandardMaterial
                color={isSelected ? '#3b82f6' : (frameColor ?? '#aab4ba')}
                metalness={0.5}
                roughness={0.5}
              />
            </mesh>
          )}
        </>
      ) : (
        <mesh
          material={material}
          castShadow
          receiveShadow
          onClick={handleClick}
          onPointerOver={onPointerOver}
          onPointerOut={onPointerOut}
        >
          <boxGeometry args={[widthM, heightM, thicknessM]} />
          <Edges color={isSelected ? '#2563eb' : '#cbd5e1'} threshold={15} />
        </mesh>
      )}

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
          onResize={
            onResizeHardware
              ? (widthMm, heightMm) => onResizeHardware(hw.id, widthMm, heightMm)
              : undefined
          }
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
