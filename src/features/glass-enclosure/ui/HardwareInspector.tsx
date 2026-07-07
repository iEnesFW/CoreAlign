import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { HARDWARE_KINDS } from '../model/hardwareDefaults';
import { clampHardwareOffsets } from '../model/hardwarePlacement';
import type { SceneHardwareKind } from '../model/project.types';

export function HardwareInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const updateHardware = useDesignerStore((s) => s.updateHardware);
  const removeHardware = useDesignerStore((s) => s.removeHardware);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const { run, panel, item } = useMemo(() => {
    const run = runs.find((r) => r.id === selection.runId);
    const panel = run?.panels.find((p) => p.id === selection.panelId);
    const item = panel?.hardware.find((h) => h.id === selection.hardwareId);
    return { run, panel, item };
  }, [runs, selection]);

  if (!run || !panel || !item) return null;

  const commit = (patch: Partial<typeof item>) => {
    const next = { ...item, ...patch };
    updateHardware(run.id, panel.id, item.id, {
      ...patch,
      ...clampHardwareOffsets(panel.widthMm, run.heightMm, next),
    });
  };
  const kindLabel = (kind: SceneHardwareKind) =>
    t(`GlassEnclosure.Hardware.Kind.${kind}` as never, { defaultValue: kind });

  const handleRemove = () => {
    removeHardware(run.id, panel.id, item.id);
    setSelection({
      kind: 'panel',
      runId: run.id,
      panelId: panel.id,
      connectionId: null,
      hardwareId: null,
    });
  };

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Hardware.Title')}
        </h3>
        <button
          type="button"
          onClick={handleRemove}
          className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
        >
          {t('Common.Delete')}
        </button>
      </header>

      <Field label={t('GlassEnclosure.Hardware.Field.Kind')}>
        <select
          value={item.kind}
          onChange={(e) => commit({ kind: e.target.value as SceneHardwareKind })}
          className={inputClass}
        >
          {HARDWARE_KINDS.map((kind) => (
            <option key={kind} value={kind}>
              {kindLabel(kind)}
            </option>
          ))}
        </select>
      </Field>

      <Field label={t('GlassEnclosure.Hardware.Field.Color')}>
        <div className="flex items-center gap-2">
          <input
            type="color"
            value={item.colorHex}
            onChange={(e) => commit({ colorHex: e.target.value })}
            className="h-8 w-10 cursor-pointer rounded border border-slate-300 dark:border-slate-600"
          />
          <input
            type="text"
            value={item.colorHex}
            onChange={(e) => commit({ colorHex: e.target.value })}
            className={inputClass}
          />
        </div>
      </Field>

      <div className="grid grid-cols-3 gap-2">
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.OffsetX')}
          value={item.offsetXmm}
          onChange={(v) => commit({ offsetXmm: v })}
        />
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.OffsetY')}
          value={item.offsetYmm}
          onChange={(v) => commit({ offsetYmm: v })}
        />
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.OffsetZ')}
          value={item.offsetZmm}
          onChange={(v) => commit({ offsetZmm: v })}
        />
      </div>

      <div className="grid grid-cols-3 gap-2">
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.Width')}
          value={item.widthMm}
          min={1}
          onChange={(v) => commit({ widthMm: v })}
        />
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.Height')}
          value={item.heightMm}
          min={1}
          onChange={(v) => commit({ heightMm: v })}
        />
        <NumberField
          label={t('GlassEnclosure.Hardware.Field.Depth')}
          value={item.depthMm}
          min={1}
          onChange={(v) => commit({ depthMm: v })}
        />
      </div>
    </section>
  );
}

const inputClass =
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
    <span className="text-xs uppercase tracking-wide">{label}</span>
    {children}
  </label>
);

const NumberField = ({
  label,
  value,
  min,
  onChange,
}: {
  label: string;
  value: number;
  min?: number;
  onChange: (value: number) => void;
}) => {
  const [draft, setDraft] = useState(String(value));
  const [tracked, setTracked] = useState(value);
  if (value !== tracked) {
    setTracked(value);
    setDraft(String(value));
  }
  const commit = () => {
    const parsed = Number(draft);
    if (!Number.isNaN(parsed)) onChange(parsed);
  };
  return (
    <label className="flex flex-col gap-1 text-[11px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      <span>{label}</span>
      <input
        type="number"
        min={min}
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={commit}
        className={inputClass}
      />
    </label>
  );
};
