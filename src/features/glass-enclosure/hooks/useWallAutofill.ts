import { useTranslation } from 'react-i18next';
import { safeRequest, safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { useDesignerStore } from '../model/designerStore';
import { enqueuePersist } from '../model/persistQueue';
import { computeOpeningEdges, panelCountForWidth } from '../model/wallAutofill';
import { computeMultiWallGapRuns, describeTwoWallGapFailure } from '../model/multiAutofill';
import { arcEndLocal, developedLengthMm } from '../model/arcGeometry';
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
const CORNER_SEAM_TOLERANCE_MM = 60;
const MIN_SEAM_ANGLE_DEG = 10;

// The two world endpoints of a created gap run (start + arc/straight end), for detecting where two
// runs meet at a shared corner.
const edgeEndpoints = (edge: GapEdge): { x: number; y: number }[] => {
  const rad = (edge.rotationDeg * Math.PI) / 180;
  const start = { x: edge.originX, y: edge.originY };
  if (edge.geomArcRadiusMm && edge.geomArcSweepDeg) {
    const e = arcEndLocal(edge.geomArcRadiusMm, edge.geomArcSweepDeg);
    return [
      start,
      {
        x: edge.originX + e.xMm * Math.cos(rad) - e.yMm * Math.sin(rad),
        y: edge.originY + e.xMm * Math.sin(rad) + e.yMm * Math.cos(rad),
      },
    ];
  }
  return [
    start,
    {
      x: edge.originX + edge.lengthMm * Math.cos(rad),
      y: edge.originY + edge.lengthMm * Math.sin(rad),
    },
  ];
};

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
    fillGlassTypeId: string | null,
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
      // WHY(B4): make the fill glass match the enclosure's glass (resolved from an existing run),
      // not the backend's fresh-panel default; AND shape the first pane to the hole silhouette so it
      // fills the opening instead of overflowing the wall as a rectangle. Only panels that actually
      // need a change (a shape, or a different glass) are persisted — no redundant round-trips.
      for (let i = 0; i < runData.panels.length; i += 1) {
        const panel = runData.panels[i];
        const isShapePanel = i === 0 && Boolean(edge.shapeKind);
        const glassTypeId = fillGlassTypeId ?? panel.glassTypeId;
        if (!isShapePanel && glassTypeId === panel.glassTypeId) continue;
        await safeRequestWithNotify(
          enqueuePersist(() =>
            updatePanelMutation.mutateAsync({
              id: projectId,
              runId: runData.id,
              panelId: panel.id,
              input: {
                widthMm: panel.widthMm,
                openingType: panel.openingType,
                glassTypeId,
                hasHandle: panel.hasHandle,
                hasLock: panel.hasLock,
                hasBrushSeal: panel.hasBrushSeal,
                heightMm: panel.heightMm ?? null,
                shapeKind: isShapePanel ? edge.shapeKind : (panel.shapeKind ?? null),
                shapePointsJson: isShapePanel
                  ? (edge.shapePointsJson ?? null)
                  : (panel.shapePointsJson ?? null),
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
    const joined = new Set<string>();
    const emit = async (aId: string, bId: string, jointAngleDeg: number) => {
      const key = aId < bId ? `${aId}|${bId}` : `${bId}|${aId}`;
      if (joined.has(key)) return;
      joined.add(key);
      await safeRequestWithNotify(
        enqueuePersist(() =>
          addConnectionMutation.mutateAsync({
            id: projectId,
            input: {
              runAId: aId,
              runBId: bId,
              jointAngleDeg,
              mitreCutDeg: Math.round(jointAngleDeg / 2),
              usesCornerPost: false,
              cornerProfileId: null,
            },
          }),
        ),
        { showSuccessNotification: false },
      );
    };

    // 1) The two legs of a single L corner (same cornerGroup).
    const groups = new Map<number, { id: string; edge: GapEdge }[]>();
    for (const item of created) {
      if (item.edge.cornerGroup === undefined) continue;
      const list = groups.get(item.edge.cornerGroup) ?? [];
      list.push(item);
      groups.set(item.edge.cornerGroup, list);
    }
    for (const members of groups.values()) {
      if (members.length !== 2) continue;
      await emit(
        members[0].id,
        members[1].id,
        Math.round(angleDiffDeg(members[0].edge.rotationDeg, members[1].edge.rotationDeg)),
      );
    }

    // 2) (A3) Runs from DIFFERENT pairs that meet at a shared corner — coincident endpoints + a real
    // angle. The per-pair cornerGroup misses these cross-pair seams, so they had no mitre/joint. The
    // `joined` set dedupes against the same-L joins above.
    const withPts = created.map((c) => ({ ...c, pts: edgeEndpoints(c.edge) }));
    for (let i = 0; i < withPts.length; i += 1) {
      for (let j = i + 1; j < withPts.length; j += 1) {
        const a = withPts[i];
        const b = withPts[j];
        const coincide = a.pts.some((pa) =>
          b.pts.some((pb) => Math.hypot(pa.x - pb.x, pa.y - pb.y) <= CORNER_SEAM_TOLERANCE_MM),
        );
        if (!coincide) continue;
        const jointAngleDeg = Math.round(angleDiffDeg(a.edge.rotationDeg, b.edge.rotationDeg));
        // Only real corners (skip near-collinear butt joints that need no mitre).
        if (jointAngleDeg < MIN_SEAM_ANGLE_DEG || jointAngleDeg > 180 - MIN_SEAM_ANGLE_DEG)
          continue;
        await emit(a.id, b.id, jointAngleDeg);
      }
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
    const catalog = profileSystemsQuery.data?.data ?? [];
    // Prefer the profile an existing run already uses (so the fill matches neighbouring glass and its
    // max-panel-width cap), not blindly catalog[0]; fall back to the first catalog entry.
    const existingProfileId = state.scene.runs.find((r) => r.profileSystemId)?.profileSystemId;
    const profileSystem = catalog.find((p) => p.id === existingProfileId) ?? catalog[0];
    const profileSystemId = profileSystem?.id;
    const maxPanelWidthMm = profileSystem?.maxPanelWidthMm;
    if (!projectId || walls.length === 0) return 0;
    if (!profileSystemId) {
      // WHY: an empty catalog used to make autofill silently return 0 (indistinguishable from
      // "no gaps") — tell the user the real reason instead.
      queueToast({
        dedupeKey: 'glass-autofill-no-profile',
        variant: 'warning',
        description: t('GlassEnclosure.Designer.Wall.AutofillNoProfile', {
          defaultValue:
            'Profil sistemi kataloğu boş — camla doldurmadan önce bir profil sistemi tanımlayın.',
        }),
      });
      return 0;
    }
    const before = structuredClone(state.scene);
    // WHY(B4): glaze the fill with the enclosure's existing glass (first run's first pane) so a hole
    // in a 10mm laminated enclosure isn't filled with the backend's default; null → server default.
    const fillGlassTypeId =
      state.scene.runs
        .flatMap((r) => r.panels)
        .map((p) => p.glassTypeId)
        .find(Boolean) ?? null;

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
      const created = (await createRuns(
        projectId,
        profileSystemId,
        maxPanelWidthMm,
        edges,
        fillGlassTypeId,
      )) as {
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
    if (edges.length === 0) {
      // WHY: a single solid wall (no openings/holes) returned 0 with no feedback — tell the user.
      queueToast({
        dedupeKey: 'glass-autofill-no-openings',
        variant: 'info',
        description: t('GlassEnclosure.Designer.Wall.AutofillNoOpenings', {
          defaultValue:
            'Bu duvarda doldurulacak açıklık veya delik yok — önce bir açıklık/delik ekleyin ya da birden fazla duvar seçin.',
        }),
      });
      return 0;
    }
    const created = await createRuns(
      projectId,
      profileSystemId,
      maxPanelWidthMm,
      edges,
      fillGlassTypeId,
    );
    if (created.length > 0) await recordAutofillHistory(projectId, before);
    return created.length;
  };

  return { autofill };
};
