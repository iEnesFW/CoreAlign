import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { snapshotPanelDims, useRunEntityActions } from '../hooks/useDesignerEntityActions';
import { queueToast } from '@/shared/api/toastQueue';
import { arcFromCornerResize, isRealArc, minArcRadiusMm } from '../model/arcGeometry';
import { SHADOW_GAP_MM } from '../model/mountDepth';
import { RunArcSection } from './RunArcSection';
import { findAttachedWallIds } from '../model/wallAttachment';
import { buildRunFootprint } from '../scene/interaction/planCollision';
import { solidObstaclesExcept, transformAllowed } from '../scene/interaction/editCollisionGuard';
import type {
  ColorOptionDto,
  GlassTypeDto,
  InspectorSection,
  ProfileSystemDto,
} from '../model/glassEnclosure.types';

interface RunInspectorProps {
  profileSystems: ProfileSystemDto[];
  colors: ColorOptionDto[];
  glassTypes: GlassTypeDto[];
  sections: InspectorSection[];
}

export function RunInspector({ profileSystems, colors, glassTypes, sections }: RunInspectorProps) {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const updateRun = useDesignerStore((s) => s.updateRun);
  const setRunFrame = useDesignerStore((s) => s.setRunFrame);
  const { persistRunAndChangedPanels, deleteRun, rebalance } = useRunEntityActions();

  const run = useMemo(() => runs.find((r) => r.id === selection.runId), [runs, selection.runId]);
  const [draft, setDraft] = useState<typeof run>(run);
  const [panelCount, setPanelCount] = useState<number>(run?.panels.length || 1);
  const [trackedRun, setTrackedRun] = useState<typeof run>(run);
  if (run !== trackedRun) {
    setTrackedRun(run);
    setDraft(run);
    setPanelCount(run?.panels.length || 1);
  }

  if (!run || !draft) return null;

  // Geometry fields move the body; anything else (labels, colours, hardware flags) cannot collide.
  const GEOMETRY_KEYS = [
    'lengthMm',
    'heightMm',
    'originX',
    'originY',
    'rotationDeg',
    'geomZ',
    'geomArcRadiusMm',
    'geomArcSweepDeg',
  ] as const;

  const commit = (patch: Partial<typeof run>) => {
    // WHY the guard is here too: the transform toolbar already gated these SAME six fields, but the
    // inspector wrote them raw — so typing a neighbouring wall's X into the inspector drove the
    // glass into it with no rejection, while the toolbar next to it refused the identical edit.
    // The host wall is excluded: mounted glass legitimately sits inside its wall.
    if (GEOMETRY_KEYS.some((key) => patch[key] !== undefined)) {
      const candidate = { ...run, ...patch };
      const attached = findAttachedWallIds(run, useDesignerStore.getState().scene.walls ?? []);
      if (
        !transformAllowed(
          buildRunFootprint(run, 0, 0, run.rotationDeg),
          buildRunFootprint(candidate, 0, 0, candidate.rotationDeg),
          solidObstaclesExcept(new Set([run.id, ...attached])),
          t('GlassEnclosure.Designer.CollisionBlocked', {
            defaultValue: 'Bu değer başka bir nesneyle çakışıyor — uygulanmadı.',
          }),
        )
      ) {
        setDraft(run);
        return;
      }
    }
    // Persist the STORE's post-commit state, not the raw patch — the store clamps lengthMm
    // (withClampedRunLength) and re-fits panels, so persisting the raw value diverged local vs server.
    const before = snapshotPanelDims(run);
    updateRun(run.id, patch);
    void persistRunAndChangedPanels(run.id, before);
  };

  // Editing the Length of an ARC run changes the CHORD: keep the curl angle (sweep) and re-derive
  // the radius so chord = 2r·sin(sweep/2) stays true — otherwise the rendered end (from the stale
  // radius) no longer matches the logical span and the endpoints visibly drift apart. The server
  // rejects GeomArcRadiusMm < 100, so a too-small derived radius warns instead of silently
  // applying-then-reverting on the failed persist.
  const commitLength = (lengthMm: number) => {
    if (isRealArc(draft.geomArcRadiusMm, draft.geomArcSweepDeg)) {
      const scaled = arcFromCornerResize(lengthMm, draft.geomArcSweepDeg ?? 1);
      if (scaled.geomArcRadiusMm < 100) {
        queueToast({
          dedupeKey: 'glass-arc-radius-too-small',
          variant: 'warning',
          description: t('GlassEnclosure.Designer.Arc.RadiusTooSmall', {
            defaultValue:
              'Bu ölçüler {{r}} mm yarıçap üretiyor — minimum 100 mm. Kirişi büyütün veya oku küçültün.',
            r: scaled.geomArcRadiusMm,
          }),
        });
        setDraft(run);
        return;
      }
      commit({ lengthMm: scaled.lengthMm, geomArcRadiusMm: scaled.geomArcRadiusMm });
      return;
    }
    commit({ lengthMm });
  };
  // customColorHex is scene-local (persistRun/toRunInput never sends it) — update the store only
  // and let the debounced scene autosave persist it. Calling persistRun on every color-picker
  // input event floods the run endpoint (HTTP 429); this keeps the live preview without the flood.
  const commitLocal = (patch: Partial<typeof run>) => updateRun(run.id, patch);
  const defaultGlassTypeId = glassTypes.find((g) => g.isActive)?.id ?? glassTypes[0]?.id ?? '';
  const show = (section: InspectorSection) => (sections ?? []).includes(section);
  const minRadius = Math.max(100, minArcRadiusMm(draft.lengthMm));

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Designer.RunInspector')}
        </h3>
        <button
          type="button"
          onClick={() => void deleteRun(run.id)}
          className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
        >
          {t('GlassEnclosure.Designer.DeleteRun', { defaultValue: 'Delete run' })}
        </button>
      </header>

      {show('general') && (
        <>
          <Field label={t('GlassEnclosure.Field.Label')}>
            <input
              type="text"
              value={draft.label}
              onChange={(e) => setDraft({ ...draft, label: e.target.value })}
              onBlur={() => commit({ label: draft.label })}
              className={inputClass}
            />
          </Field>

          <Field label={t('GlassEnclosure.Field.ProfileSystem')}>
            <select
              value={draft.profileSystemId}
              onChange={(e) => {
                setDraft({ ...draft, profileSystemId: e.target.value });
                commit({ profileSystemId: e.target.value });
              }}
              className={inputClass}
            >
              {profileSystems.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
          </Field>

          <Field label={t('GlassEnclosure.Field.Color')}>
            <div className="grid grid-cols-6 gap-1.5">
              {colors.map((color) => (
                <button
                  key={color.id}
                  type="button"
                  onClick={() => commit({ colorId: color.id, customColorHex: null })}
                  title={color.name}
                  className={`h-7 w-full rounded border ${
                    draft.colorId === color.id && !draft.customColorHex
                      ? 'border-primary-500 ring-2 ring-primary-400/60'
                      : 'border-slate-300 dark:border-slate-600'
                  }`}
                  style={{ backgroundColor: color.hexColor }}
                />
              ))}
            </div>
            <div className="flex items-center gap-2 pt-2">
              <label
                title={t('GlassEnclosure.Field.ColorCustom', { defaultValue: 'Özel renk' })}
                className={`inline-flex h-7 w-9 cursor-pointer items-center justify-center overflow-hidden rounded border ${
                  draft.customColorHex
                    ? 'border-primary-500 ring-2 ring-primary-400/60'
                    : 'border-slate-300 dark:border-slate-600'
                }`}
              >
                <span className="sr-only">
                  {t('GlassEnclosure.Field.ColorCustom', { defaultValue: 'Özel renk' })}
                </span>
                <input
                  type="color"
                  value={
                    draft.customColorHex ??
                    colors.find((c) => c.id === draft.colorId)?.hexColor ??
                    '#cfd5d9'
                  }
                  onChange={(e) => commitLocal({ customColorHex: e.target.value })}
                  className="h-9 w-11 cursor-pointer border-0 bg-transparent p-0"
                />
              </label>
              <span className="text-[11px] text-slate-400">
                {t('GlassEnclosure.Field.ColorCustom', { defaultValue: 'Özel renk' })}
              </span>
            </div>
          </Field>

          <Field label={t('GlassEnclosure.Designer.PanelCountLabel')}>
            <div className="flex items-center gap-2">
              <input
                type="number"
                min={1}
                max={20}
                value={panelCount}
                onChange={(e) => setPanelCount(Number(e.target.value))}
                className={inputClass}
              />
              <button
                type="button"
                disabled={!defaultGlassTypeId}
                onClick={() => {
                  const firstPanel = run.panels[0];
                  const glassTypeId = firstPanel?.glassTypeId ?? defaultGlassTypeId;
                  const openingType = firstPanel?.openingType ?? 'Fixed';
                  // WHY(C3-full): rebalance now re-maps placed hardware onto the new panels by
                  // position (see useRunEntityActions.rebalance), so no confirm/loss warning is needed.
                  if (glassTypeId) void rebalance(run.id, panelCount, openingType, glassTypeId);
                }}
                className="shrink-0 rounded bg-primary-600 px-3 py-1 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
              >
                {t('GlassEnclosure.Designer.Rebalance')}
              </button>
            </div>
            <span className="text-[11px] text-slate-400">
              {t('GlassEnclosure.Designer.PanelCountHint', {
                defaultValue: 'Set how many glass panels this run splits into, then apply.',
              })}
            </span>
          </Field>
        </>
      )}

      {show('dimensions') && (
        <>
          <div className="grid grid-cols-2 gap-2">
            <Field label={`${t('GlassEnclosure.Field.Length')} (mm)`}>
              <input
                type="number"
                min={100}
                max={20000}
                value={draft.lengthMm}
                onChange={(e) => setDraft({ ...draft, lengthMm: Number(e.target.value) })}
                onBlur={() => commitLength(draft.lengthMm)}
                className={inputClass}
              />
            </Field>
            <Field label={`${t('GlassEnclosure.Field.Height')} (mm)`}>
              <input
                type="number"
                min={100}
                max={5000}
                value={draft.heightMm}
                onChange={(e) => setDraft({ ...draft, heightMm: Number(e.target.value) })}
                onBlur={() => commit({ heightMm: draft.heightMm })}
                className={inputClass}
              />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-2">
            <Field label={`${t('GlassEnclosure.Field.OriginX')} (mm)`}>
              <input
                type="number"
                value={draft.originX}
                onChange={(e) => setDraft({ ...draft, originX: Number(e.target.value) })}
                onBlur={() => commit({ originX: draft.originX })}
                className={inputClass}
              />
            </Field>
            <Field label={`${t('GlassEnclosure.Field.OriginY')} (mm)`}>
              <input
                type="number"
                value={draft.originY}
                onChange={(e) => setDraft({ ...draft, originY: Number(e.target.value) })}
                onBlur={() => commit({ originY: draft.originY })}
                className={inputClass}
              />
            </Field>
          </div>

          <Field label={`${t('GlassEnclosure.Field.Rotation')} (°)`}>
            <input
              type="range"
              min={-180}
              max={180}
              step={1}
              value={draft.rotationDeg}
              onChange={(e) => setDraft({ ...draft, rotationDeg: Number(e.target.value) })}
              // WHY four handlers: onChange only moves the local draft, so the body never rotates
              // until something commits. Mouse/touch were covered; a KEYBOARD user (arrow keys on a
              // focused slider) or anyone who dragged and then tabbed away saw the readout change
              // and the scene stay put — the edit was silently discarded on the next refetch.
              onPointerUp={() => commit({ rotationDeg: draft.rotationDeg })}
              onKeyUp={() => commit({ rotationDeg: draft.rotationDeg })}
              onBlur={() => commit({ rotationDeg: draft.rotationDeg })}
              className="w-full"
            />
            <div className="text-xs text-slate-500">{draft.rotationDeg}°</div>
          </Field>

          <RunArcSection
            draft={draft}
            panels={run.panels}
            minRadius={minRadius}
            committedArcRadiusMm={run.geomArcRadiusMm ?? 0}
            onDraftRadius={(value) => setDraft({ ...draft, geomArcRadiusMm: value })}
            commit={commit}
          />
        </>
      )}

      {show('hardware') && (
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={draft.hasTopDrip}
              onChange={(e) => commit({ hasTopDrip: e.target.checked })}
            />
            {t('GlassEnclosure.Field.TopDrip')}
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={draft.hasBottomThreshold}
              onChange={(e) => commit({ hasBottomThreshold: e.target.checked })}
            />
            {t('GlassEnclosure.Field.BottomThreshold')}
          </label>
        </div>
      )}

      {show('hardware') && (
        <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.Frame.Title', { defaultValue: 'Profil kenarları' })}
          </p>
          <div className="grid grid-cols-2 gap-1.5">
            {(['top', 'bottom', 'left', 'right'] as const).map((edge) => {
              const fe = run.frameEdges ?? { top: true, bottom: true, left: true, right: true };
              return (
                <label key={edge} className="flex items-center gap-2 text-xs">
                  <input
                    type="checkbox"
                    checked={fe[edge]}
                    onChange={(e) =>
                      setRunFrame(run.id, { frameEdges: { ...fe, [edge]: e.target.checked } })
                    }
                  />
                  {t(`GlassEnclosure.Designer.Frame.${edge}` as never, {
                    defaultValue: { top: 'Üst', bottom: 'Alt', left: 'Sol', right: 'Sağ' }[edge],
                  })}
                </label>
              );
            })}
          </div>
          <label className="flex items-center gap-2 text-xs">
            <input
              type="checkbox"
              checked={run.hasMullions !== false}
              onChange={(e) => setRunFrame(run.id, { hasMullions: e.target.checked })}
            />
            {t('GlassEnclosure.Designer.Frame.Mullions', {
              defaultValue: 'Ara dikmeler (kapalıyken camlar macunla birleşir)',
            })}
          </label>
          <label className="flex items-center justify-between gap-2 text-xs">
            <span>
              {t('GlassEnclosure.Designer.Frame.ShadowGap', { defaultValue: 'Gölge derzi (mm)' })}
            </span>
            <input
              type="number"
              min={0}
              max={50}
              step={1}
              value={run.mountShadowGapMm ?? SHADOW_GAP_MM}
              onChange={(e) => {
                const parsed = Number(e.target.value);
                if (!Number.isFinite(parsed)) return;
                setRunFrame(run.id, { mountShadowGapMm: Math.max(0, Math.min(50, parsed)) });
              }}
              className="w-20 rounded border border-slate-300 bg-white px-2 py-1 text-right text-xs text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
            />
          </label>
          <p className="text-[11px] leading-relaxed text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Designer.Frame.ShadowGapHint', {
              defaultValue:
                'Camın duvar yüzünden ne kadar içeride kalacağı. 0 = deliğe tam sıfır oturur.',
            })}
          </p>
        </div>
      )}

      {show('glass') && (
        <p className="rounded-md border border-dashed border-slate-300 p-3 text-xs text-slate-500 dark:border-slate-600 dark:text-slate-400">
          {t('GlassEnclosure.Designer.GlassPerPanelHint', {
            defaultValue:
              'Glass is chosen per panel. Select a panel in the layout to set its glass type.',
          })}
        </p>
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
