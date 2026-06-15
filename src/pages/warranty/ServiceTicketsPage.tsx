import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Ticket } from 'lucide-react';
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
  Normal: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  High: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Urgent: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const STATUS_TONE: Record<ServiceTicketStatus, string> = {
  Open: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Assigned: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  InProgress: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Resolved: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
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
    <div className="space-y-4 p-4 sm:p-6">
      <header>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100 sm:text-2xl">
          {t('Warranty.ServiceTicket.Title', { defaultValue: 'Service tickets' })}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Warranty.ServiceTicket.Subtitle', {
            defaultValue: 'Customer reported issues and scheduled maintenance.',
          })}
        </p>
      </header>

      <div className="flex flex-wrap items-center gap-2">
        {STATUS_OPTIONS.map((opt) => (
          <button
            key={opt}
            type="button"
            onClick={() => setStatus(opt)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition ${
              status === opt
                ? 'bg-indigo-600 text-white'
                : 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700'
            }`}
          >
            {opt === 'All'
              ? t('Common.Filter.All', { defaultValue: 'All' })
              : t(`Warranty.ServiceTicket.Status.${opt}`, { defaultValue: opt })}
          </button>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <label
          htmlFor="ticket-type-filter"
          className="text-xs font-medium text-slate-500 dark:text-slate-400"
        >
          {t('Warranty.ServiceTicket.Type.Label', { defaultValue: 'Type' })}
        </label>
        <select
          id="ticket-type-filter"
          value={type}
          onChange={(event) => setType(event.target.value as ServiceTicketType | 'All')}
          className="rounded-md border border-slate-200 bg-white px-3 py-1 text-xs text-slate-700 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          {TYPE_OPTIONS.map((opt) => (
            <option key={opt} value={opt}>
              {opt === 'All'
                ? t('Common.Filter.All', { defaultValue: 'All' })
                : t(`Warranty.ServiceTicket.Type.${opt}`, { defaultValue: opt })}
            </option>
          ))}
        </select>
      </div>

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
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900">
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
    </div>
  );
};

export default ServiceTicketsPage;
