import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Download, FileSpreadsheet, FileText, Filter } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

type ReportFormat = 'pdf' | 'xlsx';

type FilterKey =
  | 'fromDate'
  | 'toDate'
  | 'asOfDate'
  | 'warehouseId'
  | 'productId'
  | 'accountId'
  | 'onlyBelowReorder';

interface ReportDescriptor {
  key: string;
  categoryKey: string;
  titleKey: string;
  descKey: string;
  filters: FilterKey[];
}

const CATALOG: ReportDescriptor[] = [
  {
    key: 'inventory-stock-on-hand',
    categoryKey: 'categoryInventory',
    titleKey: 'inventoryStockOnHand',
    descKey: 'inventoryStockOnHandDesc',
    filters: ['warehouseId', 'productId', 'onlyBelowReorder'],
  },
  {
    key: 'inventory-stock-movements',
    categoryKey: 'categoryInventory',
    titleKey: 'inventoryStockMovements',
    descKey: 'inventoryStockMovementsDesc',
    filters: ['fromDate', 'toDate', 'warehouseId', 'productId'],
  },
  {
    key: 'inventory-reorder-alerts',
    categoryKey: 'categoryInventory',
    titleKey: 'inventoryReorder',
    descKey: 'inventoryReorderDesc',
    filters: ['warehouseId'],
  },
  {
    key: 'accounting-cash-flow',
    categoryKey: 'categoryAccounting',
    titleKey: 'accountingCashFlow',
    descKey: 'accountingCashFlowDesc',
    filters: ['fromDate', 'toDate'],
  },
  {
    key: 'accounting-gl-detail',
    categoryKey: 'categoryAccounting',
    titleKey: 'accountingGlDetail',
    descKey: 'accountingGlDetailDesc',
    filters: ['accountId', 'fromDate', 'toDate'],
  },
  {
    key: 'accounting-ap-aging',
    categoryKey: 'categoryAccounting',
    titleKey: 'accountingApAging',
    descKey: 'accountingApAgingDesc',
    filters: ['asOfDate'],
  },
  {
    key: 'purchase-by-vendor',
    categoryKey: 'categoryPurchase',
    titleKey: 'purchaseByVendor',
    descKey: 'purchaseByVendorDesc',
    filters: ['fromDate', 'toDate'],
  },
  {
    key: 'purchase-by-product',
    categoryKey: 'categoryPurchase',
    titleKey: 'purchaseByProduct',
    descKey: 'purchaseByProductDesc',
    filters: ['fromDate', 'toDate'],
  },
  {
    key: 'purchase-open-pos',
    categoryKey: 'categoryPurchase',
    titleKey: 'purchaseOpenPos',
    descKey: 'purchaseOpenPosDesc',
    filters: [],
  },
];

const isoDate = (d: Date) => d.toISOString().slice(0, 10);

export const ReportLibraryPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [activeKey, setActiveKey] = useState<string | null>(null);

  const grouped = useMemo(() => {
    const map = new Map<string, ReportDescriptor[]>();
    for (const r of CATALOG) {
      const list = map.get(r.categoryKey) ?? [];
      list.push(r);
      map.set(r.categoryKey, list);
    }
    return Array.from(map.entries());
  }, []);

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => navigate('/dashboard/reports')}
            className="inline-flex items-center gap-1 rounded border border-slate-200 px-2 py-1 text-[11px] hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"
          >
            <ArrowLeft size={12} /> {t('reports.library.back')}
          </button>
          <div>
            <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
              {t('reports.library.title')}
            </h1>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('reports.library.subtitle')}
            </p>
          </div>
        </div>
      </header>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {grouped.map(([categoryKey, reports]) => (
          <section
            key={categoryKey}
            className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
          >
            <header className="text-[11px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t(`reports.library.${categoryKey}`)}
            </header>
            <ul className="mt-2 space-y-1">
              {reports.map((r) => (
                <li key={r.key}>
                  <button
                    type="button"
                    onClick={() => setActiveKey(r.key)}
                    className={`flex w-full flex-col items-start gap-0.5 rounded border px-2 py-2 text-left text-[11px] transition ${
                      activeKey === r.key
                        ? 'border-indigo-300 bg-indigo-50 dark:border-indigo-500/40 dark:bg-indigo-500/10'
                        : 'border-slate-200 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/50'
                    }`}
                  >
                    <span className="font-semibold text-slate-900 dark:text-slate-100">
                      {t(`reports.library.${r.titleKey}`)}
                    </span>
                    <span className="text-[10px] text-slate-500 dark:text-slate-400">
                      {t(`reports.library.${r.descKey}`)}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>

      {activeKey && (
        <ReportFilterPanel
          report={CATALOG.find((r) => r.key === activeKey)!}
          onClose={() => setActiveKey(null)}
        />
      )}
    </div>
  );
};

interface FilterState {
  fromDate: string;
  toDate: string;
  asOfDate: string;
  warehouseId: string;
  productId: string;
  accountId: string;
  onlyBelowReorder: boolean;
}

const defaultFilters = (): FilterState => {
  const now = new Date();
  const from = new Date(now);
  from.setDate(from.getDate() - 30);
  return {
    fromDate: isoDate(from),
    toDate: isoDate(now),
    asOfDate: isoDate(now),
    warehouseId: '',
    productId: '',
    accountId: '',
    onlyBelowReorder: false,
  };
};

const ReportFilterPanel = ({
  report,
  onClose,
}: {
  report: ReportDescriptor;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<FilterState>(defaultFilters);
  const [busyFormat, setBusyFormat] = useState<ReportFormat | null>(null);

  const trigger = async (format: ReportFormat) => {
    setBusyFormat(format);
    const params: Record<string, string | boolean> = { format };
    if (report.filters.includes('fromDate')) {
      params.fromUtc = new Date(`${filters.fromDate}T00:00:00Z`).toISOString();
    }
    if (report.filters.includes('toDate')) {
      params.toUtc = new Date(`${filters.toDate}T23:59:59Z`).toISOString();
    }
    if (report.filters.includes('asOfDate')) {
      params.asOfUtc = new Date(`${filters.asOfDate}T23:59:59Z`).toISOString();
    }
    if (report.filters.includes('warehouseId') && filters.warehouseId.trim()) {
      params.warehouseId = filters.warehouseId.trim();
    }
    if (report.filters.includes('productId') && filters.productId.trim()) {
      params.productId = filters.productId.trim();
    }
    if (report.filters.includes('accountId') && filters.accountId.trim()) {
      params.accountId = filters.accountId.trim();
    }
    if (report.filters.includes('onlyBelowReorder') && filters.onlyBelowReorder) {
      params.onlyBelowReorder = true;
    }

    const [response, error] = await safeRequest(
      apiClient.get<Blob>(`/reports/${report.key}`, {
        params,
        responseType: 'blob',
      }),
    );
    setBusyFormat(null);
    if (error || !response) {
      logger.warn('Report download failed', { reportKey: report.key, error: String(error) });
      return;
    }
    const stamp = new Date().toISOString().slice(0, 16).replace(/[:T]/g, '-');
    const fileName = `${report.key}-${stamp}.${format}`;
    const blob = response.data instanceof Blob ? response.data : new Blob([response.data]);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  };

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <Filter size={13} className="text-slate-500" />
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t(`reports.library.${report.titleKey}`)} · {t('reports.library.filters')}
          </h2>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="rounded border border-slate-200 px-2 py-1 text-[11px] hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"
        >
          {t('reports.library.back')}
        </button>
      </header>

      <div className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {report.filters.includes('fromDate') && (
          <LabeledInput
            label={t('reports.library.fromDate')}
            type="date"
            value={filters.fromDate}
            onChange={(v) => setFilters((f) => ({ ...f, fromDate: v }))}
          />
        )}
        {report.filters.includes('toDate') && (
          <LabeledInput
            label={t('reports.library.toDate')}
            type="date"
            value={filters.toDate}
            onChange={(v) => setFilters((f) => ({ ...f, toDate: v }))}
          />
        )}
        {report.filters.includes('asOfDate') && (
          <LabeledInput
            label={t('reports.library.asOfDate')}
            type="date"
            value={filters.asOfDate}
            onChange={(v) => setFilters((f) => ({ ...f, asOfDate: v }))}
          />
        )}
        {report.filters.includes('warehouseId') && (
          <LabeledInput
            label={t('reports.library.warehouseId')}
            value={filters.warehouseId}
            onChange={(v) => setFilters((f) => ({ ...f, warehouseId: v }))}
            placeholder="GUID"
          />
        )}
        {report.filters.includes('productId') && (
          <LabeledInput
            label={t('reports.library.productId')}
            value={filters.productId}
            onChange={(v) => setFilters((f) => ({ ...f, productId: v }))}
            placeholder="GUID"
          />
        )}
        {report.filters.includes('accountId') && (
          <LabeledInput
            label={t('reports.library.accountId')}
            value={filters.accountId}
            onChange={(v) => setFilters((f) => ({ ...f, accountId: v }))}
            placeholder="GUID"
          />
        )}
        {report.filters.includes('onlyBelowReorder') && (
          <label className="flex items-center gap-2 text-[11px] text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={filters.onlyBelowReorder}
              onChange={(e) => setFilters((f) => ({ ...f, onlyBelowReorder: e.target.checked }))}
              className="h-3.5 w-3.5"
            />
            {t('reports.library.onlyBelowReorder')}
          </label>
        )}
      </div>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={busyFormat !== null}
          onClick={() => trigger('pdf')}
          className="inline-flex items-center gap-1.5 rounded border border-indigo-300 bg-indigo-50 px-3 py-1.5 text-[11px] font-semibold text-indigo-700 hover:bg-indigo-100 disabled:opacity-60 dark:border-indigo-500/40 dark:bg-indigo-500/10 dark:text-indigo-300"
        >
          <FileText size={13} />
          {busyFormat === 'pdf' ? t('reports.library.downloading') : t('reports.library.pdf')}
        </button>
        <button
          type="button"
          disabled={busyFormat !== null}
          onClick={() => trigger('xlsx')}
          className="inline-flex items-center gap-1.5 rounded border border-emerald-300 bg-emerald-50 px-3 py-1.5 text-[11px] font-semibold text-emerald-700 hover:bg-emerald-100 disabled:opacity-60 dark:border-emerald-500/40 dark:bg-emerald-500/10 dark:text-emerald-300"
        >
          <FileSpreadsheet size={13} />
          {busyFormat === 'xlsx' ? t('reports.library.downloading') : t('reports.library.xlsx')}
        </button>
        <span className="text-[10px] text-slate-500 dark:text-slate-400">
          <Download size={10} className="mb-0.5 inline" /> {t('reports.library.download')}
        </span>
      </div>
    </section>
  );
};

const LabeledInput = ({
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  placeholder?: string;
}) => (
  <label className="flex flex-col gap-1 text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
    <span>{label}</span>
    <input
      type={type}
      value={value}
      placeholder={placeholder}
      onChange={(e) => onChange(e.target.value)}
      className="rounded border border-slate-200 bg-white px-2 py-1 text-[11px] text-slate-900 placeholder-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
    />
  </label>
);
