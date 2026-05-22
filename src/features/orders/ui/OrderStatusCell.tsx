import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, Loader2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import type { OrderStatus } from '../model/order.types';

// Valid quick transitions per status, excluding "createShipment" (which needs
// the shipment modal in the detail panel). Mirrors the order state machine.
const QUICK_TRANSITIONS: Record<OrderStatus, string[]> = {
  Draft: ['submit', 'cancel'],
  Submitted: ['approve', 'cancel'],
  Approved: ['allocate', 'cancel'],
  Allocated: ['cancel'],
  Picking: [],
  Packed: [],
  PartiallyShipped: ['close'],
  Shipped: ['deliver', 'close'],
  Delivered: ['close'],
  Confirmed: [],
  Closed: [],
  Cancelled: [],
  Returned: [],
};

interface Props {
  status: OrderStatus;
  toneClass: string;
  busy: boolean;
  onTransition: (action: string) => void;
}

export const OrderStatusCell = ({ status, toneClass, busy, onTransition }: Props) => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const actions = QUICK_TRANSITIONS[status] ?? [];

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
      {busy && <Loader2 size={10} className="animate-spin" />}
      {t(`orders.status.${status}` as never)}
      {actions.length > 0 && <ChevronDown size={10} />}
    </span>
  );

  // No quick transitions → plain, non-interactive badge.
  if (actions.length === 0) {
    return badge;
  }

  return (
    <div className="relative" ref={ref} onClick={(e) => e.stopPropagation()} role="presentation">
      <button
        type="button"
        disabled={busy}
        onClick={() => setOpen((v) => !v)}
        className="disabled:opacity-60"
        aria-haspopup="menu"
        aria-expanded={open}
        title={t('orders.status.changeHint', { defaultValue: 'Durumu değiştir' })}
      >
        {badge}
      </button>

      {open && (
        <div
          className="absolute left-0 z-20 mt-1 min-w-[9rem] overflow-hidden rounded-md border border-slate-200 bg-white py-1 shadow-lg dark:border-slate-700 dark:bg-slate-800"
          role="menu"
        >
          {actions.map((action) => (
            <button
              key={action}
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false);
                onTransition(action);
              }}
              className={cn(
                'block w-full px-3 py-1.5 text-left text-xs',
                action === 'cancel'
                  ? 'text-red-600 hover:bg-red-50 dark:text-red-300 dark:hover:bg-red-500/10'
                  : 'text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-700/50',
              )}
            >
              {t(`orders.actions.${action}` as never)}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};
