import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { CreditCard, FileText, ShieldCheck, Wrench } from 'lucide-react';
import {
  useMyInvoicesQuery,
  useMyPaymentsQuery,
  useMyServiceTicketsQuery,
  useMyWarrantiesQuery,
} from '@/features/customer-portal/hooks/useCustomerPortalQueries';
import { useAuthStore } from '@/features/auth/model/authStore';

interface StatCardProps {
  to: string;
  icon: React.ComponentType<{ size?: number }>;
  title: string;
  value: string | number;
  hint?: string;
}

const StatCard = ({ to, icon: Icon, title, value, hint }: StatCardProps) => (
  <Link
    to={to}
    className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 flex items-center gap-3 hover:shadow-sm transition-shadow"
  >
    <div className="w-10 h-10 rounded-md bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 flex items-center justify-center">
      <Icon size={20} />
    </div>
    <div className="min-w-0">
      <div className="text-xs text-slate-500 dark:text-slate-400 truncate">{title}</div>
      <div className="text-xl font-semibold leading-tight">{value}</div>
      {hint ? (
        <div className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{hint}</div>
      ) : null}
    </div>
  </Link>
);

export const DashboardPage = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);

  const warranties = useMyWarrantiesQuery();
  const tickets = useMyServiceTicketsQuery();
  const invoices = useMyInvoicesQuery({ page: 1, pageSize: 5 });
  const payments = useMyPaymentsQuery();

  const warrantyCount = warranties.data?.data?.length ?? 0;
  const ticketCount = tickets.data?.data?.length ?? 0;
  const invoiceCount = invoices.data?.data?.total ?? 0;
  const paymentCount = payments.data?.data?.length ?? 0;

  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t('CustomerPortal.Dashboard.Welcome', { name: displayName ?? '' })}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
          {t('CustomerPortal.Dashboard.Subtitle')}
        </p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4">
        <StatCard
          to="/customer-portal/warranties"
          icon={ShieldCheck}
          title={t('CustomerPortal.Dashboard.WarrantiesActive')}
          value={warrantyCount}
        />
        <StatCard
          to="/customer-portal/service-tickets"
          icon={Wrench}
          title={t('CustomerPortal.Dashboard.OpenTickets')}
          value={ticketCount}
        />
        <StatCard
          to="/customer-portal/invoices"
          icon={FileText}
          title={t('CustomerPortal.Dashboard.Invoices')}
          value={invoiceCount}
        />
        <StatCard
          to="/customer-portal/payments"
          icon={CreditCard}
          title={t('CustomerPortal.Dashboard.RecentPayments')}
          value={paymentCount}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold">
              {t('CustomerPortal.Dashboard.RecentTicketsTitle')}
            </h2>
            <Link
              to="/customer-portal/service-tickets"
              className="text-xs text-blue-600 hover:underline"
            >
              {t('CustomerPortal.Common.ViewAll')}
            </Link>
          </div>
          {tickets.isLoading ? (
            <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
          ) : tickets.data?.data && tickets.data.data.length > 0 ? (
            <ul className="space-y-2">
              {tickets.data.data.slice(0, 5).map((ticket) => (
                <li
                  key={ticket.id}
                  className="text-sm flex items-center justify-between gap-3 truncate"
                >
                  <Link
                    to={`/customer-portal/service-tickets/${ticket.id}`}
                    className="hover:underline truncate"
                  >
                    {ticket.title}
                  </Link>
                  <span className="text-xs text-slate-500 shrink-0">
                    {t(`CustomerPortal.ServiceTicket.Status.${ticket.status}`)}
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <div className="text-sm text-slate-500">{t('CustomerPortal.Common.NoData')}</div>
          )}
        </div>

        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold">
              {t('CustomerPortal.Dashboard.RecentInvoicesTitle')}
            </h2>
            <Link to="/customer-portal/invoices" className="text-xs text-blue-600 hover:underline">
              {t('CustomerPortal.Common.ViewAll')}
            </Link>
          </div>
          {invoices.isLoading ? (
            <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
          ) : invoices.data?.data?.items && invoices.data.data.items.length > 0 ? (
            <ul className="space-y-2">
              {invoices.data.data.items.slice(0, 5).map((inv) => (
                <li
                  key={inv.id}
                  className="text-sm flex items-center justify-between gap-3 truncate"
                >
                  <Link
                    to={`/customer-portal/invoices/${inv.id}`}
                    className="hover:underline truncate"
                  >
                    {inv.invoiceNumber}
                  </Link>
                  <span className="text-xs text-slate-500 shrink-0">
                    {t(`CustomerPortal.Invoice.Status.${inv.status}`)}
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <div className="text-sm text-slate-500">{t('CustomerPortal.Common.NoData')}</div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
