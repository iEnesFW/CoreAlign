import { useMutation } from '@tanstack/react-query';
import { AlertTriangle, Send, ShieldAlert, ShieldCheck } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Spinner } from '@/shared/ui/Spinner';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { dealerApi, type NewOrderLine } from '@/features/portal/api';
import { useDealerCustomerCredit, useDealerCustomers } from '@/features/portal/hooks';
import { ProductPicker } from './ProductPicker';
import { OrderLineEditor, type DraftOrderLine } from './OrderLineEditor';

export const NewOrderForm = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const customersQuery = useDealerCustomers();

  const [customerId, setCustomerId] = useState<string>('');
  const [lines, setLines] = useState<DraftOrderLine[]>([]);
  const [notes, setNotes] = useState('');
  const [customerNotes, setCustomerNotes] = useState('');

  const selectedCustomer = customersQuery.data?.find((c) => c.customerId === customerId);
  const currency = selectedCustomer?.currency || 'TRY';
  const creditQuery = useDealerCustomerCredit(customerId || null);

  const subtotal = useMemo(
    () => lines.reduce((sum, l) => sum + l.quantity * l.unitPrice, 0),
    [lines],
  );

  const hardCreditBlock = creditQuery.data?.isHardLimitReached ?? false;
  const minQtyViolated = lines.some(
    (l) => !!l.minOrderQuantity && l.minOrderQuantity > 0 && l.quantity < l.minOrderQuantity,
  );

  const createOrderMutation = useMutation({
    mutationFn: () =>
      dealerApi.createOrder({
        customerId,
        lines: lines.map<NewOrderLine>((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          lineNotes: l.lineNotes || undefined,
        })),
        notes: notes || undefined,
        customerNotes: customerNotes || undefined,
        currency,
      }),
    onSuccess: (created) => {
      toast.success(t('b2b.newOrder.successToast'));
      navigate(`/orders/${created.id}`, { replace: true });
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; message?: string };
      toast.error(err.normalizedMessage ?? err.message ?? t('b2b.common.errorGeneric'));
    },
  });

  const submitting = createOrderMutation.isPending;

  const onSubmit = () => {
    if (!customerId) {
      toast.error(t('b2b.newOrder.noCustomer'));
      return;
    }
    if (lines.length === 0) {
      toast.error(t('b2b.newOrder.noLines'));
      return;
    }
    if (minQtyViolated) {
      toast.error(t('b2b.newOrder.minQtyError'));
      return;
    }
    if (hardCreditBlock) {
      toast.error(t('b2b.newOrder.creditBlocked'));
      return;
    }
    createOrderMutation.mutate();
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title={t('b2b.newOrder.customer')} />
        <CardBody className="space-y-3">
          {customersQuery.isLoading ? (
            <div className="flex items-center gap-2 text-sm text-slate-500">
              <Spinner /> {t('b2b.common.loading')}
            </div>
          ) : (
            <select
              value={customerId}
              onChange={(e) => {
                setCustomerId(e.target.value);
                setLines([]);
              }}
              className="h-11 w-full max-w-lg rounded-xl border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              <option value="">{t('b2b.newOrder.selectCustomer')}</option>
              {(customersQuery.data ?? []).map((c) => (
                <option key={c.customerId} value={c.customerId}>
                  {c.name} {c.code ? `(${c.code})` : ''}
                </option>
              ))}
            </select>
          )}
          {customerId && creditQuery.data && creditQuery.data.limit > 0 ? (
            <DealerCreditPanel
              limit={creditQuery.data.limit}
              outstanding={creditQuery.data.outstanding}
              available={creditQuery.data.available}
              usagePercent={creditQuery.data.usagePercent}
              currency={creditQuery.data.currency}
              isSoftLimitReached={creditQuery.data.isSoftLimitReached}
              isHardLimitReached={creditQuery.data.isHardLimitReached}
            />
          ) : null}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('b2b.newOrder.lines')} />
        <CardBody className="space-y-4">
          <ProductPicker
            customerId={customerId || null}
            onPick={(p) =>
              setLines((prev) => [
                ...prev,
                {
                  productId: p.id,
                  productSku: p.sku,
                  productName: p.name,
                  quantity: 1,
                  unitPrice: p.price,
                  currency: p.currency || currency,
                  lineNotes: '',
                  minOrderQuantity: p.minOrderQuantity ?? null,
                },
              ])
            }
          />

          {lines.length > 0 ? (
            <div className="overflow-x-auto rounded-xl border border-slate-100 dark:border-slate-800">
              <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
                <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                  <tr>
                    <th className="px-3 py-2 text-center">#</th>
                    <th className="px-3 py-2">{t('b2b.newOrder.product')}</th>
                    <th className="px-3 py-2 text-right">{t('b2b.newOrder.quantity')}</th>
                    <th className="px-3 py-2 text-right">{t('b2b.newOrder.unitPrice')}</th>
                    <th className="px-3 py-2 text-right">{t('b2b.orders.lineTotal')}</th>
                    <th className="px-3 py-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line, idx) => (
                    <OrderLineEditor
                      key={`${line.productId}-${idx}`}
                      line={line}
                      index={idx}
                      onChange={(next) =>
                        setLines((prev) => prev.map((l, i) => (i === idx ? next : l)))
                      }
                      onRemove={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                    />
                  ))}
                </tbody>
                <tfoot className="bg-slate-50 dark:bg-slate-900">
                  <tr>
                    <td colSpan={4} className="px-3 py-2 text-right text-xs text-slate-500">
                      {t('b2b.orders.subtotal')}
                    </td>
                    <td className="px-3 py-2 text-right text-base font-bold text-slate-900 dark:text-slate-100">
                      {formatCurrency(subtotal, locale, currency)}
                    </td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            </div>
          ) : null}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('b2b.newOrder.notes')} />
        <CardBody className="space-y-3">
          <textarea
            value={customerNotes}
            onChange={(e) => setCustomerNotes(e.target.value)}
            placeholder={t('b2b.newOrder.customerNotes')}
            className="min-h-[80px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder={t('b2b.newOrder.notes')}
            className="min-h-[60px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
        </CardBody>
      </Card>

      <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-200">
        <div className="flex items-start gap-3">
          <ShieldAlert size={18} className="mt-0.5 flex-shrink-0" />
          <p>{t('b2b.newOrder.notice')}</p>
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          size="lg"
          onClick={onSubmit}
          disabled={
            submitting || !customerId || lines.length === 0 || hardCreditBlock || minQtyViolated
          }
        >
          {submitting ? <Spinner size={16} className="text-white" /> : <Send size={16} />}
          {submitting ? t('b2b.newOrder.submitting') : t('b2b.newOrder.submit')}
        </Button>
      </div>
    </div>
  );
};

interface DealerCreditPanelProps {
  limit: number;
  outstanding: number;
  available: number;
  usagePercent: number;
  currency: string;
  isSoftLimitReached: boolean;
  isHardLimitReached: boolean;
}

const DealerCreditPanel = ({
  limit,
  outstanding,
  available,
  usagePercent,
  currency,
  isSoftLimitReached,
  isHardLimitReached,
}: DealerCreditPanelProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const tone = isHardLimitReached
    ? 'text-rose-700 bg-rose-50 border-rose-200 dark:text-rose-200 dark:bg-rose-900/40 dark:border-rose-700'
    : isSoftLimitReached
      ? 'text-amber-800 bg-amber-50 border-amber-200 dark:text-amber-200 dark:bg-amber-900/30 dark:border-amber-700'
      : 'text-emerald-700 bg-emerald-50 border-emerald-200 dark:text-emerald-200 dark:bg-emerald-900/30 dark:border-emerald-700';
  const Icon = isHardLimitReached ? ShieldAlert : isSoftLimitReached ? AlertTriangle : ShieldCheck;
  return (
    <div className={`flex flex-col gap-1 rounded-xl border px-4 py-3 text-xs ${tone}`}>
      <div className="flex items-center gap-2 text-sm font-semibold">
        <Icon size={16} />
        <span>{t('b2b.credit.title')}</span>
      </div>
      <div className="grid grid-cols-3 gap-2">
        <div>
          <p className="opacity-70">{t('b2b.credit.limit')}</p>
          <p className="font-medium">{formatCurrency(limit, locale, currency)}</p>
        </div>
        <div>
          <p className="opacity-70">{t('b2b.credit.outstanding')}</p>
          <p className="font-medium">{formatCurrency(outstanding, locale, currency)}</p>
        </div>
        <div>
          <p className="opacity-70">{t('b2b.credit.available')}</p>
          <p className="font-medium">{formatCurrency(available, locale, currency)}</p>
        </div>
      </div>
      <div className="mt-1 flex items-center gap-2">
        <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
          <div
            className={`h-full ${
              isHardLimitReached
                ? 'bg-rose-500'
                : isSoftLimitReached
                  ? 'bg-amber-500'
                  : 'bg-emerald-500'
            }`}
            style={{ width: `${Math.min(100, Math.max(0, usagePercent))}%` }}
          />
        </div>
        <span className="text-[11px] font-semibold">
          {t('b2b.credit.usage', { percent: usagePercent.toFixed(0) })}
        </span>
      </div>
      {isHardLimitReached ? (
        <p className="mt-1 text-[11px]">{t('b2b.credit.blocked')}</p>
      ) : isSoftLimitReached ? (
        <p className="mt-1 text-[11px]">{t('b2b.credit.warning')}</p>
      ) : null}
    </div>
  );
};
