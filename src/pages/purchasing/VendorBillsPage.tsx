import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { BadgeDollarSign, BookOpen, CheckCircle2, Link2, Plus, XCircle } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useVendorBillAction,
  useVendorBillApplicationsQuery,
  useVendorBillsQuery,
  useVendorPaymentsQuery,
} from '@/features/purchasing/hooks/useVendorBilling';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { VendorBillFormModal, VendorPaymentModal } from '@/features/purchasing/ui/VendorBillModals';
import { ApplyVendorPaymentModal } from '@/pages/purchasing/components/ApplyVendorPaymentModal';
import { SourceJournalEntriesModal } from '@/features/accounting/ui/SourceJournalEntriesModal';
import type { VendorBill, VendorBillStatus } from '@/features/purchasing/model/vendorBilling.types';

const STATUS_TONE: Record<VendorBillStatus, string> = {
  Draft: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Posted: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  PartiallyPaid: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Paid: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const STATUS_LABEL: Record<VendorBillStatus, string> = {
  Draft: 'Taslak',
  Posted: 'İşlendi',
  PartiallyPaid: 'Kısmi Ödendi',
  Paid: 'Ödendi',
  Cancelled: 'İptal',
};

const STATUSES: VendorBillStatus[] = ['Draft', 'Posted', 'PartiallyPaid', 'Paid', 'Cancelled'];

const fmtDate = (iso: string | null, locale: string) => formatDate(iso, locale);

type View = 'bills' | 'payments';

export const VendorBillsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();

  const [view, setView] = useState<View>('bills');
  const [status, setStatus] = useState<VendorBillStatus | ''>('');
  const [vendorId, setVendorId] = useState('');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [payBill, setPayBill] = useState<VendorBill | null>(null);
  const [applyBill, setApplyBill] = useState<VendorBill | null>(null);
  const [appsBillId, setAppsBillId] = useState<string | null>(null);
  const [glSource, setGlSource] = useState<{ id: string; label: string } | null>(null);

  const vendorFilter = vendorId || undefined;
  const billsQuery = useVendorBillsQuery({
    status: status || undefined,
    vendorId: vendorFilter,
    page,
    pageSize: 25,
  });
  const paymentsQuery = useVendorPaymentsQuery({ vendorId: vendorFilter, page, pageSize: 25 });
  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const action = useVendorBillAction();

  const vendors = vendorsQuery.data?.data?.items ?? [];
  const bills = billsQuery.data?.data?.items ?? [];
  const payments = paymentsQuery.data?.data?.items ?? [];
  const total =
    (view === 'bills' ? billsQuery.data?.data?.total : paymentsQuery.data?.data?.total) ?? 0;
  const totalPages =
    (view === 'bills' ? billsQuery.data?.data?.totalPages : paymentsQuery.data?.data?.totalPages) ??
    0;
  const isPending = view === 'bills' ? billsQuery.isPending : paymentsQuery.isPending;

  const switchView = (next: View) => {
    setView(next);
    setPage(1);
  };

  const run = async (bill: VendorBill, act: 'post' | 'cancel') => {
    if (act === 'cancel') {
      const ok = await confirm({
        title: t('ap.cancelTitle', { defaultValue: 'Faturayı İptal Et' }),
        message: t('ap.cancelConfirm', {
          defaultValue: '{{n}} iptal edilsin mi?',
          n: bill.billNumber,
        }),
        confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
        tone: 'danger',
      });
      if (!ok) return;
    }
    try {
      await action.mutateAsync({ id: bill.id, action: act });
      toast.success(t('ap.actionDone', { defaultValue: 'İşlem tamamlandı.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const tabBtn = (id: View, label: string) => (
    <button
      type="button"
      onClick={() => switchView(id)}
      className={`border-b-2 px-3 py-1.5 text-xs font-medium transition ${
        view === id
          ? 'border-indigo-600 text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
          : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
      }`}
    >
      {label}
    </button>
  );

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('ap.page.title', { defaultValue: 'Tedarikçi Faturaları' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('ap.page.subtitle', {
              defaultValue:
                'Tedarikçi faturalarını işle ve öde; tedarikçi cari hesabı otomatik güncellenir.',
            })}
          </p>
        </div>
        {view === 'bills' && (
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
          >
            <Plus size={13} />
            {t('ap.page.new', { defaultValue: 'Yeni Fatura' })}
          </button>
        )}
      </div>

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        {tabBtn('bills', t('ap.tab.bills', { defaultValue: 'Faturalar' }))}
        {tabBtn('payments', t('ap.tab.payments', { defaultValue: 'Ödemeler' }))}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <select
          value={vendorId}
          onChange={(e) => {
            setVendorId(e.target.value);
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">
            {t('ap.filter.allVendors', { defaultValue: 'Tüm tedarikçiler' })}
          </option>
          {vendors.map((v) => (
            <option key={v.id} value={v.id}>
              {v.name}
            </option>
          ))}
        </select>
        {view === 'bills' && (
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as VendorBillStatus | '');
              setPage(1);
            }}
            className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            <option value="">{t('ap.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABEL[s]}
              </option>
            ))}
          </select>
        )}
        <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
          {view === 'bills'
            ? t('ap.count', { defaultValue: '{{count}} fatura', count: total })
            : t('ap.paymentCount', { defaultValue: '{{count}} ödeme', count: total })}
        </span>
      </div>

      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : view === 'bills' ? (
          bills.length === 0 ? (
            <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('ap.empty', { defaultValue: 'Tedarikçi faturası bulunamadı.' })}
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
                <tr>
                  <th className="px-3 py-2 text-left">
                    {t('ap.cols.number', { defaultValue: 'Fatura No' })}
                  </th>
                  <th className="px-3 py-2 text-left">
                    {t('ap.cols.vendor', { defaultValue: 'Tedarikçi' })}
                  </th>
                  <th className="px-3 py-2 text-left">
                    {t('ap.cols.date', { defaultValue: 'Tarih' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('ap.cols.total', { defaultValue: 'Tutar' })}
                  </th>
                  <th className="px-3 py-2 text-right">
                    {t('ap.cols.due', { defaultValue: 'Kalan' })}
                  </th>
                  <th className="px-3 py-2 text-center">
                    {t('ap.cols.status', { defaultValue: 'Durum' })}
                  </th>
                  <th className="px-3 py-2" />
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {bills.map((b) => (
                  <tr key={b.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                    <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                      {b.billNumber}
                    </td>
                    <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{b.vendorName}</td>
                    <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                      {fmtDate(b.billDate, locale)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                      {formatCurrency(b.total, locale, b.currency)}
                    </td>
                    <td className="px-3 py-2 text-right font-mono text-amber-700 dark:text-amber-300">
                      {formatCurrency(b.amountDue, locale, b.currency)}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <span
                        className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[b.status]}`}
                      >
                        {STATUS_LABEL[b.status]}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="inline-flex items-center gap-1">
                        {b.status !== 'Draft' && (
                          <button
                            type="button"
                            onClick={() => setGlSource({ id: b.id, label: b.billNumber })}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                            title={t('ap.actions.glEntry', { defaultValue: 'Muhasebe Fişi' })}
                          >
                            <BookOpen size={13} />
                          </button>
                        )}
                        {b.status === 'Draft' && (
                          <button
                            type="button"
                            onClick={() => run(b, 'post')}
                            disabled={action.isPending}
                            className="rounded p-1 text-indigo-500 hover:bg-indigo-50 disabled:opacity-40 dark:hover:bg-indigo-500/10"
                            title={t('ap.actions.post', { defaultValue: 'İşle (cariye yaz)' })}
                          >
                            <CheckCircle2 size={13} />
                          </button>
                        )}
                        {(b.status === 'Posted' || b.status === 'PartiallyPaid') && (
                          <button
                            type="button"
                            onClick={() => setPayBill(b)}
                            className="rounded p-1 text-emerald-500 hover:bg-emerald-50 dark:hover:bg-emerald-500/10"
                            title={t('ap.actions.pay', { defaultValue: 'Öde' })}
                          >
                            <BadgeDollarSign size={13} />
                          </button>
                        )}
                        {(b.status === 'Posted' || b.status === 'PartiallyPaid') && (
                          <button
                            type="button"
                            onClick={() => setApplyBill(b)}
                            className="rounded p-1 text-sky-500 hover:bg-sky-50 dark:hover:bg-sky-500/10"
                            title={t('VendorPayments.applyPayment', {
                              defaultValue: 'Mevcut ödemeyi uygula',
                            })}
                          >
                            <Link2 size={13} />
                          </button>
                        )}
                        {b.status !== 'Draft' && (
                          <button
                            type="button"
                            onClick={() => setAppsBillId(b.id)}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                            title={t('VendorPayments.viewApplications', {
                              defaultValue: 'Uygulanan ödemeler',
                            })}
                          >
                            <BookOpen size={13} />
                          </button>
                        )}
                        {b.status !== 'Paid' && b.status !== 'Cancelled' && (
                          <button
                            type="button"
                            onClick={() => run(b, 'cancel')}
                            disabled={action.isPending}
                            className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 disabled:opacity-40 dark:hover:bg-rose-500/10"
                            title={t('ap.actions.cancel', { defaultValue: 'İptal' })}
                          >
                            <XCircle size={13} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        ) : payments.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('ap.payments.empty', { defaultValue: 'Tedarikçi ödemesi bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('ap.payCols.number', { defaultValue: 'Ödeme No' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('ap.cols.vendor', { defaultValue: 'Tedarikçi' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('ap.cols.date', { defaultValue: 'Tarih' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('ap.payCols.method', { defaultValue: 'Yöntem' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('ap.payCols.amount', { defaultValue: 'Tutar' })}
                </th>
                <th className="px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {payments.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {p.paymentNumber}
                  </td>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{p.vendorName}</td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {fmtDate(p.paymentDate, locale)}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {p.method ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(p.amount, locale, p.currency)}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <button
                      type="button"
                      onClick={() => setGlSource({ id: p.id, label: p.paymentNumber })}
                      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                      title={t('ap.actions.glEntry', { defaultValue: 'Muhasebe Fişi' })}
                    >
                      <BookOpen size={13} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-end gap-1 text-xs">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
          >
            {t('common.prev', { defaultValue: 'Önceki' })}
          </button>
          <span className="px-2 text-slate-500">
            {page} / {totalPages}
          </span>
          <button
            type="button"
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded border border-slate-200 bg-white px-2 py-1 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900"
          >
            {t('common.next', { defaultValue: 'Sonraki' })}
          </button>
        </div>
      )}

      {createOpen && <VendorBillFormModal onClose={() => setCreateOpen(false)} />}
      {payBill && <VendorPaymentModal bill={payBill} onClose={() => setPayBill(null)} />}
      {applyBill && <ApplyVendorPaymentModal bill={applyBill} onClose={() => setApplyBill(null)} />}
      {appsBillId && (
        <VendorBillApplicationsModal billId={appsBillId} onClose={() => setAppsBillId(null)} />
      )}
      {glSource && (
        <SourceJournalEntriesModal
          sourceDocumentId={glSource.id}
          title={t('ap.glEntryTitle', { defaultValue: 'Muhasebe Fişi · {{n}}', n: glSource.label })}
          onClose={() => setGlSource(null)}
        />
      )}
    </div>
  );
};

interface ApplicationsModalProps {
  billId: string;
  onClose: () => void;
}

const VendorBillApplicationsModal = ({ billId, onClose }: ApplicationsModalProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const { data, isPending } = useVendorBillApplicationsQuery(billId);
  const items = data?.data ?? [];

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onClick={onClose}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-lg space-y-3 rounded-lg bg-white p-4 shadow-xl dark:bg-slate-900"
      >
        <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('VendorPayments.applications.title', { defaultValue: 'Uygulanan Ödemeler' })}
        </h2>
        {isPending ? (
          <p className="text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </p>
        ) : items.length === 0 ? (
          <p className="text-sm text-slate-500">
            {t('VendorPayments.applications.empty', { defaultValue: 'Uygulanan ödeme yok.' })}
          </p>
        ) : (
          <table className="w-full text-xs">
            <thead className="text-[10px] uppercase tracking-wider text-slate-500">
              <tr>
                <th className="px-2 py-1 text-left">
                  {t('VendorPayments.applications.paymentNo', { defaultValue: 'Ödeme No' })}
                </th>
                <th className="px-2 py-1 text-left">
                  {t('VendorPayments.applications.date', { defaultValue: 'Tarih' })}
                </th>
                <th className="px-2 py-1 text-right">
                  {t('VendorPayments.applications.amount', { defaultValue: 'Tutar' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((a) => (
                <tr key={a.id}>
                  <td className="px-2 py-1 font-mono text-slate-700 dark:text-slate-300">
                    {a.paymentNumber}
                  </td>
                  <td className="px-2 py-1 text-slate-500">{formatDate(a.appliedAtUtc, locale)}</td>
                  <td className="px-2 py-1 text-right font-mono">
                    {formatCurrency(a.appliedAmount, locale, 'TRY')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <div className="flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
          >
            {t('common.close', { defaultValue: 'Kapat' })}
          </button>
        </div>
      </div>
    </div>
  );
};

export default VendorBillsPage;
