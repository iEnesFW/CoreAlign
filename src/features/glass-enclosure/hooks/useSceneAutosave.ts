import { useCallback, useEffect, useRef } from 'react';
import { useDesignerStore } from '../model/designerStore';
import { useSaveSceneMutation } from './useGlassProjectQueries';
import { enqueuePersist } from '../model/persistQueue';
import { safeRequest } from '@/shared/lib/safeRequest';

const AUTOSAVE_DELAY_MS = 1200;

// Debounced full-scene autosave: every store mutation flips isDirty, so this
// flushes the whole snapshot (walls/slabs/surfaces/features + camera) to the
// server shortly after edits settle, giving them the same durability as runs.
export function useSceneAutosave(projectId: string | null) {
  const saveMutation = useSaveSceneMutation();
  const isDirty = useDesignerStore((s) => s.isDirty);
  const savingRef = useRef(false);
  const flushRef = useRef<() => Promise<void>>(() => Promise.resolve());

  const flush = useCallback(async () => {
    if (!projectId || savingRef.current) return;
    const state = useDesignerStore.getState();
    if (!state.isDirty) return;
    const scene = state.exportScene();
    savingRef.current = true;
    // Optimistically clear dirty; edits made during the save re-set it.
    state.markSaved();
    const [, error] = await safeRequest(
      enqueuePersist(() =>
        saveMutation.mutateAsync({
          id: projectId,
          input: {
            sceneJson: JSON.stringify(scene),
            cameraStateJson: scene.camera ? JSON.stringify(scene.camera) : null,
            label: null,
          },
        }),
      ),
    );
    savingRef.current = false;
    if (error) {
      useDesignerStore.getState().commitTransaction();
    } else if (useDesignerStore.getState().isDirty) {
      // Edits arrived during the save — explicitly reschedule rather than rely
      // on an incidental re-render to re-arm the debounce.
      window.setTimeout(() => void flushRef.current(), AUTOSAVE_DELAY_MS);
    }
  }, [projectId, saveMutation]);

  useEffect(() => {
    flushRef.current = flush;
  }, [flush]);

  useEffect(() => {
    if (!projectId || !isDirty) return;
    const timer = window.setTimeout(() => void flush(), AUTOSAVE_DELAY_MS);
    return () => window.clearTimeout(timer);
  }, [projectId, isDirty, flush]);

  // Best-effort flush when the designer unmounts (in-app navigation / view
  // switch) so edits inside the debounce window are not dropped on route change.
  useEffect(
    () => () => {
      if (useDesignerStore.getState().isDirty) void flushRef.current();
    },
    [],
  );

  // Warn before the tab closes / reloads while there is unsaved work.
  useEffect(() => {
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      if (!useDesignerStore.getState().isDirty) return;
      e.preventDefault();
      e.returnValue = '';
    };
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => window.removeEventListener('beforeunload', onBeforeUnload);
  }, []);

  return { flushNow: flush };
}
