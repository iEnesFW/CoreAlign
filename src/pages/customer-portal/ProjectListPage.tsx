import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyProjectsQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const ProjectListPage = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading, isError } = useMyProjectsQuery({ page, pageSize });
  const paged = data?.data;
  const items = paged?.items ?? [];

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">{t('CustomerPortal.Project.ListTitle')}</h1>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError ? (
        <div className="text-sm text-danger-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : items.length === 0 ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Project.Empty')}</div>
      ) : (
        <>
          <ul className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {items.map((p) => (
              <li
                key={p.id}
                className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
              >
                <div className="flex items-start justify-between gap-3 mb-2">
                  <Link
                    to={`/customer-portal/projects/${p.id}`}
                    className="font-medium text-primary-600 hover:underline truncate"
                  >
                    {p.projectName}
                  </Link>
                  <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 shrink-0">
                    {t(`CustomerPortal.Project.Status.${p.status}`)}
                  </span>
                </div>
                <div className="text-xs text-slate-500 space-y-0.5">
                  <div>
                    {t('CustomerPortal.Project.Code')}: <span className="font-mono">{p.code}</span>
                  </div>
                  <div>
                    {t('CustomerPortal.Project.Total')}:{' '}
                    {p.grandTotal.toLocaleString(undefined, { maximumFractionDigits: 2 })}{' '}
                    {p.currency}
                  </div>
                  <div>
                    {t('CustomerPortal.Project.Panels')}: {p.totalPanels}
                  </div>
                </div>
              </li>
            ))}
          </ul>
          {paged ? (
            <div className="flex items-center justify-between text-sm">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1.5 rounded-md border border-slate-300 dark:border-slate-700 disabled:opacity-50"
              >
                {t('CustomerPortal.Common.Previous')}
              </button>
              <span className="text-slate-500">
                {page} / {Math.max(1, Math.ceil((paged.total ?? 0) / pageSize))}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => p + 1)}
                disabled={items.length < pageSize}
                className="px-3 py-1.5 rounded-md border border-slate-300 dark:border-slate-700 disabled:opacity-50"
              >
                {t('CustomerPortal.Common.Next')}
              </button>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
};

export default ProjectListPage;
