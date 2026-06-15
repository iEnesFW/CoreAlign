import { useTranslation } from 'react-i18next';
import { Check, X, ShieldAlert, GitCompare } from 'lucide-react';

export const FeatureComparison = () => {
  const { t } = useTranslation();

  const comparisons = [
    {
      aspect: t('LandingPage.comparison.aspectCAD'),
      corealign: { text: t('LandingPage.comparison.aspectCADCorealign'), status: 'yes' },
      legacy: { text: t('LandingPage.comparison.aspectCADLegacy'), status: 'warn' },
      excel: { text: t('LandingPage.comparison.aspectCADExcel'), status: 'no' },
    },
    {
      aspect: t('LandingPage.comparison.aspectWaste'),
      corealign: { text: t('LandingPage.comparison.aspectWasteCorealign'), status: 'yes' },
      legacy: { text: t('LandingPage.comparison.aspectWasteLegacy'), status: 'warn' },
      excel: { text: t('LandingPage.comparison.aspectWasteExcel'), status: 'no' },
    },
    {
      aspect: t('LandingPage.comparison.aspectLedger'),
      corealign: { text: t('LandingPage.comparison.aspectLedgerCorealign'), status: 'yes' },
      legacy: { text: t('LandingPage.comparison.aspectLedgerLegacy'), status: 'warn' },
      excel: { text: t('LandingPage.comparison.aspectLedgerExcel'), status: 'no' },
    },
    {
      aspect: t('LandingPage.comparison.aspectSpeed'),
      corealign: { text: t('LandingPage.comparison.aspectSpeedCorealign'), status: 'yes' },
      legacy: { text: t('LandingPage.comparison.aspectSpeedLegacy'), status: 'warn' },
      excel: { text: t('LandingPage.comparison.aspectSpeedExcel'), status: 'no' },
    },
    {
      aspect: t('LandingPage.comparison.aspectMachine'),
      corealign: { text: t('LandingPage.comparison.aspectMachineCorealign'), status: 'yes' },
      legacy: { text: t('LandingPage.comparison.aspectMachineLegacy'), status: 'warn' },
      excel: { text: t('LandingPage.comparison.aspectMachineExcel'), status: 'no' },
    },
  ];

  const renderIcon = (status: string) => {
    switch (status) {
      case 'yes':
        return (
          <span className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
            <Check size={14} />
          </span>
        );
      case 'warn':
        return (
          <span className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-amber-500/10 text-amber-600 dark:bg-amber-500/20 dark:text-amber-400">
            <ShieldAlert size={14} />
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-rose-500/10 text-rose-600 dark:bg-rose-500/20 dark:text-rose-400">
            <X size={14} />
          </span>
        );
    }
  };

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24 bg-slate-100/50 dark:bg-slate-900/10">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <GitCompare size={12} />
            MUKAYESE
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.comparison.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.comparison.subtitle')}
          </p>
        </div>

        <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white/40 shadow-xl backdrop-blur-md dark:border-slate-800 dark:bg-slate-900/40">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[700px] border-collapse text-left">
              <thead>
                <tr className="border-b border-slate-200 dark:border-slate-800 bg-slate-500/5">
                  <th className="p-6 text-xs font-bold uppercase tracking-wider text-slate-550 dark:text-slate-400 w-1/4">
                    {t('LandingPage.comparison.colAspect')}
                  </th>
                  <th className="p-6 text-xs font-extrabold uppercase tracking-wider text-indigo-650 dark:text-indigo-300 w-1/4 bg-indigo-500/5">
                    {t('LandingPage.comparison.colCorealign')}
                  </th>
                  <th className="p-6 text-xs font-bold uppercase tracking-wider text-slate-550 dark:text-slate-400 w-1/4">
                    {t('LandingPage.comparison.colLegacy')}
                  </th>
                  <th className="p-6 text-xs font-bold uppercase tracking-wider text-slate-550 dark:text-slate-400 w-1/4">
                    {t('LandingPage.comparison.colExcel')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {comparisons.map((row, index) => (
                  <tr key={index} className="hover:bg-slate-500/5 transition-colors duration-150">
                    <td className="p-6 font-bold text-xs text-slate-900 dark:text-white">
                      {row.aspect}
                    </td>
                    <td className="p-6 bg-indigo-500/5">
                      <div className="flex items-start gap-2.5">
                        {renderIcon(row.corealign.status)}
                        <span className="text-xs font-semibold text-indigo-950 dark:text-indigo-200 leading-relaxed">
                          {row.corealign.text}
                        </span>
                      </div>
                    </td>
                    <td className="p-6">
                      <div className="flex items-start gap-2.5">
                        {renderIcon(row.legacy.status)}
                        <span className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
                          {row.legacy.text}
                        </span>
                      </div>
                    </td>
                    <td className="p-6">
                      <div className="flex items-start gap-2.5">
                        {renderIcon(row.excel.status)}
                        <span className="text-xs text-slate-500 dark:text-slate-500 leading-relaxed">
                          {row.excel.text}
                        </span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </section>
  );
};
