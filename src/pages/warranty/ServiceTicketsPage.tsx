import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Ticket } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Select } from '@/shared/ui/Select/Select';
import { useServiceTicketsQuery } from '@/features/warranty/hooks/useServiceTickets';
import type {
  ServiceTicket,
  ServiceTicketPriority,
  ServiceTicketStatus,
  ServiceTicketType,
} from '@/features/warranty/model/warranty.types';

const STATUS_OPTIONS: (ServiceTicketStatus | 'All')[] = [
  'All',
  'Open',
  'Assigned',
  'InProgress',
  'Resolved',
  'Cancelled',
];

const TYPE_OPTIONS: (ServiceTicketType | 'All')[] = [
  'All',
  'PreventiveMaintenance',
  'WarrantyClaim',
  'OutOfWarrantyRepair',
  'Inspection',
];

const PRIORITY_TONE: Record<ServiceTicketPriority, string> = {
  Low: 'bg-slate-100 text-slate-600 dark:bg-slate-700/40 dark:text-slate-300',
  Normal: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  High: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Urgent: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const STATUS_TONE: Record<ServiceTicketStatus, string> = {
  Open: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Assigned: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  InProgress: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Resolved: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

export const ServiceTicketsPage = () => {
  const { t, i18n } = useTranslation();
  const [status, setStatus] = useState<ServiceTicketStatus | 'All'>('All');
  const [type, setType] = useState<ServiceTicketType | 'All'>('All');

  const params = useMemo(
    () => ({
      status: status === 'All' ? undefined : status,
      type: type === 'All' ? undefined : type,
    }),
    [status, type],
  );

  const { data, isLoading } = useServiceTicketsQuery(params);
  const tickets = data?.data ?? [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Ticket size={20} />}
          title={t('Warranty.ServiceTicket.Title', { defaultValue: 'Service tickets' })}
          subtitle={t('Warranty.ServiceTicket.Subtitle', {
            defaultValue: 'Customer reported issues and scheduled maintenance.',
          })}
        />
      }
      toolbar={
        <div className="flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            {STATUS_OPTIONS.map((opt) => (
              <button
                key={opt}
                type="button"
                onClick={() => setStatus(opt)}
                className={`rounded-full px-3 py-1 text-xs font-medium transition ${
                  status === opt
                    ? 'bg-primary-600 text-white'
                    : 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700'
                }`}
              >
                {opt === 'All'
                  ? t('Common.Filter.All', { defaultValue: 'All' })
                  : t(`Warranty.ServiceTicket.Status.${opt}`, { defaultValue: opt })}
              </button>
            ))}
          </div>
          <Select
            id="ticket-type-filter"
            value={type}
            onChange={(event) => setType(event.target.value as ServiceTicketType | 'All')}
            label={t('Warranty.ServiceTicket.Type.Label', { defaultValue: 'Type' })}
            className="w-full sm:w-48"
          >
            {TYPE_OPTIONS.map((opt) => (
              <option key={opt} value={opt}>
                {opt === 'All'
                  ? t('Common.Filter.All', { defaultValue: 'All' })
                  : t(`Warranty.ServiceTicket.Type.${opt}`, { defaultValue: opt })}
              </option>
            ))}
          </Select>
        </div>
      }
    >
      {isLoading ? (
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Common.Loading', { defaultValue: 'Loading...' })}
        </p>
      ) : tickets.length === 0 ? (
        <div className="rounded-lg border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
          <Ticket className="mx-auto mb-2 h-8 w-8" />
          {t('Warranty.ServiceTicket.Empty', {
            defaultValue: 'No service tickets match the current filters.',
          })}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs font-medium uppercase text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2">
                  {t('Warranty.ServiceTicket.Title', { defaultValue: 'Title' })}
                </th>
                <th className="px-3 py-2">
                  {t('Warranty.ServiceTicket.Type.Label', { defaultValue: 'Type' })}
                </th>
                <th className="px-3 py-2">
                  {t('Warranty.ServiceTicket.Status.Label', { defaultValue: 'Status' })}
                </th>
                <th className="px-3 py-2">
                  {t('Warranty.ServiceTicket.Priority.Label', { defaultValue: 'Priority' })}
                </th>
                <th className="px-3 py-2">
                  {t('Warranty.ServiceTicket.ReportedAt', { defaultValue: 'Reported' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              {tickets.map((ticket: ServiceTicket) => (
                <tr key={ticket.id}>
                  <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{ticket.title}</td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    {t(`Warranty.ServiceTicket.Type.${ticket.type}`, { defaultValue: ticket.type })}
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_TONE[ticket.status]}`}
                    >
                      {t(`Warranty.ServiceTicket.Status.${ticket.status}`, {
                        defaultValue: ticket.status,
                      })}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-medium ${PRIORITY_TONE[ticket.priority]}`}
                    >
                      {t(`Warranty.ServiceTicket.Priority.${ticket.priority}`, {
                        defaultValue: ticket.priority,
                      })}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                    {new Date(ticket.reportedAtUtc).toLocaleString(i18n.language)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </ListPageTemplate>
  );
};

export default ServiceTicketsPage;
