import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import type { InvoiceStatus } from '../model/invoice.types';

interface Props {
  status: InvoiceStatus;
  toneClass: string;
  onMarkPaid: () => void;
  onCancel: () => void;
}

// Mirrors the invoice row-action gating: mark-paid for Issued; cancel for Draft/Issued.
const canMarkPaid = (s: InvoiceStatus) => s === 'Issued';
const canCancel = (s: InvoiceStatus) => s === 'Draft' || s === 'Issued';

export const InvoiceStatusCell = ({ status, toneClass, onMarkPaid, onCancel }: Props) => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const actions: { key: 'markPaid' | 'cancel'; run: () => void; danger?: boolean }[] = [];
  if (canMarkPaid(status)) actions.push({ key: 'markPaid', run: onMarkPaid });
  if (canCancel(status)) actions.push({ key: 'cancel', run: onCancel, danger: true });

  useEffect(() => {
    if (!open) return;
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [open]);

  const badge = (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider',
        toneClass,
      )}
    >
      {t(`invoices.status.${status}` as never)}
      {actions.length > 0 && <ChevronDown size={10} />}
    </span>
  );

  if (actions.length === 0) return badge;

  return (
    <div className="relative" ref={ref} onClick={(e) => e.stopPropagation()} role="presentation">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        title={t('invoices.status.changeHint', { defaultValue: 'Durumu değiştir' })}
      >
        {badge}
      </button>
      {open && (
        <div
          className="absolute left-0 z-20 mt-1 min-w-[9rem] overflow-hidden rounded-md border border-slate-200 bg-white py-1 shadow-lg dark:border-slate-700 dark:bg-slate-800"
          role="menu"
        >
          {actions.map((a) => (
            <button
              key={a.key}
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false);
                a.run();
              }}
              className={cn(
                'block w-full px-3 py-1.5 text-left text-xs',
                a.danger
                  ? 'text-red-600 hover:bg-red-50 dark:text-red-300 dark:hover:bg-red-500/10'
                  : 'text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-700/50',
              )}
            >
              {t(`invoices.actions.${a.key}` as never)}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
