import { cn } from '@/shared/lib/cn';
import markUrl from './assets/corealign-mark.svg';

interface LogoProps {
  className?: string;
  size?: number;
  showText?: boolean;
  src?: string | null;
  alt?: string;
}

// WHY the src override: the uploaded tenant logo must replace the CoreAlign mark in the dashboard
// chrome only — the public landing and the auth screens stay CoreAlign-branded.
export const Logo = ({ className, size = 32, showText = true, src, alt }: LogoProps) => {
  const tenantMark = src && src.trim().length > 0 ? src : null;
  return (
    <span className={cn('inline-flex items-center gap-2.5', className)}>
      <img
        src={tenantMark ?? markUrl}
        width={size}
        height={size}
        alt={showText ? '' : (alt ?? 'CoreAlign')}
        aria-hidden={showText ? true : undefined}
        draggable={false}
        className={cn('shrink-0 select-none', tenantMark && 'object-contain')}
        style={tenantMark ? { maxHeight: size, width: 'auto' } : undefined}
      />
      {showText && !tenantMark && (
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
