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
  Order: { icon: FileText, cls: 'text-sky-500' },
  Invoice: { icon: FileText, cls: 'text-violet-500' },
  Payment: { icon: Wallet, cls: 'text-emerald-500' },
};

export const CustomerInlineCard = ({ customer, onClose, onOpenPanel }: CustomerInlineCardProps) => {
  const { i18n } = useTranslation();
  const locale = i18n.language;
  const overviewQuery = useCustomerOverviewQuery(customer.id);
  const ov = overviewQuery.data?.data;

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
        <div className="py-6 text-center text-sm text-slate-500">Yükleniyor…</div>
      ) : (
        <div className="space-y-4">
          {/* Balance KPIs */}
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Metric
              label="Cari Bakiye"
              value={formatCurrency(balance, locale, currency)}
              tone={balance > 0 ? 'rose' : 'emerald'}
            />
            <Metric
              label="Vadesi Geçen"
              value={formatCurrency(outstanding, locale, currency)}
              tone={outstanding > 0 ? 'rose' : 'slate'}
            />
            <Metric
              label="Kredi Limiti"
              value={formatCurrency(creditLimit, locale, currency)}
              tone="slate"
            />
            <Metric
              label="Kullanılabilir Kredi"
              value={formatCurrency(creditAvailable, locale, currency)}
              tone="emerald"
            />
          </div>

          {/* Recent activity */}
          <div>
            <h4 className="mb-1.5 text-[11px] font-semibold uppercase text-slate-500">
              Son Hareketler
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
                      <span className="w-16 shrink-0 text-slate-500">{a.kind}</span>
                      <span className="w-24 shrink-0 font-mono text-slate-600 dark:text-slate-300">
                        {a.sourceNumber ?? '—'}
                      </span>
                      <span className="flex-1 truncate text-slate-500">
                        {formatDate(a.occurredAtUtc, locale)}
                        {a.status ? ` · ${a.status}` : ''}
                      </span>
                      <span
                        className={`inline-flex items-center gap-0.5 font-mono font-semibold ${
                          inflow
                            ? 'text-emerald-600 dark:text-emerald-400'
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
              <p className="py-3 text-center text-xs text-slate-400">Henüz hareket yok.</p>
            )}
          </div>

          {/* Quick facts */}
          <div className="flex flex-wrap gap-x-6 gap-y-1 text-[11px] text-slate-500">
            {ov?.paymentTermsName && <span>Ödeme: {ov.paymentTermsName}</span>}
            {ov?.salesRepName && <span>Temsilci: {ov.salesRepName}</span>}
            {ov?.lastOrderAtUtc && (
              <span>Son sipariş: {formatDate(ov.lastOrderAtUtc, locale)}</span>
            )}
            {ov?.lastPaymentAtUtc && (
              <span>Son ödeme: {formatDate(ov.lastPaymentAtUtc, locale)}</span>
            )}
          </div>
        </div>
      )}
    </InlineDetailCard>
  );
};

const toneMap = {
  rose: 'text-rose-600 dark:text-rose-400',
  emerald: 'text-emerald-600 dark:text-emerald-400',
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
