import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  CircleDollarSign,
  Download,
  Layers,
  Package,
  PackageX,
  Plus,
  Sparkles,
  Trash2,
  X,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { downloadCsv } from '@/shared/lib/exportCsv';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { StatStrip, type StatStripItem } from '@/shared/ui/StatStrip/StatStrip';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { FilterChip } from '@/shared/ui/FilterChip/FilterChip';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { CollapsibleSection } from '@/shared/ui/CollapsibleSection/CollapsibleSection';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { Pagination } from '@/shared/ui/Pagination/Pagination';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { ProductDetailPanel } from '@/features/products/ui/ProductDetailPanel';
import { ProductInlineCard } from '@/features/products/ui/ProductInlineCard';
import { ProductFormModal } from '@/features/products/ui/ProductFormModal';
import { ProductList } from '@/features/products/ui/ProductList';
import { useDeleteProduct, useProductsQuery } from '@/features/products/hooks/useProductQueries';
import type { Product, ProductStatus } from '@/features/products/model/product.types';

const PAGE_SIZE = 10;

type StatusFilter = 'all' | ProductStatus;

const exportProductsCsv = (rows: Product[]) =>
  downloadCsv({
    filename: 'products',
    rows,
    columns: [
      { header: 'Sku', value: (p) => p.sku },
      { header: 'Name', value: (p) => p.name },
      { header: 'Barcode', value: (p) => p.barcode },
      { header: 'Brand', value: (p) => p.brandId },
      { header: 'Currency', value: (p) => p.currency },
      { header: 'Price', value: (p) => p.price },
      { header: 'ListPrice', value: (p) => p.listPrice },
      { header: 'StockQuantity', value: (p) => p.stockQuantity },
      { header: 'Unit', value: (p) => p.unit },
      { header: 'ReorderPoint', value: (p) => p.reorderPoint },
      { header: 'Status', value: (p) => p.status },
    ],
  });

export const ProductsPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [outOfStockOnly, setOutOfStockOnly] = useState(false);
  const [trackedOnly, setTrackedOnly] = useState(false);

  const [editing, setEditing] = useState<Product | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [bulkIds, setBulkIds] = useState<string[]>([]);
  const [panelOpen, setPanelOpen] = useState(false);

  const params = useMemo(
    () => ({
      page,
      pageSize,
      search: debouncedSearch.trim() || undefined,
    }),
    [page, pageSize, debouncedSearch],
  );

  const productsQuery = useProductsQuery(params);
  const deleteMutation = useDeleteProduct();
  const confirm = useConfirm();

  const result = productsQuery.data?.data;
  const products = useMemo(() => result?.items ?? [], [result?.items]);
  const total = result?.total ?? 0;

  const stats = useMemo(() => {
    let inventoryValue = 0;
    let lowStockCount = 0;
    let outOfStockCount = 0;
    const statusCounts: Record<ProductStatus, number> = {
      Active: 0,
      New: 0,
      Discontinued: 0,
      EndOfLife: 0,
    };
    products.forEach((p) => {
      inventoryValue += p.stockQuantity * (p.averageCost || p.standardCost || 0);
      statusCounts[p.status] += 1;
      if (p.isStockTracked) {
        if (p.stockQuantity <= 0) outOfStockCount += 1;
        else if (p.stockQuantity <= p.reorderPoint) lowStockCount += 1;
      }
    });
    return { inventoryValue, lowStockCount, outOfStockCount, statusCounts };
  }, [products]);

  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      if (statusFilter !== 'all' && p.status !== statusFilter) return false;
      if (trackedOnly && !p.isStockTracked) return false;
      if (outOfStockOnly && !(p.isStockTracked && p.stockQuantity <= 0)) return false;
      if (
        lowStockOnly &&
        !(p.isStockTracked && p.stockQuantity > 0 && p.stockQuantity <= p.reorderPoint)
      )
        return false;
      return true;
    });
  }, [products, statusFilter, trackedOnly, outOfStockOnly, lowStockOnly]);

  const hasActiveFilters =
    statusFilter !== 'all' ||
    lowStockOnly ||
    outOfStockOnly ||
    trackedOnly ||
    debouncedSearch.trim() !== '';

  const clearFilters = () => {
    setSearch('');
    setStatusFilter('all');
    setLowStockOnly(false);
    setOutOfStockOnly(false);
    setTrackedOnly(false);
    setPage(1);
  };

  const handleCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const handleEdit = (product: Product) => {
    setEditing(product);
    setModalOpen(true);
  };

  const handleDelete = async (product: Product) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('products.confirmDelete', { name: product.name }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    deleteMutation.mutate(product.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('products.toast.deleted'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const bulkSelected = filteredProducts.filter((p) => bulkIds.includes(p.id));

  const handleBulkExport = () => {
    exportProductsCsv(bulkSelected.length > 0 ? bulkSelected : filteredProducts);
  };

  const handleBulkDelete = async () => {
    if (bulkSelected.length === 0) return;
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('products.bulkConfirmDelete', {
        count: bulkSelected.length,
        defaultValue: `${bulkSelected.length} ürün silinsin mi?`,
      }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    const results = await Promise.allSettled(
      bulkSelected.map(async (p) => {
        const res = await deleteMutation.mutateAsync(p.id);
        if (!res.isSuccess) throw new Error(res.errors[0] ?? 'failed');
        return p.id;
      }),
    );
    const deletedIds = results.flatMap((r) => (r.status === 'fulfilled' ? [r.value] : []));
    const failed = results.length - deletedIds.length;
    setBulkIds((prev) => prev.filter((id) => !deletedIds.includes(id)));
    if (failed === 0) {
      toast.success(t('products.toast.deleted'));
    } else if (deletedIds.length === 0) {
      toast.error(t('auth.common.unexpectedError'));
    } else {
      toast.warning(
        t('products.bulkPartial', {
          deleted: deletedIds.length,
          failed,
          defaultValue: `${deletedIds.length} silindi, ${failed} başarısız.`,
        }),
      );
    }
  };

  const fmtCurrency = (value: number, currency = 'TRY') => {
    try {
      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        maximumFractionDigits: 0,
      }).format(value);
    } catch {
      return `${value.toFixed(0)} ${currency}`;
    }
  };

  const statItems: StatStripItem[] = [
    {
      id: 'total',
      label: t('products.stats.total', { defaultValue: 'Products (page)' }),
      value: products.length,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <Package size={14} />,
      sub: t('products.stats.totalHint', {
        defaultValue: '{{count}} of {{all}}',
        count: products.length,
        all: total,
      }),
      tone: 'emerald',
    },
    {
      id: 'inventoryValue',
      label: t('products.stats.inventoryValue', { defaultValue: 'Inventory value' }),
      value: stats.inventoryValue,
      format: (v) => fmtCurrency(v),
      icon: <CircleDollarSign size={14} />,
      sub: t('products.stats.inventoryHint', { defaultValue: 'At avg cost' }),
      tone: 'indigo',
    },
    {
      id: 'lowStock',
      label: t('products.stats.lowStock', { defaultValue: 'Low stock' }),
      value: stats.lowStockCount,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <AlertTriangle size={14} />,
      sub: t('products.stats.lowStockHint', { defaultValue: 'Tap to filter' }),
      tone: stats.lowStockCount > 0 ? 'amber' : 'slate',
      onClick: () => setLowStockOnly(true),
    },
    {
      id: 'outOfStock',
      label: t('products.stats.outOfStock', { defaultValue: 'Out of stock' }),
      value: stats.outOfStockCount,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <PackageX size={14} />,
      sub: t('products.stats.outOfStockHint', { defaultValue: 'Replenish required' }),
      tone: stats.outOfStockCount > 0 ? 'rose' : 'slate',
      onClick: () => setOutOfStockOnly(true),
    },
  ];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <PageHeader
        icon={<Package size={20} />}
        eyebrow={t('products.eyebrow', { defaultValue: 'Inventory · Catalog' })}
        title={t('products.title')}
        subtitle={t('products.subtitle')}
        crumbs={[
          { label: t('navigation.dashboard', { defaultValue: 'Dashboard' }), to: '/dashboard' },
          { label: t('products.title') },
        ]}
        tone="emerald"
        actions={
          <>
            <button
              type="button"
              onClick={() => exportProductsCsv(filteredProducts)}
              disabled={filteredProducts.length === 0}
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Download size={13} />
              {t('common.exportCsv', { defaultValue: 'Export CSV' })}
            </button>
            <button
              type="button"
              onClick={handleCreate}
              className="inline-flex items-center gap-1.5 rounded-lg bg-gradient-to-r from-emerald-600 to-teal-600 px-3 py-1.5 text-xs font-medium text-white shadow-md shadow-emerald-500/20 transition hover:-translate-y-px hover:shadow-lg hover:shadow-emerald-500/30"
            >
              <Plus size={13} />
              {t('products.addNew')}
            </button>
          </>
        }
      />

      <CollapsibleSection storageKey="products.stats" label="Özet kartları">
        <StatStrip items={statItems} />
      </CollapsibleSection>

      <DataToolbar
        search={{
          value: search,
          onChange: (v) => {
            setPage(1);
            setSearch(v);
          },
          placeholder: t('products.searchPlaceholder'),
        }}
        viewMode={
          <SegmentedControl
            value={statusFilter}
            onChange={(v) => {
              setPage(1);
              setStatusFilter(v);
            }}
            options={[
              {
                value: 'all',
                label: t('products.filter.all', { defaultValue: 'All' }),
                count: products.length,
              },
              {
                value: 'Active',
                label: t('products.statusLabel.Active', { defaultValue: 'Active' }),
                count: stats.statusCounts.Active,
              },
              {
                value: 'New',
                label: t('products.statusLabel.New', { defaultValue: 'New' }),
                count: stats.statusCounts.New,
                icon: <Sparkles size={11} />,
              },
              {
                value: 'Discontinued',
                label: t('products.statusLabel.Discontinued', { defaultValue: 'Discontinued' }),
                count: stats.statusCounts.Discontinued,
              },
              {
                value: 'EndOfLife',
                label: t('products.statusLabel.EndOfLife', { defaultValue: 'EoL' }),
                count: stats.statusCounts.EndOfLife,
              },
            ]}
          />
        }
        filters={
          <>
            <FilterChip
              label={t('products.filter.tracked', { defaultValue: 'Stock-tracked' })}
              icon={<Layers size={10} />}
              active={trackedOnly}
              tone="indigo"
              onClick={() => {
                setPage(1);
                setTrackedOnly((v) => !v);
              }}
            />
            <FilterChip
              label={t('products.filter.lowStock', { defaultValue: 'Low stock' })}
              icon={<AlertTriangle size={10} />}
              active={lowStockOnly}
              count={stats.lowStockCount}
              tone="amber"
              onClick={() => {
                setPage(1);
                setLowStockOnly((v) => !v);
              }}
            />
            <FilterChip
              label={t('products.filter.outOfStock', { defaultValue: 'Out of stock' })}
              icon={<PackageX size={10} />}
              active={outOfStockOnly}
              count={stats.outOfStockCount}
              tone="rose"
              onClick={() => {
                setPage(1);
                setOutOfStockOnly((v) => !v);
              }}
            />
          </>
        }
        resultCount={{
          count: filteredProducts.length,
          label: t('products.resultCountLabel', { defaultValue: 'products' }),
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={clearFilters}
      />

      {bulkSelected.length > 0 && (
        <div className="flex flex-wrap items-center gap-2 rounded-xl border border-indigo-200 bg-indigo-50/70 px-3 py-2 text-sm dark:border-indigo-500/30 dark:bg-indigo-500/10">
          <span className="font-medium text-indigo-700 dark:text-indigo-300">
            {t('products.bulkSelected', {
              count: bulkSelected.length,
              defaultValue: `${bulkSelected.length} seçili`,
            })}
          </span>
          <div className="ml-auto flex items-center gap-2">
            <button
              type="button"
              onClick={handleBulkExport}
              className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              <Download size={12} />
              {t('common.export', { defaultValue: 'Dışa aktar' })}
            </button>
            <button
              type="button"
              onClick={handleBulkDelete}
              className="inline-flex items-center gap-1.5 rounded bg-rose-600 px-2.5 py-1 text-xs font-semibold text-white hover:bg-rose-700"
            >
              <Trash2 size={12} />
              {t('common.delete')}
            </button>
            <button
              type="button"
              onClick={() => setBulkIds([])}
              className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
              aria-label={t('common.cancel')}
            >
              <X size={14} />
            </button>
          </div>
        </div>
      )}

      {productsQuery.isError ? (
        <QueryError onRetry={() => productsQuery.refetch()} isRetrying={productsQuery.isFetching} />
      ) : (
        <ProductList
          products={filteredProducts}
          isLoading={productsQuery.isPending}
          selectedId={selectedId}
          onSelect={(p) => setSelectedId((curr) => (curr === p.id ? null : p.id))}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onCreate={handleCreate}
          selectable
          selectedIds={bulkIds}
          onSelectionChange={setBulkIds}
        />
      )}

      {selectedId &&
        (() => {
          const sel = filteredProducts.find((p) => p.id === selectedId);
          return sel ? (
            <ProductInlineCard
              product={sel}
              onClose={() => setSelectedId(null)}
              onOpenPanel={() => setPanelOpen(true)}
            />
          ) : null;
        })()}

      {!productsQuery.isError && total > 0 && (
        <div className="rounded-xl border border-slate-200/70 bg-white/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
          <Pagination
            page={page}
            pageSize={pageSize}
            total={total}
            onPageChange={setPage}
            pageSizeOptions={[10, 25, 50, 100]}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        </div>
      )}

      {modalOpen && (
        <ProductFormModal
          open={modalOpen}
          product={editing}
          onClose={() => {
            setModalOpen(false);
            setEditing(null);
          }}
        />
      )}

      <ProductDetailPanel
        productId={panelOpen ? selectedId : null}
        onClose={() => setPanelOpen(false)}
        onEdit={(p) => {
          setEditing(p);
          setModalOpen(true);
          setPanelOpen(false);
        }}
      />
    </div>
  );
};
