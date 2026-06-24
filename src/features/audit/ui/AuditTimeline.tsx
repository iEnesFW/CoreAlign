import { useTranslation } from 'react-i18next';
import { useAuditTimelineQuery } from '../useAuditTimeline';
import type { EntityAuditLogDto } from '../auditApi';

interface AuditTimelineProps {
  entityType: string;
  entityId: string | undefined;
}

const actionBadgeClass = (action: EntityAuditLogDto['action']) => {
  switch (action) {
    case 'Create':
      return 'bg-success-100 text-success-700 dark:bg-success-900/40 dark:text-success-300';
    case 'Update':
      return 'bg-warning-100 text-warning-700 dark:bg-warning-900/40 dark:text-warning-300';
    case 'Delete':
      return 'bg-danger-100 text-danger-700 dark:bg-danger-900/40 dark:text-danger-300';
    default:
      return 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
  }
};

export function AuditTimeline({ entityType, entityId }: AuditTimelineProps) {
  const { t, i18n } = useTranslation();
  const query = useAuditTimelineQuery(entityType, entityId);

  if (query.isLoading) {
    return <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }

  const items = query.data?.data ?? [];
  if (items.length === 0) {
    return (
      <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.AuditTab.Empty')}</p>
    );
  }

  return (
    <ol className="space-y-3">
      {items.map((entry) => (
        <li
          key={entry.id}
          className="rounded-md border border-slate-200/70 bg-white p-3 shadow-sm dark:border-slate-700/60 dark:bg-slate-900"
        >
          <div className="flex items-center justify-between gap-3">
            <span
              className={`inline-flex rounded px-2 py-0.5 text-xs font-medium ${actionBadgeClass(entry.action)}`}
            >
              {t(`Common.AuditTab.Actions.${entry.action}`)}
            </span>
            <span className="text-xs text-slate-500 dark:text-slate-400">
              {new Date(entry.changedAtUtc).toLocaleString(i18n.language)}
            </span>
          </div>
          <div className="mt-2 grid gap-2 md:grid-cols-2">
            {entry.beforeJson && (
              <div>
                <p className="text-xs font-semibold text-slate-600 dark:text-slate-300">
                  {t('Common.AuditTab.Before')}
                </p>
                <pre className="mt-1 max-h-40 overflow-auto rounded bg-slate-50 p-2 text-xs text-slate-700 dark:bg-slate-800 dark:text-slate-200">
                  {entry.beforeJson}
                </pre>
              </div>
            )}
            {entry.afterJson && (
              <div>
                <p className="text-xs font-semibold text-slate-600 dark:text-slate-300">
                  {t('Common.AuditTab.After')}
                </p>
                <pre className="mt-1 max-h-40 overflow-auto rounded bg-slate-50 p-2 text-xs text-slate-700 dark:bg-slate-800 dark:text-slate-200">
                  {entry.afterJson}
                </pre>
              </div>
            )}
          </div>
        </li>
      ))}
    </ol>
  );
}

export default AuditTimeline;
