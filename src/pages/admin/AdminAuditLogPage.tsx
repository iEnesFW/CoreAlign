import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, FileText, RefreshCw } from 'lucide-react';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { AuditLogFilterBar } from '@/features/audit/ui/AuditLogFilterBar';
import { AuditLogDetailDrawer } from '@/features/audit/ui/AuditLogDetailDrawer';
import { AuditExportScheduleEditor } from '@/features/audit/ui/AuditExportScheduleEditor';
import { useAuditSearchQuery, useDownloadAuditExport } from '@/features/audit/useAuditAdmin';
import type { AuditLogExportFormat, AuditLogSearchFilter } from '@/features/audit/auditAdminApi';
import type { EntityAuditLogDto } from '@/features/audit/auditApi';

const DEFAULT_PAGE_SIZE = 25;
const FORMATS: AuditLogExportFormat[] = ['Csv', 'Json', 'Excel'];

type Tab = 'log' | 'schedule';

export const AdminAuditLogPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [tab, setTab] = useState<Tab>('log');
  const [draftFilter, setDraftFilter] = useState<AuditLogSearchFilter>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const [activeFilter, setActiveFilter] = useState<AuditLogSearchFilter>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const [selected, setSelected] = useState<EntityAuditLogDto | null>(null);

  const search = useAuditSearchQuery(activeFilter);
  const download = useDownloadAuditExport();

  const items = useMemo(() => search.data?.data?.items ?? [], [search.data]);
  const total = search.data?.data?.total ?? 0;
  const page = activeFilter.page ?? 1;
  const pageSize = activeFilter.pageSize ?? DEFAULT_PAGE_SIZE;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const applyFilter = () => {
    setActiveFilter({ ...draftFilter, page: 1, pageSize });
  };

  const resetFilter = () => {
    const next = { page: 1, pageSize };
    setDraftFilter(next);
    setActiveFilter(next);
  };

  const goToPage = (target: number) => {
    setActiveFilter((current) => ({ ...current, page: Math.min(Math.max(1, target), totalPages) }));
  };

  const handleExport = async (format: AuditLogExportFormat) => {
    const { page: _omitPage, pageSize: _omitSize, ...rest } = activeFilter;
    await safeRequestWithNotify(download.mutateAsync({ format, filter: rest }), {
      successMessage: t('Audit.Admin.ExportStarted'),
    });
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<FileText size={20} />}
          title={t('Audit.Admin.PageTitle')}
          subtitle={t('Audit.Admin.PageSubtitle')}
          actions={
            tab === 'log' ? (
              <div className="flex flex-wrap items-center gap-2">
                <Button variant="outline" size="sm" onClick={() => search.refetch()}>
                  <RefreshCw size={14} />
                  {t('Common.Refresh')}
                </Button>
                {FORMATS.map((format) => (
                  <Button
                    key={format}
                    size="sm"
                    disabled={download.isPending}
                    onClick={() => handleExport(format)}
                  >
                    <Download size={14} />
                    {format.toUpperCase()}
                  </Button>
                ))}
              </div>
            ) : undefined
          }
        />
      }
      pagination={
        tab === 'log' && total > pageSize ? (
          <div className="flex items-center justify-end gap-2 text-sm text-slate-600 dark:text-slate-300">
            <Button
              variant="outline"
              size="sm"
              onClick={() => goToPage(page - 1)}
              disabled={page <= 1}
            >
              {t('Common.Previous')}
            </Button>
            <span>
              {page} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => goToPage(page + 1)}
              disabled={page >= totalPages}
            >
              {t('Common.Next')}
            </Button>
          </div>
        ) : undefined
      }
    >
      <nav className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        <TabButton current={tab} value="log" onClick={setTab}>
          {t('Audit.Admin.Tab.Log')}
        </TabButton>
        <TabButton current={tab} value="schedule" onClick={setTab}>
          {t('Audit.Admin.Tab.Schedule')}
        </TabButton>
      </nav>

      {tab === 'log' && (
        <div className="space-y-4">
          <AuditLogFilterBar
            draft={draftFilter}
            onChange={setDraftFilter}
            onApply={applyFilter}
            onReset={resetFilter}
          />

          <div className="flex flex-wrap items-center gap-2">
            <span className="ml-auto text-xs text-slate-500 dark:text-slate-400">
              {t('Audit.Admin.TotalRows', { count: total })}
            </span>
          </div>

          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-slate-700 dark:bg-slate-900 dark:text-slate-200">
                <tr>
                  <th className="px-3 py-2 text-left font-semibold">
                    {t('Audit.Admin.ChangedAt')}
                  </th>
                  <th className="px-3 py-2 text-left font-semibold">
                    {t('Audit.Admin.EntityType')}
                  </th>
                  <th className="px-3 py-2 text-left font-semibold">{t('Audit.Admin.EntityId')}</th>
                  <th className="px-3 py-2 text-left font-semibold">{t('Audit.Admin.Action')}</th>
                  <th className="px-3 py-2 text-left font-semibold">{t('Audit.Admin.UserId')}</th>
                  <th className="px-3 py-2 text-right font-semibold">
                    {t('Audit.Admin.Sequence')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.length === 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="px-3 py-6 text-center text-slate-500 dark:text-slate-400"
                    >
                      {t('Audit.Admin.NoResults')}
                    </td>
                  </tr>
                )}
                {items.map((row) => (
                  <tr
                    key={row.id}
                    className="cursor-pointer border-t border-slate-100 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-900"
                    onClick={() => setSelected(row)}
                  >
                    <td className="px-3 py-2 tabular-nums">
                      {formatDate(row.changedAtUtc, locale)}
                    </td>
                    <td className="px-3 py-2">{row.entityType}</td>
                    <td className="px-3 py-2 font-mono text-xs">{row.entityId}</td>
                    <td className="px-3 py-2">{row.action}</td>
                    <td className="px-3 py-2 font-mono text-xs">{row.userId ?? '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{row.sequence}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {tab === 'schedule' && <AuditExportScheduleEditor />}

      <AuditLogDetailDrawer entry={selected} onClose={() => setSelected(null)} />
    </ListPageTemplate>
  );
};

interface TabButtonProps {
  current: Tab;
  value: Tab;
  onClick: (next: Tab) => void;
  children: React.ReactNode;
}

const TabButton = ({ current, value, onClick, children }: TabButtonProps) => {
  const active = current === value;
  return (
    <button
      type="button"
      onClick={() => onClick(value)}
      className={
        active
          ? 'border-b-2 border-primary-600 px-3 py-2 text-sm font-semibold text-primary-700 dark:border-primary-400 dark:text-primary-300'
          : 'px-3 py-2 text-sm font-medium text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-200'
      }
    >
      {children}
    </button>
  );
};

export default AdminAuditLogPage;
