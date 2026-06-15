import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Activity, Boxes, Clock, Edit2, Package } from 'lucide-react';
import { DetailPanel, PanelTabs } from '@/shared/ui/DetailPanel/DetailPanel';
import {
  useProductQuery,
  useStockTransactionsQuery,
} from '@/features/products/hooks/useProductQueries';
import { ProductComponentsTab } from '@/features/products/ui/ProductComponentsTab';
import { StockByWarehouseTab } from '@/features/inventory/ui/StockByWarehouseTab';
import { StockMovementsTab } from '@/features/inventory/ui/StockMovementsTab';
import { AuditTimeline } from '@/widgets/AuditTimeline';
import type {
  Product,
  StockTransaction,
  StockTransactionType,
} from '@/features/products/model/product.types';

interface Props {
  productId: string | null;
  onClose: () => void;
  onEdit: (product: Product) => void;
}

type Tab = 'overview' | 'transactions' | 'warehouses' | 'movements' | 'rules' | 'audit';

const txnStyles: Record<StockTransactionType, string> = {
  Initial: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Sale: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  SaleCancelled: 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300',
  Restock: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Adjustment: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
};

const fmtNumber = (n: number, locale: string) => new Intl.NumberFormat(locale).format(n);

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

const fmtDateTime = (iso: string, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

export const ProductDetailPanel = ({ productId, onClose, onEdit }: Props) => {
  const { t, i18n } = useTranslation();
  const [tab, setTab] = useState<Tab>('overview');

  const productQuery = useProductQuery(productId);
  const transactionsQuery = useStockTransactionsQuery(tab === 'transactions' ? productId : null);

  const product = productQuery.data?.data ?? null;
  const transactions = transactionsQuery.data?.data?.items ?? [];

  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: t('products.detail.tabs.overview'), icon: <Package size={12} /> },
    {
      id: 'transactions',
      label: t('products.detail.tabs.transactions'),
      icon: <Activity size={12} />,
    },
    {
      id: 'warehouses',
      label: t('products.detail.tabs.warehouses'),
      icon: <Boxes size={12} />,
    },
    {
      id: 'movements',
      label: t('products.detail.tabs.movements', { defaultValue: 'Movements' }),
      icon: <Activity size={12} />,
    },
    { id: 'rules', label: t('products.detail.tabs.rules'), icon: <Edit2 size={12} /> },
    {
      id: 'audit',
      label: t('Common.AuditTab.Title', { defaultValue: 'Audit' }),
      icon: <Clock size={12} />,
    },
  ];

  return (
    <DetailPanel
      open={productId !== null}
      title={product?.name ?? t('common.loading')}
      subtitle={product?.sku}
      onClose={onClose}
    >
      <PanelTabs tabs={tabs} active={tab} onSelect={setTab} />

      <div className="space-y-4 p-4">
        {tab === 'overview' && product && (
          <OverviewTab product={product} locale={i18n.language} onEdit={() => onEdit(product)} />
        )}
        {tab === 'transactions' && (
          <TransactionsTab
            transactions={transactions}
            loading={transactionsQuery.isPending}
            unit={product?.unit ?? ''}
            locale={i18n.language}
          />
        )}
        {tab === 'warehouses' && product && (
          <StockByWarehouseTab
            productId={product.id}
            productSku={product.sku}
            productName={product.name}
            currency={product.currency}
          />
        )}
        {tab === 'movements' && product && <StockMovementsTab productId={product.id} />}
        {tab === 'rules' && productId && <ProductComponentsTab productId={productId} />}
        {tab === 'audit' && productId && (
          <AuditTimeline entityType="Product" entityId={productId} />
        )}
      </div>
    </DetailPanel>
  );
};

const OverviewTab = ({
  product,
  locale,
  onEdit,
}: {
  product: Product;
  locale: string;
  onEdit: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <>
      <div className="grid grid-cols-2 gap-2">
        <Stat
          label={t('products.fields.stockQuantity')}
          value={`${fmtNumber(product.stockQuantity, locale)} ${product.unit}`}
          highlight={product.stockQuantity <= 5 ? 'amber' : 'emerald'}
        />
        <Stat
          label={t('products.fields.price')}
          value={fmtCurrency(product.price, product.currency, locale)}
          highlight="indigo"
        />
      </div>
      <div className="space-y-2 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
        <Row label={t('products.fields.sku')}>{product.sku}</Row>
        <Row label={t('products.fields.unit')}>{product.unit}</Row>
        <Row label={t('products.fields.currency')}>{product.currency}</Row>
        <Row label={t('products.fields.isActive')}>
          {product.isActive ? t('common.active') : t('common.inactive')}
        </Row>
      </div>
      {product.description && (
        <div className="rounded border border-slate-200 bg-slate-50/50 p-3 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-800/30 dark:text-slate-300">
          {product.description}
        </div>
      )}
      <button
        type="button"
        onClick={onEdit}
        className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <Edit2 size={14} />
        {t('common.edit')}
      </button>
    </>
  );
};

const TransactionsTab = ({
  transactions,
  loading,
  unit,
  locale,
}: {
  transactions: StockTransaction[];
  loading: boolean;
  unit: string;
  locale: string;
}) => {
  const { t } = useTranslation();
  if (loading && transactions.length === 0) {
    return <div className="text-sm text-slate-500">{t('common.loading')}</div>;
  }
  if (transactions.length === 0) {
    return (
      <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
        {t('products.detail.noTransactions')}
      </div>
    );
  }
  return (
    <ul className="divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
      {transactions.map((tx) => (
        <li key={tx.id} className="flex items-center justify-between gap-2 px-3 py-2 text-sm">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <span
                className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${txnStyles[tx.type]}`}
              >
                {t(`products.detail.txnType.${tx.type}`)}
              </span>
              {tx.reference && (
                <span className="font-mono text-[10px] text-slate-500">{tx.reference}</span>
              )}
            </div>
            <div className="mt-0.5 text-[10px] text-slate-500">
              {fmtDateTime(tx.occurredAtUtc, locale)}
              {tx.notes ? ` · ${tx.notes}` : ''}
            </div>
          </div>
          <div className="shrink-0 text-right">
            <div
              className={`text-sm font-semibold tabular-nums ${
                tx.quantity > 0
                  ? 'text-emerald-600 dark:text-emerald-400'
                  : 'text-amber-600 dark:text-amber-400'
              }`}
            >
              {tx.quantity > 0 ? '+' : ''}
              {fmtNumber(tx.quantity, locale)} {unit}
            </div>
            <div className="text-[10px] text-slate-500">
              {t('products.detail.balance')}: {fmtNumber(tx.balanceAfter, locale)}
            </div>
          </div>
        </li>
      ))}
    </ul>
  );
};

const Row = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div className="flex items-center justify-between gap-2">
    <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    <span className="truncate text-sm text-slate-700 dark:text-slate-200">{children}</span>
  </div>
);

const highlightClass: Record<'indigo' | 'emerald' | 'amber', string> = {
  indigo: 'border-indigo-200 dark:border-indigo-500/30',
  emerald: 'border-emerald-200 dark:border-emerald-500/30',
  amber: 'border-amber-200 dark:border-amber-500/30',
};

const Stat = ({
  label,
  value,
  highlight,
}: {
  label: string;
  value: string;
  highlight: keyof typeof highlightClass;
}) => (
  <div className={`rounded border bg-white p-2.5 dark:bg-slate-900 ${highlightClass[highlight]}`}>
    <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="mt-0.5 text-base font-bold text-slate-900 dark:text-slate-100">{value}</div>
  </div>
);
