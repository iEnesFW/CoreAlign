import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Ban,
  Check,
  Clock,
  FilePlus,
  Package,
  PackageCheck,
  Send,
  ShieldCheck,
  Truck,
} from 'lucide-react';
import type { Order } from '@/features/orders/model/order.types';

interface Props {
  order: Order;
  locale: string;
}

const fmtDateTime = (iso: string | null | undefined, locale: string) => {
  if (!iso) return null;
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const fmtRelative = (iso: string | null | undefined, locale: string): string | null => {
  if (!iso) return null;
  try {
    const diffMs = Date.now() - new Date(iso).getTime();
    const rtf = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' });
    const mins = Math.round(diffMs / 60000);
    if (Math.abs(mins) < 60) return rtf.format(-mins, 'minute');
    const hours = Math.round(mins / 60);
    if (Math.abs(hours) < 24) return rtf.format(-hours, 'hour');
    const days = Math.round(hours / 24);
    if (Math.abs(days) < 30) return rtf.format(-days, 'day');
    const months = Math.round(days / 30);
    if (Math.abs(months) < 12) return rtf.format(-months, 'month');
    return rtf.format(-Math.round(months / 12), 'year');
  } catch {
    return null;
  }
};

type EventKind =
  | 'created'
  | 'submitted'
  | 'approved'
  | 'allocated'
  | 'shipped'
  | 'delivered'
  | 'closed'
  | 'cancelled'
  | 'updated';

interface AuditEvent {
  kind: EventKind;
  label: string;
  at: string;
  detail?: string | null;
  tone: 'slate' | 'sky' | 'indigo' | 'violet' | 'amber' | 'teal' | 'emerald' | 'red';
}

const iconByKind: Record<EventKind, React.ReactNode> = {
  created: <FilePlus size={11} />,
  submitted: <Send size={11} />,
  approved: <ShieldCheck size={11} />,
  allocated: <PackageCheck size={11} />,
  shipped: <Truck size={11} />,
  delivered: <Package size={11} />,
  closed: <Check size={11} />,
  cancelled: <Ban size={11} />,
  updated: <Clock size={11} />,
};

const toneBg: Record<AuditEvent['tone'], string> = {
  slate: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  sky: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  indigo: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  violet: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  amber: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  teal: 'bg-teal-100 text-teal-700 dark:bg-teal-500/20 dark:text-teal-300',
  emerald: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  red: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
};

export const OrderAuditTab = ({ order, locale }: Props) => {
  const { t } = useTranslation();
  const events = useMemo<AuditEvent[]>(() => {
    const list: AuditEvent[] = [];
    list.push({
      kind: 'created',
      label: t('orders.audit.created'),
      at: order.createdAtUtc,
      tone: 'sky',
    });
    if (order.submittedAtUtc) {
      list.push({
        kind: 'submitted',
        label: t('orders.audit.submitted'),
        at: order.submittedAtUtc,
        tone: 'sky',
      });
    }
    if (order.approvedAtUtc) {
      list.push({
        kind: 'approved',
        label: t('orders.audit.approved'),
        at: order.approvedAtUtc,
        tone: 'indigo',
      });
    }
    if (order.actualDeliveryDate) {
      list.push({
        kind: 'delivered',
        label: t('orders.audit.delivered'),
        at: order.actualDeliveryDate,
        tone: 'teal',
      });
    }
    if (order.cancelledAtUtc) {
      list.push({
        kind: 'cancelled',
        label: t('orders.audit.cancelled'),
        at: order.cancelledAtUtc,
        detail: order.cancelReason ?? undefined,
        tone: 'red',
      });
    }
    if (
      order.updatedAtUtc &&
      order.updatedAtUtc !== order.createdAtUtc &&
      !list.some((e) => e.at === order.updatedAtUtc)
    ) {
      list.push({
        kind: 'updated',
        label: t('orders.audit.updated'),
        at: order.updatedAtUtc,
        tone: 'slate',
      });
    }

    list.sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());
    return list;
  }, [order, t]);

  return (
    <div className="space-y-3">
      <section className="flex items-center justify-between gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 dark:border-slate-800 dark:bg-slate-900">
        <div>
          <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {t('orders.audit.currentStatus', { defaultValue: 'Güncel Durum' })}
          </div>
          <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t(`orders.status.${order.status}` as never)}
          </div>
        </div>
        <div className="text-right">
          <div className="text-[10px] uppercase tracking-wider text-slate-400">
            {t('orders.audit.lastUpdate', { defaultValue: 'Son güncelleme' })}
          </div>
          <div className="text-[11px] text-slate-600 dark:text-slate-300">
            {fmtRelative(order.updatedAtUtc, locale) ?? fmtDateTime(order.updatedAtUtc, locale)}
          </div>
        </div>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
          <span className="inline-flex items-center gap-1.5">
            <Clock size={12} />
            {t('orders.audit.title')}
          </span>
          <span className="text-slate-400">{events.length}</span>
        </header>
        <ol className="relative ml-3 space-y-3 px-3 py-3 before:absolute before:left-2 before:top-3 before:bottom-3 before:w-px before:bg-slate-200 dark:before:bg-slate-800">
          {events.map((ev, idx) => (
            <li key={`${ev.kind}-${idx}`} className="relative pl-6">
              <span
                className={`absolute -left-0.5 top-0.5 inline-flex h-5 w-5 items-center justify-center rounded-full ring-2 ring-white dark:ring-slate-900 ${toneBg[ev.tone]}`}
              >
                {iconByKind[ev.kind]}
              </span>
              <div className="text-[11px] font-medium text-slate-900 dark:text-slate-100">
                {ev.label}
              </div>
              <div className="text-[10px] tabular-nums text-slate-500 dark:text-slate-400">
                {fmtDateTime(ev.at, locale)}
                {fmtRelative(ev.at, locale) && (
                  <span className="ml-1 text-slate-400">· {fmtRelative(ev.at, locale)}</span>
                )}
              </div>
              {ev.detail && (
                <div className="mt-0.5 rounded border border-slate-200 bg-slate-50 px-2 py-1 text-[10px] text-slate-600 dark:border-slate-800 dark:bg-slate-800/50 dark:text-slate-300">
                  {ev.detail}
                </div>
              )}
            </li>
          ))}
        </ol>
      </section>

      <PromisedDeliveryCard order={order} locale={locale} />
    </div>
  );
};

const PromisedDeliveryCard = ({ order, locale }: { order: Order; locale: string }) => {
  const { t } = useTranslation();
  const rows = [
    { label: t('orders.audit.requestedDelivery'), at: order.requestedDeliveryDate },
    { label: t('orders.audit.promisedDelivery'), at: order.promisedDeliveryDate },
    { label: t('orders.audit.actualDelivery'), at: order.actualDeliveryDate },
    { label: t('orders.audit.dueDate'), at: order.dueDate },
  ].filter((r) => r.at);
  if (rows.length === 0) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <Truck size={12} />
        {t('orders.audit.deliveryDates')}
      </header>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-[11px]">
        {rows.map((r) => (
          <div key={r.label} className="flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">{r.label}</dt>
            <dd className="font-mono tabular-nums text-slate-900 dark:text-slate-100">
              {fmtDateTime(r.at!, locale)}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  );
};
