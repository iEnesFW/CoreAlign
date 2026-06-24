import { useEffect, useMemo, useRef, useState, type ComponentType } from 'react';
import { createPortal } from 'react-dom';
import { Search } from 'lucide-react';

export interface CommandItem {
  id: string;
  label: string;
  hint?: string;
  group?: string;
  keywords?: string;
  icon?: ComponentType<{ size?: number | string; className?: string }>;
  onSelect: () => void;
}

interface Props {
  onClose: () => void;
  items: CommandItem[];
  placeholder?: string;
}

export const CommandPalette = ({ onClose, items, placeholder }: Props) => {
  const [query, setQuery] = useState('');
  const [highlight, setHighlight] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);

  useEffect(() => {
    requestAnimationFrame(() => inputRef.current?.focus());
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter(
      (it) =>
        it.label.toLowerCase().includes(q) ||
        (it.hint?.toLowerCase().includes(q) ?? false) ||
        (it.keywords?.toLowerCase().includes(q) ?? false),
    );
  }, [items, query]);

  const activeIndex = Math.min(highlight, Math.max(filtered.length - 1, 0));

  const choose = (item?: CommandItem) => {
    if (!item) return;
    item.onSelect();
    onClose();
  };

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlight((h) => Math.min(h + 1, filtered.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlight((h) => Math.max(h - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      choose(filtered[activeIndex]);
    } else if (e.key === 'Escape') {
      e.preventDefault();
      onClose();
    }
  };

  const content = (
    <div
      className="fixed inset-0 z-[60] flex items-start justify-center bg-black/40 p-4 pt-[12vh]"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="w-full max-w-xl overflow-hidden rounded-lg bg-white shadow-2xl ring-1 ring-slate-200 dark:bg-slate-900 dark:ring-slate-700"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label={placeholder}
      >
        <div className="flex items-center gap-2 border-b border-slate-200 px-3 py-2.5 dark:border-slate-800">
          <Search size={16} className="shrink-0 text-slate-400" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setHighlight(0);
            }}
            onKeyDown={onKeyDown}
            placeholder={placeholder ?? 'Search…'}
            className="w-full bg-transparent text-sm text-slate-900 placeholder-slate-400 focus:outline-none dark:text-slate-100"
          />
          <kbd className="rounded border border-slate-200 px-1.5 py-0.5 text-[10px] font-medium text-slate-400 dark:border-slate-700">
            ESC
          </kbd>
        </div>

        <ul ref={listRef} className="max-h-80 overflow-y-auto py-1">
          {filtered.length === 0 ? (
            <li className="px-3 py-6 text-center text-sm text-slate-400">No results</li>
          ) : (
            filtered.map((item, i) => {
              const Icon = item.icon;
              return (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={() => choose(item)}
                    onMouseEnter={() => setHighlight(i)}
                    className={`flex w-full items-center gap-2.5 px-3 py-2 text-left text-sm ${
                      i === activeIndex
                        ? 'bg-primary-50 text-primary-700 dark:bg-primary-500/15 dark:text-primary-300'
                        : 'text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800/60'
                    }`}
                  >
                    {Icon && <Icon size={15} className="shrink-0 text-slate-400" />}
                    <span className="flex-1 truncate">{item.label}</span>
                    {item.hint && <span className="text-[11px] text-slate-400">{item.hint}</span>}
                  </button>
                </li>
              );
            })
          )}
        </ul>
      </div>
    </div>
  );

  if (typeof document === 'undefined') return null;
  return createPortal(content, document.body);
};
