import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ShieldPlus } from 'lucide-react';
import { useWarrantyContractsQuery } from '@/features/warranty/hooks/useWarrantyContracts';
import { WarrantyContractCard } from '@/features/warranty/ui/WarrantyContractCard';
import { WarrantyAlertsBadge } from '@/features/warranty/ui/WarrantyAlertsBadge';
import type { WarrantyContractStatus } from '@/features/warranty/model/warranty.types';

const STATUS_OPTIONS: (WarrantyContractStatus | 'All')[] = [
  'All',
  'Active',
  'Expired',
  'Cancelled',
  'Suspended',
];

export const WarrantyContractsPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [status, setStatus] = useState<WarrantyContractStatus | 'All'>('All');
  const params = useMemo(() => (status === 'All' ? {} : { status }), [status]);
  const { data, isLoading } = useWarrantyContractsQuery(params);
  const contracts = data?.data ?? [];

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100 sm:text-2xl">
            {t('Warranty.Title', { defaultValue: 'Warranty contracts' })}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Warranty.Subtitle', {
              defaultValue: 'Track warranty coverage, expirations, and renewals.',
            })}
          </p>
        </div>
        <WarrantyAlertsBadge />
      </header>

      <div className="flex flex-wrap items-center gap-2">
        {STATUS_OPTIONS.map((opt) => (
          <button
            key={opt}
            type="button"
            onClick={() => setStatus(opt)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition ${
              status === opt
                ? 'bg-indigo-600 text-white'
                : 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700'
            }`}
          >
            {opt === 'All'
              ? t('Common.Filter.All', { defaultValue: 'All' })
              : t(`Warranty.Status.${opt}`, { defaultValue: opt })}
          </button>
        ))}
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Common.Loading', { defaultValue: 'Loading...' })}
        </p>
      ) : contracts.length === 0 ? (
        <div className="rounded-lg border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
          <ShieldPlus className="mx-auto mb-2 h-8 w-8" />
          {t('Warranty.Empty', {
            defaultValue: 'No warranty contracts match the current filters.',
          })}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {contracts.map((c) => (
            <WarrantyContractCard
              key={c.id}
              contract={c}
              onSelect={() => navigate(`/dashboard/warranty/contracts/${c.id}`)}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export default WarrantyContractsPage;
