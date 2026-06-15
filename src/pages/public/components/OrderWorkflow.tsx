import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Network, PenTool, Layers, Receipt, CheckCircle2 } from 'lucide-react';

export const OrderWorkflow = () => {
  const { t } = useTranslation();
  const [activeStep, setActiveStep] = useState(0);

  const [width, setWidth] = useState(1200);
  const [height, setHeight] = useState(1800);

  const stepIcons = [
    <PenTool size={18} key="0" />,
    <Layers size={18} key="1" />,
    <Receipt size={18} key="2" />,
  ];

  const steps = [
    {
      title: t('LandingPage.workflow.step1Title'),
      desc: t('LandingPage.workflow.step1Desc'),
    },
    {
      title: t('LandingPage.workflow.step2Title'),
      desc: t('LandingPage.workflow.step2Desc'),
    },
    {
      title: t('LandingPage.workflow.step3Title'),
      desc: t('LandingPage.workflow.step3Desc'),
    },
  ];

  const vat = Math.round(width * height * 0.0001 * 18);
  const baseRevenue = Math.round(width * height * 0.0001 * 100);
  const totalReceivable = baseRevenue + vat;

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Network size={12} />
            ENTEGRE İŞ AKIŞI
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.workflow.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.workflow.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-12 lg:grid-cols-12 items-start">
          <div className="lg:col-span-4 space-y-4">
            {steps.map((step, idx) => (
              <button
                key={idx}
                onClick={() => setActiveStep(idx)}
                className={`w-full text-left p-5 rounded-2xl border transition-all duration-300 flex items-start gap-4 ${
                  activeStep === idx
                    ? 'border-indigo-500 bg-indigo-500/5 dark:border-indigo-400 dark:bg-indigo-500/10 shadow-md scale-[1.01]'
                    : 'border-slate-200 bg-white/40 hover:bg-slate-200/40 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:bg-slate-800/40'
                }`}
              >
                <div
                  className={`rounded-xl p-2.5 transition-colors ${
                    activeStep === idx
                      ? 'bg-indigo-600 text-white'
                      : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400'
                  }`}
                >
                  {stepIcons[idx]}
                </div>
                <div>
                  <h4 className="font-bold text-xs text-slate-900 dark:text-white">{step.title}</h4>
                  <p className="mt-1 text-[11px] leading-relaxed text-slate-500 dark:text-slate-400">
                    {step.desc}
                  </p>
                </div>
              </button>
            ))}
          </div>

          <div className="lg:col-span-8 rounded-3xl border border-slate-200 bg-white p-8 shadow-xl dark:border-slate-800/80 dark:bg-[#0f1524]/65 min-h-[380px] flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between border-b border-slate-150 pb-4 mb-6 dark:border-slate-800">
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-400">
                  {t('LandingPage.workflow.interactivePreview')}
                </span>
                <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1 text-[10px] font-bold text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                  <CheckCircle2 size={10} />
                  LIVE RUNNING
                </span>
              </div>

              {activeStep === 0 && (
                <div className="space-y-6">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1.5">
                      <span className="text-[10px] font-bold text-slate-500 dark:text-slate-400">
                        {t('LandingPage.solutions.simulator.width')}
                      </span>
                      <input
                        type="range"
                        min="500"
                        max="2400"
                        step="50"
                        value={width}
                        onChange={(e) => setWidth(Number(e.target.value))}
                        className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-indigo-650 dark:bg-slate-800 dark:accent-indigo-500"
                      />
                      <span className="block text-right text-[10px] font-semibold text-indigo-600 dark:text-indigo-400">
                        {width} mm
                      </span>
                    </div>

                    <div className="space-y-1.5">
                      <span className="text-[10px] font-bold text-slate-500 dark:text-slate-400">
                        {t('LandingPage.solutions.simulator.height')}
                      </span>
                      <input
                        type="range"
                        min="500"
                        max="3000"
                        step="50"
                        value={height}
                        onChange={(e) => setHeight(Number(e.target.value))}
                        className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-indigo-650 dark:bg-slate-800 dark:accent-indigo-500"
                      />
                      <span className="block text-right text-[10px] font-semibold text-indigo-600 dark:text-indigo-400">
                        {height} mm
                      </span>
                    </div>
                  </div>

                  <div className="flex flex-col items-center justify-center border border-dashed border-slate-200 rounded-2xl bg-slate-50/50 p-6 dark:border-slate-800 dark:bg-slate-900/30">
                    <div
                      style={{
                        width: `${Math.min(180, width / 10)}px`,
                        height: `${Math.min(180, height / 10)}px`,
                      }}
                      className="border-2 border-indigo-650 bg-indigo-500/10 rounded-lg flex flex-col items-center justify-center relative shadow-sm transition-all duration-300 min-w-[70px] min-h-[70px]"
                    >
                      <span className="text-[9px] font-bold text-indigo-650 dark:text-indigo-300">
                        {width} × {height}
                      </span>
                    </div>
                    <span className="mt-4 rounded-full bg-emerald-500/10 px-2.5 py-0.5 text-[9px] font-bold text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                      {t('LandingPage.workflow.simCADStatus')}
                    </span>
                  </div>
                </div>
              )}

              {activeStep === 1 && (
                <div className="space-y-6">
                  <div className="flex justify-between items-center bg-slate-50 dark:bg-slate-900/60 p-4 rounded-2xl border border-slate-100 dark:border-slate-800">
                    <span className="text-[10px] font-bold text-slate-500 dark:text-slate-400">
                      {t('LandingPage.workflow.simMRPTitle')}
                    </span>
                    <span className="text-[10px] font-semibold text-slate-900 dark:text-white">
                      {t('LandingPage.workflow.simMRPTemp')}
                    </span>
                  </div>

                  <div className="border border-slate-200 dark:border-slate-800 rounded-2xl p-6 bg-slate-500/5">
                    <div className="grid grid-cols-4 gap-2 h-24">
                      <div className="col-span-2 bg-indigo-600/20 border border-indigo-500/40 rounded flex items-center justify-center text-[9px] font-bold text-indigo-500">
                        P1 ({width}x{height})
                      </div>
                      <div className="bg-indigo-600/10 border border-indigo-500/20 rounded flex items-center justify-center text-[9px] font-bold text-indigo-400">
                        P2
                      </div>
                      <div className="bg-indigo-600/10 border border-indigo-500/20 rounded flex items-center justify-center text-[9px] font-bold text-indigo-400">
                        P3
                      </div>
                      <div className="bg-indigo-600/20 border border-indigo-500/40 rounded flex items-center justify-center text-[9px] font-bold text-indigo-500">
                        P4
                      </div>
                      <div className="col-span-3 bg-indigo-600/10 border border-indigo-500/20 rounded flex items-center justify-center text-[9px] font-bold text-indigo-400">
                        P5
                      </div>
                    </div>
                    <span className="mt-4 block text-center text-[9px] font-bold text-indigo-600 dark:text-indigo-400">
                      {t('LandingPage.workflow.simMRPWaste')}
                    </span>
                  </div>
                </div>
              )}

              {activeStep === 2 && (
                <div className="space-y-4">
                  <span className="block text-[10px] font-bold text-slate-500 dark:text-slate-400 mb-2">
                    {t('LandingPage.workflow.simFinTitle')}
                  </span>
                  <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
                    <table className="w-full text-left text-xs">
                      <thead>
                        <tr className="bg-slate-500/5 border-b border-slate-200 dark:border-slate-800 text-[10px] font-bold uppercase text-slate-400 dark:text-slate-400">
                          <th className="p-3">{t('LandingPage.workflow.simFinAccount')}</th>
                          <th className="p-3 text-right">
                            {t('LandingPage.workflow.simFinDebit')}
                          </th>
                          <th className="p-3 text-right">
                            {t('LandingPage.workflow.simFinCredit')}
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                        <tr>
                          <td className="p-3 font-semibold text-slate-900 dark:text-white">
                            120.01.001 - AlumGlass A.Ş. B2B Cari
                          </td>
                          <td className="p-3 text-right text-emerald-600 dark:text-emerald-400 font-bold">
                            €{totalReceivable.toLocaleString('tr-TR')}
                          </td>
                          <td className="p-3 text-right text-slate-400">-</td>
                        </tr>
                        <tr>
                          <td className="p-3 font-semibold text-slate-900 dark:text-white">
                            600.01.001 - Yurtiçi Cam Satış Gelirleri
                          </td>
                          <td className="p-3 text-right text-slate-400">-</td>
                          <td className="p-3 text-right text-indigo-600 dark:text-indigo-400 font-bold">
                            €{baseRevenue.toLocaleString('tr-TR')}
                          </td>
                        </tr>
                        <tr>
                          <td className="p-3 font-semibold text-slate-900 dark:text-white">
                            391.01.018 - Hesaplanan KDV (%18)
                          </td>
                          <td className="p-3 text-right text-slate-400">-</td>
                          <td className="p-3 text-right text-indigo-600 dark:text-indigo-400 font-bold">
                            €{vat.toLocaleString('tr-TR')}
                          </td>
                        </tr>
                      </tbody>
                      <tfoot>
                        <tr className="bg-slate-500/5 font-extrabold text-[10px]">
                          <td className="p-3">{t('LandingPage.workflow.simFinTotal')}</td>
                          <td className="p-3 text-right">
                            €{totalReceivable.toLocaleString('tr-TR')}
                          </td>
                          <td className="p-3 text-right">
                            €{totalReceivable.toLocaleString('tr-TR')}
                          </td>
                        </tr>
                      </tfoot>
                    </table>
                  </div>
                </div>
              )}
            </div>

            <div className="mt-8 border-t border-slate-100 pt-4 dark:border-slate-800 text-center">
              <span className="text-[10px] font-bold text-indigo-650 dark:text-indigo-400">
                DATA INTEGRITY AUTO-AUDIT: BALANCED (MATCHED 100%)
              </span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
