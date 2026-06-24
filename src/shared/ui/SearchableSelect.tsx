import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { ChevronDown, Search } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { filterOptions, type SearchableOption } from './searchableSelectFilter';

export type { SearchableOption } from './searchableSelectFilter';

interface SearchableSelectProps {
  value: string;
  options: SearchableOption[];
  onChange: (value: string) => void;
  placeholder?: string;
  searchPlaceholder?: string;
  emptyText?: string;
  ariaLabel?: string;
  className?: string;
  disabled?: boolean;
}

export function SearchableSelect({
  value,
  options,
  onChange,
  placeholder,
  searchPlaceholder,
  emptyText,
  ariaLabel,
  className,
  disabled,
}: SearchableSelectProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [active, setActive] = useState(0);
  const rootRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listId = useId();

  const selected = options.find((o) => o.value === value);
  const filtered = useMemo(() => filterOptions(options, query), [options, query]);

  // Side-effects only (focus + outside-click) — query/active are reset in the open toggle,
  // never via setState inside an effect (which would cascade renders).
  useEffect(() => {
    if (!open) return;
    const focusTimer = window.setTimeout(() => inputRef.current?.focus(), 0);
    const onPointerDown = (e: PointerEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    window.addEventListener('pointerdown', onPointerDown);
    return () => {
      window.clearTimeout(focusTimer);
      window.removeEventListener('pointerdown', onPointerDown);
    };
  }, [open]);

  const toggleOpen = () => {
    const next = !open;
    if (next) {
      setQuery('');
      setActive(0);
    }
    setOpen(next);
  };

  // Clamp at render rather than in an effect (a shrinking list must not leave `active`
  // pointing past the end); typing resets it to 0 in the input's onChange.
  const activeIndex = filtered.length === 0 ? -1 : Math.min(active, filtered.length - 1);

  const choose = (next: string) => {
    onChange(next);
    setOpen(false);
  };

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActive(Math.min(activeIndex + 1, filtered.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActive(Math.max(activeIndex - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const option = filtered[activeIndex];
      if (option) choose(option.value);
    } else if (e.key === 'Escape') {
      e.preventDefault();
      setOpen(false);
    }
  };

  return (
    <div ref={rootRef} className={cn('relative', className)}>
      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={ariaLabel}
        onClick={toggleOpen}
        className="flex w-full items-center justify-between gap-2 rounded border border-slate-300 bg-white px-2 py-1 text-left text-sm text-slate-900 focus:border-primary-500 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
      >
        <span className={cn('min-w-0 flex-1 truncate', !selected && 'text-slate-400')}>
          {selected?.label ?? placeholder ?? ''}
        </span>
        <ChevronDown size={14} className="shrink-0 text-slate-400" />
      </button>
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-900">
          <div className="flex items-center gap-1 border-b border-slate-100 px-2 py-1 dark:border-slate-800">
            <Search size={12} className="shrink-0 text-slate-400" />
            <input
              ref={inputRef}
              value={query}
              onChange={(e) => {
                setQuery(e.target.value);
                setActive(0);
              }}
              onKeyDown={onKeyDown}
              placeholder={searchPlaceholder}
              className="w-full bg-transparent text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none dark:text-slate-100"
            />
          </div>
          <ul
            role="listbox"
            id={listId}
            aria-label={ariaLabel}
            className="max-h-56 overflow-auto py-1"
          >
            {filtered.length === 0 ? (
              <li className="px-2 py-1.5 text-xs text-slate-400">{emptyText}</li>
            ) : (
              filtered.map((option, i) => (
                <li
                  key={option.value}
                  role="option"
                  aria-selected={option.value === value}
                  onPointerEnter={() => setActive(i)}
                  onClick={() => choose(option.value)}
                  className={cn(
                    'cursor-pointer px-2 py-1.5 text-sm',
                    i === activeIndex
                      ? 'bg-primary-50 text-primary-700 dark:bg-primary-950/40 dark:text-primary-300'
                      : 'text-slate-700 dark:text-slate-200',
                    option.value === value && 'font-medium',
                  )}
                >
                  {option.render ?? option.label}
                </li>
              ))
            )}
          </ul>
        </div>
      )}
    </div>
  );
}

export default SearchableSelect;
