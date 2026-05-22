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
  Individual: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Business: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Government: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
};

const statusTone: Record<CustomerStatus, string> = {
  Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Blocked: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
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
            className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition hover:bg-indigo-700"
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
                  'flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-indigo-500/15 to-purple-500/15 text-[10px] font-bold uppercase text-indigo-700 ring-1 ring-indigo-200/50 dark:text-indigo-300 dark:ring-indigo-500/30',
                  c.status === 'Blocked' &&
                    'from-rose-500/15 to-pink-500/15 text-rose-700 ring-rose-200/50 dark:text-rose-300 dark:ring-rose-500/30',
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
                      className="inline-flex h-4 w-4 items-center justify-center rounded-full bg-rose-100 text-rose-600 dark:bg-rose-500/20 dark:text-rose-300"
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
                      ? 'text-amber-600 dark:text-amber-400'
                      : balance < 0
                        ? 'text-emerald-600 dark:text-emerald-400'
                        : 'text-slate-700 dark:text-slate-200',
                  )}
                >
                  {fmtCurrency(balance, c.defaultCurrency, locale)}
                </div>
                {overdue > 0 && (
                  <div className="text-[9px] font-medium text-rose-600 dark:text-rose-400">
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
                ? 'bg-rose-500'
                : pct >= 80
                  ? 'bg-amber-500'
                  : pct >= 50
                    ? 'bg-yellow-500'
                    : 'bg-emerald-500';
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
          {onSelect && (
            <RowActionButton
              icon={<PanelRightOpen size={14} />}
              label={t('common.details', { defaultValue: 'Details' })}
              onClick={() => onSelect(c)}
            />
          )}
          <Link
            to={`/dashboard/customers/${c.id}`}
            title={t('common.view')}
            aria-label={t('common.view')}
            className="rounded-md p-1.5 text-slate-500 transition-colors hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-300"
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
