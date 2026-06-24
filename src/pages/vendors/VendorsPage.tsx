import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { Archive, CheckCircle2, Ban, Building2, Plus, Star } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { Modal } from '@/shared/ui/Modal/Modal';
import { formatCurrency } from '@/shared/lib/format';
import {
  useApproveVendor,
  useArchiveVendor,
  useBlockVendor,
  useVendorsQuery,
} from '@/features/vendors/hooks/useVendorQueries';
import { VendorFormModal } from '@/features/vendors/ui/VendorFormModal';
import type { VendorStatus } from '@/features/vendors/model/vendor.types';

const STATUS_STYLES: Record<VendorStatus, string> = {
  Active: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Blocked: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Archived: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  PendingApproval: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
};

const STATUS_ORDER: VendorStatus[] = ['Active', 'Blocked', 'Archived', 'PendingApproval'];

export const VendorsPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const confirm = useConfirm();

  const statusLabel = (s: VendorStatus) => t(`Vendors.status.${s}`);

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<VendorStatus | ''>('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [showForm, setShowForm] = useState(false);
  const [blockTarget, setBlockTarget] = useState<string | null>(null);

  const params = useMemo(
    () => ({
      search: search.trim() || undefined,
      status: status || undefined,
      page,
      pageSize,
    }),
    [search, status, page, pageSize],
  );

  const vendors = useVendorsQuery(params);
  const approveMutation = useApproveVendor();
  const archiveMutation = useArchiveVendor();

  const items = vendors.data?.data?.items ?? [];
  const total = vendors.data?.data?.total ?? 0;

  const approve = async (id: string) => {
    try {
      await approveMutation.mutateAsync(id);
      toast.success(t('Vendors.approve.success'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const archive = async (id: string) => {
    const ok = await confirm({
      title: t('Vendors.archive.title'),
      message: t('Vendors.archive.message'),
      confirmLabel: t('Vendors.archive.confirm'),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await archiveMutation.mutateAsync(id);
      toast.success(t('Vendors.archive.success'));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Building2 size={20} />}
          title={t('Vendors.title')}
          subtitle={t('Vendors.subtitle')}
          actions={
            <Button size="sm" onClick={() => setShowForm(true)}>
              <Plus size={14} />
              {t('Vendors.newVendor')}
            </Button>
          }
        />
      }
      toolbar={
        <div className="flex flex-wrap gap-2">
          <Input
            type="search"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('Vendors.searchPlaceholder')}
            className="w-full sm:w-72"
          />
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as VendorStatus | '');
              setPage(1);
            }}
            className="w-full sm:w-48"
          >
            <option value="">{t('Vendors.allStatuses')}</option>
            {STATUS_ORDER.map((v) => (
              <option key={v} value={v}>
                {statusLabel(v)}
              </option>
            ))}
          </Select>
        </div>
      }
      pagination={
        total > 0 ? (
          <div className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
            <Pagination
              page={page}
              pageSize={pageSize}
              total={total}
              onPageChange={setPage}
              pageSizeOptions={[10, 25, 50, 100]}
              onPageSizeChange={(s) => {
                setPageSize(s);
                setPage(1);
              }}
              itemLabel={t('Vendors.itemLabel')}
            />
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">{t('Vendors.cols.code')}</th>
              <th className="px-3 py-2 text-left">{t('Vendors.cols.vendor')}</th>
              <th className="px-3 py-2 text-left">{t('Vendors.cols.taxNumber')}</th>
              <th className="px-3 py-2 text-left">{t('Vendors.cols.contact')}</th>
              <th className="px-3 py-2 text-left">{t('Vendors.cols.status')}</th>
              <th className="px-3 py-2 text-right">{t('Vendors.cols.balance')}</th>
              <th className="px-3 py-2 text-right">{t('Vendors.cols.overdue')}</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {vendors.isPending ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('Vendors.loading')}
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('Vendors.empty')}
                </td>
              </tr>
            ) : (
              items.map((v) => (
                <tr
                  key={v.id}
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs">{v.code ?? '—'}</td>
                  <td className="px-3 py-2 text-xs">
                    <Link
                      to={`/dashboard/vendors/${v.id}`}
                      className="font-semibold text-slate-900 hover:text-primary-600 dark:text-slate-100 dark:hover:text-primary-400"
                    >
                      {v.name}
                    </Link>
                    {v.legalName && <div className="text-[10px] text-slate-500">{v.legalName}</div>}
                  </td>
                  <td className="px-3 py-2 font-mono text-xs">{v.taxNumber ?? '—'}</td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-300">
                    {v.email && <div>{v.email}</div>}
                    {v.phone && <div className="text-slate-500">{v.phone}</div>}
                    {!v.email && !v.phone && <span className="text-slate-400">—</span>}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[v.status]}`}
                    >
                      {statusLabel(v.status)}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {formatCurrency(v.currentBalance, locale, v.defaultCurrency)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {v.overdueAmount > 0 ? (
                      <span className="text-danger-600 dark:text-danger-400">
                        {formatCurrency(v.overdueAmount, locale, v.defaultCurrency)}
                      </span>
                    ) : (
                      <span className="text-slate-400">—</span>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex items-center justify-end gap-0.5">
                      {v.status === 'PendingApproval' && (
                        <button
                          type="button"
                          onClick={() => approve(v.id)}
                          className="rounded p-1 text-slate-400 hover:bg-success-50 hover:text-success-700 dark:hover:bg-success-500/10"
                          title={t('Vendors.actions.approve')}
                        >
                          <CheckCircle2 size={12} />
                        </button>
                      )}
                      {v.status === 'Active' && (
                        <button
                          type="button"
                          onClick={() => setBlockTarget(v.id)}
                          className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                          title={t('Vendors.actions.block')}
                        >
                          <Ban size={12} />
                        </button>
                      )}
                      {(v.status === 'Active' || v.status === 'Blocked') && (
                        <button
                          type="button"
                          onClick={() => archive(v.id)}
                          className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
                          title={t('Vendors.actions.archive')}
                        >
                          <Archive size={12} />
                        </button>
                      )}
                      <Link
                        to={`/dashboard/vendors/${v.id}`}
                        className="rounded p-1 text-slate-400 hover:bg-primary-50 hover:text-primary-700 dark:hover:bg-primary-500/10"
                        title={t('Vendors.actions.detail')}
                      >
                        <Star size={12} />
                      </Link>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {showForm && <VendorFormModal onClose={() => setShowForm(false)} />}
      {blockTarget && (
        <BlockReasonModal vendorId={blockTarget} onClose={() => setBlockTarget(null)} />
      )}
    </ListPageTemplate>
  );
};

const BlockReasonModal = ({ vendorId, onClose }: { vendorId: string; onClose: () => void }) => {
  const { t } = useTranslation();
  const blockMutation = useBlockVendor();
  const [reason, setReason] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) return;
    try {
      await blockMutation.mutateAsync({ id: vendorId, reason: reason.trim() });
      toast.success(t('Vendors.block.success'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal open onClose={onClose} title={t('Vendors.block.title')} size="md">
      <form onSubmit={submit} className="space-y-4">
        <Textarea
          label={t('Vendors.block.reasonLabel')}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          required
          rows={3}
          maxLength={500}
        />
        <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
          <Button type="button" variant="secondary" size="sm" onClick={onClose}>
            {t('Vendors.cancel')}
          </Button>
          <Button
            type="submit"
            variant="danger"
            size="sm"
            isLoading={blockMutation.isPending}
            disabled={!reason.trim()}
          >
            {t('Vendors.block.submit')}
          </Button>
        </div>
      </form>
    </Modal>
  );
};

export default VendorsPage;
