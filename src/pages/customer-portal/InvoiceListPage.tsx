import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyInvoicesQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const InvoiceListPage = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const pageSize = 20;
  const { data, isLoading, isError } = useMyInvoicesQuery({ page, pageSize });
  const paged = data?.data;
  const items = paged?.items ?? [];

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">{t('CustomerPortal.Invoice.ListTitle')}</h1>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError ? (
        <div className="text-sm text-red-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : items.length === 0 ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Invoice.Empty')}</div>
      ) : (
        <>
          <ul className="space-y-2">
            {items.map((inv) => (
              <li
                key={inv.id}
                className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 sm:p-4"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                  <Link
                    to={`/customer-portal/invoices/${inv.id}`}
                    className="font-medium text-blue-600 hover:underline truncate"
                  >
                    {inv.invoiceNumber}
                  </Link>
                  <div className="flex items-center gap-2 text-xs">
                    <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
                      {t(`CustomerPortal.Invoice.Status.${inv.status}`)}
                    </span>
                    <span className="font-medium">
                      {inv.total.toLocaleString(undefined, { maximumFractionDigits: 2 })}{' '}
                      {inv.currency}
                    </span>
                  </div>
                </div>
                <div className="text-xs text-slate-500 mt-1">
                  {t('CustomerPortal.Invoice.IssueDate')}:{' '}
                  {new Date(inv.issueDate).toLocaleDateString()}
                  {' · '}
                  {t('CustomerPortal.Invoice.DueDate')}:{' '}
                  {new Date(inv.dueDate).toLocaleDateString()}
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

export default InvoiceListPage;
