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
} from 'lucide-react';
import { useTheme } from '@/app/providers/themeContext';
import { useSeo } from '@/shared/lib/seo';
import { Logo } from '@/shared/ui/Logo/Logo';
import { LoginForm } from '@/features/auth/ui/LoginForm/LoginForm';
import { LandingNav } from './components/LandingNav';
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

const StaticBackground = ({ theme }: { theme: 'light' | 'dark' }) => {
  const isDark = theme === 'dark';
  return (
    <div
      aria-hidden
      style={{
        position: 'absolute',
        inset: 0,
        zIndex: 0,
        backgroundColor: isDark ? '#0b0f19' : '#f8fafc',
        backgroundImage: isDark
          ? 'radial-gradient(rgba(99, 102, 241, 0.12) 1px, transparent 1px)'
          : 'radial-gradient(rgba(99, 102, 241, 0.06) 1px, transparent 1px)',
        backgroundSize: '24px 24px',
      }}
    >
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background: isDark
            ? 'radial-gradient(800px 400px at 20% 20%, rgba(99, 102, 241, 0.15), transparent 60%), radial-gradient(800px 500px at 80% 80%, rgba(168, 85, 247, 0.1), transparent 60%)'
            : 'radial-gradient(800px 400px at 20% 20%, rgba(99, 102, 241, 0.08), transparent 60%), radial-gradient(800px 500px at 80% 80%, rgba(168, 85, 247, 0.05), transparent 60%)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background:
            'linear-gradient(to bottom, transparent 40%, ' +
            (isDark ? '#0b0f19' : '#f8fafc') +
            ' 95%)',
        }}
      />
    </div>
  );
};

const HERO_FLOW = [
  { icon: PencilRuler, key: 'design', fallback: 'Tasarım' },
  { icon: FileText, key: 'quote', fallback: 'Teklif' },
  { icon: Factory, key: 'produce', fallback: 'Üretim' },
  { icon: Truck, key: 'ship', fallback: 'Sevkiyat' },
  { icon: Calculator, key: 'account', fallback: 'Muhasebe' },
];

const HERO_METRICS = [
  {
    icon: Users,
    key: 'activeDealers',
    tone: 'text-primary-600 dark:text-primary-300',
    chip: 'from-primary-500/15 to-primary-500/5 ring-primary-500/15',
  },
  {
    icon: Percent,
    key: 'wasteReduction',
    tone: 'text-success-600 dark:text-success-300',
    chip: 'from-success-500/15 to-success-500/5 ring-success-500/15',
  },
  {
    icon: Zap,
    key: 'orderSpeed',
    tone: 'text-warning-600 dark:text-warning-300',
    chip: 'from-warning-500/15 to-warning-500/5 ring-warning-500/15',
  },
  {
    icon: ShieldCheck,
    key: 'compliance',
    tone: 'text-accent-600 dark:text-accent-300',
    chip: 'from-accent-500/15 to-accent-500/5 ring-accent-500/15',
  },
] as const;

const HeroSection = () => {
  const { t } = useTranslation();
  return (
    <section className="flex min-h-[90vh] flex-col justify-center px-6 py-16 sm:px-10 sm:py-20 lg:px-16">
      <div className="mx-auto w-full max-w-5xl">
        <div className="animate-fade-up mb-6 inline-flex max-w-fit items-center gap-2 rounded-full border border-primary-500/30 bg-primary-500/10 px-3 py-1 text-xs font-semibold text-primary-600 backdrop-blur-md dark:text-primary-300">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary-400 opacity-75"></span>
            <span className="relative inline-flex h-2 w-2 rounded-full bg-primary-500"></span>
          </span>
          {t('LandingPage.hero.badge')}
        </div>
        <h1 className="animate-fade-up mb-6 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-600 bg-clip-text text-4xl font-extrabold leading-tight tracking-tight text-transparent md:text-5xl lg:text-6xl dark:from-white dark:via-slate-100 dark:to-slate-400">
          {t('LandingPage.hero.title')}
        </h1>
        <p className="animate-fade-up mb-8 max-w-xl text-base text-slate-600 md:text-lg dark:text-slate-400">
          {t('LandingPage.hero.subtitle')}
        </p>
        <div className="mb-4 flex flex-wrap gap-3">
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
            className="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white/50 px-6 py-3 font-semibold text-slate-700 backdrop-blur-sm transition hover:border-primary-500 hover:text-primary-600 dark:border-slate-700 dark:bg-white/5 dark:text-slate-200 dark:hover:border-primary-400 dark:hover:text-primary-300"
          >
            <PlayCircle size={16} />
            {t('LandingPage.hero.ctaSecondary', { defaultValue: 'Demo planlayın' })}
          </a>
        </div>
        <p className="mb-14 text-xs font-medium text-slate-500 dark:text-slate-400">
          {t('LandingPage.hero.trust', {
            defaultValue:
              'Türkçe & İngilizce · Bulut veya kurumsal kurulum · Kurulum ve veri taşıma desteği dahil',
          })}
        </p>

        <div className="relative mb-12 max-w-5xl overflow-hidden rounded-2xl border border-slate-200/60 bg-white/50 p-5 shadow-sm backdrop-blur-md dark:border-white/5 dark:bg-surface-deep/60">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute left-10 right-10 top-1/2 hidden h-px -translate-y-1/2 overflow-hidden bg-slate-200 sm:block dark:bg-slate-700"
          >
            <span className="absolute top-0 h-px w-1/3 animate-flow bg-gradient-to-r from-transparent via-primary-500 to-transparent" />
          </div>
          <div className="relative flex flex-wrap items-center justify-between gap-x-2 gap-y-3">
            {HERO_FLOW.map((step) => {
              const Icon = step.icon;
              return (
                <div
                  key={step.key}
                  className="flex items-center gap-2 rounded-xl bg-white px-2 py-1 dark:bg-surface-deep"
                >
                  <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary-500/15 to-primary-500/5 text-primary-600 ring-1 ring-primary-500/10 dark:text-primary-300">
                    <Icon size={16} />
                  </span>
                  <span className="text-xs font-semibold text-slate-700 dark:text-slate-200">
                    {t(`LandingPage.hero.flow.${step.key}`, { defaultValue: step.fallback })}
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        <div className="ca-stagger grid max-w-5xl grid-cols-2 gap-4 md:grid-cols-4">
          {HERO_METRICS.map((metric) => {
            const Icon = metric.icon;
            return (
              <div
                key={metric.key}
                className="group flex flex-col rounded-2xl border border-slate-200/60 bg-white/50 p-5 shadow-sm backdrop-blur-md transition-all duration-300 hover:-translate-y-1 hover:border-primary-300/50 hover:shadow-lg hover:shadow-primary-500/5 dark:border-white/5 dark:bg-surface-deep/60 dark:hover:border-primary-500/30"
              >
                <div
                  className={`mb-3 max-w-fit rounded-xl bg-gradient-to-br p-2 ring-1 ${metric.chip} ${metric.tone}`}
                >
                  <Icon size={20} />
                </div>
                <span className="text-xl font-bold text-slate-900 dark:text-white">
                  {t(`LandingPage.hero.metrics.${metric.key}Desc`)}
                </span>
                <span className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                  {t(`LandingPage.hero.metrics.${metric.key}`)}
                </span>
              </div>
            );
          })}
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
  const isDark = theme === 'dark';
  const location = useLocation();
  const rawPath = location.pathname;
  const isEn = rawPath === '/en' || rawPath.startsWith('/en/');
  const pathname = isEn ? rawPath.replace(/^\/en/, '') || '/' : rawPath;

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
          </>
        );
    }
  };

  return (
    <div className="relative flex min-h-screen w-full overflow-hidden bg-slate-50 dark:bg-shell">
      <div className="pointer-events-none fixed inset-0 z-0">
        <StaticBackground theme={isDark ? 'dark' : 'light'} />
      </div>

      <div className="relative z-10 flex w-full">
        <div className="relative hidden shrink-0 flex-col overflow-hidden border-r border-slate-200/60 bg-white/70 shadow-2xl backdrop-blur-xl lg:flex lg:w-[450px] xl:w-[500px] dark:border-white/10 dark:bg-surface-deep/80">
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -left-24 -top-24 h-72 w-72 rounded-full bg-primary-500/20 blur-3xl dark:bg-primary-500/15"
          />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -bottom-24 -right-24 h-72 w-72 rounded-full bg-accent-500/15 blur-3xl dark:bg-accent-500/10"
          />

          <div className="relative flex items-center justify-between p-6">
            <Logo size={24} showText={true} />
          </div>
          <div className="relative flex flex-1 flex-col justify-center px-8 pb-20">
            <LoginForm />
          </div>
        </div>

        <div className="relative h-screen flex-1 overflow-y-auto">
          <LandingNav theme={theme} toggleTheme={toggleTheme} />

          <main className="pb-24">{renderContent()}</main>

          <footer className="border-t border-slate-200/60 bg-white/50 px-6 py-8 backdrop-blur-sm sm:px-10 lg:px-16 dark:border-white/5 dark:bg-slate-900/50">
            <div className="mx-auto flex w-full max-w-5xl flex-col items-center justify-between gap-4 md:flex-row">
              <div className="flex items-center gap-2 opacity-80">
                <Logo size={20} showText={false} />
                <span className="font-semibold text-slate-900 dark:text-white">CoreAlign</span>
              </div>
              <div className="text-sm text-slate-500 dark:text-slate-400">
                © {new Date().getFullYear()} Tüm hakları saklıdır.
              </div>
            </div>
          </footer>
        </div>
      </div>
    </div>
  );
};
