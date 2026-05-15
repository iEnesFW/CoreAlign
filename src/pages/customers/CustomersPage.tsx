import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Plus, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { CustomerDetailPanel } from '@/features/customers/ui/CustomerDetailPanel';
import { CustomerFormModal } from '@/features/customers/ui/CustomerFormModal';
import { CustomerList } from '@/features/customers/ui/CustomerList';
import {
  useCustomersQuery,
  useDeleteCustomer,
} from '@/features/customers/hooks/useCustomerQueries';
import type { Customer } from '@/features/customers/model/customer.types';

const PAGE_SIZE = 20;

export const CustomersPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Customer | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const params = useMemo(
    () => ({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined }),
    [page, search],
  );

  const customersQuery = useCustomersQuery(params);
  const deleteMutation = useDeleteCustomer();
  const confirm = useConfirm();

  const result = customersQuery.data?.data;
  const customers = result?.items ?? [];
  const total = result?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const handleCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const handleEdit = (customer: Customer) => {
    setEditing(customer);
    setModalOpen(true);
  };

  const handleDelete = async (customer: Customer) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('customers.confirmDelete', { name: customer.name }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;

    deleteMutation.mutate(customer.id, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('customers.toast.deleted'));
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
            {t('customers.title')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('customers.subtitle')}</p>
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
              placeholder={t('customers.searchPlaceholder')}
              className="w-56 rounded border border-slate-200 bg-white py-1.5 pl-7 pr-3 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </div>
          <Button onClick={handleCreate}>
            <Plus size={14} className="mr-1" />
            {t('customers.addNew')}
          </Button>
        </div>
      </div>

      <CustomerList
        customers={customers}
        isLoading={customersQuery.isPending}
        selectedId={selectedId}
        onSelect={(c) => setSelectedId(c.id)}
        onEdit={handleEdit}
        onDelete={handleDelete}
      />

      {total > PAGE_SIZE && (
        <div className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-400">
          <div>
            {t('customers.pagination.summary', {
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
              aria-label={t('customers.pagination.previous')}
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
              aria-label={t('customers.pagination.next')}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}

      <CustomerFormModal
        open={modalOpen}
        customer={editing}
        onClose={() => {
          setModalOpen(false);
          setEditing(null);
        }}
      />

      <CustomerDetailPanel
        customerId={selectedId}
        onClose={() => setSelectedId(null)}
        onEdit={(c) => {
          setEditing(c);
          setModalOpen(true);
          setSelectedId(null);
        }}
        onCreateOrder={(id) => navigate(`/dashboard/orders?new=1&customerId=${id}`)}
        onCreateInvoice={(id) => navigate(`/dashboard/invoices?new=1&customerId=${id}`)}
        onRecordPayment={(id) => navigate(`/dashboard/invoices?customerId=${id}&payment=1`)}
        onOpenOrder={(orderId) => navigate(`/dashboard/orders?selected=${orderId}`)}
        onOpenInvoice={(invoiceId) => navigate(`/dashboard/invoices?selected=${invoiceId}`)}
      />
    </div>
  );
};
