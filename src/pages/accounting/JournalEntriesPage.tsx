import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { FilePlus, Plus, RotateCcw, ShieldCheck, Trash2 } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { formatDate, formatNumber } from '@/shared/lib/format';
import {
  useDeleteJournalEntry,
  useJournalEntriesQuery,
  usePostJournalEntry,
  useReverseJournalEntry,
} from '@/features/accounting/hooks/useJournalEntryQueries';
import { JournalEntryFormModal } from '@/features/accounting/ui/JournalEntryFormModal';
import type {
  JournalEntryStatus,
  JournalEntryType,
} from '@/features/accounting/model/journalEntry.types';

const STATUS_STYLES: Record<JournalEntryStatus, string> = {
  Draft: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Posted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Reversed: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const TYPE_LABELS: Record<JournalEntryType, string> = {
  Tahsil: 'Tahsil',
  Tediye: 'Tediye',
  Mahsup: 'Mahsup',
  Acilis: 'Açılış',
  Kapanis: 'Kapanış',
};

export const JournalEntriesPage = () => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const confirm = useConfirm();

  const [search, setSearch] = useState('');
  const [type, setType] = useState<JournalEntryType | ''>('');
  const [status, setStatus] = useState<JournalEntryStatus | ''>('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);

  const params = useMemo(
    () => ({
      search: search.trim() || undefined,
      type: type || undefined,
      status: status || undefined,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
      page,
      pageSize: 25,
    }),
    [search, type, status, fromDate, toDate, page],
  );

  const entries = useJournalEntriesQuery(params);
  const postMutation = usePostJournalEntry();
  const reverseMutation = useReverseJournalEntry();
  const deleteMutation = useDeleteJournalEntry();

  const items = entries.data?.data?.items ?? [];
  const total = entries.data?.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / 25));

  const post = async (id: string) => {
    try {
      await postMutation.mutateAsync(id);
      toast.success('Fiş post edildi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  const reverse = async (id: string) => {
    const ok = await confirm({
      title: 'Fişi Ters Çevir',
      message: 'Fiş ters çevrilecek ve yeni bir karşı-fiş oluşturulacak. Devam edilsin mi?',
      confirmLabel: 'Ters Çevir',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await reverseMutation.mutateAsync({ id });
      toast.success('Fiş ters çevrildi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  const remove = async (id: string) => {
    const ok = await confirm({
      title: 'Fişi Sil',
      message: 'Taslak fiş silinsin mi?',
      confirmLabel: 'Sil',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success('Fiş silindi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">Yevmiye Fişleri</h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            Tahsil / Tediye / Mahsup fişleri. Post edildiğinde Mizan'a yansır.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowForm(true)}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={12} />
          Yeni Fiş
        </button>
      </div>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
        <input
          type="search"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          placeholder="Fiş no / açıklama / ref"
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        />
        <select
          value={type}
          onChange={(e) => {
            setType(e.target.value as JournalEntryType | '');
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        >
          <option value="">Tüm Tipler</option>
          {Object.entries(TYPE_LABELS).map(([v, l]) => (
            <option key={v} value={v}>
              {l}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as JournalEntryStatus | '');
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        >
          <option value="">Tüm Durumlar</option>
          <option value="Draft">Taslak</option>
          <option value="Posted">Post Edildi</option>
          <option value="Reversed">Ters Çevrildi</option>
        </select>
        <input
          type="date"
          value={fromDate}
          onChange={(e) => {
            setFromDate(e.target.value);
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        />
        <input
          type="date"
          value={toDate}
          onChange={(e) => {
            setToDate(e.target.value);
            setPage(1);
          }}
          className="rounded border border-slate-200 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
        />
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">Fiş No</th>
              <th className="px-3 py-2 text-left">Tarih</th>
              <th className="px-3 py-2 text-left">Tip</th>
              <th className="px-3 py-2 text-left">Durum</th>
              <th className="px-3 py-2 text-left">Açıklama</th>
              <th className="px-3 py-2 text-right">Borç</th>
              <th className="px-3 py-2 text-right">Alacak</th>
              <th className="px-3 py-2 text-center">Satır</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {entries.isPending ? (
              <tr>
                <td colSpan={9} className="px-3 py-8 text-center text-sm text-slate-500">
                  Yükleniyor…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={9} className="px-3 py-8 text-center text-sm text-slate-500">
                  Filtrelere uyan fiş bulunamadı.
                </td>
              </tr>
            ) : (
              items.map((e) => (
                <tr
                  key={e.id}
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs">{e.number}</td>
                  <td className="px-3 py-2 text-xs">{formatDate(e.postingDate, locale)}</td>
                  <td className="px-3 py-2 text-xs">{TYPE_LABELS[e.type]}</td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[e.status]}`}
                    >
                      {e.status}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-300">
                    {e.description ?? <span className="text-slate-400">—</span>}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {formatNumber(e.totalDebit, locale)}
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-xs">
                    {formatNumber(e.totalCredit, locale)}
                  </td>
                  <td className="px-3 py-2 text-center text-xs">{e.lineCount}</td>
                  <td className="px-3 py-2">
                    <div className="flex items-center justify-end gap-0.5">
                      {e.status === 'Draft' && (
                        <>
                          <button
                            type="button"
                            onClick={() => post(e.id)}
                            className="rounded p-1 text-slate-400 hover:bg-emerald-50 hover:text-emerald-700 dark:hover:bg-emerald-500/10"
                            title="Post et"
                          >
                            <ShieldCheck size={12} />
                          </button>
                          <button
                            type="button"
                            onClick={() => remove(e.id)}
                            className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
                            title="Sil"
                          >
                            <Trash2 size={12} />
                          </button>
                        </>
                      )}
                      {e.status === 'Posted' && (
                        <button
                          type="button"
                          onClick={() => reverse(e.id)}
                          className="rounded p-1 text-slate-400 hover:bg-amber-50 hover:text-amber-700 dark:hover:bg-amber-500/10"
                          title="Ters çevir"
                        >
                          <RotateCcw size={12} />
                        </button>
                      )}
                      {e.status === 'Reversed' && (
                        <span className="text-[10px] text-slate-400">—</span>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-xs text-slate-500">
          <span>
            {total} fiş — sayfa {page} / {totalPages}
          </span>
          <div className="flex gap-1">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1}
              className="rounded border border-slate-200 bg-white px-2 py-1 hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
            >
              ←
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              className="rounded border border-slate-200 bg-white px-2 py-1 hover:bg-slate-50 disabled:opacity-30 dark:border-slate-700 dark:bg-slate-900"
            >
              →
            </button>
          </div>
        </div>
      )}

      {showForm && <JournalEntryFormModal onClose={() => setShowForm(false)} />}

      {items.length === 0 && entries.isFetched && (
        <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-xs text-slate-500 dark:border-slate-700">
          <FilePlus className="mx-auto mb-2 text-slate-400" size={28} />
          Hesap planınızı oluşturduktan sonra ilk yevmiye fişini buradan girebilirsiniz.
        </div>
      )}
    </div>
  );
};

export default JournalEntriesPage;
