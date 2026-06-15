import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import type { EntityAuditLogDto } from '../auditApi';

interface Props {
  entry: EntityAuditLogDto | null;
  onClose: () => void;
}

export const AuditLogDetailDrawer = ({ entry, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  if (!entry) return null;

  return (
    <div className="fixed inset-0 z-40 flex justify-end" aria-modal="true" role="dialog">
      <button
        type="button"
        aria-label={t('Common.Close')}
        className="flex-1 bg-slate-900/40"
        onClick={onClose}
      />
      <aside className="flex h-full w-full max-w-xl flex-col overflow-y-auto bg-white shadow-xl dark:bg-slate-900">
        <header className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('Audit.Admin.DetailTitle')}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
            aria-label={t('Common.Close')}
          >
            <X size={16} />
          </button>
        </header>
        <div className="space-y-4 px-4 py-3 text-sm">
          <Field label={t('Audit.Admin.Id')} value={entry.id} mono />
          <Field label={t('Audit.Admin.EntityType')} value={entry.entityType} />
          <Field label={t('Audit.Admin.EntityId')} value={entry.entityId} mono />
          <Field label={t('Audit.Admin.Action')} value={entry.action} />
          <Field label={t('Audit.Admin.UserId')} value={entry.userId ?? '—'} mono />
          <Field
            label={t('Audit.Admin.ChangedAt')}
            value={formatDate(entry.changedAtUtc, locale)}
          />
          <Field label={t('Audit.Admin.Sequence')} value={String(entry.sequence)} />
          <Field label={t('Audit.Admin.CorrelationId')} value={entry.correlationId ?? '—'} mono />
          <Field label={t('Audit.Admin.RollingHash')} value={entry.rollingHash} mono break />
          <JsonField label={t('Audit.Admin.BeforeJson')} value={entry.beforeJson} />
          <JsonField label={t('Audit.Admin.AfterJson')} value={entry.afterJson} />
        </div>
      </aside>
    </div>
  );
};

interface FieldProps {
  label: string;
  value: string;
  mono?: boolean;
  break?: boolean;
}

const Field = ({ label, value, mono, break: breakAll }: FieldProps) => (
  <div>
    <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </p>
    <p
      className={[
        'text-sm text-slate-900 dark:text-slate-100',
        mono ? 'font-mono' : '',
        breakAll ? 'break-all' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {value}
    </p>
  </div>
);

interface JsonFieldProps {
  label: string;
  value: string | null | undefined;
}

const JsonField = ({ label, value }: JsonFieldProps) => {
  const formatted = formatJson(value);
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {label}
      </p>
      <pre className="mt-1 max-h-64 overflow-auto rounded-md bg-slate-50 p-3 text-xs text-slate-800 dark:bg-slate-800 dark:text-slate-100">
        {formatted}
      </pre>
    </div>
  );
};

const formatJson = (raw: string | null | undefined): string => {
  if (!raw) return '—';
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
};
