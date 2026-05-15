import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Edit2, Eye, PanelRightOpen, Trash2 } from 'lucide-react';
import type { Customer } from '../model/customer.types';

interface Props {
  customers: Customer[];
  isLoading: boolean;
  selectedId?: string | null;
  onSelect?: (customer: Customer) => void;
  onEdit: (customer: Customer) => void;
  onDelete: (customer: Customer) => void;
}

export const CustomerList = ({
  customers,
  isLoading,
  selectedId,
  onSelect,
  onEdit,
  onDelete,
}: Props) => {
  const { t } = useTranslation();

  if (isLoading && customers.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('common.loading')}
      </div>
    );
  }

  if (customers.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
        {t('customers.empty')}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>
              <Th>{t('customers.columns.name')}</Th>
              <Th>{t('customers.columns.email')}</Th>
              <Th>{t('customers.columns.phone')}</Th>
              <Th>{t('customers.columns.status')}</Th>
              <th className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('customers.columns.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {customers.map((customer) => {
              const isSelected = selectedId === customer.id;
              return (
                <tr
                  key={customer.id}
                  aria-selected={onSelect ? isSelected : undefined}
                  className={
                    isSelected
                      ? 'bg-indigo-50 dark:bg-indigo-500/10'
                      : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'
                  }
                >
                  <Td className="font-medium text-slate-900 dark:text-slate-100">
                    {customer.name}
                  </Td>
                  <Td>{customer.email ?? '—'}</Td>
                  <Td>{customer.phone ?? '—'}</Td>
                  <Td>
                    <span
                      className={
                        customer.isActive
                          ? 'inline-flex items-center rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
                          : 'inline-flex items-center rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600 dark:bg-slate-700/40 dark:text-slate-300'
                      }
                    >
                      {customer.isActive ? t('common.active') : t('common.inactive')}
                    </span>
                  </Td>
                  <td className="px-3 py-2 text-right">
                    <div className="inline-flex items-center gap-1">
                      {onSelect && (
                        <button
                          type="button"
                          onClick={() => onSelect(customer)}
                          className="rounded p-1.5 text-slate-500 hover:bg-indigo-50 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-indigo-500/10 dark:hover:text-indigo-300"
                          aria-label={t('common.details', { defaultValue: 'Details' })}
                          title={t('common.details', { defaultValue: 'Details' })}
                        >
                          <PanelRightOpen size={14} />
                        </button>
                      )}
                      <Link
                        to={`/dashboard/customers/${customer.id}`}
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('common.view')}
                        title={t('common.view')}
                      >
                        <Eye size={14} />
                      </Link>
                      <button
                        type="button"
                        onClick={() => onEdit(customer)}
                        className="rounded p-1.5 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                        aria-label={t('common.edit')}
                        title={t('common.edit')}
                      >
                        <Edit2 size={14} />
                      </button>
                      <button
                        type="button"
                        onClick={() => onDelete(customer)}
                        className="rounded p-1.5 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:text-slate-400 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                        aria-label={t('common.delete')}
                        title={t('common.delete')}
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);

const Td = ({ children, className }: { children: React.ReactNode; className?: string }) => (
  <td className={`px-3 py-2 text-slate-700 dark:text-slate-200 ${className ?? ''}`}>{children}</td>
);
