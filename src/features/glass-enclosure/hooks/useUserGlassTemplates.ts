import { useEffect, useMemo, useRef } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { safeRequest } from '@/shared/lib/safeRequest';
import { createUserScopedSlot } from '@/shared/storage/userScopedSlot';
import {
  glassProjectTemplatesApi,
  type GlassProjectTemplateSummaryDto,
} from '../api/glassProjectTemplatesApi';
import { useDesignerStore } from '../model/designerStore';
import {
  captureSceneAsTemplate,
  isTemplateEmpty,
  parseTemplatePayload,
  parseUserGlassTemplates,
  templatePayloadJson,
  type UserGlassTemplate,
} from '../model/glassTemplates';

const PAGE_KEY = 'glass-designer';
const QUERY_KEY = ['glass-project-templates'] as const;

export const useUserGlassTemplates = () => {
  const queryClient = useQueryClient();

  const listQuery = useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const [resp] = await safeRequest(glassProjectTemplatesApi.list());
      return resp?.data ?? [];
    },
  });
  const templates: GlassProjectTemplateSummaryDto[] = listQuery.data ?? [];

  const localSlot = useMemo(
    () =>
      createUserScopedSlot<UserGlassTemplate[]>({
        feature: 'glassTemplates',
        pageKey: PAGE_KEY,
        schema: parseUserGlassTemplates,
      }),
    [],
  );
  const migratedSlot = useMemo(
    () =>
      createUserScopedSlot<boolean>({
        feature: 'glassTemplatesMigrated',
        pageKey: PAGE_KEY,
        schema: (value) => (typeof value === 'boolean' ? value : null),
      }),
    [],
  );

  const migrationRan = useRef(false);
  useEffect(() => {
    if (migrationRan.current) return;
    migrationRan.current = true;
    if (migratedSlot.get()) return;
    const local = localSlot.get() ?? [];
    if (local.length === 0) {
      migratedSlot.set(true);
      return;
    }
    void (async () => {
      for (const template of local) {
        if (isTemplateEmpty(template)) continue;
        await safeRequest(
          glassProjectTemplatesApi.save({
            name: template.name,
            payloadJson: templatePayloadJson(template),
          }),
        );
      }
      migratedSlot.set(true);
      await queryClient.invalidateQueries({ queryKey: QUERY_KEY });
    })();
  }, [localSlot, migratedSlot, queryClient]);

  const saveCurrentAsTemplate = async (name: string): Promise<boolean> => {
    const trimmed = name.trim();
    if (!trimmed) return false;
    const captured = captureSceneAsTemplate(
      useDesignerStore.getState().scene,
      crypto.randomUUID(),
      trimmed,
    );
    if (isTemplateEmpty(captured)) return false;
    const [, error] = await safeRequest(
      glassProjectTemplatesApi.save({ name: trimmed, payloadJson: templatePayloadJson(captured) }),
    );
    if (error) return false;
    await queryClient.invalidateQueries({ queryKey: QUERY_KEY });
    return true;
  };

  const deleteTemplate = async (id: string): Promise<void> => {
    await safeRequest(glassProjectTemplatesApi.remove(id));
    await queryClient.invalidateQueries({ queryKey: QUERY_KEY });
  };

  // Arm for click-to-place instead of dropping at a computed corner — the canvas shows the plan
  // ghost and inserts where the user clicks (same interaction as the built-in templates).
  const insertUserTemplate = async (id: string): Promise<void> => {
    const [resp] = await safeRequest(glassProjectTemplatesApi.getById(id));
    const payloadJson = resp?.data?.payloadJson;
    if (!payloadJson) return;
    const parsed = parseTemplatePayload(payloadJson);
    if (!parsed) return;
    useDesignerStore.getState().setPendingTemplate(parsed);
  };

  return { templates, saveCurrentAsTemplate, deleteTemplate, insertUserTemplate };
};
