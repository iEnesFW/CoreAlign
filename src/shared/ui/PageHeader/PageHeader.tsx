import { Link } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export interface Crumb {
  label: string;
  to?: string;
}

interface Props {
  icon?: React.ReactNode;
  eyebrow?: string;
  title: string;
  subtitle?: string;
  crumbs?: Crumb[];
  actions?: React.ReactNode;
  trailing?: React.ReactNode;
  bottomCenter?: React.ReactNode;
  className?: string;
  tone?: 'indigo' | 'emerald' | 'violet' | 'amber' | 'rose' | 'sky';
}

const toneAccent: Record<NonNullable<Props['tone']>, string> = {
  indigo: 'from-primary-500/30 to-transparent',
  emerald: 'from-success-500/30 to-transparent',
  violet: 'from-violet-500/30 to-transparent',
  amber: 'from-warning-500/30 to-transparent',
  rose: 'from-danger-500/30 to-transparent',
  sky: 'from-info-500/30 to-transparent',
};

const toneIconBg: Record<NonNullable<Props['tone']>, string> = {
  indigo: 'from-primary-500 to-purple-600',
  emerald: 'from-success-500 to-teal-600',
  violet: 'from-violet-500 to-fuchsia-600',
  amber: 'from-warning-500 to-warning-600',
  rose: 'from-danger-500 to-pink-600',
  sky: 'from-info-500 to-cyan-600',
};

export const PageHeader = ({
  icon,
  eyebrow,
  title,
  subtitle,
  crumbs,
  actions,
  trailing,
  bottomCenter,
  className,
  tone = 'indigo',
}: Props) => {
  return (
    <header
      className={cn(
        'relative isolate overflow-hidden rounded-2xl border border-slate-200/70 dark:border-slate-800/70 ca-hero-bg animate-fade-up',
        className,
      )}
    >
      <div className="absolute inset-0 ca-grid-mask pointer-events-none" />
      <div
        className={cn(
          'absolute -top-24 -right-24 h-64 w-64 rounded-full blur-3xl pointer-events-none bg-gradient-to-br',
          toneAccent[tone],
        )}
      />

      <div className="relative flex flex-col gap-4 px-4 py-4 sm:flex-row sm:items-start sm:justify-between sm:px-6 sm:py-5">
        <div className="min-w-0 flex-1">
          {crumbs && crumbs.length > 0 && (
            <nav className="mb-1.5 flex items-center gap-1 text-[11px] text-slate-500 dark:text-slate-400">
              {crumbs.map((c, i) => (
                <span key={`${c.label}-${i}`} className="inline-flex items-center gap-1">
                  {i > 0 && <ChevronRight size={11} className="text-slate-400" />}
                  {c.to ? (
                    <Link
                      to={c.to}
                      className="transition-colors hover:text-primary-600 dark:hover:text-primary-300"
                    >
                      {c.label}
                    </Link>
                  ) : (
                    <span
                      className={
                        i === crumbs.length - 1 ? 'text-slate-700 dark:text-slate-200' : ''
                      }
                    >
                      {c.label}
                    </span>
                  )}
                </span>
              ))}
            </nav>
          )}

          <div className="flex items-start gap-3">
            {icon && (
              <div
                className={cn(
                  'flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br text-white shadow-lg shadow-primary-500/20 ring-1 ring-white/20',
                  toneIconBg[tone],
                )}
              >
                {icon}
              </div>
            )}
            <div className="min-w-0">
              {eyebrow && (
                <div className="text-[10px] font-semibold uppercase tracking-[0.18em] text-primary-500 dark:text-primary-400">
                  {eyebrow}
                </div>
              )}
              <h1 className="truncate text-xl font-bold tracking-tight text-slate-900 dark:text-slate-100 sm:text-2xl">
                {title}
              </h1>
              {subtitle && (
                <p className="mt-0.5 text-xs text-slate-600 dark:text-slate-400 sm:text-sm">
                  {subtitle}
                </p>
              )}
            </div>
          </div>
        </div>

        {(actions || trailing) && (
          <div className="flex shrink-0 flex-col items-stretch gap-2 sm:items-end">
            {trailing}
            {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
          </div>
        )}
      </div>

      {bottomCenter && (
        <div className="relative z-10 flex justify-center px-4 pb-2 lg:pointer-events-none lg:absolute lg:inset-x-0 lg:bottom-2 lg:px-6 lg:pb-0">
          <div className="pointer-events-auto">{bottomCenter}</div>
        </div>
      )}
    </header>
  );
};
