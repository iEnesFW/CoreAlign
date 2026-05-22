import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { Archive, CheckCircle2, Ban, Plus, Star, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
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
  Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Blocked: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Archived: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  PendingApproval: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
};

const STATUS_LABELS: Record<VendorStatus, string> = {
  Active: 'Aktif',
  Blocked: 'Bloke',
  Archived: 'Arşiv',
  PendingApproval: 'Onay Bekliyor',
};

export const VendorsPage = () => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const confirm = useConfirm();

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
      toast.success('Tedarikçi onaylandı.');
    } catch (err) {
      toastApiError(err);
    }
  };

  const archive = async (id: string) => {
    const ok = await confirm({
      title: 'Arşivle',
      message: 'Tedarikçi arşivlensin mi?',
      confirmLabel: 'Arşivle',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await archiveMutation.mutateAsync(id);
      toast.success('Tedarikçi arşivlendi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">Tedarikçiler</h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            Tedarikçi master, cari hesap ve banka bilgileri.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowForm(true)}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={12} />
          Yeni Tedarikçi
        </button>
      </div>

      <div className="flex flex-wrap gap-2">
        <input
          type="search"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          placeholder="Ad, kod, VKN, e-posta…"
          className="w-72 rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        />
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as VendorStatus | '');
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        >
          <option value="">Tüm Durumlar</option>
          {Object.entries(STATUS_LABELS).map(([v, l]) => (
            <option key={v} value={v}>
              {l}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">Kod</th>
              <th className="px-3 py-2 text-left">Tedarikçi</th>
              <th className="px-3 py-2 text-left">VKN</th>
              <th className="px-3 py-2 text-left">İletişim</th>
              <th className="px-3 py-2 text-left">Durum</th>
              <th className="px-3 py-2 text-right">Bakiye</th>
              <th className="px-3 py-2 text-right">Vadesi Geçen</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {vendors.isPending ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-sm text-slate-500">
                  Yükleniyor…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-3 py-8 text-center text-sm text-slate-500">
                  Filtrelere uyan tedarikçi bulunamadı.
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
                      className="font-semibold text-slate-900 hover:text-indigo-600 dark:text-slate-100 dark:hover:text-indigo-400"
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
                      {STATUS_LABELS[v.status]}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {formatCurrency(v.currentBalance, locale, v.defaultCurrency)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {v.overdueAmount > 0 ? (
                      <span className="text-rose-600 dark:text-rose-400">
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
                          className="rounded p-1 text-slate-400 hover:bg-emerald-50 hover:text-emerald-700 dark:hover:bg-emerald-500/10"
                          title="Onayla"
                        >
                          <CheckCircle2 size={12} />
                        </button>
                      )}
                      {v.status === 'Active' && (
                        <button
                          type="button"
                          onClick={() => setBlockTarget(v.id)}
                          className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
                          title="Bloke et"
                        >
                          <Ban size={12} />
                        </button>
                      )}
                      {(v.status === 'Active' || v.status === 'Blocked') && (
                        <button
                          type="button"
                          onClick={() => archive(v.id)}
                          className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
                          title="Arşivle"
                        >
                          <Archive size={12} />
                        </button>
                      )}
                      <Link
                        to={`/dashboard/vendors/${v.id}`}
                        className="rounded p-1 text-slate-400 hover:bg-indigo-50 hover:text-indigo-700 dark:hover:bg-indigo-500/10"
                        title="Detay"
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

      {total > 0 && (
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
            itemLabel="tedarikçi"
          />
        </div>
      )}

      {showForm && <VendorFormModal onClose={() => setShowForm(false)} />}
      {blockTarget && (
        <BlockReasonModal vendorId={blockTarget} onClose={() => setBlockTarget(null)} />
      )}
    </div>
  );
};

const BlockReasonModal = ({ vendorId, onClose }: { vendorId: string; onClose: () => void }) => {
  const blockMutation = useBlockVendor();
  const [reason, setReason] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) return;
    try {
      await blockMutation.mutateAsync({ id: vendorId, reason: reason.trim() });
      toast.success('Tedarikçi bloke edildi.');
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            Tedarikçiyi Bloke Et
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={16} />
          </button>
        </div>
        <form onSubmit={submit} className="space-y-3 p-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Bloke etme nedeni *
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              required
              rows={3}
              maxLength={500}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              İptal
            </button>
            <button
              type="submit"
              disabled={blockMutation.isPending || !reason.trim()}
              className="rounded bg-rose-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-rose-700 disabled:opacity-50"
            >
              {blockMutation.isPending ? 'Kaydediliyor…' : 'Bloke Et'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default VendorsPage;
