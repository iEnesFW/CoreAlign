import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Building2,
  Edit2,
  Eye,
  Landmark,
  Mail,
  PanelRightOpen,
  Phone,
  ShieldOff,
  Trash2,
  User as UserIcon,
  Users as UsersIcon,
} from 'lucide-react';
import { DataTable, RowActionButton } from '@/shared/ui/DataTable/DataTable';
import { cn } from '@/shared/lib/cn';
import type { Customer, CustomerStatus, CustomerType } from '../model/customer.types';

interface Props {
  customers: Customer[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (customer: Customer) => void;
  onOpenDetails?: (customer: Customer) => void;
  onEdit: (customer: Customer) => void;
  onDelete: (customer: Customer) => void;
  onCreate?: () => void;
  selectable?: boolean;
  selectedIds?: string[];
  onSelectionChange?: (ids: string[]) => void;
}

const typeIcon: Record<CustomerType, React.ReactNode> = {
  Individual: <UserIcon size={11} />,
  Business: <Building2 size={11} />,
  Government: <Landmark size={11} />,
};

const typeTone: Record<CustomerType, string> = {
  Individual: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Business: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Government: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
};

const statusTone: Record<CustomerStatus, string> = {
  Active: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Blocked: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Archived: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
};

const initials = (name: string) =>
  name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((n) => n[0]?.toUpperCase())
    .join('') || '·';

const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

export const CustomerList = ({
  customers,
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
}: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  return (
    <DataTable
      rows={customers}
      isLoading={isLoading}
      getRowId={(c) => c.id}
      selectedId={selectedId ?? null}
      onRowClick={onSelect}
      selectable={selectable}
      selectedIds={selectedIds}
      onSelectionChange={onSelectionChange}
      emptyIcon={<UsersIcon size={20} />}
      emptyTitle={t('customers.empty')}
      emptyDescription={t('customers.emptyHint', {
        defaultValue: 'Add your first customer to start tracking orders, invoices and payments.',
      })}
      emptyAction={
        onCreate && (
          <button
            type="button"
            onClick={onCreate}
            className="rounded-lg bg-primary-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:bg-primary-700"
          >
            {t('customers.addNew')}
          </button>
        )
      }
      columns={[
        {
          key: 'name',
          label: t('customers.columns.name'),
          sortable: true,
          sortValue: (c) => c.name.toLowerCase(),
          cell: (c) => (
            <div className="flex items-center gap-2.5">
              <div
                className={cn(
                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-primary-500/15 to-purple-500/15 text-[10px] font-bold uppercase text-primary-700 ring-1 ring-primary-200/50 dark:text-primary-300 dark:ring-primary-500/30',
                  c.status === 'Blocked' &&
                    'from-danger-500/15 to-pink-500/15 text-danger-700 ring-danger-200/50 dark:text-danger-300 dark:ring-danger-500/30',
                )}
              >
                {initials(c.name)}
              </div>
              <div className="min-w-0">
                <div className="flex items-center gap-1.5">
                  <span className="truncate font-semibold text-slate-900 dark:text-slate-100">
                    {c.name}
                  </span>
                  {c.status === 'Blocked' && (
                    <span
                      title={c.blockReason ?? 'Blocked'}
                      className="inline-flex h-4 w-4 items-center justify-center rounded-full bg-danger-100 text-danger-600 dark:bg-danger-500/20 dark:text-danger-300"
                    >
                      <ShieldOff size={9} />
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-1 text-[10px] text-slate-500 dark:text-slate-400">
                  <span
                    className={cn(
                      'inline-flex items-center gap-0.5 rounded-full px-1.5 py-px font-semibold uppercase tracking-wider',
                      typeTone[c.type],
                    )}
                  >
                    {typeIcon[c.type]}
                    {t(`customers.type.${c.type}`, { defaultValue: c.type })}
                  </span>
                  {c.code && <span className="font-mono text-slate-400">· {c.code}</span>}
                </div>
              </div>
            </div>
          ),
        },
        {
          key: 'contact',
          label: t('customers.columns.email'),
          hideOnMobile: true,
          cell: (c) => (
            <div className="space-y-0.5 text-[11px]">
              <div className="flex items-center gap-1 text-slate-700 dark:text-slate-300">
                <Mail size={10} className="text-slate-400" />
                <span className="truncate">{c.email ?? '—'}</span>
              </div>
              <div className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
                <Phone size={10} className="text-slate-400" />
                <span className="truncate">{c.phone ?? '—'}</span>
              </div>
            </div>
          ),
        },
        {
          key: 'balance',
          label: t('customers.columns.balance', { defaultValue: 'Balance' }),
          align: 'right',
          hideOnMobile: true,
          sortable: true,
          sortValue: (c) => c.currentBalance,
          cell: (c) => {
            const balance = c.currentBalance;
            const overdue = c.overdueAmount;
            return (
              <div className="text-right">
                <div
                  className={cn(
                    'font-mono text-xs font-semibold tabular-nums',
                    balance > 0
                      ? 'text-warning-600 dark:text-warning-400'
                      : balance < 0
                        ? 'text-success-600 dark:text-success-400'
                        : 'text-slate-700 dark:text-slate-200',
                  )}
                >
                  {fmtCurrency(balance, c.defaultCurrency, locale)}
                </div>
                {overdue > 0 && (
                  <div className="text-[9px] font-medium text-danger-600 dark:text-danger-400">
                    {fmtCurrency(overdue, c.defaultCurrency, locale)} overdue
                  </div>
                )}
              </div>
            );
          },
        },
        {
          key: 'creditUsage',
          label: t('customers.columns.creditUsage', { defaultValue: 'Credit' }),
          align: 'right',
          hideOnMobile: true,
          sortable: true,
          sortValue: (c) => (c.creditLimit > 0 ? c.currentBalance / c.creditLimit : -1),
          cell: (c) => {
            if (c.creditLimit <= 0) {
              return <span className="text-[10px] text-slate-400">—</span>;
            }
            const pct = Math.min(120, (c.currentBalance / c.creditLimit) * 100);
            const tone =
              pct >= 100
                ? 'bg-danger-500'
                : pct >= 80
                  ? 'bg-warning-500'
                  : pct >= 50
                    ? 'bg-warning-500'
                    : 'bg-success-500';
            return (
              <div className="flex flex-col items-end gap-1">
                <div className="text-[10px] font-semibold tabular-nums text-slate-700 dark:text-slate-200">
                  {pct.toFixed(0)}%
                </div>
                <div className="h-1 w-16 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                  <div
                    className={cn('h-full rounded-full transition-all', tone)}
                    style={{ width: `${Math.min(100, pct)}%` }}
                  />
                </div>
              </div>
            );
          },
        },
        {
          key: 'status',
          label: t('customers.columns.status'),
          sortable: true,
          sortValue: (c) => c.status,
          cell: (c) => (
            <span
              className={cn(
                'inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider',
                statusTone[c.status],
              )}
            >
              {t(`customers.statusLabel.${c.status}`, { defaultValue: c.status })}
            </span>
          ),
        },
      ]}
      rowActionsHeader={
        <span className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          {t('customers.columns.actions')}
        </span>
      }
      rowActions={(c) => (
        <>
          {onOpenDetails && (
            <RowActionButton
              icon={<PanelRightOpen size={14} />}
              label={t('common.details', { defaultValue: 'Details' })}
              onClick={() => onOpenDetails(c)}
            />
          )}
          <Link
            to={`/dashboard/customers/${c.id}`}
            title={t('common.view')}
            aria-label={t('common.view')}
            className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-slate-100 hover:text-primary-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-primary-300"
          >
            <Eye size={14} />
          </Link>
          <RowActionButton
            icon={<Edit2 size={14} />}
            label={t('common.edit')}
            onClick={() => onEdit(c)}
          />
          <RowActionButton
            icon={<Trash2 size={14} />}
            label={t('common.delete')}
            tone="danger"
            onClick={() => onDelete(c)}
          />
        </>
      )}
    />
  );
};
