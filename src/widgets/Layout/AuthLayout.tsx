import React from 'react';
import { Jira3DScene } from '@/shared/ui/Background/Jira3DScene';
import { Logo } from '@/shared/ui/Logo/Logo';
import { Target, Users, BarChart2, CheckCircle2, Sun, Moon } from 'lucide-react';
import { useTheme } from '@/app/providers/ThemeProvider';
import styles from './AuthLayout.module.css';

interface AuthLayoutProps {
  children: React.ReactNode;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
  const { theme, toggleTheme } = useTheme();

  return (
    <div className={styles.container}>
      <Jira3DScene theme={theme} />

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
