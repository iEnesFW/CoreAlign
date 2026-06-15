import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

type Tone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

const orderStatusTone: Record<string, Tone> = {
  Draft: 'neutral',
  Submitted: 'info',
  Approved: 'info',
  Confirmed: 'info',
  Allocated: 'info',
  Picking: 'warning',
  Packed: 'warning',
  PartiallyShipped: 'warning',
  Shipped: 'success',
  Delivered: 'success',
  Closed: 'neutral',
  Returned: 'warning',
  Cancelled: 'danger',
};

const invoiceStatusTone: Record<string, Tone> = {
  Draft: 'neutral',
  Issued: 'info',
  Sent: 'info',
  PartiallyPaid: 'warning',
  Paid: 'success',
  Overdue: 'danger',
  Void: 'neutral',
  Cancelled: 'danger',
};

const dealerStatusTone: Record<string, Tone> = {
  Active: 'success',
  Suspended: 'warning',
  Archived: 'neutral',
};

const toneClasses: Record<Tone, string> = {
  neutral: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  info: 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
  success: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  warning: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  danger: 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
};

export const OrderStatusBadge = ({ status }: { status: string }) => {
  const { t } = useTranslation();
  const tone = orderStatusTone[status] ?? 'neutral';
  return <Badge tone={tone}>{t(`orderStatus.${status}`, status)}</Badge>;
};

export const InvoiceStatusBadge = ({
  status,
  isOverdue,
}: {
  status: string;
  isOverdue?: boolean;
}) => {
  const { t } = useTranslation();
  const effective = isOverdue && status !== 'Paid' ? 'Overdue' : status;
  const tone = invoiceStatusTone[effective] ?? 'neutral';
  return <Badge tone={tone}>{t(`invoiceStatus.${effective}`, effective)}</Badge>;
};

export const DealerStatusBadge = ({ status }: { status: string }) => {
  const { t } = useTranslation();
  const tone = dealerStatusTone[status] ?? 'neutral';
  return <Badge tone={tone}>{t(`dealers.dealerStatus.${status}`, status)}</Badge>;
};

const Badge = ({ tone, children }: { tone: Tone; children: React.ReactNode }) => (
  <span
    className={cn(
      'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
      toneClasses[tone],
    )}
  >
    {children}
  </span>
);
