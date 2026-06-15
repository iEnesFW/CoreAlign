import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { useCreateVendorBill, useCreateVendorPayment } from '../hooks/useVendorBilling';
import type { VendorBill } from '../model/vendorBilling.types';

const todayIso = () => new Date().toISOString().slice(0, 10);
const inputClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

const Shell = ({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) => {
  const { t } = useTranslation();
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('common.close', { defaultValue: 'Kapat' })}
          >
            <X size={16} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
};

export const VendorBillFormModal = ({ onClose }: { onClose: () => void }) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const createMutation = useCreateVendorBill();
  const vendors = vendorsQuery.data?.data?.items ?? [];

  const [vendorId, setVendorId] = useState('');
  const [billNumber, setBillNumber] = useState('');
  const [billDate, setBillDate] = useState(todayIso());
  const [dueDate, setDueDate] = useState('');
  const [currency, setCurrency] = useState('TRY');
  const [subtotal, setSubtotal] = useState('');
  const [taxAmount, setTaxAmount] = useState('');
  const [notes, setNotes] = useState('');

  const total = useMemo(
    () => (Number(subtotal) || 0) + (Number(taxAmount) || 0),
    [subtotal, taxAmount],
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vendorId || !billNumber.trim() || (Number(subtotal) || 0) < 0) {
      toast.error(t('ap.bill.invalid', { defaultValue: 'Tedarikçi, fatura no ve tutar giriniz.' }));
      return;
    }
    try {
      await createMutation.mutateAsync({
        vendorId,
        billNumber: billNumber.trim(),
        billDate: new Date(billDate).toISOString(),
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
        currency: currency.toUpperCase(),
        subtotal: Number(subtotal) || 0,
        taxAmount: Number(taxAmount) || 0,
        notes: notes.trim() || null,
      });
      toast.success(t('ap.bill.created', { defaultValue: 'Tedarikçi faturası oluşturuldu.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Shell
      title={t('ap.bill.newTitle', { defaultValue: 'Yeni Tedarikçi Faturası' })}
      onClose={onClose}
    >
      <form onSubmit={submit} className="dense-form space-y-3 p-4">
        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.vendor', { defaultValue: 'Tedarikçi' })} *
            </label>
            <select
              value={vendorId}
              onChange={(e) => setVendorId(e.target.value)}
              className={inputClass}
            >
              <option value="">{t('ap.bill.selectVendor', { defaultValue: 'Seçiniz…' })}</option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.number', { defaultValue: 'Fatura No' })} *
            </label>
            <input
              value={billNumber}
              onChange={(e) => setBillNumber(e.target.value)}
              maxLength={64}
              className={inputClass}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.currency', { defaultValue: 'Para Birimi' })}
            </label>
            <input
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              maxLength={3}
              className={`${inputClass} uppercase`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.date', { defaultValue: 'Fatura Tarihi' })}
            </label>
            <input
              type="date"
              value={billDate}
              onChange={(e) => setBillDate(e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.due', { defaultValue: 'Vade Tarihi' })}
            </label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.subtotal', { defaultValue: 'Ara Toplam' })} *
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={subtotal}
              onChange={(e) => setSubtotal(e.target.value)}
              className={`${inputClass} text-right`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.tax', { defaultValue: 'KDV Tutarı' })}
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={taxAmount}
              onChange={(e) => setTaxAmount(e.target.value)}
              className={`${inputClass} text-right`}
            />
          </div>
        </div>
        <div className="text-right text-sm font-bold text-slate-900 dark:text-slate-100">
          {t('ap.bill.total', { defaultValue: 'Genel Toplam' })}:{' '}
          {formatCurrency(total, locale, currency)}
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('ap.bill.notes', { defaultValue: 'Açıklama' })}
          </label>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={200}
            className={inputClass}
          />
        </div>
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel', { defaultValue: 'İptal' })}
          </button>
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {createMutation.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('common.save', { defaultValue: 'Kaydet' })}
          </button>
        </div>
      </form>
    </Shell>
  );
};

export const VendorPaymentModal = ({
  bill,
  onClose,
}: {
  bill: VendorBill;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const payMutation = useCreateVendorPayment();

  const [amount, setAmount] = useState(String(bill.amountDue));
  const [paymentDate, setPaymentDate] = useState(todayIso());
  const [method, setMethod] = useState('BankTransfer');
  const [notes, setNotes] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amt = Number(amount) || 0;
    if (amt <= 0 || amt > bill.amountDue + 0.0001) {
      toast.error(
        t('ap.pay.invalid', { defaultValue: 'Geçerli bir tutar giriniz (kalan borcu aşamaz).' }),
      );
      return;
    }
    try {
      await payMutation.mutateAsync({
        vendorId: bill.vendorId,
        amount: amt,
        paymentDate: new Date(paymentDate).toISOString(),
        currency: bill.currency,
        method,
        vendorBillId: bill.id,
        notes: notes.trim() || null,
      });
      toast.success(t('ap.pay.done', { defaultValue: 'Ödeme kaydedildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Shell
      title={`${t('ap.pay.title', { defaultValue: 'Tedarikçi Ödemesi' })} — ${bill.billNumber}`}
      onClose={onClose}
    >
      <form onSubmit={submit} className="dense-form space-y-3 p-4">
        <div className="rounded bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
          {bill.vendorName} · {t('ap.pay.due', { defaultValue: 'Kalan borç' })}:{' '}
          <span className="font-semibold">
            {formatCurrency(bill.amountDue, locale, bill.currency)}
          </span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.amount', { defaultValue: 'Tutar' })} *
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className={`${inputClass} text-right`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.date', { defaultValue: 'Tarih' })}
            </label>
            <input
              type="date"
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className={inputClass}
            />
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.method', { defaultValue: 'Ödeme Yöntemi' })}
            </label>
            <select
              value={method}
              onChange={(e) => setMethod(e.target.value)}
              className={inputClass}
            >
              <option value="BankTransfer">
                {t('ap.pay.bankTransfer', { defaultValue: 'Havale/EFT' })}
              </option>
              <option value="Cash">{t('ap.pay.cash', { defaultValue: 'Nakit' })}</option>
              <option value="Check">{t('ap.pay.check', { defaultValue: 'Çek' })}</option>
              <option value="Card">{t('ap.pay.card', { defaultValue: 'Kart' })}</option>
            </select>
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.notes', { defaultValue: 'Açıklama' })}
            </label>
            <input
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={200}
              className={inputClass}
            />
          </div>
        </div>
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel', { defaultValue: 'İptal' })}
          </button>
          <button
            type="submit"
            disabled={payMutation.isPending}
            className="rounded bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
          >
            {payMutation.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('ap.pay.submit', { defaultValue: 'Ödeme Yap' })}
          </button>
        </div>
      </form>
    </Shell>
  );
};
