import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { calculateCost } from '../model/costCalculator';
import { mapBomSummaryToBreakdown } from '../model/bomPreviewBreakdown';
import { useBomPreviewQuery } from '../hooks/useBomPreviewQuery';
import type {
  ColorOptionDto,
  GlassEnclosureSettingsDto,
  GlassTypeDto,
  HardwareItemDto,
  HardwareKitDto,
  ProfileSystemDto,
} from '../model/glassEnclosure.types';

interface LiveCostPreviewProps {
  profileSystems: ProfileSystemDto[];
  glassTypes: GlassTypeDto[];
  colors: ColorOptionDto[];
  hardwareItems: HardwareItemDto[];
  hardwareKits: HardwareKitDto[];
  settings: GlassEnclosureSettingsDto | null;
  floorNumber: number | null;
  taxRatePercent?: number;
  projectId: string | null;
}

export function LiveCostPreview({
  profileSystems,
  glassTypes,
  colors,
  hardwareItems,
  hardwareKits,
  settings,
  floorNumber,
  taxRatePercent = 20,
  projectId,
}: LiveCostPreviewProps) {
  const { t, i18n } = useTranslation();
  const runs = useDesignerStore((s) => s.scene.runs);
  const revision = useDesignerStore((s) => s.historyIndex);

  // Backend BOM preview is the single source of truth; the local estimate is only a fallback while
  // the first preview loads (so there is no regression if the endpoint is slow or errors).
  const preview = useBomPreviewQuery(projectId, revision, runs.length > 0);

  const localBreakdown = useMemo(
    () =>
      calculateCost({
        scene: { runs },
        catalog: { profileSystems, glassTypes, colors, hardwareItems, hardwareKits },
        settings,
        floorNumber,
        // Fallback estimate honours the tenant's configured VAT (same source the backend uses).
        taxRatePercent: settings?.defaultTaxRatePercent ?? taxRatePercent,
      }),
    [
      runs,
      profileSystems,
      glassTypes,
      colors,
      hardwareItems,
      hardwareKits,
      settings,
      floorNumber,
      taxRatePercent,
    ],
  );

  const breakdown = preview.data ? mapBomSummaryToBreakdown(preview.data) : localBreakdown;

  const formatter = useMemo(
    () =>
      new Intl.NumberFormat(i18n.language, {
        style: 'currency',
        currency: breakdown.currency,
        maximumFractionDigits: 0,
      }),
    [i18n.language, breakdown.currency],
  );

  if (runs.length === 0) {
    return (
      <section className="text-xs text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.NoCostYet')}
      </section>
    );
  }

  return (
    <section className="space-y-2">
      <header className="flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.LiveCost')}
        </h3>
        <span className="rounded bg-success-500/10 px-1.5 py-0.5 text-[10px] font-medium text-success-700 dark:bg-success-500/20 dark:text-success-300">
          {t('GlassEnclosure.Designer.LivePreview')}
        </span>
      </header>

      <dl className="space-y-1 text-xs">
        <Row
          label={t('GlassEnclosure.Cost.Materials')}
          value={formatter.format(breakdown.materials)}
        />
        <Row label={t('GlassEnclosure.Cost.Glass')} value={formatter.format(breakdown.glass)} />
        <Row
          label={t('GlassEnclosure.Cost.Hardware')}
          value={formatter.format(breakdown.hardware)}
        />
        <Row
          label={t('GlassEnclosure.Cost.Waste')}
          value={formatter.format(breakdown.waste)}
          muted
        />
        <Row label={t('GlassEnclosure.Cost.Labor')} value={formatter.format(breakdown.labor)} />
        {breakdown.scaffolding > 0 && (
          <Row
            label={t('GlassEnclosure.Cost.Scaffolding')}
            value={formatter.format(breakdown.scaffolding)}
          />
        )}
        {breakdown.crane > 0 && (
          <Row label={t('GlassEnclosure.Cost.Crane')} value={formatter.format(breakdown.crane)} />
        )}
        {breakdown.transport > 0 && (
          <Row
            label={t('GlassEnclosure.Cost.Transport')}
            value={formatter.format(breakdown.transport)}
          />
        )}
        <Divider />
        <Row
          label={t('GlassEnclosure.Cost.BaseCost')}
          value={formatter.format(breakdown.totalBaseCost)}
          bold
        />
        <Row
          label={t('GlassEnclosure.Cost.Margin')}
          value={formatter.format(breakdown.margin)}
          muted
        />
        <Row
          label={t('GlassEnclosure.Cost.Tax')}
          value={formatter.format(breakdown.taxAmount)}
          muted
        />
        <Divider />
        <Row
          label={t('GlassEnclosure.Cost.GrandTotal')}
          value={formatter.format(breakdown.grandTotal)}
          accent
        />
      </dl>

      <p className="text-[10px] italic text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.LiveCostDisclaimer')}
      </p>
    </section>
  );
}

const Row = ({
  label,
  value,
  bold,
  accent,
  muted,
}: {
  label: string;
  value: string;
  bold?: boolean;
  accent?: boolean;
  muted?: boolean;
}) => (
  <div className="flex items-center justify-between gap-2">
    <dt
      className={`${muted ? 'text-slate-400 dark:text-slate-500' : 'text-slate-600 dark:text-slate-400'}`}
    >
      {label}
    </dt>
    <dd
      className={`font-mono ${
        accent
          ? 'text-base font-semibold text-success-700 dark:text-success-300'
          : bold
            ? 'font-semibold text-slate-900 dark:text-slate-100'
            : muted
              ? 'text-slate-500 dark:text-slate-400'
              : 'text-slate-800 dark:text-slate-200'
      }`}
    >
      {value}
    </dd>
  </div>
);

const Divider = () => <div className="my-1 border-t border-slate-200 dark:border-slate-700" />;
