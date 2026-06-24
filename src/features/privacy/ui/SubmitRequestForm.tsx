import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Send } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useSubmitPrivacyRequest } from '../hooks/usePrivacyRequests';
import type { DataSubjectRequestType } from '../model/privacy.types';

const REQUEST_TYPES: DataSubjectRequestType[] = [
  'Access',
  'Rectification',
  'Erasure',
  'Portability',
  'Restriction',
  'Objection',
];

interface Props {
  defaultEmail?: string;
  onSubmitted?: (requestId: string) => void;
}

export const SubmitRequestForm = ({ defaultEmail, onSubmitted }: Props) => {
  const { t } = useTranslation();
  const submitter = useSubmitPrivacyRequest();
  const [requestType, setRequestType] = useState<DataSubjectRequestType>('Access');
  const [email, setEmail] = useState(defaultEmail ?? '');
  const [notes, setNotes] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const [result] = await safeRequestWithNotify(
      submitter.mutateAsync({
        type: requestType,
        requesterEmail: email,
        notes: notes || null,
      }),
      { successMessage: t('Privacy.Request.SubmittedSuccess') },
    );
    if (result?.data) {
      setNotes('');
      onSubmitted?.(result.data.id);
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800"
    >
      <div>
        <label
          htmlFor="privacy-request-type"
          className="block text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Privacy.Request.Type')}
        </label>
        <select
          id="privacy-request-type"
          value={requestType}
          onChange={(e) => setRequestType(e.target.value as DataSubjectRequestType)}
          className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          {REQUEST_TYPES.map((rt) => (
            <option key={rt} value={rt}>
              {t(`Privacy.Request.TypeOption.${rt}`)}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label
          htmlFor="privacy-request-email"
          className="block text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Privacy.Request.Email')}
        </label>
        <input
          id="privacy-request-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>

      <div>
        <label
          htmlFor="privacy-request-notes"
          className="block text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Privacy.Request.Notes')}
        </label>
        <textarea
          id="privacy-request-notes"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={4}
          className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>

      <button
        type="submit"
        disabled={submitter.isPending || !email}
        className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
      >
        <Send size={14} />
        {submitter.isPending ? t('Privacy.Request.Submitting') : t('Privacy.Request.Submit')}
      </button>
    </form>
  );
};
