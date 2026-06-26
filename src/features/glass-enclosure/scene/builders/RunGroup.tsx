import { useLayoutEffect, useMemo, useRef } from 'react';
import { Billboard, Text } from '@react-three/drei';
import type { Group } from 'three';
import { PanelMesh } from './PanelMesh';
import { ProfileBar } from './ProfileBar';
import { useDrag3D } from '../interaction/useDrag3D';
import { useObjectGestures } from '../interaction/useObjectGestures';
import { StretchFaces } from '../interaction/StretchFaces';
import { FootprintCornerHandles } from '../interaction/FootprintCornerHandles';
import { setBodyPreview } from '../interaction/bodyPreview';
import { registerSceneRef } from '../interaction/sceneRefs';
import { captureMultiSnapshots, multiSelectionHas } from '../interaction/multiMove';
import { previewSnapshotsMove } from '../interaction/attachedRunPreview';
import {
  EMPTY_SNAP_TARGETS,
  filterSnapTargets,
  lineProbePoints,
  stickyDimensionMm,
} from '../interaction/planSnap';
import {
  RUN_PLAN_THICKNESS_MM,
  buildRunFootprint,
  clampPlanStretch,
  firstPenetratingOwner,
  restElevationMm,
} from '../interaction/planCollision';
import { useDesignerStore } from '../../model/designerStore';
import { parsePanelPolygonPoints } from '../../model/panelPolygon';
import { panelIsShaped } from '../../model/panelOutline';
import type { HardwareDragDelta } from './HardwareObject';
import type {
  ColorOptionDto,
  GlassTypeDto,
  ProfileSystemDto,
} from '../../model/glassEnclosure.types';
import type { QualityPreset } from '@/shared/three-engine';
import type { SceneRunState } from '../../model/project.types';
import type { PlanGestureAdapter, PlanRotationCommit } from '../interaction/useObjectGestures';
import type { AttachedRunSnapshot } from '../interaction/attachedRunPreview';
import type { StretchFaceDef } from '../interaction/StretchFaces';
import type { PlanFootprint } from '../interaction/planCollision';
import type { PlanMoveDelta, PlanPoint, PlanSnapTargets } from '../interaction/planSnap';

export type RunStretchPatch = Partial<
  Pick<SceneRunState, 'lengthMm' | 'heightMm' | 'originX' | 'originY'>
>;

interface RunGroupProps {
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
  onResizePanel?: (runId: string, panelId: string, deltaMm: number) => void;
  onMoveRun?: (runId: string, delta: PlanMoveDelta) => void;
  onRotateRun?: (runId: string, commit: PlanRotationCommit) => void;
  onStretchRun?: (runId: string, patch: RunStretchPatch) => void;
  onPushStretchRun?: (
    runId: string,
    face: 'start' | 'end',
    targetMm: number,
    clampedMm: number,
    blockerId: string,
  ) => boolean;
  onStackRun?: (runId: string, delta: PlanMoveDelta, geomZMm: number) => void;
  snapTargets?: PlanSnapTargets;
  obstacles?: PlanFootprint[];
  supports?: PlanFootprint[];
}

const DEFAULT_PROFILE_CROSS_SECTION = { width: 50, height: 60 };
const DEFAULT_HEX_COLOR = '#cfd5d9';
const DEG2RAD = Math.PI / 180;
const HALF_PI = Math.PI / 2;
const MULLION_AXIS: [number, number, number] = [1, 0, 0];
const MIN_RUN_LENGTH_MM = 100;
const MIN_RUN_HEIGHT_MM = 100;
const FACE_LIFT_M = 0.002;
const FACE_HIT_SIZE_M = 0.16;
const EMPTY_OBSTACLES: PlanFootprint[] = [];

export function RunGroup({
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
  onResizePanel,
  onMoveRun,
  onRotateRun,
  onStretchRun,
  onPushStretchRun,
  onStackRun,
  snapTargets,
  obstacles,
  supports,
}: RunGroupProps) {
  const activeTool = useDesignerStore((s) => s.activeTool);
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const sceneState = useDesignerStore((s) => s.scene);
  const multiSiblingsRef = useRef<AttachedRunSnapshot[]>([]);
  const lengthM = run.lengthMm / 1000;
  const heightM = run.heightMm / 1000;
  const profileColor = color?.hexColor ?? DEFAULT_HEX_COLOR;
  const finish = color?.finishType ?? 'PowderCoated';

  const panels = run.panels;
  const panelLayout = useMemo(() => {
    type PanelLayout = { panel: (typeof panels)[number]; centerX: number; widthM: number };
    const start = -lengthM / 2;
    const rawTotalM = panels.reduce((sum, p) => sum + p.widthMm / 1000, 0);
    const scale = rawTotalM > 0 ? lengthM / rawTotalM : 1;
    const layouts: PanelLayout[] = [];
    let nextStart = start;
    for (const panel of panels) {
      const widthM = (panel.widthMm / 1000) * scale;
      const centerX = nextStart + widthM / 2;
      layouts.push({ panel, centerX, widthM });
      nextStart += widthM;
    }
    return layouts;
  }, [panels, lengthM]);

  const isRunSelected = selectedRunId === run.id;
  // A single shaped pane (e.g. an autofilled oval/polygon hole) draws its own shape-matched
  // frame band (PanelMesh), so the rectangular run perimeter is suppressed — otherwise a
  // square frame boxes in the shaped glass ("panel kare gibi").
  const firstPanel = panels[0];
  // Matches PanelMesh's frameShaped (a subset of panelIsShaped): arched needs a positive
  // rise, else suppressing the rect frame would leave the pane with no frame at all.
  // Single shaped pane drops the rect perimeter for its matching shaped band — one source of
  // truth (panelIsShaped) so suppression tracks the glass + frame gate (shape / radii / notch).
  const isSingleShapedPanel =
    panels.length === 1 && Boolean(firstPanel && panelIsShaped(firstPanel));
  // Per-edge frame visibility (frameless / silicone-joined designs); missing = all on.
  const fe = run.frameEdges;
  const showTopRail = fe ? fe.top : true;
  const showBottomRail = fe ? fe.bottom : true;
  const showLeftRail = fe ? fe.left : true;
  const showRightRail = fe ? fe.right : true;
  const showMullions = run.hasMullions !== false;
  const halfWidth = lengthM / 2;
  const profileHalf = DEFAULT_PROFILE_CROSS_SECTION.height / 1000 / 2;

  const groupRef = useRef<Group>(null);
  const bodyRef = useRef<Group>(null);
  const planObstacles = obstacles ?? EMPTY_OBSTACLES;
  // WHY: every wall stays a collision obstacle for a dragged run — a run does NOT co-move the
  // walls it sits beside (unlike a wall, which carries its attached runs), so excluding them
  // let a run be pushed straight THROUGH a wall. The no-deepen gate still lets a mounted run
  // slide ALONG a wall (depth constant) and out of an existing overlap; it just can't go deeper.
  const gestureObstacles = planObstacles;

  const setGroupRef = (group: Group | null) => {
    groupRef.current = group;
    registerSceneRef(run.id, group);
  };

  const filteredTargets = useMemo<PlanSnapTargets>(
    () => (snapTargets ? filterSnapTargets(snapTargets, run.id) : EMPTY_SNAP_TARGETS),
    [snapTargets, run.id],
  );

  const baseY = (run.geomZ ?? 0) / 1000;
  const rad = run.rotationDeg * DEG2RAD;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);

  const moveProbes: PlanPoint[] = lineProbePoints(
    run.originX,
    run.originY,
    run.lengthMm,
    run.rotationDeg,
    RUN_PLAN_THICKNESS_MM / 2,
  );

  // Stacking is an explicit Alt-drag (default drag stays lateral so runs still butt flush /
  // sit side by side). Every support counts — including a wall the run sits beside — so the
  // run can rest ON it (Alt = "stack on whatever I'm over"); excluding nearby walls was what
  // broke resting a run on a wall. Rest falls back to the GROUND (0), not the run's current
  // geomZ, so dragging a stacked run off its support drops it back to the floor, no ratchet.
  const stackSupports = useMemo(
    () => (supports ?? EMPTY_OBSTACLES).filter((o) => o.ownerId !== run.id),
    [supports, run.id],
  );
  const restElevAt = (dxMm: number, dyMm: number) =>
    restElevationMm(buildRunFootprint(run, dxMm, dyMm, run.rotationDeg), stackSupports, 0);

  const isMultiMember = multiSelectionHas(multiSelection, 'run', run.id);
  const canStack = Boolean(onStackRun) && !isMultiMember;

  const adapter: PlanGestureAdapter = {
    originXMm: run.originX,
    originYMm: run.originY,
    rotationDeg: run.rotationDeg,
    baseYM: baseY,
    centerXMm: run.originX + (run.lengthMm / 2) * dirX,
    centerYMm: run.originY + (run.lengthMm / 2) * dirY,
    moveProbes,
    footprintAt: (dxMm, dyMm, rotationDeg) => buildRunFootprint(run, dxMm, dyMm, rotationDeg),
    altLiftYMAt: canStack ? (dxMm, dyMm) => restElevAt(dxMm, dyMm) / 1000 : undefined,
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
      // Alt-drag stacks the run onto whatever it overlaps (rest-on-top); a plain
      // drag stays lateral so it can butt flush against neighbours.
      if (canStack && onStackRun && meta.alt) {
        onStackRun(run.id, delta, Math.round(restElevAt(delta.dxMm, delta.dyMm)));
        return;
      }
      onMoveRun?.(run.id, delta);
    },
    onRotateCommit: (commit) => onRotateRun?.(run.id, commit),
  });

  const resetBody = () => {
    bodyRef.current?.scale.set(1, 1, 1);
    bodyRef.current?.position.set(0, 0, 0);
  };
  useLayoutEffect(() => resetBody(), [run.lengthMm, run.heightMm]);

  const previewLength = (deltaMm: number, fromStart: boolean) => {
    const scale = Math.max(0.05, (run.lengthMm + deltaMm) / run.lengthMm);
    setBodyPreview(bodyRef, [scale, 1, 1], [fromStart ? -deltaMm / 1000 : 0, 0, 0]);
  };

  const previewHeight = (deltaMm: number) => {
    const scale = Math.max(0.05, (run.heightMm + deltaMm) / run.heightMm);
    setBodyPreview(bodyRef, [1, scale, 1], [0, 0, 0]);
  };

  const stickyDelta = (base: number, deltaMm: number) => stickyDimensionMm(base + deltaMm) - base;

  const commitLength = (deltaMm: number, fromStart: boolean) => {
    if (!onStretchRun) {
      resetBody();
      return;
    }
    const target = stickyDelta(run.lengthMm, deltaMm);
    const footprintAt = (d: number) =>
      buildRunFootprint(
        { ...run, lengthMm: run.lengthMm + d },
        fromStart ? -d * dirX : 0,
        fromStart ? -d * dirY : 0,
        run.rotationDeg,
      );
    const clamped = clampPlanStretch(footprintAt, gestureObstacles, target);
    if (target > 0 && clamped < target && onPushStretchRun) {
      const blockerId = firstPenetratingOwner(footprintAt(clamped + 1), gestureObstacles);
      if (
        blockerId &&
        onPushStretchRun(run.id, fromStart ? 'start' : 'end', target, clamped, blockerId)
      ) {
        return;
      }
    }
    const next = Math.max(MIN_RUN_LENGTH_MM, Math.round(run.lengthMm + clamped));
    if (next === run.lengthMm) {
      resetBody();
      return;
    }
    if (!fromStart) {
      onStretchRun(run.id, { lengthMm: next });
      return;
    }
    const shift = next - run.lengthMm;
    onStretchRun(run.id, {
      lengthMm: next,
      originX: Math.round(run.originX - shift * dirX),
      originY: Math.round(run.originY - shift * dirY),
    });
  };

  const commitHeight = (deltaMm: number) => {
    const next = Math.max(
      MIN_RUN_HEIGHT_MM,
      Math.round(run.heightMm + stickyDelta(run.heightMm, deltaMm)),
    );
    if (next === run.heightMm) {
      resetBody();
      return;
    }
    onStretchRun?.(run.id, { heightMm: next });
  };

  const labelMm = (value: number) => `${Math.round(value)} mm`;
  const lengthLabel = (d: number) =>
    labelMm(Math.max(MIN_RUN_LENGTH_MM, run.lengthMm + stickyDelta(run.lengthMm, d)));
  const heightLabel = (d: number) =>
    labelMm(Math.max(MIN_RUN_HEIGHT_MM, run.heightMm + stickyDelta(run.heightMm, d)));

  const transformActive = useDesignerStore((s) => s.transformHandlesActive);
  const stretchActive = activeTool === 'stretch' && Boolean(onStretchRun) && !run.locked;
  const vertexEditActive = transformActive && isRunSelected && Boolean(onStretchRun) && !run.locked;
  const endFaceM = RUN_PLAN_THICKNESS_MM / 1000;
  const stretchFaces: StretchFaceDef[] = stretchActive
    ? [
        {
          id: 'start',
          centerM: [-FACE_LIFT_M, heightM / 2, 0],
          rotation: [0, -HALF_PI, 0],
          widthM: endFaceM,
          heightM,
          hitWidthM: FACE_HIT_SIZE_M,
          axis: [-1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(run.lengthMm, d), true),
          onCommit: (d) => commitLength(d, true),
        },
        {
          id: 'end',
          centerM: [lengthM + FACE_LIFT_M, heightM / 2, 0],
          rotation: [0, HALF_PI, 0],
          widthM: endFaceM,
          heightM,
          hitWidthM: FACE_HIT_SIZE_M,
          axis: [1, 0, 0],
          label: lengthLabel,
          onPreview: (d) => previewLength(stickyDelta(run.lengthMm, d), false),
          onCommit: (d) => commitLength(d, false),
        },
        {
          id: 'top',
          centerM: [lengthM / 2, heightM + FACE_LIFT_M, 0],
          rotation: [-HALF_PI, 0, 0],
          widthM: lengthM,
          heightM: endFaceM,
          hitHeightM: FACE_HIT_SIZE_M,
          axis: [0, 1, 0],
          label: heightLabel,
          onPreview: (d) => previewHeight(stickyDelta(run.heightMm, d)),
          onCommit: commitHeight,
        },
      ]
    : [];

  return (
    <>
      <group
        ref={setGroupRef}
        position={[run.originX / 1000, baseY, run.originY / 1000]}
        rotation={[0, -run.rotationDeg * DEG2RAD, 0]}
        {...gestures.handlers}
        onClick={(e) => {
          e.stopPropagation();
          if (gestures.consumeClick()) return;
          onSelectRun(run.id);
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          const canDrag = activeTool === 'move' || (activeTool === 'select' && isRunSelected);
          document.body.style.cursor = canDrag ? 'grab' : 'pointer';
        }}
        onPointerOut={() => {
          document.body.style.cursor = 'auto';
        }}
      >
        <group ref={bodyRef}>
          <group position={[halfWidth, 0, 0]}>
            {!isSingleShapedPanel && (
              <>
                {showTopRail && (
                  <ProfileBar
                    lengthM={lengthM}
                    crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                    hexColor={profileColor}
                    finish={finish}
                    quality={quality}
                    position={[0, heightM, 0]}
                  />
                )}
                {showBottomRail && (
                  <ProfileBar
                    lengthM={lengthM}
                    crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                    hexColor={profileColor}
                    finish={finish}
                    quality={quality}
                    position={[0, 0, 0]}
                  />
                )}
                {showLeftRail && (
                  <ProfileBar
                    lengthM={heightM}
                    crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                    hexColor={profileColor}
                    finish={finish}
                    quality={quality}
                    position={[-halfWidth, heightM / 2, 0]}
                    rotation={[0, 0, Math.PI / 2]}
                  />
                )}
                {showRightRail && (
                  <ProfileBar
                    lengthM={heightM}
                    crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                    hexColor={profileColor}
                    finish={finish}
                    quality={quality}
                    position={[halfWidth, heightM / 2, 0]}
                    rotation={[0, 0, Math.PI / 2]}
                  />
                )}
              </>
            )}
            {showMullions &&
              panelLayout.length > 1 &&
              panelLayout.slice(0, -1).map((layout, i) => {
                const dividerX = layout.centerX + layout.widthM / 2;
                const leftPanel = panels[i];
                const rightPanel = panels[i + 1];
                const isLastDivider = i + 1 === panels.length - 1;
                const resizesLeft = Boolean(onResizePanel) && selectedPanelId === leftPanel?.id;
                const resizesRight =
                  Boolean(onResizePanel) && isLastDivider && selectedPanelId === rightPanel?.id;
                return (
                  <Mullion
                    key={`mullion-${i}`}
                    positionX={dividerX}
                    heightM={heightM}
                    hexColor={profileColor}
                    finish={finish}
                    quality={quality}
                    interactive={resizesLeft || resizesRight}
                    onCommit={(deltaMm) => {
                      if (!onResizePanel) return;
                      if (resizesLeft && leftPanel) onResizePanel(run.id, leftPanel.id, deltaMm);
                      else if (resizesRight && rightPanel)
                        onResizePanel(run.id, rightPanel.id, -deltaMm);
                    }}
                    onSelect={() => {
                      if (leftPanel) onSelectPanel(run.id, leftPanel.id);
                    }}
                  />
                );
              })}
            {panelLayout.map(({ panel, centerX, widthM }) => {
              const glass = glassTypes.get(panel.glassTypeId);
              const glassWidthMm = Math.round(Math.max(0.05, widthM - 0.012) * 1000);
              const runGlassHeightMm = Math.round(Math.max(0.05, heightM - 2 * profileHalf) * 1000);
              const panelGlassHeightMm = panel.heightMm ?? runGlassHeightMm;
              const shapeSpec = {
                widthMm: glassWidthMm,
                heightMm: panelGlassHeightMm,
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
              return (
                <PanelMesh
                  key={panel.id}
                  panelId={panel.id}
                  centerX={centerX}
                  baseY={profileHalf}
                  widthM={Math.max(0.05, widthM - 0.012)}
                  heightM={panelGlassHeightMm / 1000}
                  shapeSpec={shapeSpec}
                  frameColor={profileColor}
                  showFrameBand={isSingleShapedPanel}
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
              );
            })}
            {showAnnotations && (
              <Billboard position={[0, heightM + 0.5, 0]} follow>
                <Text
                  fontSize={0.12}
                  color={isRunSelected ? '#1d4ed8' : '#0f172a'}
                  anchorX="center"
                  anchorY="bottom"
                  outlineWidth={0.004}
                  outlineColor="#ffffff"
                >
                  {`${run.label} · ${run.lengthMm} × ${run.heightMm} mm`}
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
        </group>
        {stretchActive && <StretchFaces faces={stretchFaces} />}
      </group>
      {vertexEditActive && (
        <FootprintCornerHandles
          box={{
            originX: run.originX,
            originY: run.originY,
            lengthMm: run.lengthMm,
            crossMm: RUN_PLAN_THICKNESS_MM,
            rotationDeg: run.rotationDeg,
          }}
          topYM={((run.geomZ ?? 0) + run.heightMm) / 1000}
          onCommit={(next) => {
            // WHY: a run keeps its thin glass cross + axis — project the corner shift onto the
            // run direction so it only changes length, never drifts sideways.
            const along = (next.originX - run.originX) * dirX + (next.originY - run.originY) * dirY;
            onStretchRun?.(run.id, {
              lengthMm: Math.max(MIN_RUN_LENGTH_MM, Math.round(next.lengthMm)),
              originX: Math.round(run.originX + along * dirX),
              originY: Math.round(run.originY + along * dirY),
            });
          }}
        />
      )}
    </>
  );
}

interface MullionProps {
  positionX: number;
  heightM: number;
  hexColor: string;
  finish: 'Anodized' | 'PowderCoated' | 'WoodLook' | 'Raw';
  quality: QualityPreset;
  interactive: boolean;
  onCommit: (deltaMm: number) => void;
  onSelect: () => void;
}

function Mullion({
  positionX,
  heightM,
  hexColor,
  finish,
  quality,
  interactive,
  onCommit,
  onSelect,
}: MullionProps) {
  const groupRef = useRef<Group>(null);

  const drag = useDrag3D({
    constraint: { mode: 'axis', targetRef: groupRef, localAxis: MULLION_AXIS },
    enabled: interactive,
    onMove: (delta) => {
      const group = groupRef.current;
      if (group) group.position.x = positionX + delta.x / 1000;
    },
    onCommit: (delta) => {
      const group = groupRef.current;
      if (group) group.position.x = positionX;
      const deltaMm = Math.round(delta.x);
      if (deltaMm !== 0) onCommit(deltaMm);
    },
  });

  return (
    <group
      ref={groupRef}
      position={[positionX, heightM / 2, 0]}
      {...drag.handlers}
      onClick={(e) => {
        e.stopPropagation();
        if (drag.consumeClick()) return;
        onSelect();
      }}
      onPointerOver={(e) => {
        e.stopPropagation();
        document.body.style.cursor = interactive ? 'ew-resize' : 'pointer';
      }}
      onPointerOut={() => {
        document.body.style.cursor = 'auto';
      }}
    >
      <ProfileBar
        lengthM={heightM}
        crossSectionMm={{ width: 30, height: 40 }}
        hexColor={hexColor}
        finish={finish}
        quality={quality}
        position={[0, 0, 0]}
        rotation={[0, 0, Math.PI / 2]}
      />
    </group>
  );
}
