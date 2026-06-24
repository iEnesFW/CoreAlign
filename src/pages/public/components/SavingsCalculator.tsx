import { useState } from 'react';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Calculator,
  TrendingUp,
  Clock,
  Coins,
  Info,
  Recycle,
  Sparkles,
  ArrowDownRight,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

const OPTIMIZED_WASTE_RATE = 1.2;
const MATERIAL_COST_PER_M2 = 140;
const ADMIN_HOURS_PER_ORDER = 0.25;
const ESTIMATED_SETUP_COST = 35000;

export const SavingsCalculator = () => {
  const { t } = useTranslation();
  const [volume, setVolume] = useState(3000);
  const [waste, setWaste] = useState(15);
  const [orders, setOrders] = useState(300);

  const savingsRate = Math.max(0, waste - OPTIMIZED_WASTE_RATE);
  const annualVol = volume * 12;
  const annualWasteSavedM2 = annualVol * (savingsRate / 100);
  const annualSavingsEuro = Math.round(annualWasteSavedM2 * MATERIAL_COST_PER_M2);
  const hoursSavedPerMonth = Math.round(orders * ADMIN_HOURS_PER_ORDER);
  const roiMonths = Math.max(
    1,
    Math.min(12, Math.round(ESTIMATED_SETUP_COST / (annualSavingsEuro / 12 || 1))),
  );

  const optimizedHoursPerMonth = Math.max(0, Math.round(orders * ADMIN_HOURS_PER_ORDER * 0.25));
  const currentHoursPerMonth = optimizedHoursPerMonth + hoursSavedPerMonth;

  const formatNumber = (num: number) => {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  };

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Calculator size={12} aria-hidden="true" />
            {t('LandingPage.savings.badge', { defaultValue: 'ROI ANALİZİ' })}
          </>
        }
        title={t('LandingPage.savings.title')}
        subtitle={t('LandingPage.savings.subtitle')}
      />

      <div className="grid grid-cols-1 items-stretch gap-12 lg:grid-cols-12">
        {renderInputPanel()}
        {renderResultPanel()}
      </div>

      {renderChartPanel()}

      <p className="mt-8 max-w-3xl text-xs leading-relaxed text-slate-500 dark:text-slate-500">
        {t('LandingPage.savings.disclaimer', {
          defaultValue:
            'Bu rakamlar yalnızca açıklayıcı tahminlerdir; sektör ortalaması varsayımlarına dayanır ve taahhüt içermez. Gerçek sonuçlar üretim profilinize göre değişir.',
        })}
      </p>
    </Section>
  );

  function renderInputPanel() {
    return (
      <div className="space-y-8 rounded-3xl border border-slate-200 bg-white/40 p-8 backdrop-blur-sm lg:col-span-6 dark:border-slate-800 dark:bg-slate-900/40">
        <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
          {t('LandingPage.savings.desc')}
        </p>

        <div className="space-y-6 ca-stagger">
          <div className="space-y-2">
            <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
              <span>{t('LandingPage.savings.volLabel')}</span>
              <span className="text-primary-600 dark:text-primary-400">
                {formatNumber(volume)} m² / ay
              </span>
            </div>
            <input
              type="range"
              min="500"
              max="15000"
              step="100"
              value={volume}
              onChange={(e) => setVolume(Number(e.target.value))}
              aria-label={t('LandingPage.savings.volLabel')}
              className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-primary-600 dark:bg-slate-800 dark:accent-primary-500"
            />
          </div>

          <div className="space-y-2">
            <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
              <span>{t('LandingPage.savings.wasteLabel')}</span>
              <span className="text-primary-600 dark:text-primary-400">%{waste.toFixed(1)}</span>
            </div>
            <input
              type="range"
              min="5"
              max="25"
              step="0.5"
              value={waste}
              onChange={(e) => setWaste(Number(e.target.value))}
              aria-label={t('LandingPage.savings.wasteLabel')}
              className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-primary-600 dark:bg-slate-800 dark:accent-primary-500"
            />
          </div>

          <div className="space-y-2">
            <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
              <span>{t('LandingPage.savings.orderLabel')}</span>
              <span className="text-primary-600 dark:text-primary-400">{orders} adet / ay</span>
            </div>
            <input
              type="range"
              min="50"
              max="2000"
              step="50"
              value={orders}
              onChange={(e) => setOrders(Number(e.target.value))}
              aria-label={t('LandingPage.savings.orderLabel')}
              className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-primary-600 dark:bg-slate-800 dark:accent-primary-500"
            />
          </div>
        </div>

        <div className="flex gap-2 rounded-2xl border border-primary-500/10 bg-primary-500/5 p-4 text-[11px] leading-relaxed text-slate-600 dark:bg-primary-500/10 dark:text-slate-400">
          <Info size={16} className="shrink-0 text-primary-500" aria-hidden="true" />
          <span>{t('LandingPage.savings.compareText')}</span>
        </div>
      </div>
    );
  }

  function renderResultPanel() {
    return (
      <div className="flex flex-col justify-between rounded-3xl border border-slate-200 bg-white p-8 shadow-xl lg:col-span-6 dark:border-slate-800/80 dark:bg-slate-900/65">
        <div className="space-y-8 ca-stagger">
          <div className="flex items-center gap-4">
            <div className="rounded-2xl bg-success-500/10 p-3.5 text-success-600 dark:bg-success-500/20 dark:text-success-400">
              <Coins size={24} aria-hidden="true" />
            </div>
            <div>
              <span className="text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400">
                {t('LandingPage.savings.annualSaving')}
              </span>
              <h3 className="mt-1 text-3xl font-extrabold text-success-600 dark:text-success-400">
                €{formatNumber(annualSavingsEuro)}
              </h3>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="rounded-2xl bg-primary-500/10 p-3.5 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400">
              <Clock size={24} aria-hidden="true" />
            </div>
            <div>
              <span className="text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400">
                {t('LandingPage.savings.timeSaved')}
              </span>
              <h3 className="mt-1 text-2xl font-extrabold text-slate-900 dark:text-white">
                {t('LandingPage.savings.timeSavedVal', { count: hoursSavedPerMonth })}
              </h3>
            </div>
          </div>

          <div className="flex items-center gap-4">
            <div className="rounded-2xl bg-warning-500/10 p-3.5 text-warning-600 dark:bg-warning-500/20 dark:text-warning-400">
              <TrendingUp size={24} aria-hidden="true" />
            </div>
            <div>
              <span className="text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400">
                {t('LandingPage.savings.roiLabel')}
              </span>
              <h3 className="mt-1 text-2xl font-extrabold text-slate-900 dark:text-white">
                {t('LandingPage.savings.roiValue', { count: roiMonths })}
              </h3>
            </div>
          </div>
        </div>

        <div className="mt-8 border-t border-slate-100 pt-6 dark:border-slate-800">
          <div className="flex items-center justify-center gap-2 rounded-2xl bg-success-500/5 px-4 py-3 text-center dark:bg-success-500/10">
            <Sparkles
              size={14}
              className="text-success-600 dark:text-success-400"
              aria-hidden="true"
            />
            <span className="text-[11px] font-semibold leading-relaxed text-success-700 dark:text-success-300">
              {t('LandingPage.savings.optimizedNote', {
                defaultValue:
                  'CoreAlign nesting motoru fireyi %1.2 hedef bandına çeker — kazancın çoğu ilk yıl içinde geri döner.',
              })}
            </span>
          </div>
        </div>
      </div>
    );
  }

  function renderComparisonBars(
    title: string,
    icon: ReactNode,
    beforeLabel: string,
    afterLabel: string,
    beforeValue: number,
    afterValue: number,
    beforeText: string,
    afterText: string,
    deltaText: string,
  ) {
    const scaleBase = Math.max(beforeValue, afterValue, 1);
    const beforePct = Math.max(4, Math.round((beforeValue / scaleBase) * 100));
    const afterPct = Math.max(4, Math.round((afterValue / scaleBase) * 100));

    return (
      <div className="space-y-5 rounded-2xl border border-slate-200 bg-white/60 p-6 backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/50">
        <div className="flex items-center justify-between">
          <h3 className="flex items-center gap-2 text-sm font-bold text-slate-900 dark:text-white">
            {icon}
            {title}
          </h3>
          <span className="inline-flex items-center gap-1 rounded-full bg-success-500/10 px-2 py-0.5 text-[10px] font-bold text-success-600 dark:bg-success-500/20 dark:text-success-400">
            <ArrowDownRight size={11} aria-hidden="true" />
            {deltaText}
          </span>
        </div>

        <div className="space-y-3" aria-hidden="true">
          <div className="space-y-1">
            <div className="flex justify-between text-[11px] font-semibold text-slate-500 dark:text-slate-400">
              <span>{beforeLabel}</span>
              <span className="text-slate-700 dark:text-slate-200">{beforeText}</span>
            </div>
            <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
              <div
                className="h-full rounded-full bg-warning-500 transition-all duration-700 ease-out dark:bg-warning-400"
                style={{ width: `${beforePct}%` }}
              />
            </div>
          </div>

          <div className="space-y-1">
            <div className="flex justify-between text-[11px] font-semibold text-slate-500 dark:text-slate-400">
              <span>{afterLabel}</span>
              <span className="text-success-600 dark:text-success-400">{afterText}</span>
            </div>
            <div className="h-2.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
              <div
                className="h-full rounded-full bg-success-500 transition-all duration-700 ease-out dark:bg-success-400"
                style={{ width: `${afterPct}%` }}
              />
            </div>
          </div>
        </div>

        <p className="text-[11px] leading-relaxed text-slate-500 dark:text-slate-500">
          {t('LandingPage.savings.barCaption', {
            defaultValue: 'Açıklayıcı tahmin — girdilerinize göre anlık güncellenir.',
          })}
        </p>
      </div>
    );
  }

  function renderChartPanel() {
    return (
      <div className="mt-12 animate-fade-up">
        <div className="mb-6">
          <h3 className="text-xl font-bold text-slate-900 dark:text-white">
            {t('LandingPage.savings.chartTitle', {
              defaultValue: 'CoreAlign öncesi ve sonrası: tahmini etki',
            })}
          </h3>
          <p className="mt-2 max-w-2xl text-sm text-slate-600 dark:text-slate-400">
            {t('LandingPage.savings.chartSubtitle', {
              defaultValue:
                'Sürgüleri oynattıkça çubuklar anında güncellenir. Soldaki çubuk mevcut durumunuzu, sağdaki CoreAlign optimizasyonuyla beklenen seviyeyi temsil eder.',
            })}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {renderComparisonBars(
            t('LandingPage.savings.wasteChartTitle', { defaultValue: 'Malzeme Firesi' }),
            <Recycle
              size={16}
              className="text-warning-600 dark:text-warning-400"
              aria-hidden="true"
            />,
            t('LandingPage.savings.beforeLabel', { defaultValue: 'Mevcut süreç' }),
            t('LandingPage.savings.afterLabel', { defaultValue: 'CoreAlign ile' }),
            waste,
            OPTIMIZED_WASTE_RATE,
            `%${waste.toFixed(1)}`,
            `%${OPTIMIZED_WASTE_RATE.toFixed(1)}`,
            `-%${savingsRate.toFixed(1)}`,
          )}

          {renderComparisonBars(
            t('LandingPage.savings.timeChartTitle', { defaultValue: 'Aylık Operasyon Süresi' }),
            <Clock
              size={16}
              className="text-warning-600 dark:text-warning-400"
              aria-hidden="true"
            />,
            t('LandingPage.savings.beforeLabel', { defaultValue: 'Mevcut süreç' }),
            t('LandingPage.savings.afterLabel', { defaultValue: 'CoreAlign ile' }),
            currentHoursPerMonth,
            optimizedHoursPerMonth,
            t('LandingPage.savings.hoursShort', {
              count: currentHoursPerMonth,
              defaultValue: '{{count}} sa',
            }),
            t('LandingPage.savings.hoursShort', {
              count: optimizedHoursPerMonth,
              defaultValue: '{{count}} sa',
            }),
            t('LandingPage.savings.hoursDelta', {
              count: hoursSavedPerMonth,
              defaultValue: '-{{count}} sa',
            }),
          )}
        </div>
      </div>
    );
  }
};
