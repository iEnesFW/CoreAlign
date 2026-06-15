import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { useMyServiceTicketsQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const ServiceTicketListPage = () => {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useMyServiceTicketsQuery();
  const items = data?.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-xl font-semibold">{t('CustomerPortal.ServiceTicket.ListTitle')}</h1>
        <Link
          to="/customer-portal/service-tickets/new"
          className="inline-flex items-center gap-1.5 px-3 py-2 rounded-md bg-blue-600 text-white text-sm hover:bg-blue-700"
        >
          <Plus size={16} /> {t('CustomerPortal.ServiceTicket.New')}
        </Link>
      </div>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError ? (
        <div className="text-sm text-red-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : items.length === 0 ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.ServiceTicket.Empty')}</div>
      ) : (
        <ul className="space-y-2">
          {items.map((tk) => (
            <li
              key={tk.id}
              className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 sm:p-4"
            >
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <Link
                  to={`/customer-portal/service-tickets/${tk.id}`}
                  className="font-medium text-blue-600 hover:underline truncate"
                >
                  {tk.title}
                </Link>
                <div className="flex items-center gap-2 text-xs">
                  <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
                    {t(`CustomerPortal.ServiceTicket.Status.${tk.status}`)}
                  </span>
                  <span className="px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300">
                    {t(`CustomerPortal.ServiceTicket.Priority.${tk.priority}`)}
                  </span>
                </div>
              </div>
              <div className="text-xs text-slate-500 mt-1">
                {new Date(tk.reportedAtUtc).toLocaleString()}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default ServiceTicketListPage;
