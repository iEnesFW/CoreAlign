import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Coins, Percent, TrendingDown, TrendingUp } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { Order, OrderLine } from '@/features/orders/model/order.types';

interface Props {
  order: Order;
  locale: string;
}

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtNumber = (value: number, locale: string, decimals = 2) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);

const fmtPercent = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value) + '%';
  } catch {
    return `${value.toFixed(1)}%`;
  }
};

interface LineMargin {
  line: OrderLine;
  revenue: number;
  cost: number;
  profit: number;
  marginPct: number;
  markupPct: number;
}

const computeMargins = (lines: OrderLine[]): LineMargin[] =>
  lines.map((line) => {
    const revenue = line.lineNetAmount;
    const cost = line.unitCostSnapshot * line.quantity;
    const profit = revenue - cost;
    const marginPct = revenue !== 0 ? (profit / revenue) * 100 : 0;
    const markupPct = cost !== 0 ? (profit / cost) * 100 : 0;
    return { line, revenue, cost, profit, marginPct, markupPct };
  });

const marginTone = (pct: number): 'red' | 'amber' | 'emerald' | 'slate' => {
  if (pct < 0) return 'red';
  if (pct < 10) return 'amber';
  if (pct < 25) return 'slate';
  return 'emerald';
};

const toneText: Record<'red' | 'amber' | 'emerald' | 'slate' | 'indigo', string> = {
  red: 'text-danger-600 dark:text-danger-400',
  amber: 'text-warning-600 dark:text-warning-400',
  emerald: 'text-success-600 dark:text-success-400',
  slate: 'text-slate-700 dark:text-slate-200',
  indigo: 'text-primary-700 dark:text-primary-300',
};

const toneBg: Record<'red' | 'amber' | 'emerald' | 'slate', string> = {
  red: 'bg-danger-500',
  amber: 'bg-warning-500',
  emerald: 'bg-success-500',
  slate: 'bg-slate-400',
};

export const OrderMarginTab = ({ order, locale }: Props) => {
  const { t } = useTranslation();
  const items = useMemo(() => computeMargins(order.lines), [order.lines]);

  const totals = useMemo(() => {
    const revenue = items.reduce((s, i) => s + i.revenue, 0);
    const cost = items.reduce((s, i) => s + i.cost, 0);
    const profit = revenue - cost;
    const marginPct = revenue !== 0 ? (profit / revenue) * 100 : 0;
    const markupPct = cost !== 0 ? (profit / cost) * 100 : 0;
    return { revenue, cost, profit, marginPct, markupPct };
  }, [items]);

  const hasMissingCost = order.lines.some((l) => l.unitCostSnapshot <= 0);

  return (
    <div className="space-y-3">
      <KpiRow
        totals={totals}
        currency={order.currency}
        locale={locale}
        hasMissingCost={hasMissingCost}
      />

      <section className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
          <span>{t('orders.margin.lineTitle')}</span>
          <span className="text-slate-400">{items.length}</span>
        </header>
        <table className="w-full text-left text-xs">
          <thead className="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
            <tr>
              <th className="px-2 py-1.5">{t('orders.lines.product')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.lines.quantity')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.unitCost')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.unitPrice')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.revenue')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.cost')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.profit')}</th>
              <th className="px-2 py-1.5 text-right">{t('orders.margin.marginPct')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.map(({ line, revenue, cost, profit, marginPct }) => {
              const tone = marginTone(marginPct);
              const missing = line.unitCostSnapshot <= 0;
              return (
                <tr key={line.id} className="hover:bg-slate-50/60 dark:hover:bg-slate-800/30">
                  <td className="px-2 py-1.5">
                    <div className="font-medium text-slate-900 dark:text-slate-100">
                      {line.productName}
                    </div>
                    <div className="font-mono text-[10px] text-slate-500">{line.productSku}</div>
                  </td>
                  <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                    {fmtNumber(line.quantity, locale)}
                  </td>
                  <td className="px-2 py-1.5 text-right tabular-nums">
                    {missing ? (
                      <span className="text-warning-600 dark:text-warning-400">—</span>
                    ) : (
                      <span className="text-slate-700 dark:text-slate-300">
                        {fmtCurrency(line.unitCostSnapshot, order.currency, locale)}
                      </span>
                    )}
                  </td>
                  <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                    {fmtCurrency(line.unitPrice, order.currency, locale)}
                  </td>
                  <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                    {fmtCurrency(revenue, order.currency, locale)}
                  </td>
                  <td className="px-2 py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                    {missing ? '—' : fmtCurrency(cost, order.currency, locale)}
                  </td>
                  <td
                    className={`px-2 py-1.5 text-right font-medium tabular-nums ${toneText[tone]}`}
                  >
                    {missing ? '—' : fmtCurrency(profit, order.currency, locale)}
                  </td>
                  <td className="px-2 py-1.5 text-right">
                    {missing ? (
                      <span className="text-warning-600 dark:text-warning-400">—</span>
                    ) : (
                      <div className="flex items-center justify-end gap-1.5">
                        <span className={`font-semibold tabular-nums ${toneText[tone]}`}>
                          {fmtPercent(marginPct, locale)}
                        </span>
                        <div className="h-1 w-10 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                          <div
                            className={`h-full ${toneBg[tone]}`}
                            style={{ width: `${Math.min(100, Math.max(0, marginPct))}%` }}
                          />
                        </div>
                      </div>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
          <tfoot className="bg-slate-50 text-xs dark:bg-slate-800/40">
            <tr>
              <td
                colSpan={4}
                className="px-2 py-2 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400"
              >
                {t('orders.margin.total')}
              </td>
              <td className="px-2 py-2 text-right font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                {fmtCurrency(totals.revenue, order.currency, locale)}
              </td>
              <td className="px-2 py-2 text-right font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                {fmtCurrency(totals.cost, order.currency, locale)}
              </td>
              <td
                className={`px-2 py-2 text-right font-bold tabular-nums ${toneText[marginTone(totals.marginPct)]}`}
              >
                {fmtCurrency(totals.profit, order.currency, locale)}
              </td>
              <td
                className={`px-2 py-2 text-right font-bold tabular-nums ${toneText[marginTone(totals.marginPct)]}`}
              >
                {fmtPercent(totals.marginPct, locale)}
              </td>
            </tr>
          </tfoot>
        </table>
      </section>

      {hasMissingCost && (
        <div className="flex items-start gap-2 rounded-lg border border-warning-200 bg-warning-50/70 p-2.5 text-[11px] text-warning-800 dark:border-warning-500/30 dark:bg-warning-500/10 dark:text-warning-300">
          <AlertTriangle size={12} className="mt-0.5 shrink-0" />
          <span>{t('orders.margin.missingCostWarning')}</span>
        </div>
      )}
    </div>
  );
};

const KpiRow = ({
  totals,
  currency,
  locale,
  hasMissingCost,
}: {
  totals: { revenue: number; cost: number; profit: number; marginPct: number; markupPct: number };
  currency: string;
  locale: string;
  hasMissingCost: boolean;
}) => {
  const { t } = useTranslation();
  const tone = marginTone(totals.marginPct);
  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Kpi
        icon={<Coins size={11} />}
        label={t('orders.margin.revenue')}
        value={fmtCurrency(totals.revenue, currency, locale)}
        tone="indigo"
      />
      <Kpi
        icon={<Coins size={11} />}
        label={t('orders.margin.cost')}
        value={fmtCurrency(totals.cost, currency, locale)}
        sub={hasMissingCost ? t('orders.margin.partialCost') : undefined}
        tone={hasMissingCost ? 'amber' : 'slate'}
      />
      <Kpi
        icon={tone === 'red' ? <TrendingDown size={11} /> : <TrendingUp size={11} />}
        label={t('orders.margin.profit')}
        value={fmtCurrency(totals.profit, currency, locale)}
        tone={tone}
      />
      <Kpi
        icon={<Percent size={11} />}
        label={t('orders.margin.marginPct')}
        value={fmtPercent(totals.marginPct, locale)}
        sub={`${t('orders.margin.markupShort')}: ${fmtPercent(totals.markupPct, locale)}`}
        tone={tone}
        badge={
          tone === 'red'
            ? { label: t('orders.margin.loss'), variant: 'error' as const }
            : tone === 'amber'
              ? { label: t('orders.margin.thin'), variant: 'warning' as const }
              : tone === 'emerald'
                ? { label: t('orders.margin.healthy'), variant: 'success' as const }
                : null
        }
      />
    </div>
  );
};

const kpiTones: Record<'slate' | 'indigo' | 'amber' | 'emerald' | 'red', string> = {
  slate: 'border-slate-200 dark:border-slate-800',
  indigo: 'border-primary-200 dark:border-primary-500/30',
  amber: 'border-warning-200 dark:border-warning-500/30',
  emerald: 'border-success-200 dark:border-success-500/30',
  red: 'border-danger-200 dark:border-danger-500/30',
};

const Kpi = ({
  icon,
  label,
  value,
  sub,
  tone,
  badge,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub?: string;
  tone: keyof typeof kpiTones;
  badge?: { label: string; variant: 'success' | 'warning' | 'error' } | null;
}) => (
  <div className={`rounded-lg border bg-white p-2 dark:bg-slate-900 ${kpiTones[tone]}`}>
    <div className="flex items-center justify-between gap-1 text-[9px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      <div className="flex items-center gap-1">
        {icon}
        <span>{label}</span>
      </div>
      {badge && (
        <Badge variant={badge.variant} pill>
          {badge.label}
        </Badge>
      )}
    </div>
    <div className={`mt-0.5 text-sm font-bold tabular-nums ${toneText[tone]}`}>{value}</div>
    {sub && <div className="text-[9px] text-slate-500 dark:text-slate-400">{sub}</div>}
  </div>
);
