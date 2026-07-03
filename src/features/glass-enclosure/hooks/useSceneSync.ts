import { useCallback } from 'react';
import { safeRequest } from '@/shared/lib/safeRequest';
import { enqueuePersist } from '../model/persistQueue';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { useDesignerStore } from '../model/designerStore';
import {
  useAddConnectionMutation,
  useAddPanelMutation,
  useAddRunMutation,
  useRemoveConnectionMutation,
  useRemovePanelMutation,
  useRemoveRunMutation,
  useUpdateConnectionMutation,
  useUpdatePanelMutation,
  useUpdateRunMutation,
} from './useGlassProjectQueries';
import type {
  AddPanelInput,
  AddRunInput,
  CornerRadiiMm,
  GlassProjectDto,
  GlassProjectPanelDto,
  GlassProjectRunDto,
  RunConnectionDto,
  SceneConnectionState,
  ScenePanelState,
  SceneRunState,
  SceneState,
} from '../model/project.types';

const toRunInput = (run: SceneRunState): AddRunInput => ({
  label: run.label,
  lengthMm: run.lengthMm,
  heightMm: run.heightMm,
  originX: run.originX,
  originY: run.originY,
  rotationDeg: run.rotationDeg,
  profileSystemId: run.profileSystemId,
  colorId: run.colorId,
  hasTopDrip: run.hasTopDrip,
  hasBottomThreshold: run.hasBottomThreshold,
  geomZ: run.geomZ ?? null,
  // Round-trip notes/geomTiltDeg from the scene (carried off the DTO) — hard-coding null here
  // WIPED the server values on every designer-driven run update.
  geomTiltDeg: run.geomTiltDeg ?? null,
  geomArcRadiusMm: run.geomArcRadiusMm ?? null,
  geomArcSweepDeg: run.geomArcSweepDeg ?? null,
  arcGlassBent: run.arcGlassBent ?? false,
  notes: run.notes ?? null,
});

const toPanelInput = (panel: ScenePanelState): AddPanelInput => ({
  widthMm: panel.widthMm,
  openingType: panel.openingType,
  glassTypeId: panel.glassTypeId,
  hasHandle: panel.hasHandle,
  hasLock: panel.hasLock,
  hasBrushSeal: panel.hasBrushSeal,
  notes: panel.notes ?? null,
  heightMm: panel.heightMm ?? null,
  topShape: panel.topShape ?? null,
  topRightHeightMm: panel.topRightHeightMm ?? null,
  archRiseMm: panel.archRiseMm ?? null,
  cornerRadiiMm: panel.cornerRadiiMm ?? null,
  shapeKind: panel.shapeKind ?? null,
  shapePointsJson: panel.shapePointsJson ?? null,
});

const cornerRadiiDiffer = (a?: CornerRadiiMm | null, b?: CornerRadiiMm | null) =>
  (a?.tl ?? null) !== (b?.tl ?? null) ||
  (a?.tr ?? null) !== (b?.tr ?? null) ||
  (a?.br ?? null) !== (b?.br ?? null) ||
  (a?.bl ?? null) !== (b?.bl ?? null);

const runDiffers = (server: GlassProjectRunDto, target: SceneRunState) =>
  server.label !== target.label ||
  server.lengthMm !== target.lengthMm ||
  server.heightMm !== target.heightMm ||
  server.originX !== target.originX ||
  server.originY !== target.originY ||
  server.rotationDeg !== target.rotationDeg ||
  server.profileSystemId !== target.profileSystemId ||
  server.colorId !== target.colorId ||
  server.hasTopDrip !== target.hasTopDrip ||
  server.hasBottomThreshold !== target.hasBottomThreshold ||
  (server.geomZ ?? null) !== (target.geomZ ?? null) ||
  (server.geomTiltDeg ?? null) !== (target.geomTiltDeg ?? null) ||
  (server.geomArcRadiusMm ?? null) !== (target.geomArcRadiusMm ?? null) ||
  (server.geomArcSweepDeg ?? null) !== (target.geomArcSweepDeg ?? null) ||
  (server.arcGlassBent ?? false) !== (target.arcGlassBent ?? false) ||
  (server.notes ?? null) !== (target.notes ?? null);

const panelDiffers = (server: GlassProjectPanelDto, target: ScenePanelState) =>
  server.widthMm !== target.widthMm ||
  server.openingType !== target.openingType ||
  server.glassTypeId !== target.glassTypeId ||
  server.hasHandle !== target.hasHandle ||
  server.hasLock !== target.hasLock ||
  server.hasBrushSeal !== target.hasBrushSeal ||
  (server.notes ?? null) !== (target.notes ?? null) ||
  (server.heightMm ?? null) !== (target.heightMm ?? null) ||
  (server.topShape ?? null) !== (target.topShape ?? null) ||
  (server.topRightHeightMm ?? null) !== (target.topRightHeightMm ?? null) ||
  (server.archRiseMm ?? null) !== (target.archRiseMm ?? null) ||
  (server.shapeKind ?? null) !== (target.shapeKind ?? null) ||
  (server.shapePointsJson ?? null) !== (target.shapePointsJson ?? null) ||
  cornerRadiiDiffer(server.cornerRadiiMm, target.cornerRadiiMm);

const connectionDiffers = (server: RunConnectionDto, target: SceneConnectionState) =>
  server.jointAngleDeg !== target.jointAngleDeg ||
  server.mitreCutDeg !== target.mitreCutDeg ||
  server.usesCornerPost !== target.usesCornerPost ||
  server.cornerProfileId !== target.cornerProfileId;

export const useSceneSync = () => {
  const addRunMutation = useAddRunMutation();
  const updateRunMutation = useUpdateRunMutation();
  const removeRunMutation = useRemoveRunMutation();
  const addPanelMutation = useAddPanelMutation();
  const updatePanelMutation = useUpdatePanelMutation();
  const removePanelMutation = useRemovePanelMutation();
  const addConnectionMutation = useAddConnectionMutation();
  const updateConnectionMutation = useUpdateConnectionMutation();
  const removeConnectionMutation = useRemoveConnectionMutation();

  const syncSceneToServer = useCallback(
    (project: GlassProjectDto, target: SceneState) =>
      enqueuePersist(async () => {
        const id = project.id;
        // Diff against FRESH server truth, not the caller's cached DTO: the snapshot is captured
        // synchronously but this task runs later in the persist queue, and a stale base re-issued
        // deletes against already-deleted runs (404 'ProjectRun not found') or skipped re-creating
        // runs on redo (permanent client-only runs where every later mutation 404s).
        const [freshResp] = await safeRequest(glassProjectsApi.getById(id));
        const server = freshResp?.data ?? project;
        const idMap = new Map<string, string>();
        const mapRunId = (runId: string) => idMap.get(runId) ?? runId;
        const targetRuns = new Map(target.runs.map((r) => [r.id, r]));
        const serverRuns = new Map(server.runs.map((r) => [r.id, r]));
        const targetConnections = new Map(target.connections.map((c) => [c.id, c]));
        const serverConnections = new Map(server.connections.map((c) => [c.id, c]));

        for (const connection of server.connections) {
          const targetConnection = targetConnections.get(connection.id);
          const refRemoved =
            !targetRuns.has(connection.runAId) || !targetRuns.has(connection.runBId);
          if (!targetConnection || refRemoved) {
            await removeConnectionMutation.mutateAsync({ id, connectionId: connection.id });
            serverConnections.delete(connection.id);
          }
        }

        for (const serverRun of server.runs) {
          const targetRun = targetRuns.get(serverRun.id);
          if (!targetRun) {
            await removeRunMutation.mutateAsync({ id, runId: serverRun.id });
            continue;
          }
          const targetPanels = new Map(targetRun.panels.map((p) => [p.id, p]));
          for (const serverPanel of serverRun.panels) {
            if (!targetPanels.has(serverPanel.id)) {
              await removePanelMutation.mutateAsync({
                id,
                runId: serverRun.id,
                panelId: serverPanel.id,
              });
            }
          }
          if (runDiffers(serverRun, targetRun)) {
            await updateRunMutation.mutateAsync({
              id,
              runId: serverRun.id,
              input: toRunInput(targetRun),
            });
          }
          const serverPanels = new Map(serverRun.panels.map((p) => [p.id, p]));
          for (const targetPanel of targetRun.panels) {
            const serverPanel = serverPanels.get(targetPanel.id);
            if (!serverPanel) {
              await addPanelMutation.mutateAsync({
                id,
                runId: serverRun.id,
                input: toPanelInput(targetPanel),
              });
            } else if (panelDiffers(serverPanel, targetPanel)) {
              await updatePanelMutation.mutateAsync({
                id,
                runId: serverRun.id,
                panelId: targetPanel.id,
                input: toPanelInput(targetPanel),
              });
            }
          }
        }

        for (const targetRun of target.runs) {
          if (serverRuns.has(targetRun.id)) continue;
          const response = await addRunMutation.mutateAsync({
            id,
            input: { ...toRunInput(targetRun), panelCount: null },
          });
          const createdId = response?.data?.id;
          if (!createdId) continue;
          idMap.set(targetRun.id, createdId);
          for (const targetPanel of targetRun.panels) {
            await addPanelMutation.mutateAsync({
              id,
              runId: createdId,
              input: toPanelInput(targetPanel),
            });
          }
        }

        for (const targetConnection of target.connections) {
          const serverConnection = serverConnections.get(targetConnection.id);
          if (!serverConnection) {
            await addConnectionMutation.mutateAsync({
              id,
              input: {
                runAId: mapRunId(targetConnection.runAId),
                runBId: mapRunId(targetConnection.runBId),
                jointAngleDeg: targetConnection.jointAngleDeg,
                mitreCutDeg: targetConnection.mitreCutDeg,
                usesCornerPost: targetConnection.usesCornerPost,
                cornerProfileId: targetConnection.cornerProfileId,
              },
            });
          } else if (connectionDiffers(serverConnection, targetConnection)) {
            await updateConnectionMutation.mutateAsync({
              id,
              connectionId: targetConnection.id,
              input: {
                jointAngleDeg: targetConnection.jointAngleDeg,
                mitreCutDeg: targetConnection.mitreCutDeg,
                usesCornerPost: targetConnection.usesCornerPost,
                cornerProfileId: targetConnection.cornerProfileId,
              },
            });
          }
        }

        // Runs re-created on redo get NEW server ids — the store scene must adopt them or every
        // later persist against the old id 404s. Reconcile from server truth, guarded to the same
        // scene reference so a newer local edit queued behind this task is never clobbered.
        if (idMap.size > 0) {
          const [reconciled] = await safeRequest(glassProjectsApi.getById(id));
          const store = useDesignerStore.getState();
          if (reconciled?.data && store.scene === target) store.loadProject(reconciled.data);
        }
      }),
    [
      addRunMutation,
      updateRunMutation,
      removeRunMutation,
      addPanelMutation,
      updatePanelMutation,
      removePanelMutation,
      addConnectionMutation,
      updateConnectionMutation,
      removeConnectionMutation,
    ],
  );

  return { syncSceneToServer };
};
