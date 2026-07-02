import { cn } from '@/shared/lib/cn';
import markUrl from './assets/corealign-mark.svg';

interface LogoProps {
  className?: string;
  size?: number;
  showText?: boolean;
}

export const Logo = ({ className, size = 32, showText = true }: LogoProps) => {
  return (
    <span className={cn('inline-flex items-center gap-2.5', className)}>
      <img
        src={markUrl}
        width={size}
        height={size}
        alt={showText ? '' : 'CoreAlign'}
        aria-hidden={showText ? true : undefined}
        draggable={false}
        className="shrink-0 select-none"
      />
      {showText && (
        <span
          className="font-bold leading-none tracking-tight"
          style={{ fontSize: Math.round(size * 0.64) }}
        >
          <span className="text-slate-900 dark:text-white">Core</span>
          <span className="text-slate-500 dark:text-slate-300">Align</span>
        </span>
      )}
    </span>
  );
};
