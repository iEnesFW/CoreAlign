import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { usePanelEntityActions } from '../hooks/useDesignerEntityActions';
import { useHardwareItemsQuery } from '../hooks/useGlassEnclosureQueries';
import { sceneHardwareKindToCategory } from '../model/panelHardware';
import { HARDWARE_KINDS, createHardwareItem } from '../model/hardwareDefaults';
import type { ScenePanelState, SceneHardwareKind } from '../model/project.types';

interface HardwareManagerProps {
  runId: string;
  panel: ScenePanelState;
}

export function HardwareManager({ runId, panel }: HardwareManagerProps) {
  const { t } = useTranslation();
  const addHardware = useDesignerStore((s) => s.addHardware);
  const removeHardware = useDesignerStore((s) => s.removeHardware);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { persistPanelHardware } = usePanelEntityActions();
  const catalog = useHardwareItemsQuery({ isActive: true }).data?.data ?? [];

  const removeAndPersist = (hardwareId: string) => {
    removeHardware(runId, panel.id, hardwareId);
    void persistPanelHardware(runId, panel.id);
  };

  const kindLabel = (kind: SceneHardwareKind) =>
    t(`GlassEnclosure.Hardware.Kind.${kind}` as never, { defaultValue: kind });

  const selectHardware = (hardwareId: string) =>
    setSelection({ kind: 'hardware', runId, panelId: panel.id, connectionId: null, hardwareId });

  const handleAdd = (kind: SceneHardwareKind) => {
    const base = createHardwareItem(kind);
    // Auto-link the first catalog item of the matching category so the piece is quoted immediately;
    // the exact item is refined in the inspector. Unmatched kinds stay render-only until linked.
    const match = catalog.find((h) => h.category === sceneHardwareKindToCategory(kind));
    const item = match ? { ...base, hardwareItemId: match.id, quantity: 1 } : base;
    addHardware(runId, panel.id, item);
    selectHardware(item.id);
    if (match) void persistPanelHardware(runId, panel.id);
  };

  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Hardware.Objects')}
      </p>
      <div className="flex flex-wrap gap-1.5">
        {HARDWARE_KINDS.map((kind) => (
          <button
            key={kind}
            type="button"
            onClick={() => handleAdd(kind)}
            className="inline-flex items-center gap-1 rounded border border-slate-300 px-2 py-1 text-xs text-slate-600 hover:border-primary-400 hover:text-primary-600 dark:border-slate-600 dark:text-slate-300 dark:hover:border-primary-500"
          >
            <Plus size={12} />
            {kindLabel(kind)}
          </button>
        ))}
      </div>

      {panel.hardware.length === 0 ? (
        <p className="text-xs text-slate-400 dark:text-slate-500">
          {t('GlassEnclosure.Hardware.Empty')}
        </p>
      ) : (
        <ul className="space-y-1">
          {panel.hardware.map((hw) => (
            <li
              key={hw.id}
              className="flex items-center justify-between rounded border border-slate-200 px-2 py-1 dark:border-slate-700"
            >
              <button
                type="button"
                onClick={() => selectHardware(hw.id)}
                className="flex items-center gap-2 text-xs text-slate-700 hover:text-primary-600 dark:text-slate-200 dark:hover:text-primary-400"
              >
                <span
                  className="inline-block h-3 w-3 rounded-sm border border-black/10"
                  style={{ backgroundColor: hw.colorHex }}
                />
                {kindLabel(hw.kind)}
                {!hw.hardwareItemId && (
                  <span className="text-[10px] text-amber-500 dark:text-amber-400">
                    {t('GlassEnclosure.Hardware.NotQuoted', { defaultValue: '(teklife girmez)' })}
                  </span>
                )}
              </button>
              <button
                type="button"
                onClick={() => removeAndPersist(hw.id)}
                className="text-slate-400 hover:text-danger-500"
                aria-label={t('Common.Delete')}
              >
                <Trash2 size={13} />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
