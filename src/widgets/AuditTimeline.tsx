import { useTranslation } from 'react-i18next';
import { useAuditTimelineQuery, type EntityAuditLogDto } from '@/features/audit';

interface AuditTimelineProps {
  entityType: string;
  entityId: string | undefined;
}

const actionBadgeClass = (action: EntityAuditLogDto['action']) => {
  switch (action) {
    case 'Create':
      return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300';
    case 'Update':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300';
    case 'Delete':
      return 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300';
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
