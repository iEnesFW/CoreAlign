import { useTranslation } from 'react-i18next';
import { ArrowDownLeft, ArrowUpRight, FileText, Wallet } from 'lucide-react';
import { InlineDetailCard } from '@/shared/ui/InlineDetailCard/InlineDetailCard';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useCustomerOverviewQuery } from '@/features/customers/hooks/useCustomerQueries';
import type { Customer } from '@/features/customers/model/customer.types';

interface CustomerInlineCardProps {
  customer: Customer;
  onClose: () => void;
  onOpenPanel: () => void;
}

const KIND_META: Record<string, { icon: typeof FileText; cls: string }> = {
  Order: { icon: FileText, cls: 'text-info-500' },
  Invoice: { icon: FileText, cls: 'text-violet-500' },
  Payment: { icon: Wallet, cls: 'text-success-500' },
};

export const CustomerInlineCard = ({ customer, onClose, onOpenPanel }: CustomerInlineCardProps) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const overviewQuery = useCustomerOverviewQuery(customer.id);
  const ov = overviewQuery.data?.data;

  const activityStatusLabel = (kind: string, status: string) =>
    t(
      kind === 'Order'
        ? `orders.status.${status}`
        : kind === 'Invoice'
          ? `invoices.status.${status}`
          : kind === 'Payment'
            ? `invoices.paymentStatus.${status}`
            : `customers.detail.activity.${kind}`,
      { defaultValue: status },
    );

  const currency = customer.defaultCurrency;
  const balance = ov?.currentBalance ?? customer.currentBalance;
  const outstanding = ov?.outstanding ?? customer.overdueAmount;
  const creditLimit = ov?.creditLimit ?? customer.creditLimit;
  const creditAvailable = ov?.creditAvailable ?? Math.max(0, creditLimit - balance);

  return (
    <InlineDetailCard
      title={customer.name}
      subtitle={[customer.code, customer.taxNumber].filter(Boolean).join(' · ') || undefined}
      onOpenPanel={onOpenPanel}
      onClose={onClose}
    >
      {overviewQuery.isPending ? (
        <div className="py-6 text-center text-sm text-slate-500">
          {t('CustomerCard.Loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Metric
              label={t('CustomerCard.CurrentBalance', { defaultValue: 'Cari Bakiye' })}
              value={formatCurrency(balance, locale, currency)}
              tone={balance > 0 ? 'rose' : 'emerald'}
            />
            <Metric
              label={t('CustomerCard.Overdue', { defaultValue: 'Vadesi Geçen' })}
              value={formatCurrency(outstanding, locale, currency)}
              tone={outstanding > 0 ? 'rose' : 'slate'}
            />
            <Metric
              label={t('CustomerCard.CreditLimit', { defaultValue: 'Kredi Limiti' })}
              value={formatCurrency(creditLimit, locale, currency)}
              tone="slate"
            />
            <Metric
              label={t('CustomerCard.CreditAvailable', { defaultValue: 'Kullanılabilir Kredi' })}
              value={formatCurrency(creditAvailable, locale, currency)}
              tone="emerald"
            />
          </div>

          <div>
            <h4 className="mb-1.5 text-[11px] font-semibold uppercase text-slate-500">
              {t('CustomerCard.RecentActivity', { defaultValue: 'Son Hareketler' })}
            </h4>
            {ov && ov.recentActivity.length > 0 ? (
              <ul className="divide-y divide-slate-100 dark:divide-slate-800">
                {ov.recentActivity.slice(0, 6).map((a, i) => {
                  const meta = KIND_META[a.kind] ?? { icon: FileText, cls: 'text-slate-400' };
                  const Icon = meta.icon;
                  const inflow = a.kind === 'Payment';
                  return (
                    <li
                      key={`${a.sourceId}-${i}`}
                      className="flex items-center gap-2 py-1.5 text-xs"
                    >
                      <Icon size={13} className={meta.cls} />
                      <span className="w-16 shrink-0 text-slate-500">
                        {t(`customers.detail.activity.${a.kind}`, { defaultValue: a.kind })}
                      </span>
                      <span className="w-24 shrink-0 font-mono text-slate-600 dark:text-slate-300">
                        {a.sourceNumber ?? '—'}
                      </span>
                      <span className="flex-1 truncate text-slate-500">
                        {formatDate(a.occurredAtUtc, locale)}
                        {a.status ? ` · ${activityStatusLabel(a.kind, a.status)}` : ''}
                      </span>
                      <span
                        className={`inline-flex items-center gap-0.5 font-mono font-semibold ${
                          inflow
                            ? 'text-success-600 dark:text-success-400'
                            : 'text-slate-700 dark:text-slate-200'
                        }`}
                      >
                        {inflow ? <ArrowDownLeft size={11} /> : <ArrowUpRight size={11} />}
                        {formatCurrency(a.amount, locale, a.currency)}
                      </span>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p className="py-3 text-center text-xs text-slate-400">
                {t('CustomerCard.NoActivity', { defaultValue: 'Henüz hareket yok.' })}
              </p>
            )}
          </div>

          <div className="flex flex-wrap gap-x-6 gap-y-1 text-[11px] text-slate-500">
            {ov?.paymentTermsName && (
              <span>
                {t('CustomerCard.PaymentTerms', {
                  defaultValue: 'Ödeme: {{value}}',
                  value: ov.paymentTermsName,
                })}
              </span>
            )}
            {ov?.salesRepName && (
              <span>
                {t('CustomerCard.SalesRep', {
                  defaultValue: 'Temsilci: {{value}}',
                  value: ov.salesRepName,
                })}
              </span>
            )}
            {ov?.lastOrderAtUtc && (
              <span>
                {t('CustomerCard.LastOrder', {
                  defaultValue: 'Son sipariş: {{value}}',
                  value: formatDate(ov.lastOrderAtUtc, locale),
                })}
              </span>
            )}
            {ov?.lastPaymentAtUtc && (
              <span>
                {t('CustomerCard.LastPayment', {
                  defaultValue: 'Son ödeme: {{value}}',
                  value: formatDate(ov.lastPaymentAtUtc, locale),
                })}
              </span>
            )}
          </div>
        </div>
      )}
    </InlineDetailCard>
  );
};

const toneMap = {
  rose: 'text-danger-600 dark:text-danger-400',
  emerald: 'text-success-600 dark:text-success-400',
  slate: 'text-slate-900 dark:text-slate-100',
} as const;

const Metric = ({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: keyof typeof toneMap;
}) => (
  <div className="rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2 dark:border-slate-800 dark:bg-slate-800/30">
    <div className="text-[10px] font-medium uppercase text-slate-500">{label}</div>
    <div className={`mt-0.5 font-mono text-sm font-semibold ${toneMap[tone]}`}>{value}</div>
  </div>
);
