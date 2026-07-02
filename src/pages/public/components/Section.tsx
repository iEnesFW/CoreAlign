import React from 'react';
import { cn } from '@/shared/lib/cn';
import { useInView } from '@/shared/hooks/useInView';

interface SectionProps extends React.HTMLAttributes<HTMLElement> {
  divider?: boolean;
  containerClassName?: string;
}

export const Section = ({
  divider = true,
  className,
  containerClassName,
  children,
  ...props
}: SectionProps) => {
  const { ref, inView } = useInView<HTMLDivElement>();
  return (
    <section
      className={cn('relative scroll-mt-24 px-6 py-20 sm:px-10 sm:py-24 lg:px-16', className)}
      {...props}
    >
      {divider && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 mx-auto h-px w-full max-w-7xl bg-gradient-to-r from-transparent via-slate-300/60 to-transparent dark:via-slate-700/60"
        />
      )}
      <div
        ref={ref}
        className={cn(
          'mx-auto w-full max-w-7xl transition-all duration-700 ease-out motion-reduce:transition-none',
          inView
            ? 'translate-y-0 opacity-100'
            : 'translate-y-6 opacity-0 motion-reduce:translate-y-0 motion-reduce:opacity-100',
          containerClassName,
        )}
      >
        {children}
      </div>
    </section>
  );
};

interface SectionHeaderProps {
  eyebrow?: React.ReactNode;
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  align?: 'left' | 'center';
  className?: string;
}

export const SectionHeader = ({
  eyebrow,
  title,
  subtitle,
  align = 'left',
  className,
}: SectionHeaderProps) => (
  <div
    className={cn(
      'mb-12 sm:mb-16',
      align === 'center' ? 'mx-auto max-w-3xl text-center' : 'max-w-2xl',
      className,
    )}
  >
    {eyebrow && (
      <div
        className={cn(
          'mb-5 inline-flex items-center gap-2 rounded-full border border-primary-500/25 bg-primary-500/[0.07] px-3.5 py-1.5 text-[11px] font-semibold uppercase tracking-[0.14em] text-primary-600 dark:border-primary-400/20 dark:bg-primary-400/10 dark:text-primary-300',
        )}
      >
        {eyebrow}
      </div>
    )}
    <h2 className="text-balance text-3xl font-bold leading-[1.08] tracking-tight text-slate-900 sm:text-4xl lg:text-[2.85rem] dark:text-white">
      {title}
    </h2>
    {subtitle && (
      <p
        className={cn(
          'mt-5 text-base leading-relaxed text-slate-600 md:text-lg dark:text-slate-400',
          align === 'center' && 'mx-auto max-w-2xl',
        )}
      >
        {subtitle}
      </p>
    )}
  </div>
);
