import { useTranslation } from 'react-i18next';
import { safeRequest, safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { glassProjectsApi } from '../api/glassProjectsApi';
import { useDesignerStore } from '../model/designerStore';
import { enqueuePersist } from '../model/persistQueue';
import { buildGlassTemplate, type GlassTemplateKey } from '../model/templates';
import { developedLengthMm } from '../model/arcGeometry';
import { panelCountForWidth } from '../model/wallAutofill';
import { useAddRunMutation } from './useGlassProjectQueries';
import { useColorOptionsQuery, useProfileSystemsQuery } from './useGlassEnclosureQueries';
import type { SceneState } from '../model/project.types';

// Where a template lands: just past the scene's right-most occupied extent (or the origin on an
// empty scene) so it never drops on top of existing work.
const dropAnchor = (scene: SceneState): { x: number; y: number } => {
  let maxX = Number.NEGATIVE_INFINITY;
  for (const r of scene.runs) maxX = Math.max(maxX, r.originX + r.lengthMm);
  for (const w of scene.walls ?? []) maxX = Math.max(maxX, w.originX + w.lengthMm);
  for (const s of scene.slabs ?? []) maxX = Math.max(maxX, s.originX + s.lengthMm);
  if (!Number.isFinite(maxX)) return { x: 0, y: 0 };
  return { x: Math.round(maxX + 1500), y: 0 };
};

export const useTemplateInsert = () => {
  const { t } = useTranslation();
  const addRunMutation = useAddRunMutation();
  const profileSystemsQuery = useProfileSystemsQuery();
  const colorsQuery = useColorOptionsQuery();

  const insertTemplate = async (key: GlassTemplateKey) => {
    const state = useDesignerStore.getState();
    const template = buildGlassTemplate(key);
    const anchor = dropAnchor(state.scene);

    // Walls + slabs live in the scene blob — ONE applyScenePatch = one undo step.
    if (template.walls.length > 0 || template.slabs.length > 0) {
      state.applyScenePatch((scene) => ({
        ...scene,
        walls: [
          ...(scene.walls ?? []),
          ...template.walls.map((w) => ({
            ...w,
            id: crypto.randomUUID(),
            originX: Math.round(w.originX + anchor.x),
            originY: Math.round(w.originY + anchor.y),
          })),
        ],
        slabs: [
          ...(scene.slabs ?? []),
          ...template.slabs.map((s) => ({
            ...s,
            id: crypto.randomUUID(),
            originX: Math.round(s.originX + anchor.x),
            originY: Math.round(s.originY + anchor.y),
          })),
        ],
      }));
    }

    // Runs are server entities — same creation path autofill uses, single-step undo via the
    // autofill transaction (before-scene vs fresh project DTO).
    if (template.runs.length > 0) {
      const projectId = state.projectId;
      const profileSystem = profileSystemsQuery.data?.data?.[0];
      if (!projectId || !profileSystem) {
        queueToast({
          dedupeKey: 'glass-template-no-system',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Templates.NoSystem', {
            defaultValue: 'Şablon için önce bir profil sistemi tanımlı olmalı.',
          }),
        });
        return;
      }
      const before = structuredClone(state.scene);
      const runPrefix = t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Hat' });
      let created = 0;
      for (const run of template.runs) {
        const [response] = await safeRequestWithNotify(
          enqueuePersist(() =>
            addRunMutation.mutateAsync({
              id: projectId,
              input: {
                lengthMm: run.lengthMm,
                heightMm: run.heightMm,
                profileSystemId: profileSystem.id,
                originX: Math.round(run.originX + anchor.x),
                originY: Math.round(run.originY + anchor.y),
                rotationDeg: run.rotationDeg,
                geomZ: 0,
                geomArcRadiusMm: run.geomArcRadiusMm ?? null,
                geomArcSweepDeg: run.geomArcSweepDeg ?? null,
                arcGlassBent: run.arcGlassBent ?? null,
                panelCount: panelCountForWidth(
                  developedLengthMm(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg),
                  profileSystem.maxPanelWidthMm,
                ),
                label: `${runPrefix} ${state.scene.runs.length + created + 1}`,
                colorId: colorsQuery.data?.data?.[0]?.id ?? null,
                hasTopDrip: true,
                hasBottomThreshold: false,
                notes: null,
              },
            }),
          ),
          { showSuccessNotification: false },
        );
        if (response?.data) created += 1;
      }
      if (created > 0) {
        const [resp] = await safeRequest(glassProjectsApi.getById(projectId));
        if (resp?.data) useDesignerStore.getState().commitAutofillTransaction(before, resp.data);
      }
    }

    queueToast({
      dedupeKey: 'glass-template-inserted',
      variant: 'success',
      description: t('GlassEnclosure.Designer.Templates.Inserted', {
        defaultValue: 'Şablon sahneye eklendi.',
      }),
    });
  };

  return { insertTemplate };
};
