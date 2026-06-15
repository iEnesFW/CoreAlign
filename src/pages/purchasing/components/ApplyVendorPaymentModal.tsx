import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { formatCurrency } from '@/shared/lib/format';
import {
  useApplyVendorPayment,
  useVendorPaymentsQuery,
} from '@/features/purchasing/hooks/useVendorBilling';
import type { VendorBill, VendorPayment } from '@/features/purchasing/model/vendorBilling.types';

interface Props {
  bill: VendorBill;
  onClose: () => void;
}

export const ApplyVendorPaymentModal = ({ bill, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const payments = useVendorPaymentsQuery({ vendorId: bill.vendorId, page: 1, pageSize: 100 });
  const apply = useApplyVendorPayment();
  const [selectedId, setSelectedId] = useState('');
  const [amount, setAmount] = useState(bill.amountDue.toString());
  const [notes, setNotes] = useState('');

  const eligible = useMemo<VendorPayment[]>(
    () => (payments.data?.data?.items ?? []).filter((p) => !p.isVoided && p.unappliedAmount > 0),
    [payments.data],
  );

  const selected = eligible.find((p) => p.id === selectedId);
  const cap = selected ? Math.min(selected.unappliedAmount, bill.amountDue) : bill.amountDue;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected) return;
    const value = parseFloat(amount);
    if (Number.isNaN(value) || value <= 0) return;
    try {
      await apply.mutateAsync({
        vendorPaymentId: selected.id,
        vendorBillId: bill.id,
        amount: value,
        notes: notes || null,
      });
      toast.success(
        t('VendorPayments.applySuccess', { defaultValue: 'Ödeme faturaya uygulandı.' }),
      );
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onClick={onClose}
    >
      <form
        onClick={(e) => e.stopPropagation()}
        onSubmit={onSubmit}
        className="w-full max-w-md space-y-3 rounded-lg bg-white p-4 shadow-xl dark:bg-slate-900"
      >
        <div>
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('VendorPayments.apply.title', { defaultValue: 'Tedarikçi Ödemesini Uygula' })}
          </h2>
          <p className="mt-0.5 text-[11px] text-slate-500 dark:text-slate-400">
            {t('VendorPayments.apply.subtitle', {
              defaultValue: '{{n}} faturasına uygulanacak ödemeyi seç.',
              n: bill.billNumber,
            })}
          </p>
        </div>

        <div className="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs dark:border-slate-700 dark:bg-slate-800/50">
          <div className="flex justify-between">
            <span className="text-slate-500">
              {t('VendorPayments.apply.billTotal', { defaultValue: 'Fatura Toplam' })}
            </span>
            <span className="font-mono text-slate-800 dark:text-slate-100">
              {formatCurrency(bill.total, locale, bill.currency)}
            </span>
          </div>
          <div className="mt-1 flex justify-between">
            <span className="text-slate-500">
              {t('VendorPayments.apply.amountDue', { defaultValue: 'Kalan' })}
            </span>
            <span className="font-mono font-semibold text-amber-700 dark:text-amber-300">
              {formatCurrency(bill.amountDue, locale, bill.currency)}
            </span>
          </div>
        </div>

        <label className="block text-xs">
          <span className="mb-1 block text-slate-600 dark:text-slate-400">
            {t('VendorPayments.apply.payment', { defaultValue: 'Ödeme' })}
          </span>
          <select
            value={selectedId}
            onChange={(e) => {
              setSelectedId(e.target.value);
              const p = eligible.find((x) => x.id === e.target.value);
              if (p) {
                setAmount(Math.min(p.unappliedAmount, bill.amountDue).toString());
              }
            }}
            required
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            <option value="">
              {t('VendorPayments.apply.pickPayment', { defaultValue: 'Ödeme seç…' })}
            </option>
            {eligible.map((p) => (
              <option key={p.id} value={p.id}>
                {p.paymentNumber} · {formatCurrency(p.unappliedAmount, locale, p.currency)}{' '}
                {t('VendorPayments.apply.unapplied', { defaultValue: 'uygulanmamış' })}
              </option>
            ))}
          </select>
          {eligible.length === 0 && (
            <p className="mt-1 text-[10px] text-amber-600 dark:text-amber-400">
              {t('VendorPayments.apply.noEligible', {
                defaultValue: 'Bu tedarikçi için uygulanabilir ödeme bulunamadı.',
              })}
            </p>
          )}
        </label>

        <label className="block text-xs">
          <span className="mb-1 block text-slate-600 dark:text-slate-400">
            {t('VendorPayments.apply.amount', { defaultValue: 'Tutar' })}
          </span>
          <input
            type="number"
            step="0.01"
            min="0.01"
            max={cap}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm font-mono dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
          {selected && (
            <p className="mt-1 text-[10px] text-slate-500">
              {t('VendorPayments.apply.maxHint', {
                defaultValue: 'Maksimum {{m}}',
                m: formatCurrency(cap, locale, bill.currency),
              })}
            </p>
          )}
        </label>

        <label className="block text-xs">
          <span className="mb-1 block text-slate-600 dark:text-slate-400">
            {t('VendorPayments.apply.notes', { defaultValue: 'Notlar' })}
          </span>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={500}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </label>

        <div className="flex justify-end gap-2 pt-1">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
          >
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </button>
          <button
            type="submit"
            disabled={apply.isPending || !selected}
            className="rounded bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
          >
            {t('VendorPayments.apply.submit', { defaultValue: 'Uygula' })}
          </button>
        </div>
      </form>
    </div>
  );
};

export default ApplyVendorPaymentModal;
