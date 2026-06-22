import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { effectiveArcRadiusMm } from '../model/arcGeometry';
import type { GlassTypeDto, ProfileSystemDto } from '../model/glassEnclosure.types';

interface TechnicalSummaryProps {
  glassTypes: GlassTypeDto[];
  profileSystems: ProfileSystemDto[];
}

export function TechnicalSummary({ glassTypes, profileSystems }: TechnicalSummaryProps) {
  const { t } = useTranslation();
  const runs = useDesignerStore((s) => s.scene.runs);

  const summary = useMemo(() => {
    const glassMap = new Map(glassTypes.map((g) => [g.id, g]));
    const systemMap = new Map(profileSystems.map((s) => [s.id, s]));

    let totalAreaM2 = 0;
    let weightedUNumerator = 0;
    let weightedAreaForU = 0;
    let panelCount = 0;
    let totalWeightKg = 0;
    let maxThicknessMm = 0;
    const systemsUsed = new Set<string>();

    for (const run of runs) {
      if (run.profileSystemId) systemsUsed.add(run.profileSystemId);
      for (const panel of run.panels) {
        const areaM2 = (panel.widthMm * run.heightMm) / 1_000_000;
        totalAreaM2 += areaM2;
        panelCount += 1;
        const glass = glassMap.get(panel.glassTypeId);
        if (glass) {
          weightedUNumerator += areaM2 * glass.uValue;
          weightedAreaForU += areaM2;
          totalWeightKg += areaM2 * glass.weightKgPerM2;
          maxThicknessMm = Math.max(maxThicknessMm, glass.thicknessMm);
        }
      }
    }

    const weightedU = weightedAreaForU > 0 ? weightedUNumerator / weightedAreaForU : 0;
    const dbValues = runs.flatMap((r) =>
      r.panels
        .map((p) => glassMap.get(p.glassTypeId)?.soundDb)
        .filter((v): v is number => v !== undefined),
    );
    const avgDb = dbValues.length === 0 ? 0 : dbValues.reduce((a, b) => a + b, 0) / dbValues.length;
    const curvedRuns = runs
      .filter((r) => (r.geomArcRadiusMm ?? 0) > 0)
      .map((r) => ({
        label: r.label,
        radiusMm: effectiveArcRadiusMm(r.lengthMm, r.geomArcRadiusMm ?? 0),
        bent: r.arcGlassBent ?? false,
      }));

    return {
      panelCount,
      totalAreaM2,
      weightedU,
      avgDb,
      totalWeightKg,
      maxThicknessMm,
      curvedRuns,
      systemNames: Array.from(systemsUsed)
        .map((id) => systemMap.get(id)?.name)
        .filter((n): n is string => Boolean(n)),
    };
  }, [runs, glassTypes, profileSystems]);

  return (
    <section className="space-y-2">
      <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.TechnicalSummary')}
      </h3>
      <dl className="grid grid-cols-2 gap-2 text-xs">
        <Metric
          label={t('GlassEnclosure.Designer.PanelCountLabel')}
          value={summary.panelCount.toString()}
        />
        <Metric
          label={t('GlassEnclosure.Designer.TotalArea')}
          value={`${summary.totalAreaM2.toFixed(2)} m²`}
        />
        <Metric
          label={t('GlassEnclosure.Designer.WeightedU')}
          value={`${summary.weightedU.toFixed(2)} W/m²K`}
        />
        <Metric
          label={t('GlassEnclosure.Designer.AvgSoundDb')}
          value={`${summary.avgDb.toFixed(0)} dB`}
        />
        <Metric
          label={t('GlassEnclosure.Designer.TotalWeight')}
          value={`${summary.totalWeightKg.toFixed(0)} kg`}
        />
        <Metric
          label={t('GlassEnclosure.Designer.MaxThickness')}
          value={`${summary.maxThicknessMm} mm`}
        />
      </dl>
      {summary.curvedRuns.length > 0 && (
        <div className="rounded border border-warning-300 bg-warning-50 p-2 text-xs text-warning-800 dark:border-warning-700 dark:bg-warning-950/30 dark:text-warning-300">
          <span className="font-semibold">
            {t('GlassEnclosure.Designer.CurvedRunsTitle', { defaultValue: 'Kavisli hatlar' })}:
          </span>{' '}
          {summary.curvedRuns
            .map(
              (r) =>
                `${r.label} (R${r.radiusMm} mm · ${
                  r.bent
                    ? t('GlassEnclosure.Designer.Arc.ModeBent', { defaultValue: 'Bombeli cam' })
                    : t('GlassEnclosure.Designer.Arc.ModeFaceted', { defaultValue: 'Faseta' })
                })`,
            )
            .join(' · ')}
          <div className="mt-1 text-[10px] opacity-80">
            {t('GlassEnclosure.Designer.CurvedRunsHint', {
              defaultValue:
                'Hat uzunlukları açılım (gelişim) boyudur; kesim listesi ve m² doğrudur. Cam ve profiller üretimde kavise bükülmelidir.',
            })}
          </div>
        </div>
      )}
      {summary.systemNames.length > 0 && (
        <div className="text-xs text-slate-500 dark:text-slate-400">
          {summary.systemNames.join(' · ')}
        </div>
      )}
    </section>
  );
}

const Metric = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
    <dt className="truncate text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </dt>
    <dd className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">{value}</dd>
  </div>
);
