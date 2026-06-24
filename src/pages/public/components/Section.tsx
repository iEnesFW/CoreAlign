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
    <section className={cn('relative px-6 py-16 sm:px-10 sm:py-20 lg:px-16', className)} {...props}>
      {divider && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 top-0 mx-auto h-px w-full max-w-5xl bg-gradient-to-r from-transparent via-slate-300/60 to-transparent dark:via-slate-700/60"
        />
      )}
      <div
        ref={ref}
        className={cn(
          'mx-auto w-full max-w-5xl transition-all duration-700 ease-out motion-reduce:transition-none',
          inView
            ? 'translate-y-0 opacity-100'
            : 'translate-y-5 opacity-0 motion-reduce:translate-y-0 motion-reduce:opacity-100',
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
  className?: string;
}

export const SectionHeader = ({ eyebrow, title, subtitle, className }: SectionHeaderProps) => (
  <div className={cn('mb-10 max-w-2xl sm:mb-14', className)}>
    {eyebrow && (
      <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-primary-500/30 bg-primary-500/10 px-3 py-1 text-xs font-semibold tracking-wide text-primary-600 dark:text-primary-300">
        {eyebrow}
      </div>
    )}
    <h2 className="text-balance text-3xl font-extrabold leading-[1.1] tracking-tight text-slate-900 sm:text-4xl lg:text-[2.75rem] dark:text-white">
      {title}
    </h2>
    {subtitle && (
      <p className="mt-4 text-base leading-relaxed text-slate-600 md:text-lg dark:text-slate-400">
        {subtitle}
      </p>
    )}
  </div>
);
