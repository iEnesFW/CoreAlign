import { useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, Link } from 'react-router-dom';
import {
  ArrowRight,
  Users,
  Percent,
  Zap,
  ShieldCheck,
  PencilRuler,
  FileText,
  Factory,
  Truck,
  Calculator,
  PlayCircle,
  Sparkles,
} from 'lucide-react';
import { useTheme } from '@/app/providers/themeContext';
import { useSeo } from '@/shared/lib/seo';
import { LandingNav } from './components/LandingNav';
import { HeroCinematic } from './components/HeroCinematic';
import { SiteFooter } from './components/SiteFooter';
import { AboutSection } from './components/AboutSection';
import { SolutionsSection } from './components/SolutionsSection';
import { ArticlesSection } from './components/ArticlesSection';
import { ContactSection } from './components/ContactSection';
import { MigrationSection } from './components/MigrationSection';
import { FaqSection } from './components/FaqSection';
import { ModulesShowcase } from './components/ModulesShowcase';
import { SecurityHub } from './components/SecurityHub';
import { SavingsCalculator } from './components/SavingsCalculator';
import { IntegrationsGrid } from './components/IntegrationsGrid';
import { TestimonialsSection } from './components/TestimonialsSection';
import { DemoScheduler } from './components/DemoScheduler';
import { FeatureComparison } from './components/FeatureComparison';
import { OrderWorkflow } from './components/OrderWorkflow';
import { PricingSection } from './components/PricingSection';

const HERO_FLOW = [
  { icon: PencilRuler, key: 'design' },
  { icon: FileText, key: 'quote' },
  { icon: Factory, key: 'produce' },
  { icon: Truck, key: 'ship' },
  { icon: Calculator, key: 'account' },
];

const HERO_METRICS = [
  { icon: Users, key: 'activeDealers', tone: 'text-primary-600 dark:text-primary-300' },
  { icon: Percent, key: 'wasteReduction', tone: 'text-success-600 dark:text-success-300' },
  { icon: Zap, key: 'orderSpeed', tone: 'text-warning-600 dark:text-warning-300' },
  { icon: ShieldCheck, key: 'compliance', tone: 'text-accent-600 dark:text-accent-300' },
] as const;

const HeroSection = () => {
  const { t } = useTranslation();
  return (
    <section className="relative w-full px-4 pb-12 pt-8 sm:px-8 sm:pt-12 lg:px-12 2xl:px-20">
      <div className="mx-auto max-w-3xl text-center">
        <div className="animate-fade-up mb-6 inline-flex max-w-fit items-center gap-2 rounded-full border border-primary-500/25 bg-primary-500/[0.07] px-3.5 py-1.5 text-xs font-semibold text-primary-600 backdrop-blur-md dark:border-primary-400/20 dark:bg-primary-400/10 dark:text-primary-300">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary-400 opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-primary-500" />
          </span>
          {t('LandingPage.hero.badge')}
        </div>
        <h1 className="animate-fade-up text-balance text-4xl font-bold leading-[1.05] tracking-tight text-slate-900 sm:text-5xl lg:text-6xl dark:text-white">
          {t('LandingPage.hero.title')}
        </h1>
        <p className="animate-fade-up mx-auto mt-6 max-w-2xl text-pretty text-base leading-relaxed text-slate-600 md:text-lg dark:text-slate-400">
          {t('LandingPage.hero.subtitle')}
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <Link
            to="/solutions"
            className="group inline-flex items-center gap-2 rounded-xl bg-gradient-to-r from-primary-600 to-primary-500 px-6 py-3 font-semibold text-white shadow-lg shadow-primary-500/30 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-xl hover:shadow-primary-500/40"
          >
            {t('LandingPage.hero.cta')}
            <ArrowRight
              size={16}
              className="transition-transform duration-300 group-hover:translate-x-1"
            />
          </Link>
          <a
            href="#demo"
            className="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white/60 px-6 py-3 font-semibold text-slate-700 backdrop-blur-sm transition hover:border-primary-500 hover:text-primary-600 dark:border-slate-700 dark:bg-white/5 dark:text-slate-200 dark:hover:border-primary-400 dark:hover:text-primary-300"
          >
            <PlayCircle size={16} />
            {t('LandingPage.hero.ctaSecondary')}
          </a>
        </div>
        <p className="mt-5 text-xs font-medium text-slate-500 dark:text-slate-400">
          {t('LandingPage.hero.trust')}
        </p>
      </div>

      <div className="mx-auto mt-16 w-full max-w-6xl">
        <div className="ca-stagger grid grid-cols-2 gap-3 md:grid-cols-4">
          {HERO_METRICS.map((metric) => {
            const Icon = metric.icon;
            return (
              <div
                key={metric.key}
                className="ca-panel ca-card-hover flex flex-col rounded-2xl p-5"
              >
                <Icon size={20} className={`mb-3 ${metric.tone}`} />
                <span className="ca-display text-2xl font-extrabold tracking-tight text-slate-900 dark:text-white">
                  {t(`LandingPage.hero.metrics.${metric.key}Desc`)}
                </span>
                <span className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                  {t(`LandingPage.hero.metrics.${metric.key}`)}
                </span>
              </div>
            );
          })}
        </div>

        <div className="ca-panel relative mt-6 overflow-hidden rounded-2xl p-5 sm:p-6">
          <div className="mb-4 flex items-center gap-2 text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500 dark:text-slate-400">
            <Sparkles size={13} className="text-primary-500 dark:text-primary-300" />
            {t('LandingPage.hero.flowTitle')}
          </div>
          <div className="relative flex flex-wrap items-center justify-between gap-y-4">
            <div
              aria-hidden="true"
              className="pointer-events-none absolute left-6 right-6 top-5 hidden h-px overflow-hidden bg-slate-200 sm:block dark:bg-slate-700"
            >
              <span className="absolute top-0 h-px w-1/3 animate-flow bg-gradient-to-r from-transparent via-primary-500 to-transparent" />
            </div>
            {HERO_FLOW.map((step, i) => {
              const Icon = step.icon;
              return (
                <div
                  key={step.key}
                  className="relative z-10 flex flex-1 flex-col items-center gap-2"
                >
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl border border-primary-500/15 bg-gradient-to-br from-primary-500/15 to-primary-500/5 text-primary-600 dark:text-primary-300">
                    <Icon size={17} />
                  </span>
                  <span className="text-center text-[11px] font-semibold text-slate-700 dark:text-slate-200">
                    {t(`LandingPage.hero.flow.${step.key}`)}
                  </span>
                  <span className="absolute -top-1 right-2 text-[9px] font-bold text-slate-300 dark:text-slate-600">
                    0{i + 1}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
};

const LANDING_SEO: Record<string, { title: string; description: string }> = {
  '/': {
    title: 'CoreAlign — Cam & İmalat için Bulut ERP',
    description:
      'Cam kabin/cephe tasarımından üretim planlamaya, stok ve muhasebeye kadar tüm süreçleri tek platformda birleştiren çok-kiracılı bulut ERP.',
  },
  '/solutions': {
    title: 'Çözümler — CoreAlign Bulut ERP',
    description:
      'Cam kabin CPQ, 3D tasarım, MRP üretim planlama ve canlı maliyet simülasyonu — CoreAlign çözümlerini keşfedin.',
  },
  '/about': {
    title: 'Hakkımızda — CoreAlign',
    description: 'CoreAlign’in misyonu, yaklaşımı ve güvenlik/uyumluluk taahhüdü.',
  },
  '/articles': {
    title: 'Kaynaklar & Blog — CoreAlign',
    description: 'Cam imalatı, ERP ve dijital dönüşüm üzerine içerikler ve rehberler.',
  },
  '/contact': {
    title: 'İletişim — CoreAlign',
    description: 'Demo planlayın veya CoreAlign ekibiyle iletişime geçin.',
  },
};

const LANDING_SEO_EN: Record<string, { title: string; description: string }> = {
  '/': {
    title: 'CoreAlign — Cloud ERP for Glass & Manufacturing',
    description:
      'Multi-tenant cloud ERP unifying glass enclosure/façade design (3D CAD + CPQ), production planning (MRP), inventory and accounting on one platform.',
  },
  '/solutions': {
    title: 'Solutions — CoreAlign Cloud ERP',
    description:
      'Glass enclosure CPQ, 3D design, MRP production planning and live cost simulation — explore CoreAlign solutions.',
  },
  '/about': {
    title: 'About — CoreAlign',
    description: 'CoreAlign mission, approach, and security & compliance commitment.',
  },
  '/articles': {
    title: 'Resources & Blog — CoreAlign',
    description: 'Articles and guides on glass manufacturing, ERP, and digital transformation.',
  },
  '/contact': {
    title: 'Contact — CoreAlign',
    description: 'Schedule a demo or get in touch with the CoreAlign team.',
  },
};

const SITE = 'https://corealign.com';

export const LandingPage = () => {
  const { theme, toggleTheme } = useTheme();
  const { i18n } = useTranslation();
  const location = useLocation();
  const rawPath = location.pathname;
  const isEn = rawPath === '/en' || rawPath.startsWith('/en/');
  const pathname = isEn ? rawPath.replace(/^\/en/, '') || '/' : rawPath;

  // Base color the cinematic hero fades down into (matches the artifact's own
  // background). RGB triplet so the transition fades to a transparent version
  // of *this* color instead of transparent-black (which muddies the blend).
  const animRgb = theme === 'light' ? '238, 242, 250' : '4, 5, 11';

  useEffect(() => {
    if (isEn && i18n.language !== 'en') {
      void i18n.changeLanguage('en');
    }
  }, [isEn, i18n]);

  const seoMap = isEn ? LANDING_SEO_EN : LANDING_SEO;
  const trHref = `${SITE}${pathname === '/' ? '/' : pathname}`;
  const enHref = `${SITE}/en${pathname === '/' ? '' : pathname}`;
  const alternates = useMemo(
    () => [
      { lang: 'tr', href: trHref },
      { lang: 'en', href: enHref },
      { lang: 'x-default', href: trHref },
    ],
    [trHref, enHref],
  );
  useSeo({
    ...(seoMap[pathname] ?? seoMap['/']),
    canonical: isEn ? enHref : trHref,
    ogLocale: isEn ? 'en_US' : 'tr_TR',
    alternates,
  });

  const renderContent = () => {
    switch (pathname) {
      case '/about':
        return <AboutSection />;
      case '/solutions':
        return <SolutionsSection />;
      case '/articles':
        return <ArticlesSection />;
      case '/contact':
        return <ContactSection />;
      default:
        return (
          <>
            <HeroCinematic />
            <div className="relative">
              <div
                aria-hidden="true"
                className="pointer-events-none absolute inset-x-0 top-0 z-0"
                style={{
                  height: '34rem',
                  background: `linear-gradient(to bottom, rgb(${animRgb}) 0%, rgb(${animRgb}) 12%, rgba(${animRgb}, 0.6) 46%, rgba(${animRgb}, 0) 100%)`,
                }}
              />
              <div className="relative z-[1]">
                <HeroSection />
                <ModulesShowcase />
                <FeatureComparison />
                <OrderWorkflow />
                <SavingsCalculator />
                <MigrationSection />
                <IntegrationsGrid />
                <SecurityHub />
                <PricingSection />
                <TestimonialsSection />
                <FaqSection />
                <DemoScheduler />
              </div>
            </div>
          </>
        );
    }
  };

  return (
    <div className="ca-marketing ca-page-bg relative min-h-screen w-full overflow-x-clip text-slate-700 dark:text-slate-300">
      <div
        aria-hidden="true"
        className="ca-blueprint pointer-events-none fixed inset-0 z-0 opacity-[0.35] [mask-image:radial-gradient(ellipse_at_top,black_5%,transparent_60%)] dark:opacity-25"
      />
      <div className="relative z-10">
        <LandingNav theme={theme} toggleTheme={toggleTheme} />
        <main>{renderContent()}</main>
        <SiteFooter prefix={isEn ? '/en' : ''} />
      </div>
    </div>
  );
};
