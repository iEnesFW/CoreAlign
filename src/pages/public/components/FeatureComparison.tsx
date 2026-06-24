import { useTranslation } from 'react-i18next';
import {
  Check,
  X,
  ShieldAlert,
  GitCompare,
  Cloud,
  Server,
  FileSpreadsheet,
  Sparkles,
  ArrowRight,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

type ComparisonStatus = 'yes' | 'warn' | 'no';

type ComparisonCell = {
  text: string;
  status: ComparisonStatus;
};

type ComparisonRow = {
  aspect: string;
  corealign: ComparisonCell;
  legacy: ComparisonCell;
  excel: ComparisonCell;
};

export const FeatureComparison = () => {
  const { t } = useTranslation();

  const comparisons: ComparisonRow[] = [
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

  const tallyOf = (key: keyof Pick<ComparisonRow, 'corealign' | 'legacy' | 'excel'>) =>
    comparisons.reduce((count, row) => (row[key].status === 'yes' ? count + 1 : count), 0);

  const total = comparisons.length;

  const renderIcon = (status: ComparisonStatus) => {
    switch (status) {
      case 'yes':
        return (
          <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-success-500/10 text-success-600 ring-1 ring-success-500/20 dark:bg-success-500/20 dark:text-success-400 dark:ring-success-500/30">
            <Check size={14} strokeWidth={3} />
          </span>
        );
      case 'warn':
        return (
          <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-warning-500/10 text-warning-600 ring-1 ring-warning-500/20 dark:bg-warning-500/20 dark:text-warning-400 dark:ring-warning-500/30">
            <ShieldAlert size={14} />
          </span>
        );
      default:
        return (
          <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-danger-500/10 text-danger-600 ring-1 ring-danger-500/20 dark:bg-danger-500/20 dark:text-danger-400 dark:ring-danger-500/30">
            <X size={14} strokeWidth={3} />
          </span>
        );
    }
  };

  const renderConvergenceVisual = () => (
    <figure className="animate-fade-up mb-12 overflow-hidden rounded-3xl border border-slate-200 bg-white/40 p-6 shadow-sm backdrop-blur-md dark:border-slate-800 dark:bg-slate-900/40 sm:p-8">
      <svg
        viewBox="0 0 600 200"
        role="img"
        aria-hidden="true"
        className="h-auto w-full text-slate-400 dark:text-slate-600"
      >
        <defs>
          <linearGradient id="ca-flow-core" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor="rgb(99 102 241)" />
            <stop offset="100%" stopColor="rgb(168 85 247)" />
          </linearGradient>
          <path id="ca-flow-top" d="M150 50 C 280 50, 320 100, 430 100" fill="none" />
          <path id="ca-flow-mid" d="M150 100 C 270 100, 320 100, 430 100" fill="none" />
          <path id="ca-flow-bottom" d="M150 150 C 280 150, 320 100, 430 100" fill="none" />
        </defs>

        <use href="#ca-flow-top" stroke="currentColor" strokeWidth="2" strokeDasharray="4 6" />
        <use href="#ca-flow-mid" stroke="currentColor" strokeWidth="2" strokeDasharray="4 6" />
        <use href="#ca-flow-bottom" stroke="currentColor" strokeWidth="2" strokeDasharray="4 6" />

        <g className="text-warning-500 dark:text-warning-400">
          <rect
            x="70"
            y="34"
            width="80"
            height="32"
            rx="8"
            className="fill-warning-500/10"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <text
            x="110"
            y="54"
            textAnchor="middle"
            className="fill-warning-600 dark:fill-warning-400"
            fontSize="11"
            fontWeight="600"
          >
            ERP
          </text>
        </g>
        <g className="text-info-500 dark:text-info-400">
          <rect
            x="70"
            y="84"
            width="80"
            height="32"
            rx="8"
            className="fill-info-500/10"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <text
            x="110"
            y="104"
            textAnchor="middle"
            className="fill-info-600 dark:fill-info-400"
            fontSize="11"
            fontWeight="600"
          >
            MRP
          </text>
        </g>
        <g className="text-danger-500 dark:text-danger-400">
          <rect
            x="70"
            y="134"
            width="80"
            height="32"
            rx="8"
            className="fill-danger-500/10"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <text
            x="110"
            y="154"
            textAnchor="middle"
            className="fill-danger-600 dark:fill-danger-400"
            fontSize="11"
            fontWeight="600"
          >
            Excel
          </text>
        </g>

        <circle r="4" fill="rgb(99 102 241)">
          <animateMotion dur="2.6s" repeatCount="indefinite" rotate="auto">
            <mpath href="#ca-flow-top" />
          </animateMotion>
        </circle>
        <circle r="4" fill="rgb(99 102 241)">
          <animateMotion dur="2.6s" begin="0.9s" repeatCount="indefinite" rotate="auto">
            <mpath href="#ca-flow-mid" />
          </animateMotion>
        </circle>
        <circle r="4" fill="rgb(99 102 241)">
          <animateMotion dur="2.6s" begin="1.7s" repeatCount="indefinite" rotate="auto">
            <mpath href="#ca-flow-bottom" />
          </animateMotion>
        </circle>

        <circle cx="465" cy="100" r="46" fill="url(#ca-flow-core)" opacity="0.12">
          <animate attributeName="r" values="44;50;44" dur="2.4s" repeatCount="indefinite" />
        </circle>
        <circle cx="465" cy="100" r="34" fill="url(#ca-flow-core)" />
        <text
          x="465"
          y="96"
          textAnchor="middle"
          className="fill-white"
          fontSize="13"
          fontWeight="700"
        >
          Core
        </text>
        <text
          x="465"
          y="112"
          textAnchor="middle"
          className="fill-white"
          fontSize="13"
          fontWeight="700"
        >
          Align
        </text>
      </svg>
      <figcaption className="mt-3 text-center text-xs text-slate-500 dark:text-slate-400">
        {t('LandingPage.comparison.visualCaption', {
          defaultValue:
            'Dağınık tablolar ve birbiriyle konuşmayan eski sistemler yerine; CAD, üretim, stok ve muhasebe tek bir gerçeklik kaynağında birleşir.',
        })}
      </figcaption>
    </figure>
  );

  const scoreCards = [
    {
      icon: Cloud,
      label: t('LandingPage.comparison.colCorealign'),
      score: tallyOf('corealign'),
      tone: 'primary' as const,
      accent: t('LandingPage.comparison.scoreCorealign', {
        defaultValue: 'Uçtan uca tek platform',
      }),
    },
    {
      icon: Server,
      label: t('LandingPage.comparison.colLegacy'),
      score: tallyOf('legacy'),
      tone: 'warning' as const,
      accent: t('LandingPage.comparison.scoreLegacy', {
        defaultValue: 'Modüller arası boşluklar',
      }),
    },
    {
      icon: FileSpreadsheet,
      label: t('LandingPage.comparison.colExcel'),
      score: tallyOf('excel'),
      tone: 'danger' as const,
      accent: t('LandingPage.comparison.scoreExcel', {
        defaultValue: 'Kopuk, manuel, hataya açık',
      }),
    },
  ];

  const toneClasses: Record<'primary' | 'warning' | 'danger', string> = {
    primary:
      'border-primary-500/30 bg-primary-500/5 text-primary-600 dark:border-primary-500/40 dark:text-primary-300',
    warning:
      'border-warning-500/20 bg-warning-500/5 text-warning-600 dark:border-warning-500/30 dark:text-warning-400',
    danger:
      'border-danger-500/20 bg-danger-500/5 text-danger-600 dark:border-danger-500/30 dark:text-danger-400',
  };

  const renderScoreboard = () => (
    <div className="ca-stagger mb-10 grid grid-cols-1 gap-4 sm:grid-cols-3">
      {scoreCards.map((card) => {
        const Icon = card.icon;
        return (
          <div
            key={card.label}
            className={`flex flex-col gap-3 rounded-2xl border p-5 backdrop-blur-sm transition-transform duration-300 hover:-translate-y-0.5 ${toneClasses[card.tone]}`}
          >
            <div className="flex items-center gap-2.5">
              <span className="inline-flex h-9 w-9 items-center justify-center rounded-xl bg-white/60 dark:bg-slate-900/40">
                <Icon size={18} />
              </span>
              <h3 className="text-sm font-bold leading-tight">{card.label}</h3>
            </div>
            <div className="flex items-end gap-1.5">
              <span className="text-3xl font-extrabold tabular-nums">{card.score}</span>
              <span className="mb-1 text-sm font-medium text-slate-500 dark:text-slate-400">
                / {total}
              </span>
            </div>
            <div
              className="h-1.5 w-full overflow-hidden rounded-full bg-slate-500/10"
              role="presentation"
            >
              <div
                className={
                  card.tone === 'primary'
                    ? 'h-full rounded-full bg-primary-500 transition-all duration-700 ease-out'
                    : card.tone === 'warning'
                      ? 'h-full rounded-full bg-warning-500 transition-all duration-700 ease-out'
                      : 'h-full rounded-full bg-danger-500 transition-all duration-700 ease-out'
                }
                style={{ width: `${(card.score / total) * 100}%` }}
              />
            </div>
            <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
              {card.accent}
            </p>
          </div>
        );
      })}
    </div>
  );

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <GitCompare size={12} aria-hidden="true" />
            {t('LandingPage.comparison.badge', { defaultValue: 'MUKAYESE' })}
          </>
        }
        title={t('LandingPage.comparison.title')}
        subtitle={t('LandingPage.comparison.subtitle')}
      />

      {renderConvergenceVisual()}

      {renderScoreboard()}

      <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white/40 shadow-xl backdrop-blur-md dark:border-slate-800 dark:bg-slate-900/40">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[700px] border-collapse text-left">
            <caption className="sr-only">
              {t('LandingPage.comparison.tableCaption', {
                defaultValue:
                  'CoreAlign bulut platformunun geleneksel ERP ve elektronik tablo yöntemleriyle özellik bazlı karşılaştırması.',
              })}
            </caption>
            <thead>
              <tr className="border-b border-slate-200 bg-slate-500/5 dark:border-slate-800">
                <th
                  scope="col"
                  className="w-1/4 p-6 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
                >
                  {t('LandingPage.comparison.colAspect')}
                </th>
                <th
                  scope="col"
                  className="w-1/4 bg-primary-500/5 p-6 text-xs font-extrabold uppercase tracking-wider text-primary-700 dark:text-primary-300"
                >
                  <span className="inline-flex items-center gap-1.5">
                    <Sparkles size={13} aria-hidden="true" />
                    {t('LandingPage.comparison.colCorealign')}
                  </span>
                </th>
                <th
                  scope="col"
                  className="w-1/4 p-6 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
                >
                  {t('LandingPage.comparison.colLegacy')}
                </th>
                <th
                  scope="col"
                  className="w-1/4 p-6 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400"
                >
                  {t('LandingPage.comparison.colExcel')}
                </th>
              </tr>
            </thead>
            <tbody className="ca-stagger divide-y divide-slate-200 dark:divide-slate-800">
              {comparisons.map((row) => (
                <tr
                  key={row.aspect}
                  className="transition-colors duration-150 hover:bg-slate-500/5"
                >
                  <th
                    scope="row"
                    className="p-6 text-left text-xs font-bold text-slate-900 dark:text-white"
                  >
                    {row.aspect}
                  </th>
                  <td className="bg-primary-500/5 p-6">
                    <div className="flex items-start gap-2.5">
                      {renderIcon(row.corealign.status)}
                      <span className="text-xs font-semibold leading-relaxed text-primary-950 dark:text-primary-200">
                        {row.corealign.text}
                      </span>
                    </div>
                  </td>
                  <td className="p-6">
                    <div className="flex items-start gap-2.5">
                      {renderIcon(row.legacy.status)}
                      <span className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                        {row.legacy.text}
                      </span>
                    </div>
                  </td>
                  <td className="p-6">
                    <div className="flex items-start gap-2.5">
                      {renderIcon(row.excel.status)}
                      <span className="text-xs leading-relaxed text-slate-500 dark:text-slate-500">
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

      <div className="animate-fade-up mt-10 flex flex-col items-start gap-5 rounded-3xl border border-success-500/30 bg-success-500/5 p-7 backdrop-blur-md sm:flex-row sm:items-center dark:border-success-500/30 dark:bg-success-500/10">
        <span className="inline-flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-success-500/15 text-success-600 dark:text-success-400">
          <Sparkles size={22} />
        </span>
        <div className="flex-1">
          <h3 className="mb-1 text-base font-bold text-slate-900 dark:text-white">
            {t('LandingPage.comparison.summaryTitle', {
              defaultValue: 'Tek sistem, tek gerçek: kopuk araçlara veda edin',
            })}
          </h3>
          <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-300">
            {t('LandingPage.comparison.summaryDesc', {
              defaultValue:
                'CAD doğrulamadan teklife, üretim planından muhasebeye kadar her adım aynı verinin üzerinde çalışır. Manuel aktarım, mutabakat hatası ve modüller arası kopukluk ortadan kalkar.',
            })}
          </p>
        </div>
        <a
          href="#demo"
          className="inline-flex shrink-0 items-center gap-2 rounded-xl bg-primary-600 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40"
        >
          {t('LandingPage.comparison.summaryCta', { defaultValue: 'Farkı demoda görün' })}
          <ArrowRight size={16} aria-hidden="true" />
        </a>
      </div>
    </Section>
  );
};
