import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ShoppingBag, Trash2, X } from 'lucide-react';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { cn } from '@/shared/lib/cn';
import type { CartLine } from '../model/billing.types';
import { CheckoutPanel } from './CheckoutPanel';

interface Props {
  open: boolean;
  onClose: () => void;
  items: CartLine[];
  canPurchase: boolean;
  onRemove: (moduleId: string) => void;
  onClear: () => void;
}

export const CartDrawer = ({ open, onClose, items, canPurchase, onRemove, onClear }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [requestedStage, setRequestedStage] = useState<'cart' | 'checkout'>('cart');
  const stage: 'cart' | 'checkout' = !open || items.length === 0 ? 'cart' : requestedStage;
  const setStage = setRequestedStage;

  const currency = items[0]?.plan.currency ?? 'USD';
  const total = useMemo(() => items.reduce((sum, line) => sum + line.plan.price, 0), [items]);

  const proceed = () => {
    if (!canPurchase || items.length === 0) return;
    setStage('checkout');
  };

  const completeAndClose = () => {
    onClear();
    onClose();
  };

  return (
    <>
      {open && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-sm lg:hidden"
          onClick={onClose}
          aria-hidden
        />
      )}
      <aside
        className={cn(
          'fixed inset-y-0 right-0 z-50 flex w-full max-w-sm flex-col border-l border-slate-200 bg-white shadow-2xl transition-transform duration-300 dark:border-slate-800 dark:bg-slate-950',
          open ? 'translate-x-0' : 'translate-x-full',
        )}
        role="dialog"
        aria-label={t('billing.cart.title')}
      >
        {stage === 'checkout' ? (
          <CheckoutPanel
            items={items}
            onBack={() => setStage('cart')}
            onCompleted={completeAndClose}
          />
        ) : (
          <>
            <header className="flex items-center justify-between border-b border-slate-200/80 bg-slate-50/50 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40">
              <div className="flex items-center gap-2">
                <ShoppingBag size={16} className="text-primary-500" />
                <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                  {t('billing.cart.title')}
                </h2>
                <span className="rounded-full bg-primary-100 px-1.5 py-0.5 text-[10px] font-bold text-primary-700 dark:bg-primary-500/20 dark:text-primary-300">
                  {items.length}
                </span>
              </div>
              <button
                type="button"
                onClick={onClose}
                className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                aria-label={t('common.close', { defaultValue: 'Close' })}
              >
                <X size={14} />
              </button>
            </header>

            <div className="flex-1 overflow-y-auto p-3">
              {items.length === 0 ? (
                <div className="flex h-full flex-col items-center justify-center text-center">
                  <ShoppingBag size={28} className="mb-2 text-slate-300 dark:text-slate-700" />
                  <p className="text-xs text-slate-500 dark:text-slate-400">
                    {t('billing.cart.empty')}
                  </p>
                </div>
              ) : (
                <ul className="space-y-2">
                  {items.map((line) => (
                    <li
                      key={line.module.id}
                      className="flex items-start gap-2 rounded-lg border border-slate-200 bg-slate-50/40 p-2.5 dark:border-slate-800 dark:bg-slate-900/40"
                    >
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-xs font-semibold text-slate-900 dark:text-slate-100">
                          {line.module.name}
                        </p>
                        <p className="mt-0.5 text-[11px] text-slate-500 dark:text-slate-400">
                          {line.plan.displayLabel} ·{' '}
                          {t('billing.modules.durationDays', { count: line.plan.durationDays })}
                        </p>
                        <p className="mt-1 text-xs font-bold text-primary-600 tabular-nums dark:text-primary-300">
                          {formatCurrency(line.plan.price, locale, line.plan.currency)}
                        </p>
                      </div>
                      <button
                        type="button"
                        onClick={() => onRemove(line.module.id)}
                        aria-label={t('billing.cart.remove')}
                        className="rounded-md p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-600 dark:hover:bg-danger-500/10 dark:hover:text-danger-300"
                      >
                        <Trash2 size={12} />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <footer className="border-t border-slate-200/80 bg-slate-50/40 px-4 py-3 dark:border-slate-800/80 dark:bg-slate-900/40">
              <div className="mb-2 flex items-center justify-between text-sm">
                <span className="font-medium text-slate-600 dark:text-slate-300">
                  {t('billing.cart.total')}
                </span>
                <span className="text-base font-bold tabular-nums text-slate-900 dark:text-slate-100">
                  {formatCurrency(total, locale, currency)}
                </span>
              </div>

              {!canPurchase && (
                <p className="mb-2 rounded-md bg-warning-50 px-2 py-1.5 text-[11px] text-warning-700 dark:bg-warning-500/10 dark:text-warning-300">
                  {t('billing.cart.adminOnly')}
                </p>
              )}

              <button
                type="button"
                onClick={proceed}
                disabled={!canPurchase || items.length === 0}
                className="inline-flex w-full items-center justify-center gap-1.5 rounded-lg bg-primary-600 px-3 py-2 text-xs font-semibold text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500 dark:disabled:bg-slate-700"
              >
                <ShoppingBag size={13} />
                {t('billing.cart.buy')}
              </button>
            </footer>
          </>
        )}
      </aside>
    </>
  );
};
