import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useAddCustomerNote, useCustomerNotesQuery } from '../hooks/useCustomerQueries';

interface Props {
  customerId: string;
  staticNotes?: string | null;
}

export const CustomerNotesTab = ({ customerId, staticNotes }: Props) => {
  const { t, i18n } = useTranslation();
  const notesQuery = useCustomerNotesQuery(customerId);
  const addMutation = useAddCustomerNote();
  const [body, setBody] = useState('');

  const notes = notesQuery.data?.data ?? [];

  const handleAdd = async () => {
    const trimmed = body.trim();
    if (!trimmed) return;
    try {
      await addMutation.mutateAsync({ customerId, body: trimmed });
      setBody('');
      toast.success(t('customers.detail.notesTab.added', { defaultValue: 'Not eklendi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const fmtDate = (iso: string) => {
    try {
      return new Intl.DateTimeFormat(i18n.language, {
        dateStyle: 'short',
        timeStyle: 'short',
      }).format(new Date(iso));
    } catch {
      return iso.slice(0, 16);
    }
  };

  return (
    <div className="space-y-3">
      {staticNotes && (
        <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
          {staticNotes}
        </div>
      )}

      <div className="rounded border border-slate-200 p-3 dark:border-slate-800">
        <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
          {t('customers.detail.notesTab.addLabel', { defaultValue: 'Not ekle' })}
        </label>
        <textarea
          rows={3}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          maxLength={4000}
          placeholder={t('customers.detail.notesTab.placeholder', {
            defaultValue: 'Müşteriyle ilgili bir not yazın...',
          })}
          className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
        <div className="mt-2 flex justify-end">
          <button
            type="button"
            onClick={handleAdd}
            disabled={addMutation.isPending || body.trim().length === 0}
            className="inline-flex items-center gap-1.5 rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-primary-700 disabled:opacity-50"
          >
            <Plus size={12} />
            {t('customers.detail.notesTab.addButton', { defaultValue: 'Not Ekle' })}
          </button>
        </div>
      </div>

      {notes.length === 0 && !staticNotes ? (
        <div className="rounded border border-slate-200 p-4 text-center text-sm italic text-slate-500 dark:border-slate-800 dark:text-slate-400">
          {t('customers.detail.noNotes')}
        </div>
      ) : (
        <ul className="space-y-2">
          {notes.map((note) => (
            <li
              key={note.id}
              className="rounded border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
            >
              <p className="whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">
                {note.body}
              </p>
              <p className="mt-1 text-[11px] text-slate-400 dark:text-slate-500">
                {fmtDate(note.createdAtUtc)}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
