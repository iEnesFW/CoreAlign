import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Calculator, TrendingUp, Clock, Coins, Info } from 'lucide-react';

export const SavingsCalculator = () => {
  const { t } = useTranslation();
  const [volume, setVolume] = useState(3000);
  const [waste, setWaste] = useState(15);
  const [orders, setOrders] = useState(300);

  const savingsRate = Math.max(0, waste - 1.2);
  const annualVol = volume * 12;
  const annualWasteSavedM2 = annualVol * (savingsRate / 100);
  const annualSavingsEuro = Math.round(annualWasteSavedM2 * 140);
  const hoursSavedPerMonth = Math.round(orders * 0.25);
  const roiMonths = Math.max(1, Math.min(12, Math.round(35000 / (annualSavingsEuro / 12 || 1))));

  const formatNumber = (num: number) => {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  };

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Calculator size={12} />
            ROI ANALİZİ
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.savings.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.savings.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-12 lg:grid-cols-12 items-stretch">
          <div className="lg:col-span-6 space-y-8 rounded-3xl border border-slate-200 bg-white/40 p-8 backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/40">
            <p className="text-sm leading-relaxed text-slate-650 dark:text-slate-450">
              {t('LandingPage.savings.desc')}
            </p>

            <div className="space-y-6">
              <div className="space-y-2">
                <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
                  <span>{t('LandingPage.savings.volLabel')}</span>
                  <span className="text-indigo-600 dark:text-indigo-400">
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
                  className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-indigo-600 dark:bg-slate-800 dark:accent-indigo-500"
                />
              </div>

              <div className="space-y-2">
                <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
                  <span>{t('LandingPage.savings.wasteLabel')}</span>
                  <span className="text-indigo-600 dark:text-indigo-400">%{waste.toFixed(1)}</span>
                </div>
                <input
                  type="range"
                  min="5"
                  max="25"
                  step="0.5"
                  value={waste}
                  onChange={(e) => setWaste(Number(e.target.value))}
                  className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-indigo-600 dark:bg-slate-800 dark:accent-indigo-500"
                />
              </div>

              <div className="space-y-2">
                <div className="flex justify-between text-xs font-bold text-slate-900 dark:text-white">
                  <span>{t('LandingPage.savings.orderLabel')}</span>
                  <span className="text-indigo-600 dark:text-indigo-400">{orders} adet / ay</span>
                </div>
                <input
                  type="range"
                  min="50"
                  max="2000"
                  step="50"
                  value={orders}
                  onChange={(e) => setOrders(Number(e.target.value))}
                  className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-indigo-600 dark:bg-slate-800 dark:accent-indigo-500"
                />
              </div>
            </div>

            <div className="flex gap-2 rounded-2xl bg-indigo-500/5 p-4 text-[11px] leading-relaxed text-indigo-650 dark:bg-indigo-500/10 dark:text-indigo-405 border border-indigo-500/10">
              <Info size={16} className="shrink-0 text-indigo-550" />
              <span>{t('LandingPage.savings.compareText')}</span>
            </div>
          </div>

          <div className="lg:col-span-6 flex flex-col justify-between rounded-3xl border border-slate-200 bg-white p-8 shadow-xl dark:border-slate-800/80 dark:bg-[#0f1524]/65">
            <div className="space-y-8">
              <div className="flex items-center gap-4">
                <div className="rounded-2xl bg-emerald-500/10 p-3.5 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                  <Coins size={24} />
                </div>
                <div>
                  <span className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">
                    {t('LandingPage.savings.annualSaving')}
                  </span>
                  <h3 className="text-3xl font-extrabold text-emerald-600 dark:text-emerald-400 mt-1">
                    €{formatNumber(annualSavingsEuro)}
                  </h3>
                </div>
              </div>

              <div className="flex items-center gap-4">
                <div className="rounded-2xl bg-indigo-500/10 p-3.5 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                  <Clock size={24} />
                </div>
                <div>
                  <span className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">
                    {t('LandingPage.savings.timeSaved')}
                  </span>
                  <h3 className="text-2xl font-extrabold text-slate-900 dark:text-white mt-1">
                    {t('LandingPage.savings.timeSavedVal', { count: hoursSavedPerMonth })}
                  </h3>
                </div>
              </div>

              <div className="flex items-center gap-4">
                <div className="rounded-2xl bg-amber-500/10 p-3.5 text-amber-600 dark:bg-amber-500/20 dark:text-amber-400">
                  <TrendingUp size={24} />
                </div>
                <div>
                  <span className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">
                    {t('LandingPage.savings.roiLabel')}
                  </span>
                  <h3 className="text-2xl font-extrabold text-slate-900 dark:text-white mt-1">
                    {t('LandingPage.savings.roiValue', { count: roiMonths })}
                  </h3>
                </div>
              </div>
            </div>

            <div className="mt-8 border-t border-slate-100 pt-6 dark:border-slate-800 text-center">
              <span className="rounded-full bg-emerald-500/10 px-3 py-1 text-[10px] font-bold text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                PROVEN ROI SUCCESS INDEX: 9.8 / 10
              </span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
