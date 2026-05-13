import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Plus, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { OrderDetailPanel } from '@/features/orders/ui/OrderDetailPanel';
import { OrderFormModal } from '@/features/orders/ui/OrderFormModal';
import { OrderList } from '@/features/orders/ui/OrderList';
import {
  useDeleteOrder,
  useOrderQuery,
  useOrdersQuery,
} from '@/features/orders/hooks/useOrderQueries';
import { useGenerateInvoiceFromOrder } from '@/features/invoices/hooks/useInvoiceQueries';
import type { OrderSummary } from '@/features/orders/model/order.types';

const PAGE_SIZE = 20;

export const OrdersPage = () => {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();
  const focusFromUrl = searchParams.get('focus');
  const [selectedId, setSelectedId] = useState<string | null>(focusFromUrl);

  useEffect(() => {
    if (focusFromUrl) {
      const next = new URLSearchParams(searchParams);
      next.delete('focus');
      setSearchParams(next, { replace: true });
    }
  }, [focusFromUrl, searchParams, setSearchParams]);

  const params = useMemo(
    () => ({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined }),
    [page, search],
  );

  const ordersQuery = useOrdersQuery(params);
  const editingQuery = useOrderQuery(editingId);
  const deleteMutation = useDeleteOrder();
  const generateInvoiceMutation = useGenerateInvoiceFromOrder();
  const confirm = useConfirm();

  const result = ordersQuery.data?.data;
  const orders = result?.items ?? [];
  const total = result?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const handleCreate = () => {
    setEditingId(null);
    setModalOpen(true);
  };

  const handleEdit = (order: OrderSummary) => {
    setEditingId(order.id);
    setModalOpen(true);
  };

  const handleDelete = async (order: OrderSummary) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('orders.confirmDelete', { number: order.orderNumber }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;

    deleteMutation.mutate(order.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('orders.toast.deleted'));
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  };

  const handleGenerateInvoice = async (order: OrderSummary) => {
    const confirmed = await confirm({
      title: t('orders.actions.generateInvoice'),
      message: t('orders.confirmGenerateInvoice', { number: order.orderNumber }),
      confirmLabel: t('common.confirm'),
    });
    if (!confirmed) return;

    generateInvoiceMutation.mutate(
      { orderId: order.id },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('orders.toast.invoiceGenerated'));
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  };

  const editingOrder = editingId ? (editingQuery.data?.data ?? null) : null;

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('orders.title')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('orders.subtitle')}</p>
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
              placeholder={t('orders.searchPlaceholder')}
              className="w-56 rounded border border-slate-200 bg-white py-1.5 pl-7 pr-3 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </div>
          <Button onClick={handleCreate}>
            <Plus size={14} className="mr-1" />
            {t('orders.addNew')}
          </Button>
        </div>
      </div>

      <OrderList
        orders={orders}
        isLoading={ordersQuery.isPending}
        selectedId={selectedId}
        onSelect={(o) => setSelectedId(o.id)}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onGenerateInvoice={handleGenerateInvoice}
      />

      {total > PAGE_SIZE && (
        <div className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-400">
          <div>
            {t('orders.pagination.summary', {
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
              aria-label={t('orders.pagination.previous')}
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
              aria-label={t('orders.pagination.next')}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}

      <OrderFormModal
        open={modalOpen}
        order={editingOrder}
        onClose={() => {
          setModalOpen(false);
          setEditingId(null);
        }}
      />

      <OrderDetailPanel
        orderId={selectedId}
        onClose={() => setSelectedId(null)}
        onEdit={(id) => {
          setEditingId(id);
          setModalOpen(true);
          setSelectedId(null);
        }}
        onGenerateInvoice={(id) => {
          const found = orders.find((o) => o.id === id);
          if (found) handleGenerateInvoice(found);
        }}
      />
    </div>
  );
};
