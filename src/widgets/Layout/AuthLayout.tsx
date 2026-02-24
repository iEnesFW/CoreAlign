import React from 'react';
import { Jira3DScene } from '@/shared/ui/Background/Jira3DScene';
import { Logo } from '@/shared/ui/Logo/Logo';
import { Target, Users, BarChart2, CheckCircle2 } from 'lucide-react';
import styles from './AuthLayout.module.css';

interface AuthLayoutProps {
    children: React.ReactNode;
}

export const AuthLayout: React.FC<AuthLayoutProps> = ({ children }) => {
    return (
        <div className={styles.container}>
            <Jira3DScene />

            {/* Header */}
            <header className={styles.header}>
                <div className={styles.headerLeft}>
                    <Logo size={28} showText={true} />
                </div>
                <div className={styles.headerRight}>
                    <div className={styles.statusBadge}>
                        <div className={styles.statusDot} />
                        <span>All Systems Operational</span>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <div className={styles.content}>
                <div className={styles.card}>
                    {children}
                </div>
            </div>

            {/* Footer */}
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
