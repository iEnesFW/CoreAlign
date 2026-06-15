import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useJournalEntriesBySource } from '../hooks/useJournalEntryQueries';
import type { JournalEntry, JournalLine } from '../model/journalEntry.types';

interface Props {
  sourceDocumentId: string;
  title?: string;
  onClose: () => void;
}

const STATUS_TONE: Record<string, string> = {
  Posted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Draft: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Reversed: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

export const SourceJournalEntriesModal = ({ sourceDocumentId, title, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const query = useJournalEntriesBySource(sourceDocumentId);
  const entries = query.data?.data ?? [];

  const fmtDate = (iso: string) => formatDate(iso, locale);

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/50 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="mt-10 w-full max-w-2xl rounded-lg border border-slate-200 bg-white shadow-xl dark:border-slate-800 dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {title ?? t('gl.entriesTitle', { defaultValue: 'Muhasebe Fişleri' })}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-200"
          >
            <X size={16} />
          </button>
        </div>

        <div className="max-h-[70vh] space-y-4 overflow-y-auto p-4">
          {query.isPending ? (
            <p className="py-6 text-center text-sm text-slate-500">
              {t('common.loading', { defaultValue: 'Yükleniyor…' })}
            </p>
          ) : query.isError ? (
            <p className="py-6 text-center text-sm text-rose-600 dark:text-rose-400">
              {t('gl.loadError', { defaultValue: 'Fişler yüklenemedi.' })}
            </p>
          ) : entries.length === 0 ? (
            <p className="py-6 text-center text-sm text-slate-500 dark:text-slate-400">
              {t('gl.noEntries', {
                defaultValue:
                  'Bu belge için henüz muhasebe fişi oluşmadı (kuyrukta olabilir veya hesap eşleştirmesi eksik).',
              })}
            </p>
          ) : (
            entries.map((entry: JournalEntry) => (
              <div
                key={entry.id}
                className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800"
              >
                <div className="flex flex-wrap items-center justify-between gap-2 bg-slate-50/60 px-3 py-2 dark:bg-slate-800/40">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-xs font-semibold text-slate-700 dark:text-slate-200">
                      {entry.number}
                    </span>
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[entry.status] ?? ''}`}
                    >
                      {entry.status}
                    </span>
                  </div>
                  <span className="text-[11px] text-slate-500 dark:text-slate-400">
                    {fmtDate(entry.postingDate)}
                  </span>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
                      <tr>
                        <th className="px-3 py-1.5 text-left">
                          {t('gl.account', { defaultValue: 'Hesap' })}
                        </th>
                        <th className="px-3 py-1.5 text-right">
                          {t('gl.debit', { defaultValue: 'Borç' })}
                        </th>
                        <th className="px-3 py-1.5 text-right">
                          {t('gl.credit', { defaultValue: 'Alacak' })}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-800/60">
                      {entry.lines.map((line: JournalLine) => (
                        <tr key={line.id}>
                          <td className="px-3 py-1.5 text-slate-700 dark:text-slate-300">
                            <span className="font-mono text-slate-500 dark:text-slate-400">
                              {line.accountCode}
                            </span>{' '}
                            {line.accountName}
                          </td>
                          <td className="px-3 py-1.5 text-right font-mono text-slate-800 dark:text-slate-200">
                            {line.debit > 0
                              ? formatCurrency(line.debit, locale, line.currency)
                              : '—'}
                          </td>
                          <td className="px-3 py-1.5 text-right font-mono text-slate-800 dark:text-slate-200">
                            {line.credit > 0
                              ? formatCurrency(line.credit, locale, line.currency)
                              : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot className="border-t border-slate-200 font-semibold dark:border-slate-700">
                      <tr>
                        <td className="px-3 py-1.5 text-right text-slate-500 dark:text-slate-400">
                          {t('gl.total', { defaultValue: 'Toplam' })}
                        </td>
                        <td className="px-3 py-1.5 text-right font-mono text-slate-900 dark:text-slate-100">
                          {formatCurrency(entry.totalDebit, locale)}
                        </td>
                        <td className="px-3 py-1.5 text-right font-mono text-slate-900 dark:text-slate-100">
                          {formatCurrency(entry.totalCredit, locale)}
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};
