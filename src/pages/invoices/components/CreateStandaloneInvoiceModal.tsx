import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileText, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useCreateStandaloneInvoice } from '@/features/invoices/hooks/useInvoiceQueries';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import type { ApiResponse } from '@/shared/types/api';

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated?: (invoiceId: string) => void;
}

interface LineDraft {
  key: string;
  sku: string;
  name: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxRatePercent: number;
}

const newLine = (): LineDraft => ({
  key: crypto.randomUUID(),
  sku: '',
  name: '',
  description: '',
  quantity: 1,
  unitPrice: 0,
  taxRatePercent: 20,
});

const toIsoUtcMidnight = (date: string): string =>
  date ? new Date(`${date}T00:00:00Z`).toISOString() : new Date().toISOString();

export const CreateStandaloneInvoiceModal = ({ open, onClose, onCreated }: Props) => {
  const { t } = useTranslation();
  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const createMutation = useCreateStandaloneInvoice();

  const today = useMemo(() => new Date().toISOString().substring(0, 10), []);

  const [customerId, setCustomerId] = useState('');
  const [issueDate, setIssueDate] = useState(today);
  const [dueDays, setDueDays] = useState(30);
  const [currency, setCurrency] = useState('TRY');
  const [publicNotes, setPublicNotes] = useState('');
  const [internalNotes, setInternalNotes] = useState('');
  const [lines, setLines] = useState<LineDraft[]>([newLine()]);

  const customers = customersQuery.data?.data?.items ?? [];

  const totals = useMemo(() => {
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
    setIssueDate(today);
    setDueDays(30);
    setCurrency('TRY');
    setPublicNotes('');
    setInternalNotes('');
    setLines([newLine()]);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!customerId) {
      toast.error(t('invoices.standalone.validation.customerRequired'));
      return;
    }
    const validLines = lines.filter((l) => l.name.trim() && l.sku.trim() && l.quantity > 0);
    if (validLines.length === 0) {
      toast.error(t('invoices.standalone.validation.lineNamePositive'));
      return;
    }

    createMutation.mutate(
      {
        customerId,
        issueDate: toIsoUtcMidnight(issueDate),
        dueDays,
        currency,
        publicNotes: publicNotes.trim() || null,
        internalNotes: internalNotes.trim() || null,
        lines: validLines.map((l) => ({
          productId: null,
          productSku: l.sku.trim(),
          productName: l.name.trim(),
          description: l.description.trim() || null,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          taxRatePercent: l.taxRatePercent,
        })),
      },
      {
        onSuccess: (response: ApiResponse<Invoice>) => {
          if (response.isSuccess && response.data) {
            toast.success(t('invoices.standalone.created'));
            onCreated?.(response.data.id);
            reset();
            onClose();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error: unknown) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  };

  return (
    <Modal
      open={open}
      title={t('invoices.standalone.title')}
      subtitle={t('invoices.standalone.subtitle')}
      icon={<FileText size={18} />}
      onClose={onClose}
      size="2xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose} type="button">
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button
            type="submit"
            form="create-standalone-invoice-form"
            isLoading={createMutation.isPending}
          >
            {createMutation.isPending
              ? t('invoices.standalone.creating')
              : t('invoices.standalone.create')}
          </Button>
        </div>
      }
    >
      <form id="create-standalone-invoice-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label={t('invoices.standalone.customer')} required>
            <select
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              required
            >
              <option value="">{t('invoices.standalone.selectCustomer')}</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} {c.code ? `(${c.code})` : ''}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('invoices.standalone.currency')}>
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
          <Field label={t('invoices.standalone.issueDate')}>
            <input
              type="date"
              value={issueDate}
              onChange={(e) => setIssueDate(e.target.value)}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('invoices.standalone.dueDays')}>
            <input
              type="number"
              min={0}
              max={365}
              value={dueDays}
              onChange={(e) => setDueDays(Number.parseInt(e.target.value, 10) || 0)}
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
              {t('invoices.standalone.addLine')}
            </button>
          </div>

          <div className="divide-y divide-slate-100 dark:divide-slate-800">
            {lines.map((line, idx) => (
              <div
                key={line.key}
                className="grid grid-cols-1 gap-2 px-3 py-2 sm:grid-cols-[1fr_2fr_1fr_1fr_1fr_auto]"
              >
                <TextInput
                  placeholder={t('invoices.standalone.lineSku')}
                  value={line.sku}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, sku: v } : l)))
                  }
                />
                <TextInput
                  placeholder={t('invoices.standalone.lineName')}
                  value={line.name}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, name: v } : l)))
                  }
                />
                <NumberInput
                  value={line.quantity}
                  min={0}
                  step={0.01}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, quantity: v } : l)))
                  }
                  ariaLabel={t('invoices.standalone.lineQuantity')}
                />
                <NumberInput
                  value={line.unitPrice}
                  min={0}
                  step={0.01}
                  onChange={(v) =>
                    setLines((prev) => prev.map((l, i) => (i === idx ? { ...l, unitPrice: v } : l)))
                  }
                  ariaLabel={t('invoices.standalone.lineUnitPrice')}
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
                  ariaLabel={t('invoices.standalone.lineTaxRate')}
                />
                <button
                  type="button"
                  onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                  className="self-center rounded-md p-1.5 text-rose-600 hover:bg-rose-50 dark:text-rose-300 dark:hover:bg-rose-900/40"
                  aria-label={t('invoices.standalone.removeLine')}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>

          <div className="flex flex-col gap-1 border-t border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-800/40 dark:text-slate-200 sm:flex-row sm:justify-end sm:gap-6">
            <span>
              {t('quotes.detail.subtotal')}:{' '}
              <strong className="tabular-nums">{totals.subtotal.toFixed(2)}</strong>
            </span>
            <span>
              {t('quotes.detail.tax')}:{' '}
              <strong className="tabular-nums">{totals.tax.toFixed(2)}</strong>
            </span>
            <span>
              {t('quotes.detail.grandTotal')}:{' '}
              <strong className="tabular-nums">
                {totals.total.toFixed(2)} {currency}
              </strong>
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label={t('invoices.standalone.publicNotes')}>
            <textarea
              value={publicNotes}
              onChange={(e) => setPublicNotes(e.target.value)}
              rows={2}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('invoices.standalone.internalNotes')}>
            <textarea
              value={internalNotes}
              onChange={(e) => setInternalNotes(e.target.value)}
              rows={2}
              className="w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
        </div>
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
      {required ? <span className="text-rose-500"> *</span> : null}
    </span>
    {children}
  </label>
);

const TextInput = ({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
}) => (
  <input
    type="text"
    value={value}
    onChange={(e) => onChange(e.target.value)}
    placeholder={placeholder}
    className="w-full rounded-md border border-slate-300 bg-white px-2 py-1 text-xs text-slate-800 placeholder:text-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder:text-slate-500"
  />
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
