import { useTranslation } from 'react-i18next';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { useDesignerStore } from '../model/designerStore';
import { enqueuePersist } from '../model/persistQueue';
import { computeOpeningEdges, suggestedPanelCount } from '../model/wallAutofill';
import { computeMultiWallGapRuns } from '../model/multiAutofill';
import { useAddConnectionMutation, useAddRunMutation } from './useGlassProjectQueries';
import { useColorOptionsQuery, useProfileSystemsQuery } from './useGlassEnclosureQueries';
import type { GapEdge } from '../model/multiAutofill';
import type { OpenEdge } from '../model/wallAutofill';

const DEFAULT_RUN_HEIGHT_MM = 2400;

const angleDiffDeg = (a: number, b: number) => {
  const d = Math.abs((((a - b) % 180) + 180) % 180);
  return Math.min(d, 180 - d);
};

export const useWallAutofill = () => {
  const { t } = useTranslation();
  const addRunMutation = useAddRunMutation();
  const addConnectionMutation = useAddConnectionMutation();
  const profileSystemsQuery = useProfileSystemsQuery();
  const colorsQuery = useColorOptionsQuery();

  const createRuns = async (
    projectId: string,
    profileSystemId: string,
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
              panelCount: suggestedPanelCount(edge.lengthMm),
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
      if (response?.data) created.push({ id: response.data.id, edge });
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

  const autofill = async () => {
    const state = useDesignerStore.getState();
    const projectId = state.projectId;
    const walls = state.scene.walls ?? [];
    const profileSystemId = profileSystemsQuery.data?.data?.[0]?.id;
    if (!projectId || !profileSystemId || walls.length === 0) return 0;

    const multiWallIds = state.multiSelection.wallIds;
    if (multiWallIds.length >= 2) {
      const selectedWalls = walls.filter((wall) => multiWallIds.includes(wall.id));
      const edges = computeMultiWallGapRuns(selectedWalls, walls, state.scene.runs);
      if (edges.length === 0) {
        queueToast({
          dedupeKey: 'glass-autofill-no-gaps',
          variant: 'info',
          description: t('GlassEnclosure.Designer.Wall.AutofillNoGaps', {
            defaultValue:
              'Seçili duvarlar arasında doldurulabilir boşluk bulunamadı — serbest duvar uçları arasında 30 cm - 12 m aralık olmalı.',
          }),
        });
        return 0;
      }
      const created = (await createRuns(projectId, profileSystemId, edges)) as {
        id: string;
        edge: GapEdge;
      }[];
      await connectCornerRuns(projectId, created);
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
    const edges = computeOpeningEdges([selectedWall]);
    const created = await createRuns(projectId, profileSystemId, edges);
    return created.length;
  };

  return { autofill };
};
