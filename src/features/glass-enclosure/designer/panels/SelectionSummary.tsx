import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import type {
  ColorOptionDto,
  GlassTypeDto,
  ProfileSystemDto,
} from '@/features/glass-enclosure/model/glassEnclosure.types';

interface SelectionSummaryProps {
  profileSystems: ProfileSystemDto[];
  glassTypes: GlassTypeDto[];
  colors: ColorOptionDto[];
}

const Row = ({ label, value }: { label: string; value: string }) => (
  <div className="flex items-center justify-between gap-2 py-1 text-xs">
    <span className="shrink-0 text-slate-500 dark:text-slate-400">{label}</span>
    <span className="truncate text-right font-medium text-slate-800 dark:text-slate-100">
      {value}
    </span>
  </div>
);

export const SelectionSummary = ({ profileSystems, glassTypes, colors }: SelectionSummaryProps) => {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);

  const run = runs.find((r) => r.id === selection.runId);

  if (!run) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-1 p-6 text-center text-xs text-slate-500 dark:text-slate-400">
        <span className="text-2xl" aria-hidden>
          🪟
        </span>
        <p>{t('GlassEnclosure.Designer.NoSelection', { defaultValue: 'No selection' })}</p>
      </div>
    );
  }

  if (selection.kind === 'panel') {
    const panel = run.panels.find((p) => p.id === selection.panelId);
    if (!panel) return null;
    const glass = glassTypes.find((g) => g.id === panel.glassTypeId);
    const hardware = [
      panel.hasHandle && t('GlassEnclosure.Field.HasHandle'),
      panel.hasLock && t('GlassEnclosure.Field.HasLock'),
      panel.hasBrushSeal && t('GlassEnclosure.Field.HasBrushSeal'),
    ].filter(Boolean) as string[];

    return (
      <div className="space-y-0.5 p-3">
        <h4 className="mb-1 truncate text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {run.label} · {t('GlassEnclosure.Designer.PanelInspector')} #{panel.panelIndex + 1}
        </h4>
        <Row label={`${t('GlassEnclosure.Field.Width')} (mm)`} value={String(panel.widthMm)} />
        <Row
          label={t('GlassEnclosure.Field.OpeningType')}
          value={t(`GlassEnclosure.Opening.${panel.openingType}` as never)}
        />
        <Row
          label={t('GlassEnclosure.Field.GlassType')}
          value={glass ? `${glass.name} · ${glass.thicknessMm} mm` : '—'}
        />
        <Row
          label={t('GlassEnclosure.Field.Hardware', { defaultValue: 'Hardware' })}
          value={hardware.length ? hardware.join(', ') : '—'}
        />
      </div>
    );
  }

  const system = profileSystems.find((s) => s.id === run.profileSystemId);
  const color = run.colorId ? colors.find((c) => c.id === run.colorId) : undefined;

  return (
    <div className="space-y-0.5 p-3">
      <h4 className="mb-1 truncate text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {run.label}
      </h4>
      <Row label={`${t('GlassEnclosure.Field.Length')} (mm)`} value={String(run.lengthMm)} />
      <Row label={`${t('GlassEnclosure.Field.Height')} (mm)`} value={String(run.heightMm)} />
      <Row label={t('GlassEnclosure.Designer.PanelCountLabel')} value={String(run.panels.length)} />
      <Row label={t('GlassEnclosure.Field.ProfileSystem')} value={system?.name ?? '—'} />
      <Row label={t('GlassEnclosure.Field.Color')} value={color?.name ?? '—'} />
    </div>
  );
};

export default SelectionSummary;
