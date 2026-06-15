import { useDesignerStore } from '../model/designerStore';
import { useRunEntityActions } from './useDesignerEntityActions';

export const useMultiSelectionDelete = () => {
  const { deleteRun } = useRunEntityActions();

  const deleteMultiSelection = () => {
    const state = useDesignerStore.getState();
    const { runIds, wallIds, slabIds } = state.multiSelection;
    const total = runIds.length + wallIds.length + slabIds.length;
    if (total === 0) return 0;
    if (wallIds.length > 0 || slabIds.length > 0) {
      const wallSet = new Set(wallIds);
      const slabSet = new Set(slabIds);
      state.applyScenePatch((scene) => ({
        ...scene,
        walls: (scene.walls ?? []).filter((wall) => !wallSet.has(wall.id)),
        slabs: (scene.slabs ?? []).filter((slab) => !slabSet.has(slab.id)),
      }));
    }
    for (const id of runIds) void deleteRun(id);
    state.clearMultiSelect();
    state.setSelection({
      kind: null,
      runId: null,
      panelId: null,
      connectionId: null,
      hardwareId: null,
      wallId: null,
      slabId: null,
      featureId: null,
    });
    return total;
  };

  return { deleteMultiSelection };
};
