import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { BookOpen, FilePlus, Plus, RotateCcw, ShieldCheck, Trash2 } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Badge } from '@/shared/ui/Badge/Badge';
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
import type { BadgeVariant } from '@/shared/ui/Badge/Badge';

const STATUS_VARIANTS: Record<JournalEntryStatus, BadgeVariant> = {
  Draft: 'neutral',
  Posted: 'success',
  Reversed: 'danger',
};

const TYPE_LABELS: Record<JournalEntryType, { key: string; defaultValue: string }> = {
  Tahsil: { key: 'JournalEntries.type.Tahsil', defaultValue: 'Tahsil' },
  Tediye: { key: 'JournalEntries.type.Tediye', defaultValue: 'Tediye' },
  Mahsup: { key: 'JournalEntries.type.Mahsup', defaultValue: 'Mahsup' },
  Acilis: { key: 'JournalEntries.type.Acilis', defaultValue: 'Açılış' },
  Kapanis: { key: 'JournalEntries.type.Kapanis', defaultValue: 'Kapanış' },
};

export const JournalEntriesPage = () => {
  const { t, i18n } = useTranslation();
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

  const post = async (id: string) => {
    try {
      await postMutation.mutateAsync(id);
      toast.success(t('JournalEntries.postSuccess', { defaultValue: 'Fiş post edildi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const reverse = async (id: string) => {
    const ok = await confirm({
      title: t('JournalEntries.reverseTitle', { defaultValue: 'Fişi Ters Çevir' }),
      message: t('JournalEntries.reverseMessage', {
        defaultValue: 'Fiş ters çevrilecek ve yeni bir karşı-fiş oluşturulacak. Devam edilsin mi?',
      }),
      confirmLabel: t('JournalEntries.reverseConfirm', { defaultValue: 'Ters Çevir' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await reverseMutation.mutateAsync({ id });
      toast.success(t('JournalEntries.reverseSuccess', { defaultValue: 'Fiş ters çevrildi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const remove = async (id: string) => {
    const ok = await confirm({
      title: t('JournalEntries.deleteTitle', { defaultValue: 'Fişi Sil' }),
      message: t('JournalEntries.deleteMessage', { defaultValue: 'Taslak fiş silinsin mi?' }),
      confirmLabel: t('JournalEntries.deleteConfirm', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success(t('JournalEntries.deleteSuccess', { defaultValue: 'Fiş silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<BookOpen size={20} />}
          title={t('JournalEntries.title', { defaultValue: 'Yevmiye Fişleri' })}
          subtitle={t('JournalEntries.subtitle', {
            defaultValue: "Tahsil / Tediye / Mahsup fişleri. Post edildiğinde Mizan'a yansır.",
          })}
          actions={
            <Button size="sm" onClick={() => setShowForm(true)}>
              <Plus size={14} />
              {t('JournalEntries.newEntry', { defaultValue: 'Yeni Fiş' })}
            </Button>
          }
        />
      }
      toolbar={
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
          <Input
            type="search"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('JournalEntries.searchPlaceholder', {
              defaultValue: 'Fiş no / açıklama / ref',
            })}
            className="w-full"
          />
          <Select
            value={type}
            onChange={(e) => {
              setType(e.target.value as JournalEntryType | '');
              setPage(1);
            }}
            className="w-full"
          >
            <option value="">{t('JournalEntries.allTypes', { defaultValue: 'Tüm Tipler' })}</option>
            {Object.entries(TYPE_LABELS).map(([v, l]) => (
              <option key={v} value={v}>
                {t(l.key, { defaultValue: l.defaultValue })}
              </option>
            ))}
          </Select>
          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as JournalEntryStatus | '');
              setPage(1);
            }}
            className="w-full"
          >
            <option value="">
              {t('JournalEntries.allStatuses', { defaultValue: 'Tüm Durumlar' })}
            </option>
            <option value="Draft">
              {t('JournalEntries.status.Draft', { defaultValue: 'Taslak' })}
            </option>
            <option value="Posted">
              {t('JournalEntries.status.Posted', { defaultValue: 'Post Edildi' })}
            </option>
            <option value="Reversed">
              {t('JournalEntries.status.Reversed', { defaultValue: 'Ters Çevrildi' })}
            </option>
          </Select>
          <Input
            type="date"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value);
              setPage(1);
            }}
            className="w-full"
          />
          <Input
            type="date"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value);
              setPage(1);
            }}
            className="w-full"
          />
        </div>
      }
      pagination={
        total > 0 ? (
          <div className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
            <Pagination
              page={page}
              pageSize={25}
              total={total}
              onPageChange={setPage}
              itemLabel={t('JournalEntries.itemLabel', { defaultValue: 'fiş' })}
            />
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('JournalEntries.columnNumber', { defaultValue: 'Fiş No' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('JournalEntries.columnDate', { defaultValue: 'Tarih' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('JournalEntries.columnType', { defaultValue: 'Tip' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('JournalEntries.columnStatus', { defaultValue: 'Durum' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('JournalEntries.columnDescription', { defaultValue: 'Açıklama' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('JournalEntries.columnDebit', { defaultValue: 'Borç' })}
              </th>
              <th className="px-3 py-2 text-right">
                {t('JournalEntries.columnCredit', { defaultValue: 'Alacak' })}
              </th>
              <th className="px-3 py-2 text-center">
                {t('JournalEntries.columnLines', { defaultValue: 'Satır' })}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {entries.isPending ? (
              <tr>
                <td colSpan={9} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('JournalEntries.loading', { defaultValue: 'Yükleniyor…' })}
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={9} className="px-3 py-8 text-center text-sm text-slate-500">
                  {t('JournalEntries.emptyFiltered', {
                    defaultValue: 'Filtrelere uyan fiş bulunamadı.',
                  })}
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
                  <td className="px-3 py-2 text-xs">
                    {t(TYPE_LABELS[e.type].key, { defaultValue: TYPE_LABELS[e.type].defaultValue })}
                  </td>
                  <td className="px-3 py-2">
                    <Badge variant={STATUS_VARIANTS[e.status]}>
                      {t(`JournalEntries.status.${e.status}`, { defaultValue: e.status })}
                    </Badge>
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
                            className="rounded p-1 text-slate-400 hover:bg-success-50 hover:text-success-700 dark:hover:bg-success-500/10"
                            title={t('JournalEntries.postAction', { defaultValue: 'Post et' })}
                          >
                            <ShieldCheck size={12} />
                          </button>
                          <button
                            type="button"
                            onClick={() => remove(e.id)}
                            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                            title={t('JournalEntries.deleteAction', { defaultValue: 'Sil' })}
                          >
                            <Trash2 size={12} />
                          </button>
                        </>
                      )}
                      {e.status === 'Posted' && (
                        <button
                          type="button"
                          onClick={() => reverse(e.id)}
                          className="rounded p-1 text-slate-400 hover:bg-warning-50 hover:text-warning-700 dark:hover:bg-warning-500/10"
                          title={t('JournalEntries.reverseAction', { defaultValue: 'Ters çevir' })}
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

      {showForm && <JournalEntryFormModal onClose={() => setShowForm(false)} />}

      {items.length === 0 && entries.isFetched && (
        <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-xs text-slate-500 dark:border-slate-700">
          <FilePlus className="mx-auto mb-2 text-slate-400" size={28} />
          {t('JournalEntries.emptyHint', {
            defaultValue:
              'Hesap planınızı oluşturduktan sonra ilk yevmiye fişini buradan girebilirsiniz.',
          })}
        </div>
      )}
    </ListPageTemplate>
  );
};

export default JournalEntriesPage;
