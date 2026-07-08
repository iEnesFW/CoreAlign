import { useEffect, useMemo } from 'react';
import { DoubleSide } from 'three';
import { Billboard, Edges, Text } from '@react-three/drei';
import type { ThreeEvent } from '@react-three/fiber';
import { useGlassMaterial } from '../materials/glassMaterial';
import { HardwareObject, type HardwareDragDelta } from './HardwareObject';
import { PanelFittings } from './PanelFittings';
import {
  buildCurvedBandGeometry,
  buildCurvedShapedFrameGeometry,
  buildCurvedShapedGeometry,
} from './curvedExtrude';
import type { PanelGlassSpec } from './panelGeometry';
import { panelIsShaped, panelOutlinePointsMm } from '../../model/panelOutline';
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
  onResizeHardware,
  quality,
  showAnnotations,
  panelIndex,
  isSelected,
  onSelect,
  shapeSpec,
  frameColor,
  showFrameBand = false,
}: CurvedPanelMeshProps) {
  const material = useGlassMaterial({
    quality,
    thicknessMm,
    structure: glassStructure,
    isSelected,
  });
  const thicknessM = thicknessMm / 1000;

  // A shaped pane (triangle / oval / polygon / raked / arched / corners) follows the arc by
  // sampling its silhouette straight into cylindrical coords (smooth, no faceting); a plain
  // rectangle stays the uniform curved band.
  const shaped = Boolean(shapeSpec && panelIsShaped(shapeSpec));
  const sw = shapeSpec?.widthMm;
  const sh = shapeSpec?.heightMm;
  const st = shapeSpec?.topShape ?? null;
  const str = shapeSpec?.topRightHeightMm ?? null;
  const sa = shapeSpec?.archRiseMm ?? null;
  const sc = shapeSpec?.cornerRadiiMm ?? null;
  const sn = shapeSpec?.cornerNotchMm ?? null;
  const sk = shapeSpec?.shapeKind ?? null;
  const sp = shapeSpec?.points ?? null;
  const outline = useMemo(() => {
    if (!shaped || sw === undefined || sh === undefined) return null;
    const o = panelOutlinePointsMm({
      widthMm: sw,
      heightMm: sh,
      topShape: st,
      topRightHeightMm: str,
      archRiseMm: sa,
      cornerRadiiMm: sc,
      cornerNotchMm: sn,
      shapeKind: sk,
      points: sp,
    });
    return o.length >= 3 ? o : null;
  }, [shaped, sw, sh, st, str, sa, sc, sn, sk, sp]);

  const geometry = useMemo(() => {
    if (outline && sw !== undefined) {
      return buildCurvedShapedGeometry(
        outline,
        sw,
        radiusM,
        direction,
        phiStart,
        phiEnd,
        thicknessM,
      );
    }
    return buildCurvedBandGeometry(radiusM, direction, phiStart, phiEnd, thicknessM, heightM);
  }, [outline, sw, radiusM, direction, phiStart, phiEnd, thicknessM, heightM]);
  useEffect(() => () => geometry.dispose(), [geometry]);

  // Silhouette-hugging frame for a single shaped pane (its rails are suppressed upstream, like the
  // flat PanelMesh path) — otherwise a shaped arc hole-fill reads as bare frameless glass.
  const frameGeometry = useMemo(() => {
    if (!showFrameBand || !outline || sw === undefined) return null;
    return buildCurvedShapedFrameGeometry(
      outline,
      sw,
      radiusM,
      direction,
      phiStart,
      phiEnd,
      Math.max(thicknessM * 1.6, 0.02),
      35,
    );
  }, [showFrameBand, outline, sw, radiusM, direction, phiStart, phiEnd, thicknessM]);
  useEffect(() => () => frameGeometry?.dispose(), [frameGeometry]);

  const annotationAnchor = useMemo(
    () => arcPointAt(radiusM, direction, (phiStart + phiEnd) / 2),
    [radiusM, direction, phiStart, phiEnd],
  );

  // Hardware/fittings anchor ON the cylinder, not the chord plane: the chord frame floats off the
  // curved glass by the panel-span sagitta (R·(1−cos(Δφ/2)) — tens to hundreds of mm), which is
  // exactly the "pins hovering off the glass" report. Each item's offsetXmm is treated as the
  // DEVELOPED (arc-length) coordinate from the panel mid — matching the developed panel widths —
  // and baked into its own tangent-frame anchor (offsetXmm passed as 0 so it isn't double-applied).
  const phiMid = (phiStart + phiEnd) / 2;
  const tangentYawAt = (phi: number) => Math.atan2(direction * Math.sin(phi), Math.cos(phi));
  const surfaceAnchor = (phi: number) => arcPointAt(radiusM, direction, phi);

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

      {frameGeometry && (
        <mesh
          geometry={frameGeometry}
          rotation={[-Math.PI / 2, 0, 0]}
          position={[0, baseY, 0]}
          castShadow
          receiveShadow
        >
          <meshStandardMaterial
            color={isSelected ? '#3b82f6' : (frameColor ?? '#aab4ba')}
            metalness={0.5}
            roughness={0.5}
            side={DoubleSide}
          />
        </mesh>
      )}

      <group
        position={[surfaceAnchor(phiMid).x, baseY + heightM / 2, surfaceAnchor(phiMid).z]}
        rotation={[0, -tangentYawAt(phiMid), 0]}
      >
        <PanelFittings
          widthM={chord.chordM}
          thicknessM={thicknessM}
          openingType={openingType}
          hasHandle={hasHandle}
          hasLock={hasLock}
          onSelect={onSelect}
        />
      </group>
      {hardware.map((hw) => {
        const phi = phiMid + hw.offsetXmm / 1000 / radiusM;
        const anchor = surfaceAnchor(phi);
        return (
          <group
            key={hw.id}
            position={[anchor.x, baseY + heightM / 2, anchor.z]}
            rotation={[0, -tangentYawAt(phi), 0]}
          >
            <HardwareObject
              item={{ ...hw, offsetXmm: 0 }}
              isSelected={selectedHardwareId === hw.id}
              onSelect={() => onSelectHardware(hw.id)}
              onCommitDrag={onDragHardware ? (delta) => onDragHardware(hw.id, delta) : undefined}
              onResize={
                onResizeHardware
                  ? (widthMm, heightMm) => onResizeHardware(hw.id, widthMm, heightMm)
                  : undefined
              }
            />
          </group>
        );
      })}

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
