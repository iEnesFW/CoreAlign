import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { presetPolygonPoints, serializePanelPolygonPoints } from '../model/panelPolygon';
import { usePanelEntityActions, useRunEntityActions } from '../hooks/useDesignerEntityActions';
import { HardwareManager } from './HardwareManager';
import { PanelPolygonEditor } from './PanelPolygonEditor';
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

  // Only a single-panel run may be shaped — a triangle/oval/polygon pane next to rectangular
  // siblings is not a real product (industry never does this). Multi-panel runs stay rectangular.
  const canShape = run.panels.length === 1;

  const commit = (patch: Partial<typeof panel>) => {
    // Persist the STORE's post-commit state: on an ARC run a width edit is pinned/clamped and
    // REDISTRIBUTES the sibling widths (pinPanelWidth) — persisting the raw patch would leave the
    // server with the unclamped value and stale siblings (Σ ≠ the developed length).
    const beforeWidths = new Map(run.panels.map((p) => [p.id, p.widthMm]));
    updatePanel(run.id, panel.id, patch);
    const freshRun = useDesignerStore.getState().scene.runs.find((r) => r.id === run.id);
    const freshPanel = freshRun?.panels.find((p) => p.id === panel.id);
    void persistPanel(run.id, freshPanel ?? { ...panel, ...patch });
    if (patch.widthMm !== undefined && freshRun) {
      freshRun.panels.forEach((p) => {
        if (p.id !== panel.id && beforeWidths.get(p.id) !== p.widthMm) {
          void persistPanel(run.id, p);
        }
      });
      void persistRun(freshRun);
    }
  };
  const show = (section: InspectorSection) => (sections ?? []).includes(section);

  const isEllipse = (draft.shapeKind ?? null) === 'ellipse';
  const isPolygon = (draft.shapeKind ?? null) === 'polygon';
  const isRect = !draft.shapeKind;
  const shapeKindValue = isPolygon
    ? 'polygon'
    : isEllipse
      ? (draft.heightMm ?? 0) === draft.widthMm
        ? 'round'
        : 'oval'
      : 'rect';
  // Switching shape resets the other shape fields so no orphaned data survives the
  // transition (a leftover polygon JSON or top-shape under a rectangle was the R1 crash).
  const clearedShapeFields = {
    shapePointsJson: null,
    topShape: null,
    topRightHeightMm: null,
    archRiseMm: null,
  } as const;

  const selectShapeKind = (k: 'rect' | 'round' | 'oval') =>
    commit(
      k === 'rect'
        ? { shapeKind: null, ...clearedShapeFields }
        : k === 'round'
          ? { shapeKind: 'ellipse', heightMm: draft.widthMm, ...clearedShapeFields }
          : { shapeKind: 'ellipse', ...clearedShapeFields },
    );

  const applyPolygonPreset = (sides: number) =>
    commit({
      shapeKind: 'polygon',
      shapePointsJson: serializePanelPolygonPoints(
        presetPolygonPoints(sides, draft.widthMm, draft.heightMm ?? run.heightMm),
      ),
      topShape: null,
      topRightHeightMm: null,
      archRiseMm: null,
    });

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
            className="inline-flex items-center gap-1 rounded border border-primary-500/40 px-2 py-1 text-xs text-primary-600 hover:bg-primary-50 dark:hover:bg-primary-950/30"
          >
            <Plus size={12} />
            {t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
          </button>
          <button
            type="button"
            onClick={handleDeletePanel}
            className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
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
                    ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-950/40 dark:text-primary-300'
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
        <>
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

          <Field
            label={`${t('GlassEnclosure.Designer.Panel.Height', { defaultValue: 'Panel yüksekliği' })} (mm)`}
          >
            <input
              type="number"
              min={100}
              value={draft.heightMm ?? ''}
              placeholder={t('GlassEnclosure.Designer.Panel.FullHeight', {
                defaultValue: 'Tam yükseklik (hat)',
              })}
              onChange={(e) =>
                setDraft({
                  ...draft,
                  heightMm: e.target.value === '' ? null : Number(e.target.value),
                })
              }
              onBlur={() => commit({ heightMm: draft.heightMm ?? null })}
              className={inputClass}
            />
          </Field>

          {canShape ? (
            <>
              <Field
                label={t('GlassEnclosure.Designer.Panel.Shape', { defaultValue: 'Panel şekli' })}
              >
                <div className="grid grid-cols-3 gap-1.5">
                  {(['rect', 'round', 'oval'] as const).map((k) => (
                    <button
                      key={k}
                      type="button"
                      onClick={() => selectShapeKind(k)}
                      className={`rounded border px-2 py-1.5 text-xs font-medium transition ${
                        shapeKindValue === k
                          ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-950/40 dark:text-primary-300'
                          : 'border-slate-300 text-slate-600 hover:border-slate-400 dark:border-slate-600 dark:text-slate-300'
                      }`}
                    >
                      {t(`GlassEnclosure.Designer.Panel.Kind.${k}` as never, {
                        defaultValue: { rect: 'Dikdörtgen', round: 'Yuvarlak', oval: 'Oval' }[k],
                      })}
                    </button>
                  ))}
                </div>
              </Field>

              <Field
                label={t('GlassEnclosure.Designer.Panel.Polygon', {
                  defaultValue: 'Çokgen (ön ayar)',
                })}
              >
                <div className="grid grid-cols-3 gap-1.5">
                  {([3, 5, 6] as const).map((sides) => (
                    <button
                      key={sides}
                      type="button"
                      onClick={() => applyPolygonPreset(sides)}
                      className={`rounded border px-2 py-1.5 text-xs font-medium transition ${
                        isPolygon
                          ? 'border-primary-400 text-primary-600 hover:bg-primary-50 dark:text-primary-300'
                          : 'border-slate-300 text-slate-600 hover:border-slate-400 dark:border-slate-600 dark:text-slate-300'
                      }`}
                    >
                      {t(`GlassEnclosure.Designer.Panel.Poly.${sides}` as never, {
                        defaultValue: { 3: 'Üçgen', 5: 'Beşgen', 6: 'Altıgen' }[sides],
                      })}
                    </button>
                  ))}
                </div>
              </Field>

              {isPolygon && (
                <PanelPolygonEditor
                  widthMm={draft.widthMm}
                  heightMm={draft.heightMm ?? run.heightMm}
                  pointsJson={draft.shapePointsJson}
                  onPreview={(json) =>
                    updatePanel(run.id, panel.id, { shapeKind: 'polygon', shapePointsJson: json })
                  }
                  onCommit={(json) => commit({ shapeKind: 'polygon', shapePointsJson: json })}
                />
              )}

              {isRect && (
                <>
                  <Field
                    label={t('GlassEnclosure.Designer.Panel.TopShape', {
                      defaultValue: 'Üst kenar',
                    })}
                  >
                    <div className="grid grid-cols-3 gap-1.5">
                      {(['flat', 'raked', 'arched'] as const).map((s) => (
                        <button
                          key={s}
                          type="button"
                          onClick={() => commit({ topShape: s })}
                          className={`rounded border px-2 py-1.5 text-xs font-medium transition ${
                            (draft.topShape ?? 'flat') === s
                              ? 'border-primary-500 bg-primary-50 text-primary-700 dark:bg-primary-950/40 dark:text-primary-300'
                              : 'border-slate-300 text-slate-600 hover:border-slate-400 dark:border-slate-600 dark:text-slate-300'
                          }`}
                        >
                          {t(`GlassEnclosure.Designer.Panel.Top.${s}` as never, {
                            defaultValue: { flat: 'Düz', raked: 'Eğimli', arched: 'Kemerli' }[s],
                          })}
                        </button>
                      ))}
                    </div>
                  </Field>

                  {draft.topShape === 'raked' && (
                    <Field
                      label={`${t('GlassEnclosure.Designer.Panel.TopRightHeight', { defaultValue: 'Sağ üst yükseklik' })} (mm)`}
                    >
                      <input
                        type="number"
                        min={100}
                        value={draft.topRightHeightMm ?? ''}
                        onChange={(e) =>
                          setDraft({
                            ...draft,
                            topRightHeightMm: e.target.value === '' ? null : Number(e.target.value),
                          })
                        }
                        onBlur={() => commit({ topRightHeightMm: draft.topRightHeightMm ?? null })}
                        className={inputClass}
                      />
                    </Field>
                  )}

                  {draft.topShape === 'arched' && (
                    <Field
                      label={`${t('GlassEnclosure.Designer.Panel.ArchRise', { defaultValue: 'Kemer yüksekliği' })} (mm)`}
                    >
                      <input
                        type="number"
                        min={0}
                        value={draft.archRiseMm ?? ''}
                        onChange={(e) =>
                          setDraft({
                            ...draft,
                            archRiseMm: e.target.value === '' ? null : Number(e.target.value),
                          })
                        }
                        onBlur={() => commit({ archRiseMm: draft.archRiseMm ?? null })}
                        className={inputClass}
                      />
                    </Field>
                  )}

                  <Field
                    label={t('GlassEnclosure.Designer.Panel.CornerRadii', {
                      defaultValue: 'Köşe ovalliği (mm)',
                    })}
                  >
                    <div className="grid grid-cols-4 gap-1.5">
                      {(['tl', 'tr', 'bl', 'br'] as const).map((k) => (
                        <input
                          key={k}
                          type="number"
                          min={0}
                          placeholder={k.toUpperCase()}
                          value={draft.cornerRadiiMm?.[k] ?? ''}
                          onChange={(e) =>
                            setDraft({
                              ...draft,
                              cornerRadiiMm: {
                                ...draft.cornerRadiiMm,
                                [k]:
                                  e.target.value === ''
                                    ? undefined
                                    : Math.max(0, Number(e.target.value)),
                              },
                            })
                          }
                          onBlur={() => commit({ cornerRadiiMm: draft.cornerRadiiMm })}
                          className={inputClass}
                        />
                      ))}
                    </div>
                  </Field>

                  <Field
                    label={t('GlassEnclosure.Designer.Panel.CornerNotch', {
                      defaultValue: 'Köşe girintisi (mm) — ovalliği geçersiz kılar',
                    })}
                  >
                    <div className="grid grid-cols-4 gap-1.5">
                      {(['tl', 'tr', 'bl', 'br'] as const).map((k) => (
                        <input
                          key={k}
                          type="number"
                          min={0}
                          placeholder={k.toUpperCase()}
                          value={draft.cornerNotchMm?.[k] ?? ''}
                          onChange={(e) =>
                            setDraft({
                              ...draft,
                              cornerNotchMm: {
                                ...draft.cornerNotchMm,
                                [k]:
                                  e.target.value === ''
                                    ? undefined
                                    : Math.max(0, Number(e.target.value)),
                              },
                            })
                          }
                          onBlur={() => commit({ cornerNotchMm: draft.cornerNotchMm })}
                          className={inputClass}
                        />
                      ))}
                    </div>
                  </Field>
                </>
              )}
            </>
          ) : (
            <p className="rounded border border-dashed border-slate-300 px-3 py-2 text-xs text-slate-500 dark:border-slate-600 dark:text-slate-400">
              {t('GlassEnclosure.Designer.Panel.ShapeSinglePanelOnly', {
                defaultValue: 'Şekil yalnızca tek panelli hatlarda verilebilir.',
              })}
            </p>
          )}
        </>
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
                {g.name} · {g.thicknessMm}mm
                {g.uValue > 0 ? ` · U${g.uValue}` : ''}
                {g.soundDb > 0 ? ` · ${g.soundDb}dB` : ''}
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
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

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
