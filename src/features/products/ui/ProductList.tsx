import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Edit2, Package, PanelRightOpen, Pencil, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { DataTable, RowActionButton, type SortState } from '@/shared/ui/DataTable/DataTable';
import { InlineTextEditor } from '@/shared/ui/DataTable/InlineTextEditor';
import type { ColumnState } from '@/shared/ui/DataTable/columnState';
import { cn } from '@/shared/lib/cn';
import { useUpdateProduct } from '../hooks/useProductQueries';
import { buildProductUpdateInput } from '../model/productUpdateMerge';
import type { Product, ProductStatus } from '../model/product.types';

interface Props {
  products: Product[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (product: Product) => void;
  onOpenDetails?: (product: Product) => void;
  onEdit: (product: Product) => void;
  onDelete: (product: Product) => void;
  onCreate?: () => void;
  selectable?: boolean;
  selectedIds?: string[];
  onSelectionChange?: (ids: string[]) => void;
  columnState?: ColumnState;
  externalSort?: SortState;
  onSortChange?: (sort: SortState | null) => void;
}

const statusTone: Record<ProductStatus, string> = {
  Active: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  New: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Discontinued: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  EndOfLife: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

export const ProductList = ({
  products,
  isLoading,
  selectedId,
  onSelect,
  onOpenDetails,
  onEdit,
  onDelete,
  onCreate,
  selectable,
  selectedIds,
  onSelectionChange,
  columnState,
  externalSort,
  onSortChange,
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const [editingId, setEditingId] = useState<string | null>(null);
  const updateMutation = useUpdateProduct();

  const commitName = (product: Product, rawValue: string) => {
    setEditingId(null);
    const trimmed = rawValue.trim();
    if (!trimmed || trimmed === product.name) return;
    updateMutation.mutate(buildProductUpdateInput(product, { name: trimmed }), {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('products.toast.updated'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  return (
    <DataTable
      rows={products}
      getRowId={(p) => p.id}
      isLoading={isLoading}
      selectedId={selectedId ?? null}
      onRowClick={onSelect}
      selectable={selectable}
      selectedIds={selectedIds}
      onSelectionChange={onSelectionChange}
      columnState={columnState}
      externalSort={externalSort}
      onSortChange={onSortChange}
      editingCell={editingId ? { rowId: editingId, key: 'product' } : null}
      emptyIcon={<Package size={20} />}
      emptyTitle={t('products.empty')}
      emptyDescription={t('products.emptyHint', {
        defaultValue:
          'Create your first product to enable orders, invoicing and inventory tracking.',
      })}
      emptyAction={
        onCreate && (
          <button
            type="button"
            onClick={onCreate}
            className="rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:bg-primary-700"
          >
            {t('products.addNew')}
          </button>
        )
      }
      columns={[
        {
          key: 'product',
          label: t('products.columns.name'),
          sortable: true,
          sortValue: (p) => p.name.toLowerCase(),
          cell: (p) => (
            <div className="flex items-center gap-2.5">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-success-500/15 to-teal-500/15 text-success-700 ring-1 ring-success-200/40 dark:text-success-300 dark:ring-success-500/30">
                <Package size={14} />
              </div>
              <div className="min-w-0">
                <div className="truncate font-semibold text-slate-900 dark:text-slate-100">
                  {p.name}
                </div>
                <div className="flex items-center gap-1.5 text-[10px]">
                  <span className="font-mono text-slate-500">{p.sku}</span>
                  {p.barcode && <span className="font-mono text-slate-400">· {p.barcode}</span>}
                </div>
              </div>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setEditingId(p.id);
                }}
                aria-label={t('products.inlineEdit.editName', { defaultValue: 'Adı düzenle' })}
                title={t('products.inlineEdit.editName', { defaultValue: 'Adı düzenle' })}
                className="ml-auto shrink-0 rounded p-1 text-slate-400 opacity-0 transition group-hover:opacity-100 hover:bg-primary-50 hover:text-primary-600 focus-visible:opacity-100 dark:hover:bg-primary-500/10 dark:hover:text-primary-300"
              >
                <Pencil size={12} />
              </button>
            </div>
          ),
          editable: {
            editor: (p) => (
              <InlineTextEditor
                initial={p.name}
                ariaLabel={t('products.inlineEdit.nameInput', { defaultValue: 'Ürün adı' })}
                disabled={updateMutation.isPending}
                onCommit={(value) => commitName(p, value)}
                onCancel={() => setEditingId(null)}
              />
            ),
          },
        },
        {
          key: 'price',
          label: t('products.columns.price'),
          align: 'right',
          sortable: true,
          sortValue: (p) => p.price,
          hideOnMobile: true,
          cell: (p) => (
            <div className="text-right">
              <div className="font-mono text-xs font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                {fmtCurrency(p.price, p.currency, locale)}
              </div>
              {p.listPrice > p.price && (
                <div className="text-[9px] text-slate-400 line-through">
                  {fmtCurrency(p.listPrice, p.currency, locale)}
                </div>
              )}
            </div>
          ),
        },
        {
          key: 'stock',
          label: t('products.columns.stock'),
          align: 'right',
          sortable: true,
          sortValue: (p) => p.stockQuantity,
          cell: (p) => {
            const belowReorder = p.isStockTracked && p.stockQuantity <= p.reorderPoint;
            const outOfStock = p.isStockTracked && p.stockQuantity <= 0;
            return (
              <div className="text-right">
                <div
                  className={cn(
                    'font-mono text-xs font-semibold tabular-nums',
                    outOfStock
                      ? 'text-danger-600 dark:text-danger-400'
                      : belowReorder
                        ? 'text-warning-600 dark:text-warning-400'
                        : 'text-slate-900 dark:text-slate-100',
                  )}
                >
                  {p.stockQuantity.toLocaleString(locale)} {p.unit}
                </div>
                {p.isStockTracked && (
                  <div className="text-[9px] text-slate-500 dark:text-slate-400">
                    {t('products.reorderAt', { defaultValue: 'Reorder' })}: {p.reorderPoint}
                  </div>
                )}
                {belowReorder && (
                  <div className="mt-0.5 inline-flex items-center gap-0.5 rounded bg-warning-100 px-1 py-px text-[9px] font-semibold uppercase text-warning-700 dark:bg-warning-500/20 dark:text-warning-300">
                    <AlertTriangle size={8} />
                    {outOfStock
                      ? t('products.outOfStock', { defaultValue: 'Out' })
                      : t('products.lowStock', { defaultValue: 'Low' })}
                  </div>
                )}
              </div>
            );
          },
        },
        {
          key: 'status',
          label: t('products.columns.status'),
          sortable: true,
          sortValue: (p) => p.status,
          cell: (p) => (
            <span
              className={cn(
                'inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider',
                statusTone[p.status],
              )}
            >
              {t(`products.statusLabel.${p.status}`, { defaultValue: p.status })}
            </span>
          ),
        },
      ]}
      rowActionsHeader={
        <span className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('products.columns.actions')}
        </span>
      }
      rowActions={(p) => (
        <>
          {onOpenDetails && (
            <RowActionButton
              icon={<PanelRightOpen size={14} />}
              label={t('common.details', { defaultValue: 'Details' })}
              onClick={() => onOpenDetails(p)}
            />
          )}
          <RowActionButton
            icon={<Edit2 size={14} />}
            label={t('common.edit')}
            onClick={() => onEdit(p)}
          />
          <RowActionButton
            icon={<Trash2 size={14} />}
            label={t('common.delete')}
            tone="danger"
            onClick={() => onDelete(p)}
          />
        </>
      )}
    />
  );
};
