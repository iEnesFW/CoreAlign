import React, { Suspense, lazy, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Logo } from '@/shared/ui/Logo/Logo';
import { Target, Users, BarChart2, CheckCircle2, Sun, Moon } from 'lucide-react';
import { useTheme } from '@/app/providers/themeContext';
import styles from './AuthLayout.module.css';

interface AuthLayoutProps {
  children: React.ReactNode;
}

const CoreAlign3DLogin = lazy(() =>
  import('@/shared/ui/Background/CoreAlign3DLogin').then((m) => ({ default: m.CoreAlign3DLogin })),
);

const StaticBackground = ({ theme }: { theme: 'light' | 'dark' }) => (
  <div
    aria-hidden
    className={styles.staticBg}
    data-theme={theme}
    style={{
      position: 'absolute',
      inset: 0,
      zIndex: 0,
      background:
        theme === 'dark'
          ? 'radial-gradient(1000px 500px at 20% 20%, rgba(99,102,241,0.25), transparent 60%), radial-gradient(900px 600px at 80% 80%, rgba(168,85,247,0.18), transparent 60%), #0b0f19'
          : 'radial-gradient(1000px 500px at 20% 20%, rgba(99,102,241,0.18), transparent 60%), radial-gradient(900px 600px at 80% 80%, rgba(168,85,247,0.12), transparent 60%), #f8fafc',
    }}
  />
);

const useAllowExpensiveScene = (): boolean => {
  const [allow, setAllow] = useState(() => {
    if (typeof window === 'undefined') return false;
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) return false;
    return true;
  });

  useEffect(() => {
    if (typeof window === 'undefined') return;
    const mq = window.matchMedia('(prefers-reduced-motion: reduce)');
    const handler = () => setAllow((curr) => (mq.matches ? false : curr));
    mq.addEventListener?.('change', handler);
    return () => mq.removeEventListener?.('change', handler);
  }, []);

  return allow;
};

import { LightModeBackground } from '@/shared/ui/Background/LightModeBackground';

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
  const { t } = useTranslation();
  const { theme, toggleTheme } = useTheme();
  const allow3d = useAllowExpensiveScene();

  const isDark = theme === 'dark';

  return (
    <div className={styles.container}>
      {isDark ? (
        allow3d ? (
          <Suspense fallback={<StaticBackground theme="dark" />}>
            <CoreAlign3DLogin theme="dark" />
          </Suspense>
        ) : (
          <StaticBackground theme="dark" />
        )
      ) : (
        <LightModeBackground />
      )}

      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <Logo size={28} showText={true} />
        </div>
        <div className={styles.headerRight}>
          <button
            onClick={toggleTheme}
            className={styles.themeToggle}
            aria-label={t('AuthLayout.ToggleTheme', { defaultValue: 'Toggle theme' })}
          >
            {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
          </button>
          <div className={styles.statusBadge}>
            <div className={styles.statusDot} />
            <span>
              {t('AuthLayout.AllSystemsOperational', { defaultValue: 'All Systems Operational' })}
            </span>
          </div>
        </div>
      </header>

      <div className={styles.content}>
        <div className={styles.card}>{children}</div>
      </div>

      <footer className={styles.footer}>
        <div className={styles.features}>
          <div
            className={styles.featureItem}
            title={t('AuthLayout.StrategicPlanning', { defaultValue: 'Strategic Planning' })}
          >
            <div className={styles.featureIcon}>
              <Target size={20} />
            </div>
            <span className={styles.featureText}>
              {t('AuthLayout.Strategy', { defaultValue: 'Strategy' })}
            </span>
          </div>
          <div
            className={styles.featureItem}
            title={t('AuthLayout.TeamAlignment', { defaultValue: 'Team Alignment' })}
          >
            <div className={styles.featureIcon}>
              <Users size={20} />
            </div>
            <span className={styles.featureText}>
              {t('AuthLayout.Teams', { defaultValue: 'Teams' })}
            </span>
          </div>
          <div
            className={styles.featureItem}
            title={t('AuthLayout.PerformanceAnalytics', { defaultValue: 'Performance Analytics' })}
          >
            <div className={styles.featureIcon}>
              <BarChart2 size={20} />
            </div>
            <span className={styles.featureText}>
              {t('AuthLayout.Analytics', { defaultValue: 'Analytics' })}
            </span>
          </div>
          <div
            className={styles.featureItem}
            title={t('AuthLayout.EnterpriseSecurity', { defaultValue: 'Enterprise Security' })}
          >
            <div className={styles.featureIcon}>
              <CheckCircle2 size={20} />
            </div>
            <span className={styles.featureText}>
              {t('AuthLayout.Secure', { defaultValue: 'Secure' })}
            </span>
          </div>
        </div>
        <div className={styles.copyright}>
          {t('AuthLayout.Copyright', {
            defaultValue: '© {{year}} CoreAlign Inc. All rights reserved.',
            year: new Date().getFullYear(),
          })}
        </div>
      </footer>
    </div>
  );
};
