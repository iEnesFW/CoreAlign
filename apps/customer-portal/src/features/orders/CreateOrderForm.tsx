import { Send } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Spinner } from '@/shared/ui/Spinner';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useCreateDirectOrder, useCreditSnapshot } from '@/features/portal/hooks';
import { AddressPicker } from './AddressPicker';
import { CreditBadge } from './CreditBadge';
import { LineEditor, type DraftDirectOrderLine } from './LineEditor';
import { ProductPicker } from './ProductPicker';

const DEFAULT_CURRENCY = 'TRY';

export const CreateOrderForm = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();

  const [lines, setLines] = useState<DraftDirectOrderLine[]>([]);
  const [notes, setNotes] = useState('');
  const [customerNotes, setCustomerNotes] = useState('');
  const [shippingAddressId, setShippingAddressId] = useState<string | null>(null);
  const [billingAddressId, setBillingAddressId] = useState<string | null>(null);

  const createMutation = useCreateDirectOrder();
  const credit = useCreditSnapshot();

  const currency = lines[0]?.currency || DEFAULT_CURRENCY;

  const subtotal = useMemo(
    () => lines.reduce((sum, l) => sum + l.quantity * l.unitPrice, 0),
    [lines],
  );

  const hardCreditBlock = credit.data?.isHardLimitReached ?? false;
  const minQtyViolated = lines.some(
    (l) => !!l.minOrderQuantity && l.minOrderQuantity > 0 && l.quantity < l.minOrderQuantity,
  );

  const onSubmit = () => {
    if (lines.length === 0) {
      toast.error(t('orders.create.noLines'));
      return;
    }
    if (lines.some((l) => l.quantity <= 0)) {
      toast.error(t('orders.create.invalidQuantity'));
      return;
    }
    if (minQtyViolated) {
      toast.error(t('orders.create.minQtyError'));
      return;
    }
    if (hardCreditBlock) {
      toast.error(t('orders.create.creditBlocked'));
      return;
    }
    createMutation.mutate(
      {
        lines: lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          lineNotes: l.lineNotes || undefined,
        })),
        notes: notes || undefined,
        customerNotes: customerNotes || undefined,
        shippingAddressId: shippingAddressId || undefined,
        billingAddressId: billingAddressId || undefined,
      },
      {
        onSuccess: (orderId) => {
          toast.success(t('orders.create.successToast'));
          navigate(`/orders/${orderId}`, { replace: true });
        },
        onError: (caught) => {
          const err = caught as { normalizedMessage?: string; message?: string };
          toast.error(err.normalizedMessage ?? err.message ?? t('errors.unknown'));
        },
      },
    );
  };

  const submitting = createMutation.isPending;

  return (
    <div className="space-y-6">
      <CreditBadge />

      <Card>
        <CardHeader title={t('orders.create.shippingAndBilling')} />
        <CardBody>
          <AddressPicker
            selectedShippingAddressId={shippingAddressId}
            selectedBillingAddressId={billingAddressId}
            onChangeShipping={setShippingAddressId}
            onChangeBilling={setBillingAddressId}
          />
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('orders.create.lines')} />
        <CardBody className="space-y-4">
          <ProductPicker
            onPick={(p) =>
              setLines((prev) => [
                ...prev,
                {
                  productId: p.id,
                  productSku: p.sku,
                  productName: p.name,
                  quantity: 1,
                  unitPrice: p.price,
                  currency: p.currency || DEFAULT_CURRENCY,
                  unit: p.unit,
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
                    <th className="px-3 py-2">{t('orders.create.product')}</th>
                    <th className="px-3 py-2 text-right">{t('orders.create.quantity')}</th>
                    <th className="px-3 py-2 text-right">{t('orders.create.unitPrice')}</th>
                    <th className="px-3 py-2 text-right">{t('orders.create.lineTotal')}</th>
                    <th className="px-3 py-2" />
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line, idx) => (
                    <LineEditor
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
                      {t('orders.create.subtotal')}
                    </td>
                    <td className="px-3 py-2 text-right text-base font-bold text-slate-900 dark:text-slate-100">
                      {formatCurrency(subtotal, locale, currency)}
                    </td>
                    <td />
                  </tr>
                </tfoot>
              </table>
            </div>
          ) : (
            <p className="rounded-xl border border-dashed border-slate-200 px-4 py-6 text-center text-xs text-slate-500 dark:border-slate-700">
              {t('orders.create.noLinesYet')}
            </p>
          )}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('orders.create.notes')} />
        <CardBody className="space-y-3">
          <textarea
            value={customerNotes}
            onChange={(event) => setCustomerNotes(event.target.value)}
            placeholder={t('orders.create.customerNotes')}
            className="min-h-[80px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
          <textarea
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            placeholder={t('orders.create.internalNotes')}
            className="min-h-[60px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
        </CardBody>
      </Card>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => navigate('/orders')}
          disabled={submitting}
        >
          {t('common.cancel')}
        </Button>
        <Button
          type="button"
          size="lg"
          onClick={onSubmit}
          disabled={submitting || lines.length === 0 || hardCreditBlock || minQtyViolated}
        >
          {submitting ? <Spinner size={16} className="text-white" /> : <Send size={16} />}
          {submitting ? t('orders.create.submitting') : t('orders.create.submit')}
        </Button>
      </div>
    </div>
  );
};
