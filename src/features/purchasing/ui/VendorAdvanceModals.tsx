import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Wallet } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import {
  useCreateVendorPayment,
  useOffsetVendorAdvance,
  useVendorPaymentsQuery,
} from '../hooks/useVendorBilling';
import type { VendorBill, VendorPayment } from '../model/vendorBilling.types';

const todayIso = () => new Date().toISOString().slice(0, 10);

export const VendorAdvancePaymentModal = ({ onClose }: { onClose: () => void }) => {
  const { t } = useTranslation();
  const createMutation = useCreateVendorPayment();
  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const vendors = vendorsQuery.data?.data?.items ?? [];

  const [vendorId, setVendorId] = useState('');
  const [amount, setAmount] = useState('');
  const [paymentDate, setPaymentDate] = useState(todayIso());
  const [currency, setCurrency] = useState('TRY');
  const [method, setMethod] = useState('BankTransfer');
  const [notes, setNotes] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amt = Number(amount) || 0;
    if (!vendorId || amt <= 0) {
      toast.error(
        t('Vendors.advance.invalid', { defaultValue: 'Tedarikçi ve geçerli bir tutar giriniz.' }),
      );
      return;
    }
    try {
      await createMutation.mutateAsync({
        vendorId,
        amount: amt,
        paymentDate: new Date(paymentDate).toISOString(),
        currency: currency.toUpperCase(),
        method,
        notes: notes.trim() || null,
        isAdvance: true,
      });
      toast.success(t('Vendors.advance.created', { defaultValue: 'Tedarikçi avansı kaydedildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('Vendors.advance.title', { defaultValue: 'Tedarikçi Avansı' })}
      subtitle={t('Vendors.advance.subtitle', {
        defaultValue: 'Faturasız ön ödeme; sonra bir faturaya mahsup edilir.',
      })}
      icon={<Wallet size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="vendor-advance-form" isLoading={createMutation.isPending}>
            {t('Vendors.advance.submit', { defaultValue: 'Avansı Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="vendor-advance-form" onSubmit={submit} className="dense-form space-y-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Vendors.advance.vendor', { defaultValue: 'Tedarikçi' })} *
          </label>
          <select
            value={vendorId}
            onChange={(e) => setVendorId(e.target.value)}
            className={`mt-1 ${fieldBaseClasses(false)}`}
          >
            <option value="">
              {t('Vendors.advance.selectVendor', { defaultValue: 'Seçiniz…' })}
            </option>
            {vendors.map((v) => (
              <option key={v.id} value={v.id}>
                {v.name}
              </option>
            ))}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('Vendors.advance.amount', { defaultValue: 'Tutar' })} *
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className={`mt-1 text-right ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('Vendors.advance.currency', { defaultValue: 'Para Birimi' })}
            </label>
            <input
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              maxLength={3}
              className={`mt-1 uppercase ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('Vendors.advance.date', { defaultValue: 'Tarih' })}
            </label>
            <input
              type="date"
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('Vendors.advance.method', { defaultValue: 'Ödeme Yöntemi' })}
            </label>
            <select
              value={method}
              onChange={(e) => setMethod(e.target.value)}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            >
              <option value="BankTransfer">
                {t('ap.pay.bankTransfer', { defaultValue: 'Havale/EFT' })}
              </option>
              <option value="Cash">{t('ap.pay.cash', { defaultValue: 'Nakit' })}</option>
              <option value="Check">{t('ap.pay.check', { defaultValue: 'Çek' })}</option>
              <option value="Card">{t('ap.pay.card', { defaultValue: 'Kart' })}</option>
            </select>
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Vendors.advance.notes', { defaultValue: 'Açıklama' })}
          </label>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={200}
            className={`mt-1 ${fieldBaseClasses(false)}`}
          />
        </div>
      </form>
    </Modal>
  );
};

export const OffsetVendorAdvanceModal = ({
  bill,
  onClose,
}: {
  bill: VendorBill;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const paymentsQuery = useVendorPaymentsQuery({ vendorId: bill.vendorId, page: 1, pageSize: 100 });
  const offset = useOffsetVendorAdvance();
  const [selectedId, setSelectedId] = useState('');
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');

  const advances = useMemo<VendorPayment[]>(
    () =>
      (paymentsQuery.data?.data?.items ?? []).filter(
        (p) => p.isAdvance && !p.isVoided && p.unappliedAmount > 0,
      ),
    [paymentsQuery.data],
  );

  const selected = advances.find((p) => p.id === selectedId);
  const cap = selected ? Math.min(selected.unappliedAmount, bill.amountDue) : bill.amountDue;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected) return;
    const value = parseFloat(amount);
    if (Number.isNaN(value) || value <= 0) {
      toast.error(
        t('Vendors.offset.invalidAmount', { defaultValue: 'Geçerli bir tutar giriniz.' }),
      );
      return;
    }
    try {
      await offset.mutateAsync({
        vendorPaymentId: selected.id,
        vendorBillId: bill.id,
        amount: value,
        notes: notes.trim() || null,
      });
      toast.success(t('Vendors.offset.success', { defaultValue: 'Avans faturaya mahsup edildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('Vendors.offset.title', { defaultValue: 'Tedarikçi Avansını Mahsup Et' })}
      subtitle={t('Vendors.offset.subtitle', {
        defaultValue: '{{n}} faturasına mahsup edilecek avansı seç.',
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
            form="offset-vendor-advance-form"
            isLoading={offset.isPending}
            disabled={offset.isPending || !selected}
          >
            {t('Vendors.offset.submit', { defaultValue: 'Mahsup Et' })}
          </Button>
        </>
      }
    >
      <form id="offset-vendor-advance-form" onSubmit={onSubmit} className="space-y-3">
        <div className="rounded border border-slate-200 bg-slate-50 px-3 py-2 text-xs dark:border-slate-700 dark:bg-slate-800/50">
          <div className="flex justify-between">
            <span className="text-slate-500">
              {t('Vendors.offset.billTotal', { defaultValue: 'Fatura Toplam' })}
            </span>
            <span className="font-mono text-slate-800 dark:text-slate-100">
              {formatCurrency(bill.total, locale, bill.currency)}
            </span>
          </div>
          <div className="mt-1 flex justify-between">
            <span className="text-slate-500">
              {t('Vendors.offset.amountDue', { defaultValue: 'Kalan' })}
            </span>
            <span className="font-mono font-semibold text-warning-700 dark:text-warning-300">
              {formatCurrency(bill.amountDue, locale, bill.currency)}
            </span>
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Vendors.offset.advance', { defaultValue: 'Avans' })}
          </label>
          <select
            value={selectedId}
            onChange={(e) => {
              setSelectedId(e.target.value);
              const p = advances.find((x) => x.id === e.target.value);
              if (p) setAmount(Math.min(p.unappliedAmount, bill.amountDue).toString());
            }}
            className={`mt-1 ${fieldBaseClasses(false)}`}
            required
          >
            <option value="">
              {t('Vendors.offset.pickAdvance', { defaultValue: 'Avans seç…' })}
            </option>
            {advances.map((p) => (
              <option key={p.id} value={p.id}>
                {p.paymentNumber} · {formatCurrency(p.unappliedAmount, locale, p.currency)}{' '}
                {t('Vendors.offset.unapplied', { defaultValue: 'kullanılmamış' })}
              </option>
            ))}
          </select>
          {advances.length === 0 && (
            <p className="mt-1 text-[10px] text-warning-600 dark:text-warning-400">
              {t('Vendors.offset.noAdvances', {
                defaultValue: 'Bu tedarikçi için mahsup edilecek avans bulunamadı.',
              })}
            </p>
          )}
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Vendors.offset.amount', { defaultValue: 'Tutar' })} *
          </label>
          <input
            type="number"
            step="0.01"
            min="0.01"
            max={cap}
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
            className={`mt-1 text-right font-mono ${fieldBaseClasses(false)}`}
          />
          {selected && (
            <p className="mt-1 text-[10px] text-slate-500">
              {t('Vendors.offset.maxHint', {
                defaultValue: 'Maksimum {{m}}',
                m: formatCurrency(cap, locale, bill.currency),
              })}
            </p>
          )}
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('Vendors.offset.notes', { defaultValue: 'Açıklama' })}
          </label>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={500}
            className={`mt-1 ${fieldBaseClasses(false)}`}
          />
        </div>
      </form>
    </Modal>
  );
};
