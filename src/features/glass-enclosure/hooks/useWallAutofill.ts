import { useTranslation } from 'react-i18next';
import { safeRequest, safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { useDesignerStore } from '../model/designerStore';
import { enqueuePersist } from '../model/persistQueue';
import { computeOpeningEdges, panelCountForWidth } from '../model/wallAutofill';
import { computeMultiWallGapRuns, describeTwoWallGapFailure } from '../model/multiAutofill';
import { developedLengthMm } from '../model/arcGeometry';
import {
  useAddConnectionMutation,
  useAddRunMutation,
  useUpdatePanelMutation,
} from './useGlassProjectQueries';
import { useColorOptionsQuery, useProfileSystemsQuery } from './useGlassEnclosureQueries';
import type { GapEdge, TwoWallGapFailure } from '../model/multiAutofill';
import type { OpenEdge } from '../model/wallAutofill';
import type { SceneState } from '../model/project.types';
import type { TFunction } from 'i18next';

const DEFAULT_RUN_HEIGHT_MM = 2400;

const angleDiffDeg = (a: number, b: number) => {
  const d = Math.abs((((a - b) % 180) + 180) % 180);
  return Math.min(d, 180 - d);
};

const twoWallGapFailureMessage = (reason: TwoWallGapFailure, t: TFunction): string => {
  switch (reason) {
    case 'joined':
      return t('GlassEnclosure.Designer.Wall.AutofillGapJoined', {
        defaultValue:
          'Seçili duvarların serbest ucu yok (zaten bağlılar) — aralarında doldurulacak boşluk yok.',
      });
    case 'tooClose':
      return t('GlassEnclosure.Designer.Wall.AutofillGapTooClose', {
        defaultValue:
          'Seçili iki duvarın uçları birbirine çok yakın — aralarında en az 30 cm boşluk olmalı.',
      });
    case 'tooFar':
      return t('GlassEnclosure.Designer.Wall.AutofillGapTooFar', {
        defaultValue:
          'Seçili iki duvarın uçları çok uzak — en fazla 60 m aralık camla doldurulabilir.',
      });
    case 'direction':
      return t('GlassEnclosure.Designer.Wall.AutofillGapDirection', {
        defaultValue:
          'Seçili iki duvar birbirine ters yönde bakıyor — açık uçlarını boşluğa bakacak şekilde konumlandırın.',
      });
    case 'blocked':
    default:
      return t('GlassEnclosure.Designer.Wall.AutofillGapBlocked', {
        defaultValue:
          'Aradaki cam başka bir gövdeyle kesişiyor — duvarları biraz ayırın veya aradaki engeli kaldırın.',
      });
  }
};

export const useWallAutofill = () => {
  const { t } = useTranslation();
  const addRunMutation = useAddRunMutation();
  const addConnectionMutation = useAddConnectionMutation();
  const updatePanelMutation = useUpdatePanelMutation();
  const profileSystemsQuery = useProfileSystemsQuery();
  const colorsQuery = useColorOptionsQuery();

  const createRuns = async (
    projectId: string,
    profileSystemId: string,
    maxPanelWidthMm: number | undefined,
    edges: OpenEdge[],
  ): Promise<{ id: string; edge: OpenEdge }[]> => {
    const state = useDesignerStore.getState();
    const runPrefix = t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Hat' });
    const created: { id: string; edge: OpenEdge }[] = [];
    for (const edge of edges) {
      const [response] = await safeRequestWithNotify(
        enqueuePersist(() =>
          addRunMutation.mutateAsync({
            id: projectId,
            input: {
              lengthMm: edge.lengthMm,
              heightMm: edge.heightMm ?? DEFAULT_RUN_HEIGHT_MM,
              profileSystemId,
              originX: edge.originX,
              originY: edge.originY,
              rotationDeg: edge.rotationDeg,
              geomZ: edge.geomZ ?? null,
              geomArcRadiusMm: edge.geomArcRadiusMm ?? null,
              geomArcSweepDeg: edge.geomArcSweepDeg ?? null,
              arcGlassBent: edge.arcGlassBent ?? null,
              // A shaped hole is glazed by a single shape-matched panel, not a strip. On an ARC
              // corner fill the manufacturer's max-panel-width cap applies to the DEVELOPED
              // (physical) width — the chord understates it by up to ×1.57 at 180°.
              panelCount: edge.shapeKind
                ? 1
                : panelCountForWidth(
                    developedLengthMm(edge.lengthMm, edge.geomArcRadiusMm, edge.geomArcSweepDeg),
                    maxPanelWidthMm,
                  ),
              label: `${runPrefix} ${state.scene.runs.length + created.length + 1}`,
              colorId: colorsQuery.data?.data?.[0]?.id ?? null,
              hasTopDrip: true,
              hasBottomThreshold: false,
              notes: null,
            },
          }),
        ),
        { showSuccessNotification: false },
      );
      if (!response?.data) continue;
      const runData = response.data;
      created.push({ id: runData.id, edge });
      // Shape the glazing panel to the hole silhouette so it fills the opening
      // instead of overflowing the wall as a rectangle.
      const panel = runData.panels[0];
      if (edge.shapeKind && panel) {
        await safeRequestWithNotify(
          enqueuePersist(() =>
            updatePanelMutation.mutateAsync({
              id: projectId,
              runId: runData.id,
              panelId: panel.id,
              input: {
                widthMm: panel.widthMm,
                openingType: panel.openingType,
                glassTypeId: panel.glassTypeId,
                hasHandle: panel.hasHandle,
                hasLock: panel.hasLock,
                hasBrushSeal: panel.hasBrushSeal,
                heightMm: panel.heightMm ?? null,
                shapeKind: edge.shapeKind,
                shapePointsJson: edge.shapePointsJson ?? null,
              },
            }),
          ),
          { showSuccessNotification: false },
        );
      }
    }
    return created;
  };

  const connectCornerRuns = async (projectId: string, created: { id: string; edge: GapEdge }[]) => {
    const groups = new Map<number, { id: string; edge: GapEdge }[]>();
    for (const item of created) {
      if (item.edge.cornerGroup === undefined) continue;
      const list = groups.get(item.edge.cornerGroup) ?? [];
      list.push(item);
      groups.set(item.edge.cornerGroup, list);
    }
    for (const members of groups.values()) {
      if (members.length !== 2) continue;
      const jointAngleDeg = Math.round(
        angleDiffDeg(members[0].edge.rotationDeg, members[1].edge.rotationDeg),
      );
      await safeRequestWithNotify(
        enqueuePersist(() =>
          addConnectionMutation.mutateAsync({
            id: projectId,
            input: {
              runAId: members[0].id,
              runBId: members[1].id,
              jointAngleDeg,
              mitreCutDeg: Math.round(jointAngleDeg / 2),
              usesCornerPost: false,
              cornerProfileId: null,
            },
          }),
        ),
        { showSuccessNotification: false },
      );
    }
  };

  // Autofill persists via the server run/connection CRUD endpoints, which the local undo history
  // never sees. After the runs land, read the fresh project and record the whole fill as one
  // [before, after] history step so Ctrl+Z removes it (via the scene→server reconciler) and Ctrl+Y
  // re-adds it.
  const recordAutofillHistory = async (projectId: string, before: SceneState) => {
    const [resp] = await safeRequest(glassProjectsApi.getById(projectId));
    if (resp?.data) {
      useDesignerStore.getState().commitAutofillTransaction(before, resp.data);
    }
  };

  const autofill = async () => {
    const state = useDesignerStore.getState();
    const projectId = state.projectId;
    const walls = state.scene.walls ?? [];
    const profileSystem = profileSystemsQuery.data?.data?.[0];
    const profileSystemId = profileSystem?.id;
    const maxPanelWidthMm = profileSystem?.maxPanelWidthMm;
    if (!projectId || !profileSystemId || walls.length === 0) return 0;
    const before = structuredClone(state.scene);

    const multiWallIds = state.multiSelection.wallIds;
    if (multiWallIds.length >= 2) {
      const selectedWalls = walls.filter((wall) => multiWallIds.includes(wall.id));
      const edges = computeMultiWallGapRuns(
        selectedWalls,
        walls,
        state.scene.runs,
        state.cornerFillMode,
      );
      if (edges.length === 0) {
        queueToast({
          dedupeKey: 'glass-autofill-no-gaps',
          variant: 'info',
          description:
            selectedWalls.length === 2
              ? twoWallGapFailureMessage(describeTwoWallGapFailure(selectedWalls, walls), t)
              : t('GlassEnclosure.Designer.Wall.AutofillNoGaps', {
                  defaultValue:
                    'Seçili duvarlar arasında doldurulabilir boşluk bulunamadı — en az iki serbest duvar ucu olan duvarlar seçin.',
                }),
        });
        return 0;
      }
      const created = (await createRuns(projectId, profileSystemId, maxPanelWidthMm, edges)) as {
        id: string;
        edge: GapEdge;
      }[];
      await connectCornerRuns(projectId, created);
      if (created.length > 0) await recordAutofillHistory(projectId, before);
      return created.length;
    }

    const selectedWall =
      multiWallIds.length === 1
        ? walls.find((w) => w.id === multiWallIds[0])
        : state.selection.kind === 'wall' || state.selection.kind === 'wallFeature'
          ? walls.find((w) => w.id === state.selection.wallId)
          : undefined;
    if (!selectedWall) {
      queueToast({
        dedupeKey: 'glass-autofill-select-wall',
        variant: 'info',
        description: t('GlassEnclosure.Designer.Wall.AutofillSelectWall', {
          defaultValue:
            'Camla doldurmak için bir duvar seçin (boşluk/delik doldurma) veya birden fazla duvarı çoklu seçin (aralarını doldurma).',
        }),
      });
      return 0;
    }
    const edges = computeOpeningEdges([selectedWall], state.scene.runs);
    const created = await createRuns(projectId, profileSystemId, maxPanelWidthMm, edges);
    if (created.length > 0) await recordAutofillHistory(projectId, before);
    return created.length;
  };

  return { autofill };
};
