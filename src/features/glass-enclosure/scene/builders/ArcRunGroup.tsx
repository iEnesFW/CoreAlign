import { useMemo, useRef } from 'react';
import { Billboard, Text } from '@react-three/drei';
import type { Group } from 'three';
import { CurvedPanelMesh } from './CurvedPanelMesh';
import { PanelMesh } from './PanelMesh';
import { ProfileBar } from './ProfileBar';
import { arcEndLocal, computeArcLayout, effectiveArcRadiusMm } from '../../model/arcGeometry';
import { parsePanelPolygonPoints } from '../../model/panelPolygon';
import { panelIsShaped } from '../../model/panelOutline';
import { useObjectGestures } from '../interaction/useObjectGestures';
import { registerSceneRef } from '../interaction/sceneRefs';
import {
  RUN_PLAN_THICKNESS_MM,
  buildRunFootprint,
  penetratesAny,
  restElevationAtPointMm,
  restElevationMm,
} from '../interaction/planCollision';
import { FootprintCornerHandles } from '../interaction/FootprintCornerHandles';
import type { RunStretchPatch } from './RunGroup';
import { captureMultiSnapshots, multiSelectionHas } from '../interaction/multiMove';
import { previewSnapshotsMove } from '../interaction/attachedRunPreview';
import { EMPTY_SNAP_TARGETS, filterSnapTargets } from '../interaction/planSnap';
import { useDesignerStore } from '../../model/designerStore';
import { findAttachedWallIds } from '../../model/wallAttachment';
import type { AttachedRunSnapshot } from '../interaction/attachedRunPreview';
import type { HardwareDragDelta } from './HardwareObject';
import type { PlanGestureAdapter, PlanRotationCommit } from '../interaction/useObjectGestures';
import type { PlanFootprint } from '../interaction/planCollision';
import type { PlanMoveDelta, PlanSnapTargets } from '../interaction/planSnap';
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
  onMoveRun?: (runId: string, delta: PlanMoveDelta) => void;
  onRotateRun?: (runId: string, commit: PlanRotationCommit) => void;
  onStretchRun?: (runId: string, patch: RunStretchPatch) => void;
  onStackRun?: (runId: string, delta: PlanMoveDelta, geomZMm: number) => void;
  snapTargets?: PlanSnapTargets;
  obstacles?: PlanFootprint[];
  supports?: PlanFootprint[];
}

const PROFILE_CROSS_SECTION = { width: 50, height: 60 };
const EMPTY_OBSTACLES: PlanFootprint[] = [];
const MULLION_CROSS_SECTION = { width: 30, height: 40 };
const DEFAULT_HEX_COLOR = '#cfd5d9';
const MIN_RUN_LENGTH_MM = 100;

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
  onMoveRun,
  onRotateRun,
  onStretchRun,
  onStackRun,
  snapTargets,
  obstacles,
  supports,
}: ArcRunGroupProps) {
  const lengthM = run.lengthMm / 1000;
  const heightM = run.heightMm / 1000;
  const effRadiusMm = effectiveArcRadiusMm(run.lengthMm, radiusMm);
  const radiusM = effRadiusMm / 1000;
  const profileColor = color?.hexColor ?? DEFAULT_HEX_COLOR;
  const finish = color?.finishType ?? 'PowderCoated';
  const profileHalf = PROFILE_CROSS_SECTION.height / 1000 / 2;

  const panels = run.panels;
  // A single shaped pane on a curved run draws its own shape-matched (curved) frame band, so the
  // rectangular rails/posts are suppressed — otherwise they box in the shaped glass (mirrors
  // RunGroup.isSingleShapedPanel for straight runs).
  const firstPanel = panels[0];
  const isSingleShapedPanel =
    panels.length === 1 && Boolean(firstPanel && panelIsShaped(firstPanel));
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

  // Per-edge frame visibility (frameless / silicone-joined designs); missing = all on.
  // Mirrors RunGroup: left = arc start post, right = arc end post, inner boundaries = mullions.
  const fe = run.frameEdges;
  const showTopRail = fe ? fe.top : true;
  const showBottomRail = fe ? fe.bottom : true;
  const showLeftRail = fe ? fe.left : true;
  const showRightRail = fe ? fe.right : true;
  const showMullions = run.hasMullions !== false;

  const activeTool = useDesignerStore((s) => s.activeTool);
  const transformActive = useDesignerStore((s) => s.transformHandlesActive);
  // WHY: a panel selection keeps its parent run's id in selection.runId; gate run handles on the
  // run actually being the active selection so they don't render on top of a selected panel.
  const selectionKind = useDesignerStore((s) => s.selection.kind);
  const sceneState = useDesignerStore((s) => s.scene);
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const isMultiMember = multiSelectionHas(multiSelection, 'run', run.id);
  const vertexEditActive =
    transformActive &&
    selectionKind === 'run' &&
    isRunSelected &&
    Boolean(onStretchRun) &&
    !run.locked;
  const multiSiblingsRef = useRef<AttachedRunSnapshot[]>([]);
  const groupRef = useRef<Group>(null);
  const setGroupRef = (group: Group | null) => {
    groupRef.current = group;
    registerSceneRef(run.id, group);
  };

  const gestureObstacles = useMemo(() => {
    const all = obstacles ?? EMPTY_OBSTACLES;
    const attached = new Set(findAttachedWallIds(run, sceneState.walls ?? []));
    return attached.size === 0 ? all : all.filter((o) => !attached.has(o.ownerId));
  }, [obstacles, run, sceneState.walls]);

  const filteredTargets = useMemo<PlanSnapTargets>(
    () => (snapTargets ? filterSnapTargets(snapTargets, run.id) : EMPTY_SNAP_TARGETS),
    [snapTargets, run.id],
  );

  const radR = (run.rotationDeg * Math.PI) / 180;
  const cosR = Math.cos(radR);
  const sinR = Math.sin(radR);
  const end = arcEndLocal(run.lengthMm, run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1);
  const endWorldX = run.originX + end.xMm * cosR - end.yMm * sinR;
  const endWorldY = run.originY + end.xMm * sinR + end.yMm * cosR;

  const stackSupports = useMemo(() => {
    const all = supports ?? EMPTY_OBSTACLES;
    const attached = new Set(findAttachedWallIds(run, sceneState.walls ?? []));
    return all.filter((o) => o.ownerId !== run.id && !attached.has(o.ownerId));
  }, [supports, run, sceneState.walls]);
  // Explicit stack rests on any overlap; precise auto-stack on what's under the chord midpoint; a
  // plain drag keeps the run's current elevation (fallback = its own base, never forced down).
  const baseElevMm = run.geomZ ?? 0;
  const restElevAt = (dx: number, dy: number) =>
    restElevationMm(buildRunFootprint(run, dx, dy, run.rotationDeg), stackSupports, baseElevMm);
  const centerXMm = (run.originX + endWorldX) / 2;
  const centerYMm = (run.originY + endWorldY) / 2;
  // Fallback 0 (ground): a support under the centre lifts it; nothing under means gravity → floor.
  const centerRestAt = (dx: number, dy: number) =>
    restElevationAtPointMm(centerXMm + dx, centerYMm + dy, stackSupports, 0);
  const restingAtStart = Math.abs(centerRestAt(0, 0) - baseElevMm) < 5;
  const canStack = Boolean(onStackRun) && !isMultiMember;

  const adapter: PlanGestureAdapter = {
    originXMm: run.originX,
    originYMm: run.originY,
    rotationDeg: run.rotationDeg,
    baseYM: (run.geomZ ?? 0) / 1000,
    centerXMm,
    centerYMm,
    moveProbes: [
      { x: run.originX, y: run.originY },
      { x: endWorldX, y: endWorldY },
    ],
    footprintAt: (dx, dy, rotationDeg) => buildRunFootprint(run, dx, dy, rotationDeg),
    altLiftYMAt: canStack ? (dx, dy) => restElevAt(dx, dy) / 1000 : undefined,
    centerLiftYMAt: canStack ? (dx, dy) => centerRestAt(dx, dy) / 1000 : undefined,
    restingAtStart,
  };

  const gestures = useObjectGestures({
    adapter,
    groupRef,
    enabled: Boolean(onMoveRun && onRotateRun) && !run.locked,
    selectedForDrag: isRunSelected && !run.locked,
    snapTargets: filteredTargets,
    obstacles: gestureObstacles,
    onPick: () => onSelectRun(run.id),
    onGestureStart: () => {
      multiSiblingsRef.current = isMultiMember
        ? captureMultiSnapshots(sceneState, multiSelection, { kind: 'run', id: run.id })
        : [];
    },
    onMovePreview: (delta) =>
      previewSnapshotsMove(multiSiblingsRef.current, delta.dxMm, delta.dyMm),
    onMoveCommit: (delta, meta) => {
      // A stack (explicit or precise centre-over) rests at stackElevMm; a plain lateral drag
      // (null) keeps the arc run's current elevation.
      if (canStack && onStackRun && meta.stackElevMm !== null) {
        onStackRun(run.id, delta, meta.stackElevMm);
        return;
      }
      onMoveRun?.(run.id, delta);
    },
    onRotateCommit: (commit) => onRotateRun?.(run.id, commit),
  });

  return (
    <>
      <group
        ref={setGroupRef}
        position={[run.originX / 1000, (run.geomZ ?? 0) / 1000, run.originY / 1000]}
        rotation={[0, (-run.rotationDeg * Math.PI) / 180, 0]}
        {...gestures.handlers}
        onClick={(e) => {
          e.stopPropagation();
          if (gestures.consumeClick()) return;
          onSelectRun(run.id);
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          const canDrag = activeTool === 'move' || (activeTool === 'select' && isRunSelected);
          document.body.style.cursor = canDrag && !run.locked ? 'grab' : 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
      >
        {!isSingleShapedPanel &&
          layout.barSegments.map((seg, i) => (
            <group
              key={`arcbar-${i}`}
              position={[seg.midX, 0, seg.midZ]}
              rotation={[0, -seg.yawRad, 0]}
            >
              {showTopRail && (
                <ProfileBar
                  lengthM={seg.chordM * 1.02}
                  crossSectionMm={PROFILE_CROSS_SECTION}
                  hexColor={profileColor}
                  finish={finish}
                  quality={quality}
                  position={[0, heightM, 0]}
                />
              )}
              {showBottomRail && (
                <ProfileBar
                  lengthM={seg.chordM * 1.02}
                  crossSectionMm={PROFILE_CROSS_SECTION}
                  hexColor={profileColor}
                  finish={finish}
                  quality={quality}
                  position={[0, 0, 0]}
                />
              )}
            </group>
          ))}

        {!isSingleShapedPanel &&
          layout.boundaries.map((b, i) => {
            const isFirst = i === 0;
            const isLast = i === layout.boundaries.length - 1;
            const isOuter = isFirst || isLast;
            const visible = isOuter ? (isFirst ? showLeftRail : showRightRail) : showMullions;
            if (!visible) return null;
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
          const facetWidthMm = Math.round(Math.max(0.05, chord.chordM - 0.012) * 1000);
          const facetHeightMm =
            panel.heightMm ?? Math.round(Math.max(0.05, heightM - 2 * profileHalf) * 1000);
          const shapeSpec = {
            widthMm: facetWidthMm,
            heightMm: facetHeightMm,
            topShape: panel.topShape,
            topRightHeightMm: panel.topRightHeightMm,
            archRiseMm: panel.archRiseMm,
            cornerRadiiMm: panel.cornerRadiiMm,
            cornerNotchMm: panel.cornerNotchMm,
            shapeKind: panel.shapeKind,
            points:
              panel.shapeKind === 'polygon' ? parsePanelPolygonPoints(panel.shapePointsJson) : null,
          };
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
                shapeSpec={shapeSpec}
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
                heightM={facetHeightMm / 1000}
                shapeSpec={shapeSpec}
                frameColor={profileColor}
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
      {vertexEditActive && (
        <FootprintCornerHandles
          box={{
            originX: run.originX,
            originY: run.originY,
            // An arc is curved; approximate its plan extent with the START→END chord as the box.
            lengthMm: Math.hypot(endWorldX - run.originX, endWorldY - run.originY),
            crossMm: RUN_PLAN_THICKNESS_MM,
            rotationDeg:
              (Math.atan2(endWorldY - run.originY, endWorldX - run.originX) * 180) / Math.PI,
          }}
          topYM={((run.geomZ ?? 0) + run.heightMm) / 1000}
          onCommit={(next) => {
            // Scale the arc's developed length by the chord ratio (radius/sweep fixed), and shift
            // the origin along the chord — projected so the curved run only changes length.
            const oldChord = Math.hypot(endWorldX - run.originX, endWorldY - run.originY) || 1;
            const chordDeg = Math.atan2(endWorldY - run.originY, endWorldX - run.originX);
            const dirX = Math.cos(chordDeg);
            const dirY = Math.sin(chordDeg);
            const along = (next.originX - run.originX) * dirX + (next.originY - run.originY) * dirY;
            const lengthMm = Math.max(
              MIN_RUN_LENGTH_MM,
              Math.round((run.lengthMm * next.lengthMm) / oldChord),
            );
            const originX = Math.round(run.originX + along * dirX);
            const originY = Math.round(run.originY + along * dirY);
            const resized = buildRunFootprint(
              { ...run, originX, originY, lengthMm },
              0,
              0,
              run.rotationDeg,
            );
            if (penetratesAny(resized, gestureObstacles)) return;
            onStretchRun?.(run.id, { lengthMm, originX, originY });
          }}
        />
      )}
    </>
  );
}
