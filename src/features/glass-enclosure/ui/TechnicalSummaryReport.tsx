import { useTranslation } from 'react-i18next';
import { AlertTriangle, Check, Thermometer, Volume2, Wind } from 'lucide-react';
import type { TechnicalSummaryDto } from '../model/engineering.types';

interface TechnicalSummaryReportProps {
  summary: TechnicalSummaryDto | null;
}

export function TechnicalSummaryReport({ summary }: TechnicalSummaryReportProps) {
  const { t } = useTranslation();

  if (!summary) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
        {t('GlassEnclosure.Engineering.NoSummary')}
      </div>
    );
  }

  return (
    <section className="space-y-4">
      <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
        {t('GlassEnclosure.Engineering.Title')}
      </h2>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <Card icon={<Wind size={18} />} title={t('GlassEnclosure.Engineering.WindLoad')}>
          {summary.windLoad ? (
            <>
              <Metric
                label={t('GlassEnclosure.Engineering.AppliedPressure')}
                value={`${summary.windLoad.appliedPressurePa.toFixed(0)} Pa`}
              />
              <Metric
                label={t('GlassEnclosure.Engineering.HeightFactor')}
                value={summary.windLoad.heightFactor.toFixed(2)}
              />
              <div className="mt-2 space-y-1">
                {summary.windLoad.panels.map((p) => (
                  <div key={p.panelId} className="flex items-center justify-between text-xs">
                    <span className="font-mono text-slate-600 dark:text-slate-400">
                      {p.currentThicknessMm}mm → {p.requiredMinThicknessMm}mm
                    </span>
                    {p.isSufficient ? (
                      <span className="text-success-600 dark:text-success-400">
                        <Check size={14} />
                      </span>
                    ) : (
                      <span className="text-danger-600 dark:text-danger-400">
                        <AlertTriangle size={14} />
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </>
          ) : (
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Engineering.WindZoneMissing')}
            </p>
          )}
        </Card>

        <Card icon={<Thermometer size={18} />} title={t('GlassEnclosure.Engineering.Thermal')}>
          <Metric
            label={t('GlassEnclosure.Engineering.WeightedU')}
            value={`${summary.thermal.weightedUValue.toFixed(2)} W/m²K`}
          />
          <Metric
            label={t('GlassEnclosure.Engineering.HeatLoss')}
            value={t('GlassEnclosure.Engineering.KwhPerYear', {
              value: summary.thermal.estimatedWinterHeatLossKwh.toFixed(0),
            })}
          />
        </Card>

        <Card icon={<Volume2 size={18} />} title={t('GlassEnclosure.Engineering.Acoustic')}>
          <Metric
            label={t('GlassEnclosure.Engineering.WeightedDb')}
            value={`${summary.thermal.weightedSoundDb.toFixed(0)} dB`}
          />
          <Metric
            label={t('GlassEnclosure.Engineering.DbReduction')}
            value={`-${summary.thermal.estimatedDbReductionVsOpen.toFixed(0)} dB`}
          />
        </Card>
      </div>

      <div className="rounded border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">
        <p className="mb-2 font-semibold uppercase tracking-wide">
          {t('GlassEnclosure.Engineering.Overall')}
        </p>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <Metric
            label={t('GlassEnclosure.Engineering.Runs')}
            value={summary.runCount.toString()}
          />
          <Metric
            label={t('GlassEnclosure.Engineering.Panels')}
            value={summary.panelCount.toString()}
          />
          <Metric
            label={t('GlassEnclosure.Engineering.TotalArea')}
            value={`${summary.totalAreaM2.toFixed(2)} m²`}
          />
          <Metric
            label={t('GlassEnclosure.Engineering.TotalWeight')}
            value={`${summary.totalWeightKg.toFixed(0)} kg`}
          />
        </div>
      </div>

      <p className="text-[10px] italic text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Engineering.Disclaimer')}
      </p>
    </section>
  );
}

const Card = ({
  icon,
  title,
  children,
}: {
  icon: React.ReactNode;
  title: string;
  children: React.ReactNode;
}) => (
  <div className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
    <div className="mb-2 flex items-center gap-2 text-slate-600 dark:text-slate-300">
      {icon}
      <h3 className="text-sm font-semibold uppercase tracking-wide">{title}</h3>
    </div>
    <div className="space-y-1">{children}</div>
  </div>
);

const Metric = ({ label, value }: { label: string; value: string }) => (
  <div className="flex items-center justify-between text-xs">
    <dt className="text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="font-mono text-slate-800 dark:text-slate-200">{value}</dd>
  </div>
);
