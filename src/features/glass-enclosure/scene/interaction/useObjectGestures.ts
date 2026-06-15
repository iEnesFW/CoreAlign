import { useObjectGestures as useEngineObjectGestures } from '@/shared/three-engine';
import { useDesignerStore } from '../../model/designerStore';
import type { ObjectGestures, UseObjectGesturesOptions } from '@/shared/three-engine';

export type {
  ObjectGestures,
  PlanGestureAdapter,
  PlanRotationCommit,
  ObjectGestureMode,
} from '@/shared/three-engine';

type DesignerObjectGestureOptions = Omit<UseObjectGesturesOptions, 'mode'> & {
  // When true, the already-selected object can be grabbed and dragged directly
  // in the default Select tool (no need to switch to the Move tool first).
  selectedForDrag?: boolean;
};

export function useObjectGestures({
  selectedForDrag,
  ...options
}: DesignerObjectGestureOptions): ObjectGestures {
  const activeTool = useDesignerStore((s) => s.activeTool);
  const mode =
    activeTool === 'move' || activeTool === 'rotate'
      ? activeTool
      : activeTool === 'select' && selectedForDrag
        ? 'move'
        : null;
  return useEngineObjectGestures({ ...options, mode });
}
