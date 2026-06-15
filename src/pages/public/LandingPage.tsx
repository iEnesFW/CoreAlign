import { useTranslation } from 'react-i18next';
import { useLocation, Link } from 'react-router-dom';
import { ArrowRight, Users, Percent, Zap, ShieldCheck } from 'lucide-react';
import { useTheme } from '@/app/providers/ThemeProvider';
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

const HeroSection = () => {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-[90vh] flex-col justify-center px-8 py-16 sm:px-16 lg:px-24">
      <div className="mb-6 inline-flex max-w-fit items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
        <span className="relative flex h-2 w-2">
          <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-indigo-400 opacity-75"></span>
          <span className="relative inline-flex h-2 w-2 rounded-full bg-indigo-500"></span>
        </span>
        {t('LandingPage.hero.badge')}
      </div>
      <h1 className="mb-6 text-4xl font-extrabold tracking-tight text-slate-900 md:text-5xl lg:text-6xl dark:text-white leading-tight">
        {t('LandingPage.hero.title')}
      </h1>
      <p className="mb-8 max-w-xl text-base text-slate-600 dark:text-slate-400 md:text-lg">
        {t('LandingPage.hero.subtitle')}
      </p>
      <div className="flex flex-wrap gap-4 mb-16">
        <Link
          to="/solutions"
          className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-6 py-3 font-semibold text-white shadow-lg shadow-indigo-500/30 transition hover:bg-indigo-700 hover:shadow-indigo-500/40"
        >
          {t('LandingPage.hero.cta')}
          <ArrowRight size={16} />
        </Link>
      </div>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4 max-w-5xl">
        <div className="flex flex-col p-5 rounded-2xl border border-slate-200/50 bg-white/40 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60 hover:scale-[1.02] transition-all duration-300 shadow-sm">
          <div className="mb-3 rounded-lg bg-indigo-500/10 p-2 max-w-fit text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
            <Users size={20} />
          </div>
          <span className="text-xl font-bold text-slate-900 dark:text-white">
            {t('LandingPage.hero.metrics.activeDealersDesc')}
          </span>
          <span className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {t('LandingPage.hero.metrics.activeDealers')}
          </span>
        </div>

        <div className="flex flex-col p-5 rounded-2xl border border-slate-200/50 bg-white/40 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60 hover:scale-[1.02] transition-all duration-300 shadow-sm">
          <div className="mb-3 rounded-lg bg-emerald-500/10 p-2 max-w-fit text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
            <Percent size={20} />
          </div>
          <span className="text-xl font-bold text-slate-900 dark:text-white">
            {t('LandingPage.hero.metrics.wasteReductionDesc')}
          </span>
          <span className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {t('LandingPage.hero.metrics.wasteReduction')}
          </span>
        </div>

        <div className="flex flex-col p-5 rounded-2xl border border-slate-200/50 bg-white/40 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60 hover:scale-[1.02] transition-all duration-300 shadow-sm">
          <div className="mb-3 rounded-lg bg-amber-500/10 p-2 max-w-fit text-amber-600 dark:bg-amber-500/20 dark:text-amber-400">
            <Zap size={20} />
          </div>
          <span className="text-xl font-bold text-slate-900 dark:text-white">
            {t('LandingPage.hero.metrics.orderSpeedDesc')}
          </span>
          <span className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {t('LandingPage.hero.metrics.orderSpeed')}
          </span>
        </div>

        <div className="flex flex-col p-5 rounded-2xl border border-slate-200/50 bg-white/40 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60 hover:scale-[1.02] transition-all duration-300 shadow-sm">
          <div className="mb-3 rounded-lg bg-indigo-500/10 p-2 max-w-fit text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
            <ShieldCheck size={20} />
          </div>
          <span className="text-xl font-bold text-slate-900 dark:text-white">
            {t('LandingPage.hero.metrics.complianceDesc')}
          </span>
          <span className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {t('LandingPage.hero.metrics.compliance')}
          </span>
        </div>
      </div>
    </div>
  );
};

export const LandingPage = () => {
  const { theme, toggleTheme } = useTheme();
  const isDark = theme === 'dark';
  const location = useLocation();
  const pathname = location.pathname;

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
    <div className="relative flex min-h-screen w-full overflow-hidden bg-slate-50 dark:bg-[#0b0f19]">
      <div className="fixed inset-0 z-0 pointer-events-none">
        <StaticBackground theme={isDark ? 'dark' : 'light'} />
      </div>

      <div className="relative z-10 flex w-full">
        <div className="hidden lg:flex w-full lg:w-[450px] xl:w-[500px] shrink-0 flex-col border-r border-slate-200/50 bg-white/70 backdrop-blur-xl dark:border-slate-800/50 dark:bg-[#0f1524]/80 shadow-2xl">
          <div className="flex items-center justify-between p-6">
            <Logo size={24} showText={true} />
          </div>
          <div className="flex flex-1 flex-col justify-center px-8 pb-20">
            <LoginForm />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto h-screen relative">
          <LandingNav theme={theme} toggleTheme={toggleTheme} />

          <main className="pb-24">{renderContent()}</main>

          <footer className="border-t border-slate-200/50 bg-white/50 px-8 py-8 backdrop-blur-sm dark:border-slate-800/50 dark:bg-slate-900/50 sm:px-16 lg:px-24">
            <div className="flex flex-col md:flex-row items-center justify-between gap-4">
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
