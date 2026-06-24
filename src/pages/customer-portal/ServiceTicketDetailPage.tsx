import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { useMyServiceTicketQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const ServiceTicketDetailPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useMyServiceTicketQuery(id);

  const ticket = data?.data;

  return (
    <div className="space-y-4">
      <Link
        to="/customer-portal/service-tickets"
        className="inline-flex items-center gap-1 text-sm text-primary-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError || !ticket ? (
        <div className="text-sm text-danger-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : (
        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-3">
          <div className="flex items-start justify-between gap-3">
            <h1 className="text-xl font-semibold">{ticket.title}</h1>
            <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
              {t(`CustomerPortal.ServiceTicket.Status.${ticket.status}`)}
            </span>
          </div>
          <div className="flex flex-wrap gap-2 text-xs">
            <span className="px-2 py-0.5 rounded-full bg-warning-100 text-warning-800 dark:bg-warning-900/30 dark:text-warning-300">
              {t(`CustomerPortal.ServiceTicket.Priority.${ticket.priority}`)}
            </span>
            <span className="px-2 py-0.5 rounded-full bg-primary-100 text-primary-800 dark:bg-primary-900/30 dark:text-primary-300">
              {t(`CustomerPortal.ServiceTicket.Type.${ticket.type}`)}
            </span>
          </div>
          <div>
            <div className="text-slate-500 text-xs mb-1">
              {t('CustomerPortal.ServiceTicket.Description')}
            </div>
            <p className="text-sm whitespace-pre-wrap">{ticket.descriptionMd}</p>
          </div>
          {ticket.resolutionNotesMd ? (
            <div>
              <div className="text-slate-500 text-xs mb-1">
                {t('CustomerPortal.ServiceTicket.Resolution')}
              </div>
              <p className="text-sm whitespace-pre-wrap">{ticket.resolutionNotesMd}</p>
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
};

export default ServiceTicketDetailPage;
