import React, { Suspense, lazy, useEffect, useState } from 'react';
import { Logo } from '@/shared/ui/Logo/Logo';
import { Target, Users, BarChart2, CheckCircle2, Sun, Moon } from 'lucide-react';
import { useTheme } from '@/app/providers/ThemeProvider';
import styles from './AuthLayout.module.css';

interface AuthLayoutProps {
  children: React.ReactNode;
}

// Lazily load the 3D scene so the ~700 KB three.js + lil-gui chunk only ships
// when actually needed (i.e. on auth pages, on capable devices, with motion
// allowed). Other dashboard routes never pay this cost.
const Jira3DScene = lazy(() =>
  import('@/shared/ui/Background/Jira3DScene').then((m) => ({ default: m.Jira3DScene })),
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
  // Skip the GPU-heavy scene on:
  //   • prefers-reduced-motion
  //   • obvious low-end devices (mobile, very low CPU/RAM, save-data, slow net)
  //   • SSR (no window)
  const [allow, setAllow] = useState(() => {
    if (typeof window === 'undefined') return false;
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) return false;
    const nav = navigator as Navigator & {
      deviceMemory?: number;
      hardwareConcurrency?: number;
      connection?: { saveData?: boolean; effectiveType?: string };
    };
    if (nav.deviceMemory !== undefined && nav.deviceMemory < 4) return false;
    if (nav.hardwareConcurrency !== undefined && nav.hardwareConcurrency < 4) return false;
    if (nav.connection?.saveData) return false;
    if (nav.connection?.effectiveType && /^(slow-2g|2g|3g)$/.test(nav.connection.effectiveType)) {
      return false;
    }
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

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
  const { theme, toggleTheme } = useTheme();
  const allow3d = useAllowExpensiveScene();

  return (
    <div className={styles.container}>
      {allow3d ? (
        <Suspense fallback={<StaticBackground theme={theme} />}>
          <Jira3DScene theme={theme} />
        </Suspense>
      ) : (
        <StaticBackground theme={theme} />
      )}

      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <Logo size={28} showText={true} />
        </div>
        <div className={styles.headerRight}>
          <button onClick={toggleTheme} className={styles.themeToggle} aria-label="Toggle theme">
            {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
          </button>
          <div className={styles.statusBadge}>
            <div className={styles.statusDot} />
            <span>All Systems Operational</span>
          </div>
        </div>
      </header>

      <div className={styles.content}>
        <div className={styles.card}>{children}</div>
      </div>

      <footer className={styles.footer}>
        <div className={styles.features}>
          <div className={styles.featureItem} title="Strategic Planning">
            <div className={styles.featureIcon}>
              <Target size={20} />
            </div>
            <span className={styles.featureText}>Strategy</span>
          </div>
          <div className={styles.featureItem} title="Team Alignment">
            <div className={styles.featureIcon}>
              <Users size={20} />
            </div>
            <span className={styles.featureText}>Teams</span>
          </div>
          <div className={styles.featureItem} title="Performance Analytics">
            <div className={styles.featureIcon}>
              <BarChart2 size={20} />
            </div>
            <span className={styles.featureText}>Analytics</span>
          </div>
          <div className={styles.featureItem} title="Enterprise Security">
            <div className={styles.featureIcon}>
              <CheckCircle2 size={20} />
            </div>
            <span className={styles.featureText}>Secure</span>
          </div>
        </div>
        <div className={styles.copyright}>
          © {new Date().getFullYear()} CoreAlign Inc. All rights reserved.
        </div>
      </footer>
    </div>
  );
};
