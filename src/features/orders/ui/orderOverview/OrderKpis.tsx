import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertCircle,
  Banknote,
  CalendarClock,
  FileText,
  Hash,
  Layers,
  Truck,
  Wallet,
} from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { Order } from '@/features/orders/model/order.types';
import { fmtCurrency, fmtDate, fmtNumber } from './format';

export const KpiRow = ({
  order,
  locale,
  totalQty,
  shippedQty,
  invoicedQty,
}: {
  order: Order;
  locale: string;
  totalQty: number;
  shippedQty: number;
  invoicedQty: number;
}) => {
  const { t } = useTranslation();
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<Banknote size={11} />}
        label={t('orders.detail.metrics.total')}
        value={fmtCurrency(order.total, order.currency, locale)}
        tone="indigo"
      />
      <Kpi
        icon={<Layers size={11} />}
        label={t('orders.detail.metrics.lines')}
        value={String(order.lines.length)}
        sub={`${fmtNumber(totalQty, locale)} ${t('orders.detail.metrics.units')}`}
        tone="blue"
      />
      <Kpi
        icon={<Truck size={11} />}
        label={t('orders.detail.metrics.shipped')}
        value={`${fmtNumber(shippedQty, locale)} / ${fmtNumber(totalQty, locale)}`}
        sub={t('orders.detail.metrics.units')}
        tone={shippedQty >= totalQty ? 'emerald' : shippedQty > 0 ? 'amber' : 'slate'}
      />
      <Kpi
        icon={<FileText size={11} />}
        label={t('orders.detail.metrics.invoiced')}
        value={`${fmtNumber(invoicedQty, locale)} / ${fmtNumber(totalQty, locale)}`}
        sub={t('orders.detail.metrics.units')}
        tone={invoicedQty >= totalQty ? 'emerald' : invoicedQty > 0 ? 'amber' : 'slate'}
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'blue' | 'emerald' | 'amber', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-primary-200 dark:border-primary-500/30',
  blue: 'border-primary-200 dark:border-primary-500/30',
  emerald: 'border-success-200 dark:border-success-500/30',
  amber: 'border-warning-200 dark:border-warning-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof kpiTones;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      <span>{label}</span>
    </div>
    <div className="mt-0.5 text-sm font-bold tabular-nums text-slate-900 dark:text-slate-100">
      {value}
    </div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);

export const MetaChips = ({ order, dueIn }: { order: Order; dueIn: number | null }) => {
  const { t } = useTranslation();
  const chips: { icon: ReactNode; label: string; value: string }[] = [];
  chips.push({
    icon: <Wallet size={11} />,
    label: t('orders.fields.currency'),
    value: `${order.currency}${order.exchangeRate && order.exchangeRate !== 1 ? ` · ${order.exchangeRate.toFixed(4)}` : ''}`,
  });
  if (order.paymentTermsNetDaysSnapshot !== null) {
    chips.push({
      icon: <CalendarClock size={11} />,
      label: t('orders.fields.terms'),
      value: t('customers.detail.meta.netDays', { count: order.paymentTermsNetDaysSnapshot }),
    });
  }
  if (order.dueDate) {
    chips.push({
      icon: <CalendarClock size={11} />,
      label: t('orders.fields.dueDate'),
      value:
        dueIn !== null
          ? dueIn === 0
            ? t('orders.dueToday', { defaultValue: 'Due today' })
            : dueIn > 0
              ? t('orders.dueIn', { count: dueIn, defaultValue: `Due in ${dueIn}d` })
              : t('orders.overdueBy', { count: -dueIn, defaultValue: `Overdue ${-dueIn}d` })
          : fmtDate(order.dueDate, 'en-US'),
    });
  }
  if (order.requestedDeliveryDate) {
    chips.push({
      icon: <Truck size={11} />,
      label: t('orders.fields.requestedDelivery'),
      value: fmtDate(order.requestedDeliveryDate, 'en-US'),
    });
  }
  if (order.channel) {
    chips.push({
      icon: <Hash size={11} />,
      label: t('orders.fields.channel'),
      value: order.channel,
    });
  }
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {chips.map((chip) => (
        <span
          key={`${chip.label}-${chip.value}`}
          className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200"
        >
          {chip.icon}
          <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {chip.label}
          </span>
          <span className="font-medium">{chip.value}</span>
        </span>
      ))}
      {dueIn !== null && dueIn < 0 && (
        <Badge variant="error" pill>
          <AlertCircle size={9} className="mr-1" />
          {t('orders.overdue', { defaultValue: 'Overdue' })}
        </Badge>
      )}
    </div>
  );
};
