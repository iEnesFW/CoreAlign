import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { BookOpen, Plus, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
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

const ENTRY_TYPES: { value: JournalEntryType; labelKey: string; defaultLabel: string }[] = [
  { value: 'Tahsil', labelKey: 'JournalEntries.TypeTahsil', defaultLabel: 'Tahsil Fişi' },
  { value: 'Tediye', labelKey: 'JournalEntries.TypeTediye', defaultLabel: 'Tediye Fişi' },
  { value: 'Mahsup', labelKey: 'JournalEntries.TypeMahsup', defaultLabel: 'Mahsup Fişi' },
  { value: 'Acilis', labelKey: 'JournalEntries.TypeAcilis', defaultLabel: 'Açılış Fişi' },
  { value: 'Kapanis', labelKey: 'JournalEntries.TypeKapanis', defaultLabel: 'Kapanış Fişi' },
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
      toast.error(
        t('JournalEntries.ErrorDebitCreditEqual', {
          defaultValue: 'Borç ve alacak toplamları eşit olmalı.',
        }),
      );
      return;
    }
    if (lines.some((l) => !l.accountId)) {
      toast.error(
        t('JournalEntries.ErrorAccountRequired', {
          defaultValue: 'Tüm satırlar için hesap seçilmelidir.',
        }),
      );
      return;
    }
    if (lines.some((l) => (l.debit > 0 && l.credit > 0) || (l.debit === 0 && l.credit === 0))) {
      toast.error(
        t('JournalEntries.ErrorLineDebitOrCredit', {
          defaultValue:
            'Her satır ya borçlu ya alacaklı olmalı (ikisi birden veya ikisi de boş olamaz).',
        }),
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
    <Modal
      open={true}
      title={t('JournalEntries.NewEntryTitle', { defaultValue: 'Yeni Yevmiye Fişi' })}
      icon={<BookOpen size={18} />}
      onClose={onClose}
      size="2xl"
      className="max-w-5xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('JournalEntries.Cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button
            type="submit"
            form="journal-entry-form"
            isLoading={createMutation.isPending}
            disabled={createMutation.isPending || !isBalanced}
          >
            {createMutation.isPending
              ? t('JournalEntries.Saving', { defaultValue: 'Kaydediliyor…' })
              : t('JournalEntries.Save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="journal-entry-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Select
            label={t('JournalEntries.FieldType', { defaultValue: 'Fiş Tipi' })}
            value={type}
            onChange={(e) => setType(e.target.value as JournalEntryType)}
          >
            {ENTRY_TYPES.map((entryType) => (
              <option key={entryType.value} value={entryType.value}>
                {t(entryType.labelKey, { defaultValue: entryType.defaultLabel })}
              </option>
            ))}
          </Select>
          <Input
            label={t('JournalEntries.FieldEntryDate', { defaultValue: 'Fiş Tarihi' })}
            type="date"
            value={entryDate}
            onChange={(e) => setEntryDate(e.target.value)}
            required
          />
          <Input
            label={t('JournalEntries.FieldPostingDate', { defaultValue: 'Yevmiye Tarihi' })}
            type="date"
            value={postingDate}
            onChange={(e) => setPostingDate(e.target.value)}
            required
          />
          <Input
            label={t('JournalEntries.FieldReference', { defaultValue: 'Referans' })}
            type="text"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            maxLength={200}
            placeholder={t('JournalEntries.ReferencePlaceholder', {
              defaultValue: 'Belge no, açıklama ref…',
            })}
          />
        </div>
        <Input
          label={t('JournalEntries.FieldDescription', { defaultValue: 'Açıklama' })}
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={1000}
        />

        <div className="rounded-lg border border-slate-200 dark:border-slate-800">
          <div className="grid grid-cols-12 gap-2 border-b border-slate-200 bg-slate-50 px-2 py-1.5 text-[10px] font-semibold uppercase text-slate-600 dark:border-slate-800 dark:bg-slate-800/50 dark:text-slate-300">
            <div className="col-span-1">#</div>
            <div className="col-span-4">
              {t('JournalEntries.ColumnAccount', { defaultValue: 'Hesap' })}
            </div>
            <div className="col-span-2 text-right">
              {t('JournalEntries.ColumnDebit', { defaultValue: 'Borç' })}
            </div>
            <div className="col-span-2 text-right">
              {t('JournalEntries.ColumnCredit', { defaultValue: 'Alacak' })}
            </div>
            <div className="col-span-2">
              {t('JournalEntries.ColumnDescription', { defaultValue: 'Açıklama' })}
            </div>
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
                className={`${fieldBaseClasses(false)} col-span-4 h-8 px-2 text-xs`}
                required
              >
                <option value="">
                  {t('JournalEntries.SelectAccountOption', { defaultValue: '— Hesap seç —' })}
                </option>
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
                className={`${fieldBaseClasses(false)} col-span-2 h-8 px-2 text-right text-xs`}
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
                className={`${fieldBaseClasses(false)} col-span-2 h-8 px-2 text-right text-xs`}
              />
              <input
                type="text"
                value={line.description ?? ''}
                onChange={(e) => updateLine(line.key, { description: e.target.value })}
                maxLength={500}
                className={`${fieldBaseClasses(false)} col-span-2 h-8 px-2 text-xs`}
              />
              <button
                type="button"
                onClick={() => removeLine(line.key)}
                disabled={lines.length <= 2}
                className="col-span-1 rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
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
              {t('JournalEntries.AddLine', { defaultValue: 'Satır ekle' })}
            </button>
            <div className="flex gap-6 text-xs">
              <div>
                <span className="text-slate-500">
                  {t('JournalEntries.TotalDebit', { defaultValue: 'Toplam Borç: ' })}
                </span>
                <span className="font-mono font-semibold">{totals.debit.toFixed(2)}</span>
              </div>
              <div>
                <span className="text-slate-500">
                  {t('JournalEntries.TotalCredit', { defaultValue: 'Toplam Alacak: ' })}
                </span>
                <span className="font-mono font-semibold">{totals.credit.toFixed(2)}</span>
              </div>
              <div
                className={`font-mono font-semibold ${
                  isBalanced
                    ? 'text-success-600 dark:text-success-400'
                    : 'text-danger-600 dark:text-danger-400'
                }`}
              >
                {t('JournalEntries.Difference', {
                  defaultValue: 'Fark: {{value}}',
                  value: totals.diff.toFixed(2),
                })}
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
          {t('JournalEntries.PostImmediately', {
            defaultValue: 'Kaydet ve hemen post et (kapatılan dönemler için reddedilir)',
          })}
        </label>
      </form>
    </Modal>
  );
};
