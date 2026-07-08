import { useLayoutEffect, useMemo, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Billboard, Text } from '@react-three/drei';
import type { Group } from 'three';
import { queueToast } from '@/shared/api/toastQueue';
import { ArcOutline } from './ArcOutline';
import { CurvedPanelMesh } from './CurvedPanelMesh';
import { PanelMesh } from './PanelMesh';
import { ProfileBar } from './ProfileBar';
import {
  arcEndLocal,
  arcFromBow,
  arcFromCornerResize,
  bowArcPlanPoints,
  bowFromArc,
  computeArcLayout,
  radiusFromChordSweep,
  resolveArc,
} from '../../model/arcGeometry';
import { ArcSweepHandle } from '../interaction/ArcSweepHandle';
import { StretchFaces } from '../interaction/StretchFaces';
import { setBodyPreview } from '../interaction/bodyPreview';
import type { StretchFaceDef } from '../interaction/StretchFaces';
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
import { stickyDimensionMm } from '@/shared/three-engine';
import type { QualityPreset } from '@/shared/three-engine';
import type { SceneRunState } from '../../model/project.types';

interface ArcRunGroupProps {
  run: SceneRunState;
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
  onResizeHardware?: (
    runId: string,
    panelId: string,
    hardwareId: string,
    widthMm: number,
    heightMm: number,
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
  onResizeHardware,
  onMoveRun,
  onRotateRun,
  onStretchRun,
  onStackRun,
  snapTargets,
  obstacles,
  supports,
}: ArcRunGroupProps) {
  const { t } = useTranslation();
  const heightM = run.heightMm / 1000;
  // CHORD-INVARIANT: run.lengthMm is the chord (the fixed span). The radius is re-derived from
  // chord+sweep at read time — the persisted integer radius would otherwise render a chord that
  // misses lengthMm by millimetres and drift it on every bow commit.
  const arc = resolveArc(
    radiusFromChordSweep(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg),
    run.geomArcSweepDeg ?? 1,
  );
  const radiusM = arc.radiusM;
  const profileColor = run.customColorHex ?? color?.hexColor ?? DEFAULT_HEX_COLOR;
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
        arc.arcLengthM,
        radiusM,
        run.geomArcSweepDeg ?? 1,
        panels.map((p) => p.widthMm / 1000),
      ),
    [arc.arcLengthM, radiusM, run.geomArcSweepDeg, panels],
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
    const excluded = new Set(findAttachedWallIds(run, sceneState.walls ?? []));
    // Co-moving multi-selection members travel with this run — their stale footprints must not
    // register as collisions mid-drag (same rule as SlabObject/WallObject).
    if (isMultiMember) {
      for (const id of multiSelection.runIds) excluded.add(id);
      for (const id of multiSelection.wallIds) excluded.add(id);
      for (const id of multiSelection.slabIds) excluded.add(id);
    }
    return excluded.size === 0 ? all : all.filter((o) => !excluded.has(o.ownerId));
  }, [obstacles, run, sceneState.walls, isMultiMember, multiSelection]);

  // Exclude co-moving multi-selection members too — their stale pre-move endpoints must not
  // act as snap targets while the group drags.
  const filteredTargets = useMemo<PlanSnapTargets>(() => {
    if (!snapTargets) return EMPTY_SNAP_TARGETS;
    const excluded = new Set<string>([run.id]);
    if (isMultiMember) {
      for (const id of multiSelection.runIds) excluded.add(id);
      for (const id of multiSelection.wallIds) excluded.add(id);
      for (const id of multiSelection.slabIds) excluded.add(id);
    }
    return filterSnapTargets(snapTargets, excluded);
  }, [snapTargets, run.id, isMultiMember, multiSelection]);

  const radR = (run.rotationDeg * Math.PI) / 180;
  const cosR = Math.cos(radR);
  const sinR = Math.sin(radR);
  const end = arcEndLocal(arc.radiusMm, run.geomArcSweepDeg ?? 1);
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

  // ARC run stretch ('s' tool): a keep-sweep chord resize is a UNIFORM in-plane scale about the
  // pinned end, so the imperative [s,1,s] body preview is exact; end faces drag the chord along
  // the chord direction, the top face drags the height.
  const bodyRef = useRef<Group>(null);
  const resetBody = () => {
    bodyRef.current?.scale.set(1, 1, 1);
    bodyRef.current?.position.set(0, 0, 0);
  };
  useLayoutEffect(() => resetBody(), [run.lengthMm, run.heightMm, run.geomArcRadiusMm]);

  const stickyDelta = (base: number, deltaMm: number) => stickyDimensionMm(base + deltaMm) - base;
  const chordMm = Math.hypot(end.xMm, end.yMm);
  const chordUx = chordMm > 0 ? end.xMm / chordMm : 1;
  const chordUz = chordMm > 0 ? end.yMm / chordMm : 0;
  const chordThetaL = Math.atan2(chordUz, chordUx);

  const previewArcChord = (deltaMm: number, fromStart: boolean) => {
    if (chordMm < 1) return;
    const scale = Math.max(0.05, (chordMm + deltaMm) / chordMm);
    setBodyPreview(
      bodyRef,
      [scale, 1, scale],
      fromStart ? [((1 - scale) * end.xMm) / 1000, 0, ((1 - scale) * end.yMm) / 1000] : [0, 0, 0],
    );
  };
  const previewArcHeight = (deltaMm: number) => {
    const scale = Math.max(0.05, (run.heightMm + deltaMm) / run.heightMm);
    setBodyPreview(bodyRef, [1, scale, 1], [0, 0, 0]);
  };

  const commitArcChord = (deltaMm: number, fromStart: boolean) => {
    const target = stickyDelta(chordMm, deltaMm);
    const newChord = Math.max(MIN_RUN_LENGTH_MM, Math.round(chordMm + target));
    const scaled = arcFromCornerResize(newChord, run.geomArcSweepDeg ?? 1);
    if (newChord === Math.round(chordMm)) {
      resetBody();
      return;
    }
    if (scaled.geomArcRadiusMm < 100) {
      // Toast LOCALLY (like WallObject/SlabObject) — a silent snap-back read as "stretch is broken"
      // on tight arcs. Routing the refused radius-only patch through onStretchRun would ship a
      // chord/radius pair that is out of sync across a component boundary (the split-brain class
      // the arc model prevents), relying on a remote guard to reject it.
      resetBody();
      queueToast({
        dedupeKey: 'glass-arc-radius-too-small',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Arc.RadiusTooSmall', {
          defaultValue:
            'Bu ölçüler {{r}} mm yarıçap üretiyor — minimum 100 mm. Kirişi büyütün veya oku küçültün.',
          r: scaled.geomArcRadiusMm,
        }),
      });
      return;
    }
    const chordRad = Math.atan2(endWorldY - run.originY, endWorldX - run.originX);
    const shift = fromStart ? -(newChord - chordMm) : 0;
    const nextOriginX = Math.round(run.originX + shift * Math.cos(chordRad));
    const nextOriginY = Math.round(run.originY + shift * Math.sin(chordRad));
    const resized = buildRunFootprint(
      {
        ...run,
        originX: nextOriginX,
        originY: nextOriginY,
        lengthMm: scaled.lengthMm,
        geomArcRadiusMm: scaled.geomArcRadiusMm,
      },
      0,
      0,
      run.rotationDeg,
    );
    if (penetratesAny(resized, gestureObstacles)) {
      resetBody();
      return;
    }
    onStretchRun?.(run.id, {
      lengthMm: scaled.lengthMm,
      originX: nextOriginX,
      originY: nextOriginY,
      geomArcRadiusMm: scaled.geomArcRadiusMm,
    });
  };
  const commitArcHeight = (deltaMm: number) => {
    const next = Math.max(300, Math.round(run.heightMm + stickyDelta(run.heightMm, deltaMm)));
    if (next === run.heightMm) {
      resetBody();
      return;
    }
    onStretchRun?.(run.id, { heightMm: next });
  };

  const stretchActive = activeTool === 'stretch' && Boolean(onStretchRun) && !run.locked;
  const chordLabel = (d: number) =>
    `${Math.round(Math.max(MIN_RUN_LENGTH_MM, chordMm + stickyDelta(chordMm, d)))} mm`;
  const runHeightLabel = (d: number) =>
    `${Math.round(Math.max(300, run.heightMm + stickyDelta(run.heightMm, d)))} mm`;
  const stretchFaces: StretchFaceDef[] = stretchActive
    ? [
        {
          // The grab planes face the local END TANGENTS (start = local +x) — a chord-normal
          // plane sits up to sweep/2 off the visible band end and is hard to hover on deep arcs.
          id: 'start',
          centerM: [0, heightM / 2, 0],
          rotation: [0, -Math.PI / 2, 0],
          widthM: RUN_PLAN_THICKNESS_MM / 1000,
          heightM,
          hitWidthM: 0.16,
          axis: [-chordUx, 0, -chordUz],
          label: chordLabel,
          onPreview: (d) => previewArcChord(stickyDelta(chordMm, d), true),
          onCommit: (d) => commitArcChord(d, true),
        },
        {
          id: 'end',
          centerM: [end.xMm / 1000, heightM / 2, end.yMm / 1000],
          rotation: [
            0,
            Math.atan2(Math.cos(arc.sweepRad), arc.direction * Math.sin(arc.sweepRad)),
            0,
          ],
          widthM: RUN_PLAN_THICKNESS_MM / 1000,
          heightM,
          hitWidthM: 0.16,
          axis: [chordUx, 0, chordUz],
          label: chordLabel,
          onPreview: (d) => previewArcChord(stickyDelta(chordMm, d), false),
          onCommit: (d) => commitArcChord(d, false),
        },
        {
          id: 'top',
          centerM: [end.xMm / 2000, heightM + 0.002, end.yMm / 2000],
          rotation: [-Math.PI / 2, -chordThetaL, 0],
          widthM: chordMm / 1000,
          heightM: RUN_PLAN_THICKNESS_MM / 1000,
          hitHeightM: 0.16,
          axis: [0, 1, 0],
          label: runHeightLabel,
          onPreview: (d) => previewArcHeight(stickyDelta(run.heightMm, d)),
          onCommit: commitArcHeight,
        },
      ]
    : [];

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
        <group ref={bodyRef}>
          {isRunSelected && (
            <ArcOutline
              radiusMm={arc.radiusMm}
              sweepDeg={run.geomArcSweepDeg ?? 1}
              baseYM={0}
              topYM={heightM}
              halfWidthM={RUN_PLAN_THICKNESS_MM / 2000}
              color="#1d4ed8"
            />
          )}
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
                <group
                  key={`arcpost-${i}`}
                  position={[b.x, 0, b.z]}
                  rotation={[0, -b.tangentRad, 0]}
                >
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
                panel.shapeKind === 'polygon'
                  ? parsePanelPolygonPoints(panel.shapePointsJson)
                  : null,
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
                  onResizeHardware={
                    onResizeHardware
                      ? (hardwareId, widthMm, heightMm) =>
                          onResizeHardware(run.id, panel.id, hardwareId, widthMm, heightMm)
                      : undefined
                  }
                  quality={quality}
                  showAnnotations={showAnnotations}
                  panelIndex={panel.panelIndex}
                  isSelected={selectedPanelId === panel.id}
                  onSelect={() => onSelectPanel(run.id, panel.id)}
                  shapeSpec={shapeSpec}
                  frameColor={profileColor}
                  showFrameBand={isSingleShapedPanel}
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
                  onResizeHardware={
                    onResizeHardware
                      ? (hardwareId, widthMm, heightMm) =>
                          onResizeHardware(run.id, panel.id, hardwareId, widthMm, heightMm)
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
                {`${run.label} · R${Math.round(arc.radiusMm)} · ${run.lengthMm} × ${run.heightMm} mm · yay ${Math.round(arc.arcLengthMm)} mm`}
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
        {stretchActive && <StretchFaces faces={stretchFaces} />}
      </group>
      {vertexEditActive && (
        <FootprintCornerHandles
          mode="ends"
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
          previewOutline={(next) => {
            // Preview the EXACT arc the commit would produce, not a dashed phantom rectangle.
            const rad = (next.rotationDeg * Math.PI) / 180;
            const ex = next.originX + next.lengthMm * Math.cos(rad);
            const ey = next.originY + next.lengthMm * Math.sin(rad);
            const scaled = arcFromCornerResize(
              Math.max(MIN_RUN_LENGTH_MM, next.lengthMm),
              run.geomArcSweepDeg ?? 1,
            );
            const sag = bowFromArc(next.lengthMm, scaled.geomArcRadiusMm, run.geomArcSweepDeg ?? 1);
            const topY = ((run.geomZ ?? 0) + run.heightMm) / 1000;
            return bowArcPlanPoints(next.originX, next.originY, ex, ey, sag).map(
              (p): [number, number, number] => [p.x / 1000, topY, p.y / 1000],
            );
          }}
          onCommit={(next) => {
            // The footprint box length is the CHORD (the span between the fixed ends). Dragging an
            // end changes that span while keeping the sweep angle (curl shape); lengthMm = the new
            // chord and the radius re-derives for it (arcFromCornerResize); the origin shifts along
            // the chord direction.
            const chordDeg = Math.atan2(endWorldY - run.originY, endWorldX - run.originX);
            const dirX = Math.cos(chordDeg);
            const dirY = Math.sin(chordDeg);
            const along = (next.originX - run.originX) * dirX + (next.originY - run.originY) * dirY;
            const newChord = Math.max(MIN_RUN_LENGTH_MM, Math.round(next.lengthMm));
            const scaled = arcFromCornerResize(newChord, run.geomArcSweepDeg ?? 1);
            const originX = Math.round(run.originX + along * dirX);
            const originY = Math.round(run.originY + along * dirY);
            const resized = buildRunFootprint(
              {
                ...run,
                originX,
                originY,
                lengthMm: scaled.lengthMm,
                geomArcRadiusMm: scaled.geomArcRadiusMm,
              },
              0,
              0,
              run.rotationDeg,
            );
            if (penetratesAny(resized, gestureObstacles)) return;
            onStretchRun?.(run.id, {
              lengthMm: scaled.lengthMm,
              originX,
              originY,
              geomArcRadiusMm: scaled.geomArcRadiusMm,
            });
          }}
        />
      )}
      {vertexEditActive && onStretchRun && (
        <ArcSweepHandle
          startX={run.originX}
          startY={run.originY}
          endX={endWorldX}
          endY={endWorldY}
          currentSagittaMm={bowFromArc(run.lengthMm, arc.radiusMm, run.geomArcSweepDeg ?? 0)}
          topYM={((run.geomZ ?? 0) + run.heightMm) / 1000}
          onCommit={(sagittaMm) => {
            // CHORD-INVARIANT: the two ends stay FIXED. The chord is the STORED lengthMm and the
            // chord direction is the exact unroll (rotation + sweep/2) — never re-measured from
            // the rounded radius (that drifted lengthMm on every shallow commit).
            const chordDeg = run.rotationDeg + (run.geomArcSweepDeg ?? 0) / 2;
            const bow = arcFromBow(run.lengthMm, chordDeg, sagittaMm);
            onStretchRun(run.id, {
              lengthMm: bow.lengthMm,
              rotationDeg: bow.rotationDeg,
              geomArcRadiusMm: bow.geomArcRadiusMm,
              geomArcSweepDeg: bow.geomArcSweepDeg,
            });
          }}
        />
      )}
    </>
  );
}
