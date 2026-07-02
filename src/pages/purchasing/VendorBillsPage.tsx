import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  BadgeDollarSign,
  BookOpen,
  CheckCircle2,
  HandCoins,
  Link2,
  Pencil,
  Plus,
  Receipt,
  ShieldCheck,
  Wallet,
  XCircle,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { Badge } from '@/shared/ui/Badge/Badge';
import { Modal } from '@/shared/ui/Modal/Modal';
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';
import {
  useVendorBillAction,
  useVendorBillApplicationsQuery,
  useVendorBillsQuery,
  useVendorPaymentsQuery,
} from '@/features/purchasing/hooks/useVendorBilling';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { VendorBillFormModal, VendorPaymentModal } from '@/features/purchasing/ui/VendorBillModals';
import {
  OffsetVendorAdvanceModal,
  VendorAdvancePaymentModal,
} from '@/features/purchasing/ui/VendorAdvanceModals';
import { ApplyVendorPaymentModal } from '@/pages/purchasing/components/ApplyVendorPaymentModal';
import { SourceJournalEntriesModal } from '@/features/accounting/ui/SourceJournalEntriesModal';
import { usePurchasingApprove } from '@/features/purchasing/hooks/usePurchasingApprove';
import type { VendorBill, VendorBillStatus } from '@/features/purchasing/model/vendorBilling.types';

const STATUS_VARIANT: Record<VendorBillStatus, BadgeVariant> = {
  Draft: 'neutral',
  Posted: 'info',
  PartiallyPaid: 'warning',
  Paid: 'success',
  Cancelled: 'danger',
  PendingApproval: 'warning',
};

const STATUS_LABEL_KEY: Record<VendorBillStatus, string> = {
  Draft: 'VendorBills.Status.Draft',
  Posted: 'VendorBills.Status.Posted',
  PartiallyPaid: 'VendorBills.Status.PartiallyPaid',
  Paid: 'VendorBills.Status.Paid',
  Cancelled: 'VendorBills.Status.Cancelled',
  PendingApproval: 'VendorBills.Status.PendingApproval',
};

const STATUS_LABEL_FALLBACK: Record<VendorBillStatus, string> = {
  Draft: 'Taslak',
  Posted: 'İşlendi',
  PartiallyPaid: 'Kısmi Ödendi',
  Paid: 'Ödendi',
  Cancelled: 'İptal',
  PendingApproval: 'Onay Bekliyor',
};

const STATUSES: VendorBillStatus[] = [
  'Draft',
  'Posted',
  'PartiallyPaid',
  'Paid',
  'Cancelled',
  'PendingApproval',
];

const fmtDate = (iso: string | null, locale: string) => formatDate(iso, locale);

type View = 'bills' | 'payments';

export const VendorBillsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();
  const canApprove = usePurchasingApprove();

  const statusLabel = (s: VendorBillStatus) =>
    t(STATUS_LABEL_KEY[s], { defaultValue: STATUS_LABEL_FALLBACK[s] });

  const hasLedger = (s: VendorBillStatus) =>
    s === 'Posted' || s === 'PartiallyPaid' || s === 'Paid';

  const [view, setView] = useState<View>('bills');
  const [status, setStatus] = useState<VendorBillStatus | ''>('');
  const [vendorId, setVendorId] = useState('');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [editBill, setEditBill] = useState<VendorBill | null>(null);
  const [payBill, setPayBill] = useState<VendorBill | null>(null);
  const [applyBill, setApplyBill] = useState<VendorBill | null>(null);
  const [offsetBill, setOffsetBill] = useState<VendorBill | null>(null);
  const [advanceOpen, setAdvanceOpen] = useState(false);
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

  const run = async (bill: VendorBill, act: 'post' | 'approve' | 'cancel') => {
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
    if (act === 'approve') {
      const ok = await confirm({
        title: t('ap.approveTitle', { defaultValue: 'Faturayı Onayla' }),
        message: t('ap.approveConfirm', {
          defaultValue: '{{n}} onaylanıp muhasebeleştirilsin mi?',
          n: bill.billNumber,
        }),
        confirmLabel: t('common.confirm', { defaultValue: 'Onayla' }),
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
          ? 'border-primary-600 text-primary-700 dark:border-primary-400 dark:text-primary-300'
          : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
      }`}
    >
      {label}
    </button>
  );

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Receipt size={20} />}
          title={t('ap.page.title', { defaultValue: 'Tedarikçi Faturaları' })}
          subtitle={t('ap.page.subtitle', {
            defaultValue:
              'Tedarikçi faturalarını işle ve öde; tedarikçi cari hesabı otomatik güncellenir.',
          })}
          actions={
            view === 'bills' ? (
              <Button size="sm" onClick={() => setCreateOpen(true)}>
                <Plus size={14} />
                {t('ap.page.new', { defaultValue: 'Yeni Fatura' })}
              </Button>
            ) : (
              <Button size="sm" variant="secondary" onClick={() => setAdvanceOpen(true)}>
                <Wallet size={14} />
                {t('Vendors.advance.newButton', { defaultValue: 'Yeni Avans' })}
              </Button>
            )
          }
        />
      }
      toolbar={
        <div className="flex flex-col gap-3">
          <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
            {tabBtn('bills', t('ap.tab.bills', { defaultValue: 'Faturalar' }))}
            {tabBtn('payments', t('ap.tab.payments', { defaultValue: 'Ödemeler' }))}
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Select
              value={vendorId}
              onChange={(e) => {
                setVendorId(e.target.value);
                setPage(1);
              }}
              className="w-full sm:w-48"
            >
              <option value="">
                {t('ap.filter.allVendors', { defaultValue: 'Tüm tedarikçiler' })}
              </option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </Select>
            {view === 'bills' && (
              <Select
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value as VendorBillStatus | '');
                  setPage(1);
                }}
                className="w-full sm:w-48"
              >
                <option value="">
                  {t('ap.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}
                </option>
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {statusLabel(s)}
                  </option>
                ))}
              </Select>
            )}
            <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
              {view === 'bills'
                ? t('ap.count', { defaultValue: '{{count}} fatura', count: total })
                : t('ap.paymentCount', { defaultValue: '{{count}} ödeme', count: total })}
            </span>
          </div>
        </div>
      }
      pagination={
        totalPages > 1 ? (
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
        ) : undefined
      }
    >
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
                    <td className="px-3 py-2 text-right font-mono text-warning-700 dark:text-warning-300">
                      {formatCurrency(b.amountDue, locale, b.currency)}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <Badge
                        variant={STATUS_VARIANT[b.status]}
                        className={
                          b.status === 'PendingApproval' && b.holdReason ? 'cursor-help' : undefined
                        }
                      >
                        <span
                          title={
                            b.status === 'PendingApproval' && b.holdReason
                              ? b.holdReason
                              : undefined
                          }
                        >
                          {statusLabel(b.status)}
                        </span>
                      </Badge>
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="inline-flex items-center gap-1">
                        {hasLedger(b.status) && (
                          <button
                            type="button"
                            onClick={() => setGlSource({ id: b.id, label: b.billNumber })}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                            title={t('ap.actions.glEntry', { defaultValue: 'Muhasebe Fişi' })}
                          >
                            <BookOpen size={13} />
                          </button>
                        )}
                        {(b.status === 'Draft' || b.status === 'PendingApproval') && (
                          <button
                            type="button"
                            onClick={() => setEditBill(b)}
                            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                            title={t('ap.actions.edit', { defaultValue: 'Düzenle' })}
                          >
                            <Pencil size={13} />
                          </button>
                        )}
                        {b.status === 'Draft' && (
                          <button
                            type="button"
                            onClick={() => run(b, 'post')}
                            disabled={action.isPending}
                            className="rounded p-1 text-primary-500 hover:bg-primary-50 disabled:opacity-40 dark:hover:bg-primary-500/10"
                            title={t('ap.actions.post', { defaultValue: 'İşle (cariye yaz)' })}
                          >
                            <CheckCircle2 size={13} />
                          </button>
                        )}
                        {b.status === 'PendingApproval' && canApprove && (
                          <button
                            type="button"
                            onClick={() => run(b, 'approve')}
                            disabled={action.isPending}
                            className="rounded p-1 text-warning-500 hover:bg-warning-50 disabled:opacity-40 dark:hover:bg-warning-500/10"
                            title={t('ap.actions.approve', {
                              defaultValue: 'Onayla ve muhasebeleştir',
                            })}
                          >
                            <ShieldCheck size={13} />
                          </button>
                        )}
                        {(b.status === 'Posted' || b.status === 'PartiallyPaid') && (
                          <button
                            type="button"
                            onClick={() => setPayBill(b)}
                            className="rounded p-1 text-success-500 hover:bg-success-50 dark:hover:bg-success-500/10"
                            title={t('ap.actions.pay', { defaultValue: 'Öde' })}
                          >
                            <BadgeDollarSign size={13} />
                          </button>
                        )}
                        {(b.status === 'Posted' || b.status === 'PartiallyPaid') && (
                          <button
                            type="button"
                            onClick={() => setApplyBill(b)}
                            className="rounded p-1 text-info-500 hover:bg-info-50 dark:hover:bg-info-500/10"
                            title={t('VendorPayments.applyPayment', {
                              defaultValue: 'Mevcut ödemeyi uygula',
                            })}
                          >
                            <Link2 size={13} />
                          </button>
                        )}
                        {(b.status === 'Posted' || b.status === 'PartiallyPaid') && (
                          <button
                            type="button"
                            onClick={() => setOffsetBill(b)}
                            className="rounded p-1 text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-500/10"
                            title={t('Vendors.offset.action', { defaultValue: 'Avans Mahsup Et' })}
                          >
                            <HandCoins size={13} />
                          </button>
                        )}
                        {hasLedger(b.status) && (
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
                            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-40 dark:hover:bg-danger-500/10"
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
                    <span className="inline-flex items-center gap-1.5">
                      {p.paymentNumber}
                      {p.isAdvance && (
                        <span className="rounded bg-amber-100 px-1.5 text-[10px] font-medium text-amber-800 dark:bg-amber-500/20 dark:text-amber-300">
                          {t('Vendors.advance.badge', { defaultValue: 'Avans' })}
                        </span>
                      )}
                    </span>
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

      {createOpen && <VendorBillFormModal onClose={() => setCreateOpen(false)} />}
      {editBill && <VendorBillFormModal bill={editBill} onClose={() => setEditBill(null)} />}
      {payBill && <VendorPaymentModal bill={payBill} onClose={() => setPayBill(null)} />}
      {applyBill && <ApplyVendorPaymentModal bill={applyBill} onClose={() => setApplyBill(null)} />}
      {offsetBill && (
        <OffsetVendorAdvanceModal bill={offsetBill} onClose={() => setOffsetBill(null)} />
      )}
      {advanceOpen && <VendorAdvancePaymentModal onClose={() => setAdvanceOpen(false)} />}
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
    </ListPageTemplate>
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
    <Modal
      open
      onClose={onClose}
      size="lg"
      title={t('VendorPayments.applications.title', { defaultValue: 'Uygulanan Ödemeler' })}
      footer={
        <Button type="button" variant="secondary" size="sm" onClick={onClose}>
          {t('common.close', { defaultValue: 'Kapat' })}
        </Button>
      }
    >
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
    </Modal>
  );
};

export default VendorBillsPage;
