import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Banknote, CheckCircle2, CreditCard, Receipt, Wallet, X } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { useOpenInvoicesForCustomer, useCreatePayment } from '../hooks/usePaymentQueries';
import type { PaymentMethod } from '../model/payment.types';

interface Props {
  customerId: string;
  customerName: string;
  currency: string;
  onClose: () => void;
  defaultDate?: string;
}

interface AllocationDraft {
  invoiceId: string;
  invoiceNumber: string;
  amountDue: number;
  selected: boolean;
  amount: number;
}

const METHODS: { value: PaymentMethod; icon: React.ReactNode; labelKey: string }[] = [
  {
    value: 'BankTransfer',
    icon: <Receipt size={14} />,
    labelKey: 'invoices.paymentMethod.BankTransfer',
  },
  { value: 'Cash', icon: <Wallet size={14} />, labelKey: 'invoices.paymentMethod.Cash' },
  {
    value: 'CreditCard',
    icon: <CreditCard size={14} />,
    labelKey: 'invoices.paymentMethod.CreditCard',
  },
  { value: 'Check', icon: <Banknote size={14} />, labelKey: 'invoices.paymentMethod.Check' },
];

const fmtCurrency = (n: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(n);
  } catch {
    return `${n.toFixed(2)} ${currency}`;
  }
};

const fmtDate = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short' }).format(new Date(iso));
  } catch {
    return iso.slice(0, 10);
  }
};

export const PaymentCreateModal = ({
  customerId,
  customerName,
  currency,
  onClose,
  defaultDate,
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const openInvoicesQuery = useOpenInvoicesForCustomer(customerId);
  const createMutation = useCreatePayment();

  const [paymentDate, setPaymentDate] = useState(
    defaultDate ?? new Date().toISOString().slice(0, 10),
  );
  const [method, setMethod] = useState<PaymentMethod>('BankTransfer');
  const [amount, setAmount] = useState('');
  const [referenceNumber, setReferenceNumber] = useState('');
  const [bankAccountInfo, setBankAccountInfo] = useState('');
  const [checkNumber, setCheckNumber] = useState('');
  const [checkDueDate, setCheckDueDate] = useState('');
  const [notes, setNotes] = useState('');

  const invoices = useMemo(
    () => openInvoicesQuery.data?.data ?? [],
    [openInvoicesQuery.data?.data],
  );
  const [allocationOverrides, setAllocationOverrides] = useState<
    Record<string, { selected: boolean; amount: number }>
  >({});

  const allocations: AllocationDraft[] = useMemo(
    () =>
      invoices.map((inv) => {
        const override = allocationOverrides[inv.id];
        return {
          invoiceId: inv.id,
          invoiceNumber: inv.invoiceNumber,
          amountDue: inv.amountDue,
          selected: override?.selected ?? false,
          amount: override?.amount ?? 0,
        };
      }),
    [invoices, allocationOverrides],
  );

  const setAllocations = (updater: (prev: AllocationDraft[]) => AllocationDraft[]) => {
    const next = updater(allocations);
    const map: Record<string, { selected: boolean; amount: number }> = {};
    next.forEach((a) => {
      map[a.invoiceId] = { selected: a.selected, amount: a.amount };
    });
    setAllocationOverrides(map);
  };

  const totalAllocated = allocations
    .filter((a) => a.selected)
    .reduce((s, a) => s + (Number.isFinite(a.amount) ? a.amount : 0), 0);
  const numericAmount = Number(amount) || 0;
  const remaining = Math.max(0, numericAmount - totalAllocated);
  const overAllocated = totalAllocated > numericAmount + 0.001;

  const applyOptimal = () => {
    // FIFO: oldest invoice first
    let pool = numericAmount;
    setAllocations((prev) =>
      prev.map((a) => {
        if (pool <= 0) return { ...a, selected: false, amount: 0 };
        const take = Math.min(pool, a.amountDue);
        pool -= take;
        return { ...a, selected: take > 0, amount: take };
      }),
    );
  };

  const updateAllocation = (idx: number, patch: Partial<AllocationDraft>) =>
    setAllocations((prev) => prev.map((a, i) => (i === idx ? { ...a, ...patch } : a)));

  const handleSubmit = async () => {
    if (numericAmount <= 0) {
      toast.error(
        t('payments.create.amountRequired', { defaultValue: 'Amount must be positive.' }),
      );
      return;
    }
    if (overAllocated) {
      toast.error(
        t('payments.create.overAllocated', { defaultValue: 'Allocated exceeds payment amount.' }),
      );
      return;
    }
    try {
      await createMutation.mutateAsync({
        customerId,
        paymentDate,
        method,
        amount: numericAmount,
        currency,
        bankAccountInfo: bankAccountInfo || null,
        referenceNumber: referenceNumber || null,
        checkNumber: method === 'Check' ? checkNumber || null : null,
        checkDueDate: method === 'Check' && checkDueDate ? checkDueDate : null,
        notes: notes || null,
        autoConfirm: true,
        applications: allocations
          .filter((a) => a.selected && a.amount > 0)
          .map((a) => ({ invoiceId: a.invoiceId, appliedAmount: a.amount })),
      });
      toast.success(t('payments.create.success', { defaultValue: 'Payment recorded' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="w-full max-w-2xl overflow-hidden rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        onChange={() => setDirty(true)}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-3 dark:border-slate-800">
          <div>
            <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
              {t('payments.create.title', { defaultValue: 'Record payment' })}
            </h2>
            <p className="text-[11px] text-slate-500 dark:text-slate-400">{customerName}</p>
          </div>
          <button
            type="button"
            onClick={requestClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <div className="max-h-[70vh] space-y-4 overflow-y-auto px-5 py-4">
          <div className="grid grid-cols-2 gap-3">
            <Field label={t('payments.create.paymentDate', { defaultValue: 'Payment date' })}>
              <input
                type="date"
                value={paymentDate}
                onChange={(e) => setPaymentDate(e.target.value)}
                className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
              />
            </Field>
            <Field label={t('payments.create.amount', { defaultValue: 'Amount' })}>
              <input
                type="number"
                step="0.01"
                min="0"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full rounded border border-slate-200 px-3 py-2 text-sm font-semibold dark:border-slate-700 dark:bg-slate-900"
                placeholder="0.00"
              />
            </Field>
          </div>

          <Field label={t('payments.create.method', { defaultValue: 'Method' })}>
            <div className="grid grid-cols-4 gap-2">
              {METHODS.map((m) => (
                <button
                  type="button"
                  key={m.value}
                  onClick={() => setMethod(m.value)}
                  className={`inline-flex items-center justify-center gap-1.5 rounded border px-2 py-2 text-xs font-medium transition ${
                    method === m.value
                      ? 'border-indigo-400 bg-indigo-50 text-indigo-700 dark:border-indigo-500/40 dark:bg-indigo-500/20 dark:text-indigo-300'
                      : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
                  }`}
                >
                  {m.icon}
                  {t(m.labelKey as never)}
                </button>
              ))}
            </div>
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label={t('payments.create.reference', { defaultValue: 'Reference / TX no' })}>
              <input
                value={referenceNumber}
                onChange={(e) => setReferenceNumber(e.target.value)}
                className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
              />
            </Field>
            <Field label={t('payments.create.bankAccount', { defaultValue: 'Bank account' })}>
              <input
                value={bankAccountInfo}
                onChange={(e) => setBankAccountInfo(e.target.value)}
                className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
              />
            </Field>
          </div>

          {method === 'Check' && (
            <div className="grid grid-cols-2 gap-3 rounded border border-amber-200 bg-amber-50/40 p-2 dark:border-amber-500/30 dark:bg-amber-500/10">
              <Field label={t('payments.create.checkNumber', { defaultValue: 'Check #' })}>
                <input
                  value={checkNumber}
                  onChange={(e) => setCheckNumber(e.target.value)}
                  className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
                />
              </Field>
              <Field label={t('payments.create.checkDueDate', { defaultValue: 'Check due date' })}>
                <input
                  type="date"
                  value={checkDueDate}
                  onChange={(e) => setCheckDueDate(e.target.value)}
                  className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
                />
              </Field>
            </div>
          )}

          <div className="rounded-lg border border-slate-200 dark:border-slate-800">
            <div className="flex items-center justify-between border-b border-slate-200 bg-slate-50 px-3 py-2 dark:border-slate-800 dark:bg-slate-900/40">
              <div className="text-xs font-semibold text-slate-700 dark:text-slate-200">
                {t('payments.create.applyToInvoices', { defaultValue: 'Apply to open invoices' })}
              </div>
              <button
                type="button"
                onClick={applyOptimal}
                disabled={numericAmount <= 0 || invoices.length === 0}
                className="inline-flex items-center gap-1 rounded border border-indigo-200 bg-white px-2 py-1 text-[11px] font-medium text-indigo-700 hover:bg-indigo-50 disabled:opacity-50 dark:border-indigo-500/30 dark:bg-slate-900 dark:text-indigo-300"
              >
                {t('payments.create.autoApply', { defaultValue: 'Auto apply (FIFO)' })}
              </button>
            </div>
            {invoices.length === 0 ? (
              <div className="px-3 py-4 text-center text-xs text-slate-500">
                {t('payments.create.noOpenInvoices', {
                  defaultValue: 'No open invoices to apply.',
                })}
              </div>
            ) : (
              <ul className="divide-y divide-slate-200 dark:divide-slate-800">
                {allocations.map((a, idx) => (
                  <li key={a.invoiceId} className="flex items-center gap-3 px-3 py-2">
                    <input
                      type="checkbox"
                      checked={a.selected}
                      onChange={(e) => updateAllocation(idx, { selected: e.target.checked })}
                      className="h-4 w-4 rounded border-slate-300 text-indigo-600"
                    />
                    <div className="flex-1">
                      <div className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {a.invoiceNumber}
                      </div>
                      <div className="text-[11px] text-slate-500">
                        {t('payments.create.amountDue', { defaultValue: 'Due' })}:{' '}
                        {fmtCurrency(a.amountDue, currency, locale)}
                        {invoices[idx]?.isOverdue && (
                          <span className="ml-2 inline-flex items-center gap-1 rounded bg-red-100 px-1.5 text-[10px] font-medium text-red-700 dark:bg-red-500/20 dark:text-red-300">
                            {t('invoices.status.Overdue')}
                          </span>
                        )}
                        {invoices[idx] && ' · '}
                        {invoices[idx] && t('invoices.fields.dueDate')}:{' '}
                        {invoices[idx] && fmtDate(invoices[idx].dueDate, locale)}
                      </div>
                    </div>
                    <input
                      type="number"
                      min="0"
                      max={a.amountDue}
                      step="0.01"
                      disabled={!a.selected}
                      value={a.amount}
                      onChange={(e) => updateAllocation(idx, { amount: Number(e.target.value) })}
                      className="w-28 rounded border border-slate-200 px-2 py-1 text-right text-sm dark:border-slate-700 dark:bg-slate-900"
                    />
                  </li>
                ))}
              </ul>
            )}
          </div>

          <Field label={t('payments.create.notes', { defaultValue: 'Notes' })}>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full rounded border border-slate-200 px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
            />
          </Field>

          <div
            className={`rounded border p-3 text-xs ${
              overAllocated
                ? 'border-red-200 bg-red-50/60 dark:border-red-500/30 dark:bg-red-500/10'
                : 'border-emerald-200 bg-emerald-50/60 dark:border-emerald-500/30 dark:bg-emerald-500/10'
            }`}
          >
            <div className="flex justify-between font-medium">
              <span>{t('payments.create.amount', { defaultValue: 'Amount' })}</span>
              <span>{fmtCurrency(numericAmount, currency, locale)}</span>
            </div>
            <div className="flex justify-between">
              <span>{t('payments.create.allocated', { defaultValue: 'Allocated' })}</span>
              <span>{fmtCurrency(totalAllocated, currency, locale)}</span>
            </div>
            <div className="flex justify-between font-semibold">
              <span>
                {t('payments.create.unapplied', { defaultValue: 'Will remain unapplied' })}
              </span>
              <span>{fmtCurrency(remaining, currency, locale)}</span>
            </div>
            {overAllocated && (
              <div className="mt-1 text-[11px] text-red-700 dark:text-red-300">
                {t('payments.create.overAllocated', {
                  defaultValue: 'Allocated exceeds payment amount.',
                })}
              </div>
            )}
          </div>
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-3 dark:border-slate-800">
          <button
            type="button"
            onClick={requestClose}
            className="rounded px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={createMutation.isPending || numericAmount <= 0 || overAllocated}
            className="inline-flex items-center gap-1.5 rounded bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
          >
            <CheckCircle2 size={14} />
            {t('payments.create.confirm', { defaultValue: 'Record payment' })}
          </button>
        </div>
      </div>
    </div>
  );
};

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div>
    <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
      {label}
    </label>
    {children}
  </div>
);
