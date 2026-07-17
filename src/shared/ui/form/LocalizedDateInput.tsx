import { forwardRef, useMemo } from 'react';
import { CalendarDays } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface Props {
  value?: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  locale: string;
  ariaLabel: string;
  disabled?: boolean;
  min?: string;
  max?: string;
  className?: string;
}

const isoDateToLocalDate = (value?: string): Date | null => {
  if (!value) return null;
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return null;
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
    ? date
    : null;
};

export const LocalizedDateInput = forwardRef<HTMLInputElement, Props>(
  ({ value, onChange, onBlur, locale, ariaLabel, disabled, min, max, className }, ref) => {
    const formatter = useMemo(() => {
      try {
        return new Intl.DateTimeFormat(locale, {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
        });
      } catch {
        return new Intl.DateTimeFormat('en', {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
        });
      }
    }, [locale]);

    const parsedDate = isoDateToLocalDate(value);
    const displayValue = parsedDate ? formatter.format(parsedDate) : '';
    const placeholder = formatter.format(new Date(2026, 11, 31));

    return (
      <span
        className={cn(
          'relative flex min-h-[34px] w-full items-center rounded-md border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-900 transition-all focus-within:border-indigo-500 focus-within:ring-1 focus-within:ring-indigo-500 dark:border-[#2a3143] dark:bg-[#0f111a] dark:text-slate-200',
          disabled && 'cursor-not-allowed opacity-60',
          className,
        )}
      >
        <span
          aria-hidden="true"
          className={cn(
            'pointer-events-none min-w-0 flex-1 truncate',
            !displayValue && 'text-slate-400 dark:text-slate-500',
          )}
        >
          {displayValue || placeholder}
        </span>
        <CalendarDays
          aria-hidden="true"
          className="pointer-events-none ml-2 h-4 w-4 shrink-0 text-slate-400"
        />
        <input
          ref={ref}
          type="date"
          value={value ?? ''}
          min={min}
          max={max}
          disabled={disabled}
          aria-label={ariaLabel}
          onChange={(event) => onChange(event.target.value)}
          onBlur={onBlur}
          onClick={(event) => {
            try {
              event.currentTarget.showPicker?.();
            } catch {
              // Some browsers only allow showPicker during a direct user gesture.
            }
          }}
          className="absolute inset-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
        />
      </span>
    );
  },
);

LocalizedDateInput.displayName = 'LocalizedDateInput';
