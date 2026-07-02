import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ChevronUp, Columns3, Eye, EyeOff, RotateCcw } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import type { ColumnMeta, ColumnState } from './columnState';

interface Props {
  columns: ColumnMeta[];
  columnState: ColumnState;
  onToggle: (key: string) => void;
  onMove: (key: string, direction: -1 | 1) => void;
  onReset: () => void;
}

export function ColumnSettingsMenu({ columns, columnState, onToggle, onMove, onReset }: Props) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  const hidden = new Set(columnState.hidden);
  const visibleCount = columns.filter((column) => !hidden.has(column.key)).length;
  const ordered = [...columns].sort(
    (a, b) => columnState.order.indexOf(a.key) - columnState.order.indexOf(b.key),
  );

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <Columns3 size={13} />
        {t('DataTable.columns.button', { defaultValue: 'Kolonlar' })}
      </button>
      {open && (
        <>
          <button
            type="button"
            aria-hidden
            tabIndex={-1}
            onClick={() => setOpen(false)}
            className="fixed inset-0 z-40 cursor-default"
          />
          <div
            role="menu"
            className="absolute right-0 z-50 mt-1 w-64 rounded-xl border border-slate-200 bg-white p-2 shadow-lg dark:border-slate-700 dark:bg-slate-900"
          >
            <div className="mb-1 flex items-center justify-between px-1">
              <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
                {t('DataTable.columns.title', { defaultValue: 'Kolonları özelleştir' })}
              </span>
              <button
                type="button"
                onClick={onReset}
                className="inline-flex items-center gap-1 rounded px-1 py-0.5 text-[10px] text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
              >
                <RotateCcw size={10} />
                {t('DataTable.columns.reset', { defaultValue: 'Sıfırla' })}
              </button>
            </div>
            <ul className="space-y-0.5">
              {ordered.map((column, index) => {
                const isHidden = hidden.has(column.key);
                const disableHide = !isHidden && visibleCount <= 1;
                return (
                  <li
                    key={column.key}
                    className="flex items-center gap-1 rounded-md px-1.5 py-1 hover:bg-slate-50 dark:hover:bg-slate-800/60"
                  >
                    <button
                      type="button"
                      onClick={() => !disableHide && onToggle(column.key)}
                      disabled={disableHide}
                      aria-pressed={!isHidden}
                      className={cn(
                        'inline-flex flex-1 items-center gap-1.5 text-left text-xs',
                        isHidden ? 'text-slate-400' : 'text-slate-700 dark:text-slate-200',
                        disableHide && 'cursor-not-allowed opacity-60',
                      )}
                    >
                      {isHidden ? (
                        <EyeOff size={12} />
                      ) : (
                        <Eye size={12} className="text-primary-600 dark:text-primary-400" />
                      )}
                      <span className="truncate">{column.label}</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => onMove(column.key, -1)}
                      disabled={index === 0}
                      aria-label={t('DataTable.columns.moveUp', { defaultValue: 'Yukarı taşı' })}
                      className="rounded p-0.5 text-slate-400 transition hover:bg-slate-100 disabled:opacity-30 dark:hover:bg-slate-700"
                    >
                      <ChevronUp size={12} />
                    </button>
                    <button
                      type="button"
                      onClick={() => onMove(column.key, 1)}
                      disabled={index === ordered.length - 1}
                      aria-label={t('DataTable.columns.moveDown', { defaultValue: 'Aşağı taşı' })}
                      className="rounded p-0.5 text-slate-400 transition hover:bg-slate-100 disabled:opacity-30 dark:hover:bg-slate-700"
                    >
                      <ChevronDown size={12} />
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>
        </>
      )}
    </div>
  );
}
