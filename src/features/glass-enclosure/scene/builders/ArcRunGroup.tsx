import { useMemo } from 'react';
import { Billboard, Text } from '@react-three/drei';
import { CurvedPanelMesh } from './CurvedPanelMesh';
import { PanelMesh } from './PanelMesh';
import { ProfileBar } from './ProfileBar';
import { computeArcLayout, effectiveArcRadiusMm } from '../../model/arcGeometry';
import type { HardwareDragDelta } from './HardwareObject';
import type {
  ColorOptionDto,
  GlassTypeDto,
  ProfileSystemDto,
} from '../../model/glassEnclosure.types';
import type { QualityPreset } from '@/shared/three-engine';
import type { SceneRunState } from '../../model/project.types';

interface ArcRunGroupProps {
  run: SceneRunState;
  radiusMm: number;
  system?: ProfileSystemDto;
  color?: ColorOptionDto;
  glassTypes: Map<string, GlassTypeDto>;
  quality: QualityPreset;
  showAnnotations: boolean;
  selectedPanelId: string | null;
  selectedRunId: string | null;
  selectedHardwareId: string | null;
  onSelectRun: (runId: string) => void;
  onSelectPanel: (runId: string, panelId: string) => void;
  onSelectHardware: (runId: string, panelId: string, hardwareId: string) => void;
  onDragHardware?: (
    runId: string,
    panelId: string,
    hardwareId: string,
    delta: HardwareDragDelta,
  ) => void;
}

const PROFILE_CROSS_SECTION = { width: 50, height: 60 };
const MULLION_CROSS_SECTION = { width: 30, height: 40 };
const DEFAULT_HEX_COLOR = '#cfd5d9';

export function ArcRunGroup({
  run,
  radiusMm,
  system,
  color,
  glassTypes,
  quality,
  showAnnotations,
  selectedPanelId,
  selectedRunId,
  selectedHardwareId,
  onSelectRun,
  onSelectPanel,
  onSelectHardware,
  onDragHardware,
}: ArcRunGroupProps) {
  const lengthM = run.lengthMm / 1000;
  const heightM = run.heightMm / 1000;
  const effRadiusMm = effectiveArcRadiusMm(run.lengthMm, radiusMm);
  const radiusM = effRadiusMm / 1000;
  const profileColor = color?.hexColor ?? DEFAULT_HEX_COLOR;
  const finish = color?.finishType ?? 'PowderCoated';
  const profileHalf = PROFILE_CROSS_SECTION.height / 1000 / 2;

  const panels = run.panels;
  const layout = useMemo(
    () =>
      computeArcLayout(
        lengthM,
        radiusM,
        run.geomArcSweepDeg ?? 1,
        panels.map((p) => p.widthMm / 1000),
      ),
    [lengthM, radiusM, run.geomArcSweepDeg, panels],
  );

  const isRunSelected = selectedRunId === run.id;

  return (
    <group
      position={[run.originX / 1000, (run.geomZ ?? 0) / 1000, run.originY / 1000]}
      rotation={[0, (-run.rotationDeg * Math.PI) / 180, 0]}
      onClick={(e) => {
        e.stopPropagation();
        onSelectRun(run.id);
      }}
    >
      {layout.barSegments.map((seg, i) => (
        <group
          key={`arcbar-${i}`}
          position={[seg.midX, 0, seg.midZ]}
          rotation={[0, -seg.yawRad, 0]}
        >
          <ProfileBar
            lengthM={seg.chordM * 1.02}
            crossSectionMm={PROFILE_CROSS_SECTION}
            hexColor={profileColor}
            finish={finish}
            quality={quality}
            position={[0, heightM, 0]}
          />
          <ProfileBar
            lengthM={seg.chordM * 1.02}
            crossSectionMm={PROFILE_CROSS_SECTION}
            hexColor={profileColor}
            finish={finish}
            quality={quality}
            position={[0, 0, 0]}
          />
        </group>
      ))}

      {layout.boundaries.map((b, i) => {
        const isOuter = i === 0 || i === layout.boundaries.length - 1;
        return (
          <group key={`arcpost-${i}`} position={[b.x, 0, b.z]} rotation={[0, -b.tangentRad, 0]}>
            <ProfileBar
              lengthM={heightM}
              crossSectionMm={isOuter ? PROFILE_CROSS_SECTION : MULLION_CROSS_SECTION}
              hexColor={profileColor}
              finish={finish}
              quality={quality}
              position={[0, heightM / 2, 0]}
              rotation={[0, 0, Math.PI / 2]}
            />
          </group>
        );
      })}

      {layout.panelSpans.map((span, i) => {
        const panel = panels[i];
        const chord = layout.panelChords[i];
        if (!panel || !chord) return null;
        const glass = glassTypes.get(panel.glassTypeId);
        if (run.arcGlassBent) {
          return (
            <CurvedPanelMesh
              key={panel.id}
              panelId={panel.id}
              radiusM={radiusM}
              direction={layout.direction}
              phiStart={span.phiStart}
              phiEnd={span.phiEnd}
              chord={chord}
              baseY={profileHalf}
              heightM={Math.max(0.05, heightM - 2 * profileHalf)}
              thicknessMm={glass?.thicknessMm ?? 8}
              glassStructure={glass?.structure}
              openingType={panel.openingType}
              hasHandle={panel.hasHandle}
              hasLock={panel.hasLock}
              hardware={panel.hardware}
              selectedHardwareId={selectedHardwareId}
              onSelectHardware={(hardwareId) => onSelectHardware(run.id, panel.id, hardwareId)}
              onDragHardware={
                onDragHardware
                  ? (hardwareId, delta) => onDragHardware(run.id, panel.id, hardwareId, delta)
                  : undefined
              }
              quality={quality}
              showAnnotations={showAnnotations}
              panelIndex={panel.panelIndex}
              isSelected={selectedPanelId === panel.id}
              onSelect={() => onSelectPanel(run.id, panel.id)}
            />
          );
        }
        return (
          <group
            key={panel.id}
            position={[chord.midX, 0, chord.midZ]}
            rotation={[0, -chord.yawRad, 0]}
          >
            <PanelMesh
              panelId={panel.id}
              centerX={0}
              baseY={profileHalf}
              widthM={Math.max(0.05, chord.chordM - 0.012)}
              heightM={Math.max(0.05, heightM - 2 * profileHalf)}
              thicknessMm={glass?.thicknessMm ?? 8}
              glassStructure={glass?.structure}
              openingType={panel.openingType}
              hasHandle={panel.hasHandle}
              hasLock={panel.hasLock}
              hasBrushSeal={panel.hasBrushSeal}
              hardware={panel.hardware}
              selectedHardwareId={selectedHardwareId}
              onSelectHardware={(hardwareId) => onSelectHardware(run.id, panel.id, hardwareId)}
              onDragHardware={
                onDragHardware
                  ? (hardwareId, delta) => onDragHardware(run.id, panel.id, hardwareId, delta)
                  : undefined
              }
              quality={quality}
              showAnnotations={showAnnotations}
              panelIndex={panel.panelIndex}
              isSelected={selectedPanelId === panel.id}
              onSelect={() => onSelectPanel(run.id, panel.id)}
            />
          </group>
        );
      })}

      {showAnnotations && (
        <Billboard position={[layout.apex.x, heightM + 0.5, layout.apex.z]} follow>
          <Text
            fontSize={0.12}
            color={isRunSelected ? '#1d4ed8' : '#0f172a'}
            anchorX="center"
            anchorY="bottom"
            outlineWidth={0.004}
            outlineColor="#ffffff"
          >
            {`${run.label} · R${Math.round(radiusMm)} · ${run.lengthMm} × ${run.heightMm} mm`}
          </Text>
          {system && (
            <Text
              position={[0, -0.16, 0]}
              fontSize={0.07}
              color="#64748b"
              anchorX="center"
              anchorY="top"
              outlineWidth={0.003}
              outlineColor="#ffffff"
            >
              {system.name}
            </Text>
          )}
        </Billboard>
      )}
    </group>
  );
}
