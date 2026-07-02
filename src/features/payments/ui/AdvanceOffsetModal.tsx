import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, Wallet } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useOffsetCustomerAdvance,
  useOpenInvoicesForCustomer,
  usePaymentsByCustomer,
} from '../hooks/usePaymentQueries';

interface Props {
  customerId: string;
  customerName: string;
  currency: string;
  onClose: () => void;
}

interface AllocationDraft {
  invoiceId: string;
  invoiceNumber: string;
  amountDue: number;
  selected: boolean;
  amount: number;
}

const fmtCurrency = (n: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(n);
  } catch {
    return `${n.toFixed(2)} ${currency}`;
  }
};

export const AdvanceOffsetModal = ({ customerId, customerName, currency, onClose }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const paymentsQuery = usePaymentsByCustomer(customerId);
  const openInvoicesQuery = useOpenInvoicesForCustomer(customerId);
  const offsetMutation = useOffsetCustomerAdvance();

  const [selectedAdvanceId, setSelectedAdvanceId] = useState('');
  const [allocationOverrides, setAllocationOverrides] = useState<
    Record<string, { selected: boolean; amount: number }>
  >({});

  const advances = useMemo(
    () => (paymentsQuery.data?.data ?? []).filter((p) => p.isAdvance && p.unappliedAmount > 0),
    [paymentsQuery.data?.data],
  );
  const selectedAdvance = advances.find((a) => a.id === selectedAdvanceId);
  const advanceBalance = selectedAdvance?.unappliedAmount ?? 0;

  const invoices = useMemo(
    () => openInvoicesQuery.data?.data ?? [],
    [openInvoicesQuery.data?.data],
  );

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

  const updateAllocation = (idx: number, patch: Partial<AllocationDraft>) =>
    setAllocations((prev) => prev.map((a, i) => (i === idx ? { ...a, ...patch } : a)));

  const totalAllocated = allocations
    .filter((a) => a.selected)
    .reduce((s, a) => s + (Number.isFinite(a.amount) ? a.amount : 0), 0);
  const remaining = Math.max(0, advanceBalance - totalAllocated);
  const overAllocated = totalAllocated > advanceBalance + 0.001;

  const applyOptimal = () => {
    let pool = advanceBalance;
    setAllocations((prev) =>
      prev.map((a) => {
        if (pool <= 0) return { ...a, selected: false, amount: 0 };
        const take = Math.min(pool, a.amountDue);
        pool -= take;
        return { ...a, selected: take > 0, amount: take };
      }),
    );
  };

  const handleSubmit = async () => {
    if (!selectedAdvance) {
      toast.error(
        t('Payments.offset.selectAdvanceRequired', { defaultValue: 'Önce bir avans seçin.' }),
      );
      return;
    }
    const applications = allocations
      .filter((a) => a.selected && a.amount > 0)
      .map((a) => ({ invoiceId: a.invoiceId, appliedAmount: a.amount }));
    if (applications.length === 0) {
      toast.error(
        t('Payments.offset.noAllocation', { defaultValue: 'En az bir fatura ve tutar girin.' }),
      );
      return;
    }
    if (overAllocated) {
      toast.error(
        t('Payments.offset.overAllocated', {
          defaultValue: 'Mahsup tutarı avans bakiyesini aşıyor.',
        }),
      );
      return;
    }
    try {
      await offsetMutation.mutateAsync({ id: selectedAdvance.id, applications });
      toast.success(t('Payments.offset.success', { defaultValue: 'Avans mahsup edildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('Payments.offset.title', { defaultValue: 'Avans Mahsup Et' })}
      subtitle={customerName}
      icon={<Wallet size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button
            type="button"
            onClick={handleSubmit}
            isLoading={offsetMutation.isPending}
            disabled={!selectedAdvance || totalAllocated <= 0 || overAllocated}
          >
            <CheckCircle2 size={14} />
            {t('Payments.offset.confirm', { defaultValue: 'Mahsubu kaydet' })}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Payments.offset.selectAdvance', { defaultValue: 'Avans ödemesi' })}
          </label>
          {advances.length === 0 ? (
            <div className="rounded-lg border border-slate-200 px-3 py-4 text-center text-xs text-slate-500 dark:border-slate-800">
              {t('Payments.offset.noAdvances', {
                defaultValue: 'Mahsup edilecek avans ödemesi yok.',
              })}
            </div>
          ) : (
            <select
              value={selectedAdvanceId}
              onChange={(e) => {
                setSelectedAdvanceId(e.target.value);
                setAllocationOverrides({});
              }}
              className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              <option value="">
                {t('Payments.offset.pickAdvance', { defaultValue: 'Avans seç…' })}
              </option>
              {advances.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.paymentNumber} · {fmtCurrency(a.unappliedAmount, a.currency, locale)}{' '}
                  {t('Payments.offset.balance', { defaultValue: 'avans bakiyesi' })}
                </option>
              ))}
            </select>
          )}
        </div>

        {selectedAdvance && (
          <div className="rounded-lg border border-slate-200 dark:border-slate-800">
            <div className="flex items-center justify-between border-b border-slate-200 bg-slate-50 px-3 py-2 dark:border-slate-800 dark:bg-slate-900/40">
              <div className="text-xs font-semibold text-slate-700 dark:text-slate-200">
                {t('Payments.offset.applyToInvoices', { defaultValue: 'Açık faturalara uygula' })}
              </div>
              <button
                type="button"
                onClick={applyOptimal}
                disabled={invoices.length === 0 || advanceBalance <= 0}
                className="inline-flex items-center gap-1 rounded border border-primary-200 bg-white px-2 py-1 text-[11px] font-medium text-primary-700 hover:bg-primary-50 disabled:opacity-50 dark:border-primary-500/30 dark:bg-slate-900 dark:text-primary-300"
              >
                {t('Payments.offset.autoApply', { defaultValue: 'Otomatik dağıt' })}
              </button>
            </div>
            {invoices.length === 0 ? (
              <div className="px-3 py-4 text-center text-xs text-slate-500">
                {t('Payments.offset.noOpenInvoices', { defaultValue: 'Açık fatura yok.' })}
              </div>
            ) : (
              <ul className="divide-y divide-slate-200 dark:divide-slate-800">
                {allocations.map((a, idx) => (
                  <li key={a.invoiceId} className="flex items-center gap-3 px-3 py-2">
                    <input
                      type="checkbox"
                      checked={a.selected}
                      onChange={(e) => updateAllocation(idx, { selected: e.target.checked })}
                      className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    />
                    <div className="flex-1">
                      <div className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {a.invoiceNumber}
                      </div>
                      <div className="text-[11px] text-slate-500">
                        {t('Payments.offset.amountDue', { defaultValue: 'Kalan' })}:{' '}
                        {fmtCurrency(a.amountDue, currency, locale)}
                      </div>
                    </div>
                    <input
                      type="number"
                      min="0"
                      max={Math.min(a.amountDue, advanceBalance)}
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
            <div
              className={`border-t p-3 text-xs ${
                overAllocated
                  ? 'border-danger-200 bg-danger-50/60 dark:border-danger-500/30 dark:bg-danger-500/10'
                  : 'border-slate-200 bg-slate-50/60 dark:border-slate-800 dark:bg-slate-900/30'
              }`}
            >
              <div className="flex justify-between">
                <span>
                  {t('Payments.offset.advanceBalance', { defaultValue: 'Avans bakiyesi' })}
                </span>
                <span className="font-mono">{fmtCurrency(advanceBalance, currency, locale)}</span>
              </div>
              <div className="flex justify-between">
                <span>{t('Payments.offset.allocated', { defaultValue: 'Mahsup edilen' })}</span>
                <span className="font-mono">{fmtCurrency(totalAllocated, currency, locale)}</span>
              </div>
              <div className="flex justify-between font-semibold">
                <span>{t('Payments.offset.remaining', { defaultValue: 'Kalan avans' })}</span>
                <span className="font-mono">{fmtCurrency(remaining, currency, locale)}</span>
              </div>
              {overAllocated && (
                <div className="mt-1 text-[11px] text-danger-700 dark:text-danger-300">
                  {t('Payments.offset.overAllocated', {
                    defaultValue: 'Mahsup tutarı avans bakiyesini aşıyor.',
                  })}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};
