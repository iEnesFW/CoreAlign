import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Plus, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { ProductDetailPanel } from '@/features/products/ui/ProductDetailPanel';
import { ProductFormModal } from '@/features/products/ui/ProductFormModal';
import { ProductList } from '@/features/products/ui/ProductList';
import { useDeleteProduct, useProductsQuery } from '@/features/products/hooks/useProductQueries';
import type { Product } from '@/features/products/model/product.types';

const PAGE_SIZE = 20;

export const ProductsPage = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Product | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const params = useMemo(
    () => ({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined }),
    [page, search],
  );

  const productsQuery = useProductsQuery(params);
  const deleteMutation = useDeleteProduct();
  const confirm = useConfirm();

  const result = productsQuery.data?.data;
  const products = result?.items ?? [];
  const total = result?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

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

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('products.title')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('products.subtitle')}</p>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Search size={14} className="absolute left-2 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="search"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder={t('products.searchPlaceholder')}
              className="w-56 rounded border border-slate-200 bg-white py-1.5 pl-7 pr-3 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </div>
          <Button onClick={handleCreate}>
            <Plus size={14} className="mr-1" />
            {t('products.addNew')}
          </Button>
        </div>
      </div>

      <ProductList
        products={products}
        isLoading={productsQuery.isPending}
        selectedId={selectedId}
        onSelect={(p) => setSelectedId(p.id)}
        onEdit={handleEdit}
        onDelete={handleDelete}
      />

      {total > PAGE_SIZE && (
        <div className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-400">
          <div>
            {t('products.pagination.summary', {
              from: (page - 1) * PAGE_SIZE + 1,
              to: Math.min(page * PAGE_SIZE, total),
              total,
              defaultValue: `${(page - 1) * PAGE_SIZE + 1}-${Math.min(page * PAGE_SIZE, total)} / ${total}`,
            })}
          </div>
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('products.pagination.previous')}
            >
              <ChevronLeft size={14} />
            </button>
            <span className="px-2">
              {page} / {totalPages}
            </span>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="rounded border border-slate-200 p-1.5 text-slate-600 hover:bg-slate-100 disabled:opacity-40 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
              aria-label={t('products.pagination.next')}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}

      <ProductFormModal
        open={modalOpen}
        product={editing}
        onClose={() => {
          setModalOpen(false);
          setEditing(null);
        }}
      />

      <ProductDetailPanel
        productId={selectedId}
        onClose={() => setSelectedId(null)}
        onEdit={(p) => {
          setEditing(p);
          setModalOpen(true);
          setSelectedId(null);
        }}
      />
    </div>
  );
};
