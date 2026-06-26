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
  selectedForDrag?: boolean;
};

export function useObjectGestures({
  selectedForDrag,
  ...options
}: DesignerObjectGestureOptions): ObjectGestures {
  const activeTool = useDesignerStore((s) => s.activeTool);
  const stackOnDrop = useDesignerStore((s) => s.stackOnDrop);
  const mode =
    activeTool === 'move' || activeTool === 'rotate'
      ? activeTool
      : activeTool === 'select' && selectedForDrag
        ? 'move'
        : null;
  return useEngineObjectGestures({ ...options, mode, forceStack: stackOnDrop });
}
