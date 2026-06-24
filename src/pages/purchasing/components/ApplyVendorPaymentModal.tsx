import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Wallet } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
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
    <Modal
      open={true}
      title={t('VendorPayments.apply.title', { defaultValue: 'Tedarikçi Ödemesini Uygula' })}
      subtitle={t('VendorPayments.apply.subtitle', {
        defaultValue: '{{n}} faturasına uygulanacak ödemeyi seç.',
        n: bill.billNumber,
      })}
      icon={<Wallet size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'Vazgeç' })}
          </Button>
          <Button
            type="submit"
            form="apply-vendor-payment-form"
            isLoading={apply.isPending}
            disabled={apply.isPending || !selected}
          >
            {t('VendorPayments.apply.submit', { defaultValue: 'Uygula' })}
          </Button>
        </>
      }
    >
      <form id="apply-vendor-payment-form" onSubmit={onSubmit} className="space-y-3">
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
            <span className="font-mono font-semibold text-warning-700 dark:text-warning-300">
              {formatCurrency(bill.amountDue, locale, bill.currency)}
            </span>
          </div>
        </div>

        <div>
          <Select
            label={t('VendorPayments.apply.payment', { defaultValue: 'Ödeme' })}
            value={selectedId}
            onChange={(e) => {
              setSelectedId(e.target.value);
              const p = eligible.find((x) => x.id === e.target.value);
              if (p) {
                setAmount(Math.min(p.unappliedAmount, bill.amountDue).toString());
              }
            }}
            required
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
          </Select>
          {eligible.length === 0 && (
            <p className="mt-1 text-[10px] text-warning-600 dark:text-warning-400">
              {t('VendorPayments.apply.noEligible', {
                defaultValue: 'Bu tedarikçi için uygulanabilir ödeme bulunamadı.',
              })}
            </p>
          )}
        </div>

        <div>
          <Input
            label={t('VendorPayments.apply.amount', { defaultValue: 'Tutar' })}
            type="number"
            step="0.01"
            min="0.01"
            max={cap}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
            className="font-mono"
          />
          {selected && (
            <p className="mt-1 text-[10px] text-slate-500">
              {t('VendorPayments.apply.maxHint', {
                defaultValue: 'Maksimum {{m}}',
                m: formatCurrency(cap, locale, bill.currency),
              })}
            </p>
          )}
        </div>

        <Input
          label={t('VendorPayments.apply.notes', { defaultValue: 'Notlar' })}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={500}
        />
      </form>
    </Modal>
  );
};

export default ApplyVendorPaymentModal;
