import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AlertTriangle,
  Building2,
  CircleDollarSign,
  Download,
  Landmark,
  Plus,
  ShieldCheck,
  ShieldOff,
  Trash2,
  Users as UsersIcon,
  User as UserIcon,
  Wallet,
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
import { CustomerDetailPanel } from '@/features/customers/ui/CustomerDetailPanel';
import { CustomerInlineCard } from '@/features/customers/ui/CustomerInlineCard';
import { CustomerFormModal } from '@/features/customers/ui/CustomerFormModal';
import { CustomerList } from '@/features/customers/ui/CustomerList';
import {
  useCustomersQuery,
  useDeleteCustomer,
} from '@/features/customers/hooks/useCustomerQueries';
import type {
  Customer,
  CustomerStatus,
  CustomerType,
} from '@/features/customers/model/customer.types';

const PAGE_SIZE = 10;

type StatusFilter = 'all' | CustomerStatus;
type TypeFilter = 'all' | CustomerType;

const exportCustomersCsv = (rows: Customer[]) =>
  downloadCsv({
    filename: 'customers',
    rows,
    columns: [
      { header: 'Code', value: (c) => c.code },
      { header: 'Name', value: (c) => c.name },
      { header: 'Type', value: (c) => c.type },
      { header: 'Email', value: (c) => c.email },
      { header: 'Phone', value: (c) => c.phone },
      { header: 'TaxNumber', value: (c) => c.taxNumber },
      { header: 'Currency', value: (c) => c.defaultCurrency },
      { header: 'CreditLimit', value: (c) => c.creditLimit },
      { header: 'CurrentBalance', value: (c) => c.currentBalance },
      { header: 'OverdueAmount', value: (c) => c.overdueAmount },
      { header: 'Status', value: (c) => c.status },
    ],
  });

export const CustomersPage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const navigate = useNavigate();
  const confirm = useConfirm();
  const deleteMutation = useDeleteCustomer();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [creditAtRiskOnly, setCreditAtRiskOnly] = useState(false);

  const [editing, setEditing] = useState<Customer | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [bulkIds, setBulkIds] = useState<string[]>([]);
  const [panelOpen, setPanelOpen] = useState(false);

  const queryParams = useMemo(
    () => ({
      page,
      pageSize,
      search: debouncedSearch.trim() || undefined,
      isActive: statusFilter === 'all' ? undefined : statusFilter === 'Active' ? true : false,
    }),
    [page, pageSize, debouncedSearch, statusFilter],
  );

  const customersQuery = useCustomersQuery(queryParams);
  const result = customersQuery.data?.data;
  const rawCustomers = useMemo(() => result?.items ?? [], [result?.items]);
  const total = result?.total ?? 0;

  const filteredCustomers = useMemo(() => {
    return rawCustomers.filter((c) => {
      if (statusFilter !== 'all' && c.status !== statusFilter) return false;
      if (typeFilter !== 'all' && c.type !== typeFilter) return false;
      if (overdueOnly && c.overdueAmount <= 0) return false;
      if (creditAtRiskOnly) {
        if (c.creditLimit <= 0) return false;
        if (c.currentBalance / c.creditLimit < 0.8) return false;
      }
      return true;
    });
  }, [rawCustomers, statusFilter, typeFilter, overdueOnly, creditAtRiskOnly]);

  const stats = useMemo(() => {
    const list = rawCustomers;
    const activeCount = list.filter((c) => c.status === 'Active').length;
    const blockedCount = list.filter((c) => c.status === 'Blocked').length;
    const overdueCount = list.filter((c) => c.overdueAmount > 0).length;
    const outstandingTotal = list.reduce((s, c) => s + Math.max(0, c.currentBalance), 0);
    const overdueTotal = list.reduce((s, c) => s + c.overdueAmount, 0);
    const atRiskCount = list.filter(
      (c) => c.creditLimit > 0 && c.currentBalance / c.creditLimit >= 0.8,
    ).length;
    return {
      total,
      activeCount,
      blockedCount,
      overdueCount,
      outstandingTotal,
      overdueTotal,
      atRiskCount,
    };
  }, [rawCustomers, total]);

  const typeBreakdown = useMemo(() => {
    const counts: Record<CustomerType, number> = {
      Individual: 0,
      Business: 0,
      Government: 0,
    };
    rawCustomers.forEach((c) => {
      counts[c.type] += 1;
    });
    return counts;
  }, [rawCustomers]);

  const statusBreakdown = useMemo(() => {
    const counts: Record<CustomerStatus, number> = {
      Active: 0,
      Blocked: 0,
      Archived: 0,
    };
    rawCustomers.forEach((c) => {
      counts[c.status] += 1;
    });
    return counts;
  }, [rawCustomers]);

  const hasActiveFilters =
    statusFilter !== 'all' ||
    typeFilter !== 'all' ||
    overdueOnly ||
    creditAtRiskOnly ||
    debouncedSearch.trim() !== '';

  const clearFilters = () => {
    setStatusFilter('all');
    setTypeFilter('all');
    setOverdueOnly(false);
    setCreditAtRiskOnly(false);
    setSearch('');
    setPage(1);
  };

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

  const bulkSelected = filteredCustomers.filter((c) => bulkIds.includes(c.id));

  const handleBulkExport = () => {
    exportCustomersCsv(bulkSelected.length > 0 ? bulkSelected : filteredCustomers);
  };

  const handleBulkDelete = async () => {
    if (bulkSelected.length === 0) return;
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('customers.bulkConfirmDelete', {
        count: bulkSelected.length,
        defaultValue: `${bulkSelected.length} müşteri silinsin mi?`,
      }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    const results = await Promise.allSettled(
      bulkSelected.map(async (c) => {
        const res = await deleteMutation.mutateAsync(c.id);
        if (!res.isSuccess) throw new Error(res.errors[0] ?? 'failed');
        return c.id;
      }),
    );
    const deletedIds = results.flatMap((r) => (r.status === 'fulfilled' ? [r.value] : []));
    const failed = results.length - deletedIds.length;
    setBulkIds((prev) => prev.filter((id) => !deletedIds.includes(id)));
    if (failed === 0) {
      toast.success(t('customers.toast.deleted'));
    } else if (deletedIds.length === 0) {
      toast.error(t('auth.common.unexpectedError'));
    } else {
      toast.warning(
        t('customers.bulkPartial', {
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
      label: t('customers.stats.total', { defaultValue: 'Total customers' }),
      value: stats.total,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <UsersIcon size={14} />,
      sub: `${stats.activeCount} ${t('common.active').toLowerCase()}`,
      tone: 'indigo',
    },
    {
      id: 'outstanding',
      label: t('customers.stats.outstanding', { defaultValue: 'Outstanding (page)' }),
      value: stats.outstandingTotal,
      format: (v) => fmtCurrency(v),
      icon: <Wallet size={14} />,
      sub: t('customers.stats.outstandingHint', {
        defaultValue: 'Across {{count}} customers',
        count: stats.total,
      }),
      tone: 'amber',
    },
    {
      id: 'overdue',
      label: t('customers.stats.overdue', { defaultValue: 'Overdue receivables' }),
      value: stats.overdueTotal,
      format: (v) => fmtCurrency(v),
      icon: <AlertTriangle size={14} />,
      sub: t('customers.stats.overdueHint', {
        defaultValue: '{{count}} customers',
        count: stats.overdueCount,
      }),
      tone: stats.overdueTotal > 0 ? 'rose' : 'slate',
      onClick: () => setOverdueOnly(true),
    },
    {
      id: 'at-risk',
      label: t('customers.stats.atRisk', { defaultValue: 'Credit at risk (≥80%)' }),
      value: stats.atRiskCount,
      format: (v) => Math.round(v).toLocaleString(locale),
      icon: <ShieldCheck size={14} />,
      sub: t('customers.stats.atRiskHint', { defaultValue: 'Tap to filter' }),
      tone: stats.atRiskCount > 0 ? 'violet' : 'slate',
      onClick: () => setCreditAtRiskOnly(true),
    },
  ];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <PageHeader
        icon={<UsersIcon size={20} />}
        eyebrow={t('customers.eyebrow', { defaultValue: 'Sales · CRM' })}
        title={t('customers.title')}
        subtitle={t('customers.subtitle')}
        crumbs={[
          { label: t('navigation.dashboard', { defaultValue: 'Dashboard' }), to: '/dashboard' },
          { label: t('customers.title') },
        ]}
        tone="indigo"
        actions={
          <>
            <button
              type="button"
              onClick={() => exportCustomersCsv(filteredCustomers)}
              disabled={filteredCustomers.length === 0}
              className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Download size={13} />
              {t('common.exportCsv', { defaultValue: 'Export CSV' })}
            </button>
            <button
              type="button"
              onClick={handleCreate}
              className="inline-flex items-center gap-1.5 rounded-lg bg-gradient-to-r from-primary-600 to-purple-600 px-3 py-1.5 text-xs font-medium text-white shadow-md shadow-primary-500/20 transition hover:shadow-lg hover:shadow-primary-500/30 hover:-translate-y-px"
            >
              <Plus size={13} />
              {t('customers.addNew')}
            </button>
          </>
        }
      />

      <CollapsibleSection
        storageKey="customers.stats"
        label={t('Common.SummaryCards', { defaultValue: 'Özet kartları' })}
      >
        <StatStrip items={statItems} />
      </CollapsibleSection>

      <DataToolbar
        search={{
          value: search,
          onChange: (v) => {
            setPage(1);
            setSearch(v);
          },
          placeholder: t('customers.searchPlaceholder'),
        }}
        viewMode={
          <SegmentedControl
            value={statusFilter}
            onChange={(v) => {
              setPage(1);
              setStatusFilter(v);
            }}
            options={[
              { value: 'all', label: t('customers.filter.all', { defaultValue: 'All' }) },
              {
                value: 'Active',
                label: t('customers.statusLabel.Active', { defaultValue: 'Active' }),
                count: statusBreakdown.Active,
              },
              {
                value: 'Blocked',
                label: t('customers.statusLabel.Blocked', { defaultValue: 'Blocked' }),
                count: statusBreakdown.Blocked,
                icon: <ShieldOff size={11} />,
              },
              {
                value: 'Archived',
                label: t('customers.statusLabel.Archived', { defaultValue: 'Archived' }),
                count: statusBreakdown.Archived,
              },
            ]}
            ariaLabel={t('customers.filter.statusAria', { defaultValue: 'Filter by status' })}
          />
        }
        filters={
          <>
            <FilterChip
              label={t('customers.type.Individual', { defaultValue: 'Individual' })}
              icon={<UserIcon size={10} />}
              active={typeFilter === 'Individual'}
              count={typeBreakdown.Individual}
              tone="sky"
              onClick={() => {
                setPage(1);
                setTypeFilter((curr) => (curr === 'Individual' ? 'all' : 'Individual'));
              }}
            />
            <FilterChip
              label={t('customers.type.Business', { defaultValue: 'Business' })}
              icon={<Building2 size={10} />}
              active={typeFilter === 'Business'}
              count={typeBreakdown.Business}
              tone="indigo"
              onClick={() => {
                setPage(1);
                setTypeFilter((curr) => (curr === 'Business' ? 'all' : 'Business'));
              }}
            />
            <FilterChip
              label={t('customers.type.Government', { defaultValue: 'Government' })}
              icon={<Landmark size={10} />}
              active={typeFilter === 'Government'}
              count={typeBreakdown.Government}
              tone="amber"
              onClick={() => {
                setPage(1);
                setTypeFilter((curr) => (curr === 'Government' ? 'all' : 'Government'));
              }}
            />
            <span className="mx-1 h-4 w-px bg-slate-200 dark:bg-slate-800" />
            <FilterChip
              label={t('customers.filter.overdue', { defaultValue: 'Has overdue' })}
              icon={<AlertTriangle size={10} />}
              active={overdueOnly}
              count={stats.overdueCount}
              tone="rose"
              onClick={() => {
                setPage(1);
                setOverdueOnly((v) => !v);
              }}
            />
            <FilterChip
              label={t('customers.filter.atRisk', { defaultValue: 'Credit ≥ 80%' })}
              icon={<CircleDollarSign size={10} />}
              active={creditAtRiskOnly}
              count={stats.atRiskCount}
              tone="violet"
              onClick={() => {
                setPage(1);
                setCreditAtRiskOnly((v) => !v);
              }}
            />
          </>
        }
        resultCount={{
          count: filteredCustomers.length,
          label: t('customers.resultCountLabel', { defaultValue: 'results' }),
        }}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={clearFilters}
      />

      {bulkSelected.length > 0 && (
        <div className="flex flex-wrap items-center gap-2 rounded-xl border border-primary-200 bg-primary-50/70 px-3 py-2 text-sm dark:border-primary-500/30 dark:bg-primary-500/10">
          <span className="font-medium text-primary-700 dark:text-primary-300">
            {t('customers.bulkSelected', {
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
              className="inline-flex items-center gap-1.5 rounded bg-danger-600 px-2.5 py-1 text-xs font-semibold text-white hover:bg-danger-700"
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

      {customersQuery.isError ? (
        <QueryError
          onRetry={() => customersQuery.refetch()}
          isRetrying={customersQuery.isFetching}
        />
      ) : (
        <CustomerList
          customers={filteredCustomers}
          isLoading={customersQuery.isPending}
          selectedId={selectedId}
          onSelect={(c) => setSelectedId((curr) => (curr === c.id ? null : c.id))}
          onOpenDetails={(c) => {
            setSelectedId(c.id);
            setPanelOpen(true);
          }}
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
          const sel = filteredCustomers.find((c) => c.id === selectedId);
          return sel ? (
            <CustomerInlineCard
              customer={sel}
              onClose={() => setSelectedId(null)}
              onOpenPanel={() => setPanelOpen(true)}
            />
          ) : null;
        })()}

      {!customersQuery.isError && total > 0 && (
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
            itemLabel={t('customers.resultCountLabel', { defaultValue: 'kayıt' })}
          />
        </div>
      )}

      {modalOpen && (
        <CustomerFormModal
          open={modalOpen}
          customer={editing}
          onClose={() => {
            setModalOpen(false);
            setEditing(null);
          }}
        />
      )}

      <CustomerDetailPanel
        customerId={panelOpen ? selectedId : null}
        onClose={() => setPanelOpen(false)}
        onEdit={(c) => {
          setEditing(c);
          setModalOpen(true);
          setPanelOpen(false);
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
