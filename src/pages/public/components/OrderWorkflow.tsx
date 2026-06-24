import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Network,
  PenTool,
  Layers,
  Receipt,
  CheckCircle2,
  ShoppingCart,
  Factory,
  Truck,
  ArrowRight,
  Zap,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

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
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Network size={12} />
            {t('LandingPage.workflow.badge', { defaultValue: 'ENTEGRE İŞ AKIŞI' })}
          </>
        }
        title={t('LandingPage.workflow.title')}
        subtitle={t('LandingPage.workflow.subtitle')}
      />

      <PipelineFlow t={t} />

      <div className="grid grid-cols-1 items-start gap-12 lg:grid-cols-12">
        <div className="space-y-4 lg:col-span-4">
          <h3 className="px-1 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
            {t('LandingPage.workflow.stepsHeading', {
              defaultValue: 'Akışı adım adım inceleyin',
            })}
          </h3>
          {steps.map((step, idx) => (
            <button
              key={idx}
              onClick={() => setActiveStep(idx)}
              aria-pressed={activeStep === idx}
              className={`flex w-full items-start gap-4 rounded-2xl border p-5 text-left transition-all duration-300 ${
                activeStep === idx
                  ? 'scale-[1.01] border-primary-500 bg-primary-500/5 shadow-md dark:border-primary-400 dark:bg-primary-500/10'
                  : 'border-slate-200 bg-white/40 hover:bg-slate-200/40 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:bg-slate-800/40'
              }`}
            >
              <div
                className={`rounded-xl p-2.5 transition-colors ${
                  activeStep === idx
                    ? 'bg-primary-600 text-white'
                    : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400'
                }`}
              >
                {stepIcons[idx]}
              </div>
              <div>
                <h4 className="text-xs font-bold text-slate-900 dark:text-white">{step.title}</h4>
                <p className="mt-1 text-[11px] leading-relaxed text-slate-500 dark:text-slate-400">
                  {step.desc}
                </p>
              </div>
              {activeStep === idx && (
                <ArrowRight
                  size={14}
                  className="ml-auto mt-0.5 shrink-0 text-primary-500 dark:text-primary-400"
                  aria-hidden="true"
                />
              )}
            </button>
          ))}

          <div className="mt-2 grid grid-cols-3 gap-2">
            <Stat
              value={t('LandingPage.workflow.statHandoffValue', { defaultValue: '0' })}
              label={t('LandingPage.workflow.statHandoffLabel', {
                defaultValue: 'Manuel veri girişi',
              })}
            />
            <Stat
              value={t('LandingPage.workflow.statTraceValue', { defaultValue: '100%' })}
              label={t('LandingPage.workflow.statTraceLabel', {
                defaultValue: 'İzlenebilir kayıt',
              })}
            />
            <Stat
              value={t('LandingPage.workflow.statSourceValue', { defaultValue: '1' })}
              label={t('LandingPage.workflow.statSourceLabel', {
                defaultValue: 'Tek doğru veri',
              })}
            />
          </div>
        </div>

        <div className="flex min-h-[380px] flex-col justify-between rounded-3xl border border-slate-200 bg-white p-8 shadow-xl lg:col-span-8 dark:border-slate-800/80 dark:bg-surface-deep/65">
          <div>
            <div className="mb-6 flex items-center justify-between border-b border-slate-100 pb-4 dark:border-slate-800">
              <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-400">
                {t('LandingPage.workflow.interactivePreview')}
              </span>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-success-500/10 px-2.5 py-1 text-[10px] font-bold text-success-600 dark:bg-success-500/20 dark:text-success-400">
                <span className="relative flex h-1.5 w-1.5" aria-hidden="true">
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-success-500 opacity-75" />
                  <span className="relative inline-flex h-1.5 w-1.5 rounded-full bg-success-500" />
                </span>
                {t('LandingPage.workflow.liveRunning', { defaultValue: 'CANLI ÇALIŞIYOR' })}
              </span>
            </div>

            {activeStep === 0 && (
              <div className="animate-fade-up space-y-6">
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
                      aria-label={t('LandingPage.solutions.simulator.width')}
                      onChange={(e) => setWidth(Number(e.target.value))}
                      className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-primary-600 dark:bg-slate-800 dark:accent-primary-500"
                    />
                    <span className="block text-right text-[10px] font-semibold text-primary-600 dark:text-primary-400">
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
                      aria-label={t('LandingPage.solutions.simulator.height')}
                      onChange={(e) => setHeight(Number(e.target.value))}
                      className="h-1.5 w-full cursor-pointer appearance-none rounded-lg bg-slate-200 accent-primary-600 dark:bg-slate-800 dark:accent-primary-500"
                    />
                    <span className="block text-right text-[10px] font-semibold text-primary-600 dark:text-primary-400">
                      {height} mm
                    </span>
                  </div>
                </div>

                <div className="ca-grid-mask relative flex flex-col items-center justify-center overflow-hidden rounded-2xl border border-dashed border-slate-200 bg-slate-50/50 p-6 dark:border-slate-800 dark:bg-slate-900/30">
                  <div
                    style={{
                      width: `${Math.min(180, width / 10)}px`,
                      height: `${Math.min(180, height / 10)}px`,
                    }}
                    className="relative flex min-h-[70px] min-w-[70px] flex-col items-center justify-center rounded-lg border-2 border-primary-500 bg-primary-500/10 shadow-sm transition-all duration-300"
                  >
                    <span className="pointer-events-none absolute inset-0 overflow-hidden rounded-lg">
                      <span className="absolute -left-full top-0 h-full w-1/2 -skew-x-12 bg-primary-300/30 blur-sm motion-safe:animate-[ca-shimmer_2.6s_linear_infinite] dark:bg-primary-400/20" />
                    </span>
                    <span className="text-[9px] font-bold text-primary-600 dark:text-primary-300">
                      {width} × {height}
                    </span>
                  </div>
                  <span className="mt-4 rounded-full bg-success-500/10 px-2.5 py-0.5 text-[9px] font-bold text-success-600 dark:bg-success-500/20 dark:text-success-400">
                    {t('LandingPage.workflow.simCADStatus')}
                  </span>
                </div>
              </div>
            )}

            {activeStep === 1 && (
              <div className="animate-fade-up space-y-6">
                <div className="flex items-center justify-between rounded-2xl border border-slate-100 bg-slate-50 p-4 dark:border-slate-800 dark:bg-slate-900/60">
                  <span className="text-[10px] font-bold text-slate-500 dark:text-slate-400">
                    {t('LandingPage.workflow.simMRPTitle')}
                  </span>
                  <span className="text-[10px] font-semibold text-slate-900 dark:text-white">
                    {t('LandingPage.workflow.simMRPTemp')}
                  </span>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-slate-500/5 p-6 dark:border-slate-800">
                  <div className="grid h-24 grid-cols-4 gap-2">
                    <div className="col-span-2 flex items-center justify-center rounded border border-primary-500/40 bg-primary-600/20 text-[9px] font-bold text-primary-500">
                      P1 ({width}x{height})
                    </div>
                    <div className="flex items-center justify-center rounded border border-primary-500/20 bg-primary-600/10 text-[9px] font-bold text-primary-400">
                      P2
                    </div>
                    <div className="flex items-center justify-center rounded border border-primary-500/20 bg-primary-600/10 text-[9px] font-bold text-primary-400">
                      P3
                    </div>
                    <div className="flex items-center justify-center rounded border border-primary-500/40 bg-primary-600/20 text-[9px] font-bold text-primary-500">
                      P4
                    </div>
                    <div className="col-span-3 flex items-center justify-center rounded border border-primary-500/20 bg-primary-600/10 text-[9px] font-bold text-primary-400">
                      P5
                    </div>
                  </div>
                  <span className="mt-4 block text-center text-[9px] font-bold text-primary-600 dark:text-primary-400">
                    {t('LandingPage.workflow.simMRPWaste')}
                  </span>
                </div>
              </div>
            )}

            {activeStep === 2 && (
              <div className="animate-fade-up space-y-4">
                <span className="mb-2 block text-[10px] font-bold text-slate-500 dark:text-slate-400">
                  {t('LandingPage.workflow.simFinTitle')}
                </span>
                <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
                  <table className="w-full text-left text-xs">
                    <thead>
                      <tr className="border-b border-slate-200 bg-slate-500/5 text-[10px] font-bold uppercase text-slate-400 dark:border-slate-800 dark:text-slate-400">
                        <th className="p-3">{t('LandingPage.workflow.simFinAccount')}</th>
                        <th className="p-3 text-right">{t('LandingPage.workflow.simFinDebit')}</th>
                        <th className="p-3 text-right">{t('LandingPage.workflow.simFinCredit')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                      <tr>
                        <td className="p-3 font-semibold text-slate-900 dark:text-white">
                          120.01.001 - AlumGlass A.Ş. B2B Cari
                        </td>
                        <td className="p-3 text-right font-bold text-success-600 dark:text-success-400">
                          €{totalReceivable.toLocaleString('tr-TR')}
                        </td>
                        <td className="p-3 text-right text-slate-400">-</td>
                      </tr>
                      <tr>
                        <td className="p-3 font-semibold text-slate-900 dark:text-white">
                          600.01.001 - Yurtiçi Cam Satış Gelirleri
                        </td>
                        <td className="p-3 text-right text-slate-400">-</td>
                        <td className="p-3 text-right font-bold text-primary-600 dark:text-primary-400">
                          €{baseRevenue.toLocaleString('tr-TR')}
                        </td>
                      </tr>
                      <tr>
                        <td className="p-3 font-semibold text-slate-900 dark:text-white">
                          391.01.018 - Hesaplanan KDV (%18)
                        </td>
                        <td className="p-3 text-right text-slate-400">-</td>
                        <td className="p-3 text-right font-bold text-primary-600 dark:text-primary-400">
                          €{vat.toLocaleString('tr-TR')}
                        </td>
                      </tr>
                    </tbody>
                    <tfoot>
                      <tr className="bg-slate-500/5 text-[10px] font-extrabold">
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
                <p className="text-[11px] leading-relaxed text-slate-500 dark:text-slate-400">
                  {t('LandingPage.workflow.simFinCaption', {
                    defaultValue:
                      'Borç ve alacak toplamları kuruşu kuruşuna eşleşir; yevmiye fişi CAD ölçülerinden türetilen gelir üzerinden otomatik dengelenir.',
                  })}
                </p>
              </div>
            )}
          </div>

          <div className="mt-8 flex items-center justify-center gap-2 border-t border-slate-100 pt-4 text-center dark:border-slate-800">
            <CheckCircle2
              size={12}
              className="text-success-600 dark:text-success-400"
              aria-hidden="true"
            />
            <span className="text-[10px] font-bold text-primary-600 dark:text-primary-400">
              {t('LandingPage.workflow.auditBalanced', {
                defaultValue: 'VERİ BÜTÜNLÜĞÜ OTO-DENETİMİ: DENGELİ (%100 EŞLEŞTİ)',
              })}
            </span>
          </div>
        </div>
      </div>
    </Section>
  );
};

type Translate = ReturnType<typeof useTranslation>['t'];

const PipelineFlow = ({ t }: { t: Translate }) => {
  const stages = [
    {
      icon: ShoppingCart,
      label: t('LandingPage.workflow.pipeOrder', { defaultValue: 'Sipariş' }),
      desc: t('LandingPage.workflow.pipeOrderDesc', {
        defaultValue: 'Bayi B2B portalından siparişi açar.',
      }),
    },
    {
      icon: PenTool,
      label: t('LandingPage.workflow.pipeDesign', { defaultValue: 'Tasarım' }),
      desc: t('LandingPage.workflow.pipeDesignDesc', {
        defaultValue: '3D CAD ile cephe çizilir, kurallar denetlenir.',
      }),
    },
    {
      icon: Receipt,
      label: t('LandingPage.workflow.pipeQuote', { defaultValue: 'Teklif' }),
      desc: t('LandingPage.workflow.pipeQuoteDesc', {
        defaultValue: 'CPQ fiyatı ve onayı anında üretilir.',
      }),
    },
    {
      icon: Factory,
      label: t('LandingPage.workflow.pipeManufacture', { defaultValue: 'Üretim' }),
      desc: t('LandingPage.workflow.pipeManufactureDesc', {
        defaultValue: 'MRP nesting ile üretim planına düşer.',
      }),
    },
    {
      icon: Truck,
      label: t('LandingPage.workflow.pipeDeliver', { defaultValue: 'Teslim' }),
      desc: t('LandingPage.workflow.pipeDeliverDesc', {
        defaultValue: 'Sevkiyat, fatura ve garanti otomatik açılır.',
      }),
    },
  ];

  return (
    <div className="mb-16">
      <div className="ca-glass relative overflow-hidden rounded-3xl border border-slate-200 p-6 shadow-sm sm:p-8 dark:border-slate-800">
        <div
          className="pointer-events-none absolute inset-0 opacity-60 [background:radial-gradient(600px_200px_at_50%_-20%,rgba(99,102,241,0.12),transparent_70%)]"
          aria-hidden="true"
        />

        <div
          className="relative mb-8 hidden h-16 items-center md:flex"
          role="img"
          aria-label={t('LandingPage.workflow.pipeFlowAria', {
            defaultValue: 'Siparişten teslime tek hat üzerinde akan veri',
          })}
        >
          <svg
            viewBox="0 0 1000 64"
            preserveAspectRatio="none"
            className="absolute inset-0 h-full w-full"
            aria-hidden="true"
          >
            <line
              x1="40"
              y1="32"
              x2="960"
              y2="32"
              className="stroke-slate-200 dark:stroke-slate-700"
              strokeWidth="2"
              strokeDasharray="2 8"
              strokeLinecap="round"
            />
            <line
              x1="40"
              y1="32"
              x2="960"
              y2="32"
              className="stroke-primary-500/70 dark:stroke-primary-400/70"
              strokeWidth="2.5"
              strokeLinecap="round"
              pathLength={100}
              strokeDasharray="30 70"
            >
              <animate
                attributeName="stroke-dashoffset"
                from="100"
                to="0"
                dur="3.2s"
                repeatCount="indefinite"
              />
            </line>
            {[0, 1, 2].map((i) => (
              <circle key={i} r="4" cy="32" className="fill-primary-500 dark:fill-primary-400">
                <animate
                  attributeName="cx"
                  from="40"
                  to="960"
                  dur="3.2s"
                  begin={`${i * 1.05}s`}
                  repeatCount="indefinite"
                />
                <animate
                  attributeName="opacity"
                  values="0;1;1;0"
                  dur="3.2s"
                  begin={`${i * 1.05}s`}
                  repeatCount="indefinite"
                />
              </circle>
            ))}
          </svg>

          <div className="relative z-10 flex w-full items-center justify-between">
            {stages.map((stage, idx) => {
              const Icon = stage.icon;
              return (
                <div
                  key={idx}
                  className="flex h-12 w-12 items-center justify-center rounded-2xl border border-primary-500/30 bg-white text-primary-600 shadow-sm motion-safe:animate-pulse-soft dark:bg-surface-deep dark:text-primary-300"
                  style={{ animationDelay: `${idx * 0.4}s` }}
                  aria-hidden="true"
                >
                  <Icon size={20} />
                </div>
              );
            })}
          </div>
        </div>

        <ol className="ca-stagger relative z-10 grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-5">
          {stages.map((stage, idx) => {
            const Icon = stage.icon;
            return (
              <li
                key={idx}
                className="group flex flex-col items-center rounded-2xl border border-slate-200/70 bg-white/50 p-4 text-center transition-colors hover:border-primary-500/40 dark:border-slate-800 dark:bg-slate-900/40"
              >
                <div className="mb-3 inline-flex h-9 w-9 items-center justify-center rounded-xl bg-primary-500/10 text-primary-600 transition-transform group-hover:scale-110 dark:bg-primary-500/20 dark:text-primary-300 md:hidden">
                  <Icon size={18} />
                </div>
                <span className="mb-1 inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-primary-600 dark:text-primary-400">
                  <Zap size={10} aria-hidden="true" />
                  {String(idx + 1).padStart(2, '0')}
                </span>
                <h3 className="text-sm font-bold text-slate-900 dark:text-white">{stage.label}</h3>
                <p className="mt-1 text-[11px] leading-relaxed text-slate-500 dark:text-slate-400">
                  {stage.desc}
                </p>
              </li>
            );
          })}
        </ol>
      </div>
    </div>
  );
};

const Stat = ({ value, label }: { value: string; label: string }) => (
  <div className="rounded-xl border border-slate-200 bg-white/40 p-3 text-center dark:border-slate-800 dark:bg-slate-900/40">
    <div className="text-lg font-extrabold text-primary-600 dark:text-primary-400">{value}</div>
    <div className="mt-0.5 text-[9px] font-semibold leading-tight text-slate-500 dark:text-slate-400">
      {label}
    </div>
  </div>
);
