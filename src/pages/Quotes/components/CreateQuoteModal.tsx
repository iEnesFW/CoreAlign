import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileText, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useCreateQuote } from '@/features/quotes/hooks/useQuoteQueries';

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated: (quoteId: string) => void;
}

interface LineDraft {
  key: string;
  productId: string;
  quantity: number;
  unitPrice: number;
  taxRatePercent: number;
}

const newLine = (): LineDraft => ({
  key: crypto.randomUUID(),
  productId: '',
  quantity: 1,
  unitPrice: 0,
  taxRatePercent: 20,
});

const toIsoUtcMidnight = (date: string): string => {
  if (!date) return new Date().toISOString();
  return new Date(`${date}T00:00:00Z`).toISOString();
};

export const CreateQuoteModal = ({ open, onClose, onCreated }: Props) => {
  const { t } = useTranslation();
  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const createMutation = useCreateQuote();

  const today = useMemo(() => new Date().toISOString().substring(0, 10), []);
  const defaultValid = useMemo(() => {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return d.toISOString().substring(0, 10);
  }, []);

  const [customerId, setCustomerId] = useState('');
  const [quoteDate, setQuoteDate] = useState(today);
  const [validUntil, setValidUntil] = useState(defaultValid);
  const [currency, setCurrency] = useState('TRY');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<LineDraft[]>([newLine()]);

  const customers = customersQuery.data?.data?.items ?? [];
  const products = productsQuery.data?.data?.items ?? [];

  const lineTotals = useMemo(() => {
    let subtotal = 0;
    let tax = 0;
    lines.forEach((l) => {
      const lineSub = l.quantity * l.unitPrice;
      subtotal += lineSub;
      tax += (lineSub * l.taxRatePercent) / 100;
    });
    return { subtotal, tax, total: subtotal + tax };
  }, [lines]);

  const reset = () => {
    setCustomerId('');
    setQuoteDate(today);
    setValidUntil(defaultValid);
    setCurrency('TRY');
    setNotes('');
    setLines([newLine()]);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!customerId) {
      toast.error(t('quotes.create.validation.customerRequired'));
      return;
    }
    if (new Date(validUntil) < new Date(quoteDate)) {
      toast.error(t('quotes.create.validation.validUntilAfterDate'));
      return;
    }
    const validLines = lines.filter((l) => l.productId && l.quantity > 0);
    if (validLines.length === 0) {
      toast.error(t('quotes.create.validation.linePositive'));
      return;
    }

    createMutation.mutate(
      {
        customerId,
        quoteDate: toIsoUtcMidnight(quoteDate),
        validUntilUtc: toIsoUtcMidnight(validUntil),
        currency,
        notes: notes.trim() || null,
        lines: validLines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          taxRatePercent: l.taxRatePercent,
        })),
      },
      {
        onSuccess: (response) => {
          if (response.isSuccess && response.data) {
            onCreated(response.data.id);
            reset();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  };

  return (
    <Modal
      open={open}
      title={t('quotes.create.title')}
      subtitle={t('quotes.create.subtitle')}
      icon={<FileText size={18} />}
      onClose={onClose}
      size="2xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose} type="button">
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button type="submit" form="create-quote-form" isLoading={createMutation.isPending}>
            {createMutation.isPending ? t('quotes.create.submitting') : t('quotes.create.submit')}
          </Button>
        </div>
      }
    >
      <form id="create-quote-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label={t('quotes.fields.customer')} required>
            <select
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              required
            >
              <option value="">{t('quotes.create.selectCustomer')}</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} {c.code ? `(${c.code})` : ''}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('quotes.fields.currency')}>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              <option value="TRY">TRY</option>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
            </select>
          </Field>
          <Field label={t('quotes.fields.quoteDate')}>
            <input
              type="date"
              value={quoteDate}
              onChange={(e) => setQuoteDate(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('quotes.fields.validUntil')}>
            <input
              type="date"
              value={validUntil}
              onChange={(e) => setValidUntil(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
        </div>

        <div className="rounded-lg border border-slate-200 dark:border-slate-800">
          <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2 dark:border-slate-800">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('quotes.detail.linesHeader')}
            </span>
            <button
              type="button"
              onClick={() => setLines((prev) => [...prev, newLine()])}
              className="inline-flex items-center gap-1 rounded-md border border-slate-300 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Plus size={12} />
              {t('quotes.create.addLine')}
            </button>
          </div>
          <div className="divide-y divide-slate-100 dark:divide-slate-800">
            {lines.length === 0 && (
              <p className="px-3 py-4 text-center text-sm text-slate-500 dark:text-slate-400">
                {t('quotes.create.noLinesYet')}
              </p>
            )}
            {lines.map((line, idx) => (
              <div
                key={line.key}
                className="grid grid-cols-1 gap-2 px-3 py-2 sm:grid-cols-[3fr_1fr_1fr_1fr_auto]"
              >
                <select
                  value={line.productId}
                  onChange={(e) => {
                    const productId = e.target.value;
                    const product = products.find((p) => p.id === productId);
                    setLines((prev) =>
                      prev.map((l, i) =>
                        i === idx
                          ? {
                              ...l,
                              productId,
                              unitPrice: product?.listPrice ?? product?.price ?? l.unitPrice,
                            }
                          : l,
                      ),
                    );
                  }}
                  className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                >
                  <option value="">{t('quotes.create.selectProduct')}</option>
                  {products.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.sku} — {p.name}
                    </option>
                  ))}
                </select>
                <NumberInput
                  value={line.quantity}
                  min={0}
                  step={0.01}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, quantity: v } : l)))
                  }
                  ariaLabel={t('quotes.fields.quantity')}
                />
                <NumberInput
                  value={line.unitPrice}
                  min={0}
                  step={0.01}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, unitPrice: v } : l)))
                  }
                  ariaLabel={t('quotes.fields.unitPrice')}
                />
                <NumberInput
                  value={line.taxRatePercent}
                  min={0}
                  max={100}
                  step={0.1}
                  onChange={(v) =>
                    setLines((prev) =>
                      prev.map((l, i) => (i === idx ? { ...l, taxRatePercent: v } : l)),
                    )
                  }
                  ariaLabel={t('quotes.fields.taxRate')}
                />
                <button
                  type="button"
                  onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                  className="self-center rounded-md p-1.5 text-danger-600 hover:bg-danger-50 dark:text-danger-300 dark:hover:bg-danger-900/40"
                  aria-label={t('quotes.create.removeLine')}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
          <div className="flex flex-col gap-1 border-t border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-800/40 dark:text-slate-200 sm:flex-row sm:justify-end sm:gap-6">
            <span>
              {t('quotes.detail.subtotal')}:{' '}
              <strong className="tabular-nums">{lineTotals.subtotal.toFixed(2)}</strong>
            </span>
            <span>
              {t('quotes.detail.tax')}:{' '}
              <strong className="tabular-nums">{lineTotals.tax.toFixed(2)}</strong>
            </span>
            <span>
              {t('quotes.detail.grandTotal')}:{' '}
              <strong className="tabular-nums">
                {lineTotals.total.toFixed(2)} {currency}
              </strong>
            </span>
          </div>
        </div>

        <Field label={t('quotes.fields.notes')}>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
      </form>
    </Modal>
  );
};

const Field = ({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) => (
  <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
    <span>
      {label}
      {required ? <span className="text-danger-500"> *</span> : null}
    </span>
    {children}
  </label>
);

const NumberInput = ({
  value,
  onChange,
  min,
  max,
  step,
  ariaLabel,
}: {
  value: number;
  onChange: (v: number) => void;
  min?: number;
  max?: number;
  step?: number;
  ariaLabel: string;
}) => (
  <input
    type="number"
    value={Number.isFinite(value) ? value : 0}
    min={min}
    max={max}
    step={step}
    aria-label={ariaLabel}
    onChange={(e) => {
      const v = Number.parseFloat(e.target.value);
      onChange(Number.isFinite(v) ? v : 0);
    }}
    className="w-full rounded-md border border-slate-300 bg-white px-2 py-1 text-xs text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
  />
);
