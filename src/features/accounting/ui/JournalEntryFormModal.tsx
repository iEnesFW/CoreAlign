import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, Trash2, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useGLAccountTree } from '../hooks/useGLAccountQueries';
import { useCreateJournalEntry } from '../hooks/useJournalEntryQueries';
import type {
  CreateJournalEntryRequest,
  JournalEntryType,
  JournalLineInput,
} from '../model/journalEntry.types';
import type { GLAccount } from '../model/glAccount.types';

interface JournalEntryFormModalProps {
  onClose: () => void;
}

interface FormLine extends JournalLineInput {
  key: number;
}

const ENTRY_TYPES: { value: JournalEntryType; label: string }[] = [
  { value: 'Tahsil', label: 'Tahsil Fişi' },
  { value: 'Tediye', label: 'Tediye Fişi' },
  { value: 'Mahsup', label: 'Mahsup Fişi' },
  { value: 'Acilis', label: 'Açılış Fişi' },
  { value: 'Kapanis', label: 'Kapanış Fişi' },
];

const todayIso = () => new Date().toISOString().slice(0, 10);

const emptyLine = (key: number): FormLine => ({
  key,
  accountId: '',
  debit: 0,
  credit: 0,
  currency: 'TRY',
  description: null,
});

export const JournalEntryFormModal = ({ onClose }: JournalEntryFormModalProps) => {
  const { t } = useTranslation();
  const accountsQuery = useGLAccountTree();
  const createMutation = useCreateJournalEntry();

  const [entryDate, setEntryDate] = useState(todayIso());
  const [postingDate, setPostingDate] = useState(todayIso());
  const [type, setType] = useState<JournalEntryType>('Mahsup');
  const [description, setDescription] = useState('');
  const [reference, setReference] = useState('');
  const [lines, setLines] = useState<FormLine[]>([emptyLine(1), emptyLine(2)]);
  const [postImmediately, setPostImmediately] = useState(true);

  // Only postable + active accounts are valid line targets.
  const postableAccounts = useMemo<GLAccount[]>(() => {
    const all = accountsQuery.data?.data ?? [];
    return all.filter((a) => a.isPostable && a.isActive);
  }, [accountsQuery.data]);

  const totals = useMemo(() => {
    let debit = 0;
    let credit = 0;
    for (const l of lines) {
      debit += Number(l.debit) || 0;
      credit += Number(l.credit) || 0;
    }
    return { debit, credit, diff: Math.round((debit - credit) * 10000) / 10000 };
  }, [lines]);

  const isBalanced = totals.diff === 0 && totals.debit > 0;

  const updateLine = (key: number, patch: Partial<FormLine>) => {
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  };

  const addLine = () =>
    setLines((prev) => [...prev, emptyLine(Math.max(...prev.map((l) => l.key)) + 1)]);

  const removeLine = (key: number) =>
    setLines((prev) => (prev.length <= 2 ? prev : prev.filter((l) => l.key !== key)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isBalanced) {
      toast.error('Borç ve alacak toplamları eşit olmalı.');
      return;
    }
    if (lines.some((l) => !l.accountId)) {
      toast.error('Tüm satırlar için hesap seçilmelidir.');
      return;
    }
    if (lines.some((l) => (l.debit > 0 && l.credit > 0) || (l.debit === 0 && l.credit === 0))) {
      toast.error(
        'Her satır ya borçlu ya alacaklı olmalı (ikisi birden veya ikisi de boş olamaz).',
      );
      return;
    }

    const request: CreateJournalEntryRequest = {
      entryDate,
      postingDate,
      type,
      description: description.trim() || undefined,
      reference: reference.trim() || undefined,
      postImmediately,
      lines: lines.map((l) => ({
        accountId: l.accountId,
        debit: Number(l.debit) || 0,
        credit: Number(l.credit) || 0,
        currency: l.currency || 'TRY',
        description: l.description?.trim() || undefined,
        costCenter: l.costCenter?.trim() || undefined,
        project: l.project?.trim() || undefined,
      })),
    };
    try {
      await createMutation.mutateAsync(request);
      toast.success(
        postImmediately
          ? t('accounting.journal.posted', { defaultValue: 'Fiş kaydedildi ve post edildi.' })
          : t('accounting.journal.draftSaved', { defaultValue: 'Taslak fiş kaydedildi.' }),
      );
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-5xl rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            Yeni Yevmiye Fişi
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
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Fiş Tipi
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as JournalEntryType)}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              >
                {ENTRY_TYPES.map((t) => (
                  <option key={t.value} value={t.value}>
                    {t.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Fiş Tarihi
              </label>
              <input
                type="date"
                value={entryDate}
                onChange={(e) => setEntryDate(e.target.value)}
                required
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Yevmiye Tarihi
              </label>
              <input
                type="date"
                value={postingDate}
                onChange={(e) => setPostingDate(e.target.value)}
                required
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Referans
              </label>
              <input
                type="text"
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                maxLength={200}
                placeholder="Belge no, açıklama ref…"
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Açıklama
            </label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={1000}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>

          <div className="rounded-lg border border-slate-200 dark:border-slate-800">
            <div className="grid grid-cols-12 gap-2 border-b border-slate-200 bg-slate-50 px-2 py-1.5 text-[10px] font-semibold uppercase text-slate-600 dark:border-slate-800 dark:bg-slate-800/50 dark:text-slate-300">
              <div className="col-span-1">#</div>
              <div className="col-span-4">Hesap</div>
              <div className="col-span-2 text-right">Borç</div>
              <div className="col-span-2 text-right">Alacak</div>
              <div className="col-span-2">Açıklama</div>
              <div className="col-span-1" />
            </div>
            {lines.map((line, idx) => (
              <div
                key={line.key}
                className="grid grid-cols-12 gap-2 border-b border-slate-100 px-2 py-1 last:border-b-0 dark:border-slate-800"
              >
                <div className="col-span-1 py-1 text-xs text-slate-500">{idx + 1}</div>
                <select
                  value={line.accountId}
                  onChange={(e) => updateLine(line.key, { accountId: e.target.value })}
                  className="col-span-4 rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-800"
                  required
                >
                  <option value="">— Hesap seç —</option>
                  {postableAccounts.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.code} — {a.name}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={line.debit}
                  onChange={(e) => {
                    const v = parseFloat(e.target.value) || 0;
                    updateLine(line.key, { debit: v, credit: v > 0 ? 0 : line.credit });
                  }}
                  className="col-span-2 rounded border border-slate-300 bg-white px-2 py-1 text-right text-xs dark:border-slate-700 dark:bg-slate-800"
                />
                <input
                  type="number"
                  step="0.01"
                  min="0"
                  value={line.credit}
                  onChange={(e) => {
                    const v = parseFloat(e.target.value) || 0;
                    updateLine(line.key, { credit: v, debit: v > 0 ? 0 : line.debit });
                  }}
                  className="col-span-2 rounded border border-slate-300 bg-white px-2 py-1 text-right text-xs dark:border-slate-700 dark:bg-slate-800"
                />
                <input
                  type="text"
                  value={line.description ?? ''}
                  onChange={(e) => updateLine(line.key, { description: e.target.value })}
                  maxLength={500}
                  className="col-span-2 rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-800"
                />
                <button
                  type="button"
                  onClick={() => removeLine(line.key)}
                  disabled={lines.length <= 2}
                  className="col-span-1 rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 disabled:opacity-30 dark:hover:bg-rose-500/10"
                >
                  <Trash2 size={12} />
                </button>
              </div>
            ))}
            <div className="flex items-center justify-between border-t border-slate-200 px-2 py-2 dark:border-slate-800">
              <button
                type="button"
                onClick={addLine}
                className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
              >
                <Plus size={11} />
                Satır ekle
              </button>
              <div className="flex gap-6 text-xs">
                <div>
                  <span className="text-slate-500">Toplam Borç: </span>
                  <span className="font-mono font-semibold">{totals.debit.toFixed(2)}</span>
                </div>
                <div>
                  <span className="text-slate-500">Toplam Alacak: </span>
                  <span className="font-mono font-semibold">{totals.credit.toFixed(2)}</span>
                </div>
                <div
                  className={`font-mono font-semibold ${
                    isBalanced
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : 'text-rose-600 dark:text-rose-400'
                  }`}
                >
                  Fark: {totals.diff.toFixed(2)}
                </div>
              </div>
            </div>
          </div>

          <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={postImmediately}
              onChange={(e) => setPostImmediately(e.target.checked)}
            />
            Kaydet ve hemen post et (kapatılan dönemler için reddedilir)
          </label>

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
              disabled={createMutation.isPending || !isBalanced}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {createMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
