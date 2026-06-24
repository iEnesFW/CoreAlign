import { useTranslation } from 'react-i18next';
import { Filter } from 'lucide-react';
import type { AuditLogSearchFilter } from '../auditAdminApi';

interface Props {
  draft: AuditLogSearchFilter;
  onChange: (next: AuditLogSearchFilter) => void;
  onApply: () => void;
  onReset: () => void;
}

const ACTIONS = ['Create', 'Update', 'Delete'];

export const AuditLogFilterBar = ({ draft, onChange, onApply, onReset }: Props) => {
  const { t } = useTranslation();

  const setField = (key: keyof AuditLogSearchFilter, value: string | undefined) => {
    onChange({ ...draft, [key]: value && value.length > 0 ? value : undefined });
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onApply();
      }}
      className="grid gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800 sm:grid-cols-2 lg:grid-cols-4"
    >
      <div>
        <label
          htmlFor="fromUtc"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.FromDate')}
        </label>
        <input
          id="fromUtc"
          type="datetime-local"
          value={draft.fromUtc ?? ''}
          onChange={(e) => setField('fromUtc', e.target.value)}
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>
      <div>
        <label
          htmlFor="toUtc"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.ToDate')}
        </label>
        <input
          id="toUtc"
          type="datetime-local"
          value={draft.toUtc ?? ''}
          onChange={(e) => setField('toUtc', e.target.value)}
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>
      <div>
        <label
          htmlFor="entityType"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.EntityType')}
        </label>
        <input
          id="entityType"
          type="text"
          value={draft.entityType ?? ''}
          onChange={(e) => setField('entityType', e.target.value)}
          placeholder={t('Audit.Admin.EntityTypePlaceholder')}
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>
      <div>
        <label
          htmlFor="action"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.Action')}
        </label>
        <select
          id="action"
          value={draft.action ?? ''}
          onChange={(e) => setField('action', e.target.value)}
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">{t('Audit.Admin.AnyAction')}</option>
          {ACTIONS.map((a) => (
            <option key={a} value={a}>
              {a}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label
          htmlFor="userId"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.UserId')}
        </label>
        <input
          id="userId"
          type="text"
          value={draft.userId ?? ''}
          onChange={(e) => setField('userId', e.target.value)}
          placeholder="00000000-0000-0000-0000-000000000000"
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>
      <div>
        <label
          htmlFor="entityId"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.EntityId')}
        </label>
        <input
          id="entityId"
          type="text"
          value={draft.entityId ?? ''}
          onChange={(e) => setField('entityId', e.target.value)}
          placeholder="00000000-0000-0000-0000-000000000000"
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>
      <div className="flex items-end gap-2 sm:col-span-2 lg:col-span-2">
        <button
          type="submit"
          className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-700"
        >
          <Filter size={14} />
          {t('Audit.Admin.ApplyFilter')}
        </button>
        <button
          type="button"
          onClick={onReset}
          className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          {t('Audit.Admin.Reset')}
        </button>
      </div>
    </form>
  );
};
