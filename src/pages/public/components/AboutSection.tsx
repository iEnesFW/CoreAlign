import { useTranslation } from 'react-i18next';
import {
  History,
  Target,
  Compass,
  Award,
  Shield,
  Lock,
  Server,
  ShieldCheck,
  Boxes,
  Workflow,
  GitMerge,
  ShieldHalf,
  Sparkles,
  ArrowRight,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

const approachPrinciples = [
  {
    icon: GitMerge,
    color: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400',
    titleKey: 'a1Title',
    titleDefault: 'Tek doğruluk kaynağı',
    descKey: 'a1Desc',
    descDefault:
      'Teklif, üretim, stok ve muhasebe aynı veriyi paylaşır. Bir modülde yapılan değişiklik, kopyalama olmadan diğerlerine yansır.',
  },
  {
    icon: Boxes,
    color: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
    titleKey: 'a2Title',
    titleDefault: 'Modüler ama bütünleşik',
    descKey: 'a2Desc',
    descDefault:
      'İhtiyacınız olan modülle başlayın, büyüdükçe genişletin. Modüller ayrı ürünler değil; tek omurganın parçalarıdır.',
  },
  {
    icon: ShieldHalf,
    color: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-400',
    titleKey: 'a3Title',
    titleDefault: 'Kiracı bazlı izolasyon',
    descKey: 'a3Desc',
    descDefault:
      'Çok kiracılı mimaride her işletmenin verisi mantıksal olarak ayrılır; erişim ve denetim sınırları en baştan tasarlanmıştır.',
  },
  {
    icon: Workflow,
    color: 'bg-info-500/10 text-info-600 dark:bg-info-500/20 dark:text-info-300',
    titleKey: 'a4Title',
    titleDefault: 'Sizinle şekillenen ürün',
    descKey: 'a4Desc',
    descDefault:
      'Lansman öncesi bir ürünüz. Erken katılan işletmelerin gerçek iş akışları, yol haritamızın önceliklerini belirliyor.',
  },
] as const;

const backboneModules = [
  { labelKey: 'flowCad', labelDefault: 'CAD & CPQ' },
  { labelKey: 'flowMrp', labelDefault: 'MRP' },
  { labelKey: 'flowInventory', labelDefault: 'Envanter' },
  { labelKey: 'flowPurchasing', labelDefault: 'Satın Alma' },
  { labelKey: 'flowAccounting', labelDefault: 'Muhasebe' },
  { labelKey: 'flowBi', labelDefault: 'BI' },
] as const;

export const AboutSection = () => {
  const { t } = useTranslation();

  const mainItems = [
    {
      icon: <History className="h-6 w-6 text-primary-500" />,
      title: t('LandingPage.about.historyTitle'),
      text: t('LandingPage.about.historyText'),
    },
    {
      icon: <Target className="h-6 w-6 text-accent-500" />,
      title: t('LandingPage.about.missionTitle'),
      text: t('LandingPage.about.missionText'),
    },
    {
      icon: <Compass className="h-6 w-6 text-info-500" />,
      title: t('LandingPage.about.visionTitle'),
      text: t('LandingPage.about.visionText'),
    },
    {
      icon: <Award className="h-6 w-6 text-success-500" />,
      title: t('LandingPage.about.valuesTitle'),
      text: t('LandingPage.about.valuesText'),
    },
  ];

  const milestones = [
    {
      year: t('LandingPage.about.t1Year'),
      title: t('LandingPage.about.t1Title'),
      text: t('LandingPage.about.t1Text'),
    },
    {
      year: t('LandingPage.about.t2Year'),
      title: t('LandingPage.about.t2Title'),
      text: t('LandingPage.about.t2Text'),
    },
    {
      year: t('LandingPage.about.t3Year'),
      title: t('LandingPage.about.t3Title'),
      text: t('LandingPage.about.t3Text'),
    },
    {
      year: t('LandingPage.about.t4Year'),
      title: t('LandingPage.about.t4Title'),
      text: t('LandingPage.about.t4Text'),
    },
  ];

  const complianceItems = [
    {
      icon: <Shield className="h-6 w-6 text-primary-600 dark:text-primary-400" />,
      bg: 'bg-primary-500/10',
      title: t('LandingPage.about.c1Title'),
      desc: t('LandingPage.about.c1Desc'),
    },
    {
      icon: <Lock className="h-6 w-6 text-success-600 dark:text-success-400" />,
      bg: 'bg-success-500/10',
      title: t('LandingPage.about.c2Title'),
      desc: t('LandingPage.about.c2Desc'),
    },
    {
      icon: <Server className="h-6 w-6 text-primary-600 dark:text-primary-400" />,
      bg: 'bg-primary-500/10',
      title: t('LandingPage.about.c3Title'),
      desc: t('LandingPage.about.c3Desc'),
    },
    {
      icon: <ShieldCheck className="h-6 w-6 text-info-600 dark:text-info-400" />,
      bg: 'bg-info-500/10',
      title: t('LandingPage.about.c4Title'),
      desc: t('LandingPage.about.c4Desc'),
    },
  ];

  const renderBackboneVisual = () => (
    <figure className="relative overflow-hidden rounded-3xl border border-slate-200/60 bg-white/50 p-6 shadow-sm backdrop-blur-md dark:border-slate-800/60 dark:bg-slate-900/40 sm:p-8">
      <div
        aria-hidden="true"
        className="ca-grid-mask pointer-events-none absolute inset-0 opacity-60"
      />
      <figcaption className="relative mb-6 flex items-center gap-2 text-sm font-semibold text-slate-700 dark:text-slate-300">
        <Sparkles className="h-4 w-4 text-primary-500" aria-hidden="true" />
        {t('LandingPage.about.flowCaption', {
          defaultValue: 'Tek veri omurgası: aynı kayıt tüm modüllerde gerçek zamanlı akar',
        })}
      </figcaption>

      <svg
        viewBox="0 0 720 120"
        className="relative h-auto w-full text-primary-500"
        role="img"
        aria-label={t('LandingPage.about.flowAria', {
          defaultValue:
            'CAD ve teklif, MRP, envanter, satın alma, muhasebe ve BI modülleri arasında akan tek veri hattı',
        })}
      >
        <defs>
          <linearGradient id="ca-flow-line" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="currentColor" stopOpacity="0.15" />
            <stop offset="50%" stopColor="currentColor" stopOpacity="0.7" />
            <stop offset="100%" stopColor="currentColor" stopOpacity="0.15" />
          </linearGradient>
        </defs>

        <line
          x1="40"
          y1="60"
          x2="680"
          y2="60"
          stroke="url(#ca-flow-line)"
          strokeWidth="2.5"
          strokeLinecap="round"
        />

        {[0, 1, 2].map((i) => (
          <circle key={i} r="4" fill="currentColor">
            <animateMotion
              dur="3.2s"
              repeatCount="indefinite"
              begin={`${i * 1.05}s`}
              path="M40,60 L680,60"
              keyPoints="0;1"
              keyTimes="0;1"
              calcMode="linear"
            />
            <animate
              attributeName="opacity"
              values="0;1;1;0"
              keyTimes="0;0.1;0.9;1"
              dur="3.2s"
              repeatCount="indefinite"
              begin={`${i * 1.05}s`}
            />
          </circle>
        ))}

        {backboneModules.map((m, i) => {
          const cx = 40 + (i * 640) / (backboneModules.length - 1);
          return (
            <g key={m.labelKey}>
              <circle
                cx={cx}
                cy="60"
                r="14"
                className="fill-white dark:fill-slate-900"
                stroke="currentColor"
                strokeWidth="2"
              />
              <circle cx={cx} cy="60" r="4.5" fill="currentColor">
                <animate
                  attributeName="r"
                  values="4.5;6;4.5"
                  dur="2.4s"
                  repeatCount="indefinite"
                  begin={`${i * 0.3}s`}
                />
              </circle>
              <text
                x={cx}
                y="98"
                textAnchor="middle"
                className="fill-slate-600 text-[11px] font-semibold dark:fill-slate-400"
              >
                {t(`LandingPage.about.${m.labelKey}`, { defaultValue: m.labelDefault })}
              </text>
            </g>
          );
        })}
      </svg>
    </figure>
  );

  const renderApproach = () => (
    <div>
      <div className="mb-10 max-w-2xl">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-primary-500/30 bg-primary-500/10 px-3 py-1 text-xs font-semibold text-primary-600 backdrop-blur-md dark:text-primary-300">
          <Sparkles size={12} aria-hidden="true" />
          {t('LandingPage.about.approachBadge', { defaultValue: 'YAKLAŞIMIMIZ' })}
        </div>
        <h3 className="mb-4 text-2xl font-bold tracking-tight text-slate-900 dark:text-white md:text-3xl">
          {t('LandingPage.about.approachTitle', {
            defaultValue: 'Misyonumuz ve ürünü inşa etme biçimimiz',
          })}
        </h3>
        <p className="text-base text-slate-600 dark:text-slate-400">
          {t('LandingPage.about.approachSubtitle', {
            defaultValue:
              'CoreAlign, cam cephe tasarımından üretim planlamasına ve muhasebeye kadar tüm süreçleri tek bir veri omurgasında birleştirmek için tasarlandı. İşte bu omurgayı kurarken bağlı kaldığımız ilkeler.',
          })}
        </p>
      </div>

      {renderBackboneVisual()}

      <div className="ca-stagger mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2">
        {approachPrinciples.map((p) => {
          const Icon = p.icon;
          return (
            <div
              key={p.titleKey}
              className="flex gap-4 rounded-2xl border border-slate-200/60 bg-white/40 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-1 hover:border-primary-500/30 hover:shadow-md dark:border-slate-800/60 dark:bg-slate-900/40"
            >
              <div
                className={`inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl ${p.color}`}
              >
                <Icon size={20} aria-hidden="true" />
              </div>
              <div>
                <h4 className="mb-1.5 text-base font-bold text-slate-900 dark:text-white">
                  {t(`LandingPage.about.${p.titleKey}`, { defaultValue: p.titleDefault })}
                </h4>
                <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                  {t(`LandingPage.about.${p.descKey}`, { defaultValue: p.descDefault })}
                </p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );

  return (
    <Section containerClassName="space-y-24">
      <div className="animate-fade-up">
        <SectionHeader
          title={t('LandingPage.about.title')}
          subtitle={t('LandingPage.about.subtitle')}
        />
        <div className="ca-stagger grid grid-cols-1 gap-8 sm:grid-cols-2">
          {mainItems.map((item, index) => (
            <div
              key={index}
              className="rounded-2xl border border-slate-200/60 bg-white/40 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:translate-y-[-4px] hover:border-primary-500/30 hover:shadow-md dark:border-slate-800/60 dark:bg-slate-900/40"
            >
              <div className="mb-4 inline-flex rounded-xl bg-slate-100 p-3 dark:bg-slate-800/80">
                {item.icon}
              </div>
              <h3 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">
                {item.title}
              </h3>
              <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                {item.text}
              </p>
            </div>
          ))}
        </div>
      </div>

      {renderApproach()}

      <div>
        <div className="mb-12 max-w-2xl">
          <h3 className="mb-4 text-2xl font-bold tracking-tight text-slate-900 dark:text-white md:text-3xl">
            {t('LandingPage.about.timelineTitle')}
          </h3>
          <p className="text-base text-slate-600 dark:text-slate-400">
            {t('LandingPage.about.timelineSubtitle')}
          </p>
        </div>

        <div className="relative ml-4 space-y-8 border-l border-slate-200/80 py-2 md:ml-6 dark:border-slate-800/80">
          {milestones.map((milestone, index) => (
            <div key={index} className="relative pl-8 md:pl-10">
              <span
                aria-hidden="true"
                className="absolute -left-2 top-1.5 h-4 w-4 rounded-full border-4 border-slate-50 bg-primary-600 ring-4 ring-primary-500/10 dark:border-shell"
              >
                <span className="absolute inset-0 rounded-full bg-primary-500/40 animate-pulse-soft" />
              </span>
              <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-6 backdrop-blur-sm transition-all duration-300 hover:border-primary-500/30 dark:border-slate-800/50 dark:bg-slate-900/40">
                <span className="mb-2 inline-block rounded-full bg-primary-500/10 px-3 py-1 text-xs font-bold text-primary-600 dark:bg-primary-500/20 dark:text-primary-400">
                  {milestone.year}
                </span>
                <h4 className="text-lg font-bold text-slate-900 dark:text-white">
                  {milestone.title}
                </h4>
                <p className="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                  {milestone.text}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div>
        <div className="mb-12 max-w-2xl">
          <h3 className="mb-4 text-2xl font-bold tracking-tight text-slate-900 dark:text-white md:text-3xl">
            {t('LandingPage.about.complianceTitle')}
          </h3>
          <p className="text-base text-slate-600 dark:text-slate-400">
            {t('LandingPage.about.complianceSubtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {complianceItems.map((c, index) => (
            <div
              key={index}
              className="rounded-2xl border border-slate-200/50 bg-white/40 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:translate-y-[-4px] hover:border-primary-500/30 hover:shadow-md dark:border-slate-800/50 dark:bg-slate-900/40"
            >
              <div className={`mb-4 inline-flex rounded-xl ${c.bg} p-3`}>{c.icon}</div>
              <h4 className="mb-2 text-base font-bold leading-tight text-slate-900 dark:text-slate-100">
                {c.title}
              </h4>
              <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">{c.desc}</p>
            </div>
          ))}
        </div>

        <div className="mt-12 flex justify-start">
          <a
            href="#demo"
            className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40"
          >
            {t('LandingPage.about.cta', {
              defaultValue: 'Yaklaşımımızı yakından görün — demo planlayın',
            })}
            <ArrowRight size={16} aria-hidden="true" />
          </a>
        </div>
      </div>
    </Section>
  );
};
