import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useReportsQuery, useDeleteReport } from '@/features/bi/hooks/useReports';
import { ExportButton } from '@/features/bi/ui/ExportButton';

export const ReportsListPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useReportsQuery();
  const deleteMutation = useDeleteReport();

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-50">
          {t('BI.Reports.Title', { defaultValue: 'Saved Reports' })}
        </h1>
        <Link
          to="/bi/reports/new"
          className="rounded bg-primary-600 px-4 py-2 text-sm text-white hover:bg-primary-700"
        >
          {t('BI.Reports.Create', { defaultValue: 'New report' })}
        </Link>
      </div>
      {isLoading ? (
        <div className="text-sm text-slate-500">
          {t('BI.Common.Loading', { defaultValue: 'Loading...' })}
        </div>
      ) : (
        <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500 dark:bg-slate-800">
              <tr>
                <th className="px-3 py-2">{t('BI.Reports.Name', { defaultValue: 'Name' })}</th>
                <th className="px-3 py-2">
                  {t('BI.Reports.DataSource', { defaultValue: 'Data source' })}
                </th>
                <th className="px-3 py-2">
                  {t('BI.Reports.Visibility', { defaultValue: 'Visibility' })}
                </th>
                <th className="px-3 py-2">
                  {t('BI.Reports.LastRun', { defaultValue: 'Last run' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('BI.Reports.Actions', { defaultValue: 'Actions' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {(data ?? []).map((r) => (
                <tr key={r.id}>
                  <td className="px-3 py-2 font-medium">{r.name}</td>
                  <td className="px-3 py-2">{r.dataSource}</td>
                  <td className="px-3 py-2">{r.isPublic ? 'Public' : 'Private'}</td>
                  <td className="px-3 py-2 text-slate-500">{r.lastRunAtUtc ?? '-'}</td>
                  <td className="px-3 py-2 text-right">
                    <div className="inline-flex items-center gap-2">
                      <Link to={`/bi/reports/${r.id}`} className="text-primary-600 hover:underline">
                        {t('BI.Reports.Open', { defaultValue: 'Open' })}
                      </Link>
                      <ExportButton reportId={r.id} fileName={r.name} />
                      <button
                        type="button"
                        onClick={() => deleteMutation.mutate(r.id)}
                        className="text-danger-600 hover:underline"
                      >
                        {t('BI.Reports.Delete', { defaultValue: 'Delete' })}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {(data ?? []).length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-slate-500">
                    {t('BI.Reports.Empty', { defaultValue: 'No saved reports yet.' })}
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default ReportsListPage;
