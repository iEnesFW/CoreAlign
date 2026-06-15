import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { usePanelEntityActions, useRunEntityActions } from '../hooks/useDesignerEntityActions';
import { HardwareManager } from './HardwareManager';
import type {
  GlassOpeningType,
  GlassTypeDto,
  InspectorSection,
} from '../model/glassEnclosure.types';

interface PanelInspectorProps {
  glassTypes: GlassTypeDto[];
  sections: InspectorSection[];
}

const OPENING_KEYS: GlassOpeningType[] = [
  'Fixed',
  'SlidingLeft',
  'SlidingRight',
  'Folding',
  'Hinged',
  'Guillotine',
];

export function PanelInspector({ glassTypes, sections }: PanelInspectorProps) {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const updatePanel = useDesignerStore((s) => s.updatePanel);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { createPanel, persistPanel, deletePanel } = usePanelEntityActions();
  const { persistRun } = useRunEntityActions();

  const { run, panel } = useMemo(() => {
    const run = runs.find((r) => r.id === selection.runId);
    const panel = run?.panels.find((p) => p.id === selection.panelId);
    return { run, panel };
  }, [runs, selection]);

  const [draft, setDraft] = useState(panel);
  const [trackedPanel, setTrackedPanel] = useState(panel);
  if (panel !== trackedPanel) {
    setTrackedPanel(panel);
    setDraft(panel);
  }

  if (!run || !panel || !draft) return null;

  const commit = (patch: Partial<typeof panel>) => {
    updatePanel(run.id, panel.id, patch);
    void persistPanel(run.id, { ...panel, ...patch });
    if (patch.widthMm !== undefined) {
      const freshRun = useDesignerStore.getState().scene.runs.find((r) => r.id === run.id);
      if (freshRun) void persistRun(freshRun);
    }
  };
  const show = (section: InspectorSection) => (sections ?? []).includes(section);

  const selectRun = () =>
    setSelection({
      kind: 'run',
      runId: run.id,
      panelId: null,
      connectionId: null,
      hardwareId: null,
    });

  const handleAddPanel = async () => {
    const template = run.panels[run.panels.length - 1];
    const created = await createPanel(run.id, template, glassTypes[0]?.id ?? '');
    if (created) selectRun();
  };

  const handleDeletePanel = () => {
    void deletePanel(run.id, panel.id);
    selectRun();
  };

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Designer.PanelInspector')} #{panel.panelIndex + 1}
        </h3>
        <div className="flex items-center gap-1.5">
          <button
            type="button"
            onClick={() => void handleAddPanel()}
            className="inline-flex items-center gap-1 rounded border border-blue-500/40 px-2 py-1 text-xs text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950/30"
          >
            <Plus size={12} />
            {t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
          </button>
          <button
            type="button"
            onClick={handleDeletePanel}
            className="rounded border border-red-500/40 px-2 py-1 text-xs text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
          >
            {t('GlassEnclosure.Designer.DeletePanel', { defaultValue: 'Delete panel' })}
          </button>
        </div>
      </header>

      {show('general') && (
        <Field label={t('GlassEnclosure.Field.OpeningType')}>
          <div className="grid grid-cols-3 gap-1.5">
            {OPENING_KEYS.map((kind) => (
              <button
                key={kind}
                type="button"
                onClick={() => commit({ openingType: kind })}
                className={`rounded border px-2 py-1.5 text-xs font-medium transition ${
                  draft.openingType === kind
                    ? 'border-blue-500 bg-blue-50 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300'
                    : 'border-slate-300 text-slate-600 hover:border-slate-400 dark:border-slate-600 dark:text-slate-300'
                }`}
              >
                {t(`GlassEnclosure.Opening.${kind}` as never)}
              </button>
            ))}
          </div>
        </Field>
      )}

      {show('dimensions') && (
        <Field label={`${t('GlassEnclosure.Field.Width')} (mm)`}>
          <input
            type="number"
            min={100}
            max={3000}
            value={draft.widthMm}
            onChange={(e) => setDraft({ ...draft, widthMm: Number(e.target.value) })}
            onBlur={() => commit({ widthMm: draft.widthMm })}
            className={inputClass}
          />
        </Field>
      )}

      {show('glass') && (
        <Field label={t('GlassEnclosure.Field.GlassType')}>
          <select
            value={draft.glassTypeId}
            onChange={(e) => commit({ glassTypeId: e.target.value })}
            className={inputClass}
          >
            {glassTypes.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name} · {g.thicknessMm} mm
              </option>
            ))}
          </select>
        </Field>
      )}

      {show('hardware') && (
        <div className="space-y-3 text-sm">
          <div className="space-y-1">
            <Toggle
              label={t('GlassEnclosure.Field.HasHandle')}
              checked={draft.hasHandle}
              onChange={(v) => commit({ hasHandle: v })}
            />
            <Toggle
              label={t('GlassEnclosure.Field.HasLock')}
              checked={draft.hasLock}
              onChange={(v) => commit({ hasLock: v })}
            />
            <Toggle
              label={t('GlassEnclosure.Field.HasBrushSeal')}
              checked={draft.hasBrushSeal}
              onChange={(v) => commit({ hasBrushSeal: v })}
            />
          </div>
          <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
            <HardwareManager runId={run.id} panel={panel} />
          </div>
        </div>
      )}
    </section>
  );
}

const inputClass =
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-blue-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
    <span className="text-xs uppercase tracking-wide">{label}</span>
    {children}
  </label>
);

const Toggle = ({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) => (
  <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
    <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
    {label}
  </label>
);
