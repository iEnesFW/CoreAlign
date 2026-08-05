import { useDesignerStore } from '../model/designerStore';
import { dropLockedIds, lockedBodyIds } from '../model/sceneGuards';
import { notifyLockedBlocked } from '../model/lockFeedback';
import { useRunEntityActions } from './useDesignerEntityActions';

export const useMultiSelectionDelete = () => {
  const { deleteRun } = useRunEntityActions();

  const deleteMultiSelection = () => {
    const state = useDesignerStore.getState();
    // The single removes gate on blockedByLockOnDelete; this one writes through applyScenePatch and
    // deleted a locked body outright. The lock is meant to survive exactly that.
    const locked = lockedBodyIds(state.scene);
    const runs = dropLockedIds(state.multiSelection.runIds, locked);
    const walls = dropLockedIds(state.multiSelection.wallIds, locked);
    const slabs = dropLockedIds(state.multiSelection.slabIds, locked);
    if (runs.blocked || walls.blocked || slabs.blocked) notifyLockedBlocked();
    const runIds = [...runs.ids];
    const total = runs.ids.size + walls.ids.size + slabs.ids.size;
    if (total === 0) {
      state.clearMultiSelect();
      return 0;
    }
    if (walls.ids.size > 0 || slabs.ids.size > 0) {
      state.applyScenePatch((scene) => ({
        ...scene,
        walls: (scene.walls ?? []).filter((wall) => !walls.ids.has(wall.id)),
        slabs: (scene.slabs ?? []).filter((slab) => !slabs.ids.has(slab.id)),
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
