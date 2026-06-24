import { useTranslation } from 'react-i18next';
import { Shield, ShieldAlert, ShieldOff, ShieldCheck } from 'lucide-react';
import type { WarrantyContract, WarrantyContractStatus } from '../model/warranty.types';

interface Props {
  contract: WarrantyContract;
  onSelect?: (contract: WarrantyContract) => void;
}

const STATUS_TONE: Record<WarrantyContractStatus, string> = {
  Active: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Expired: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Cancelled: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Suspended: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
};

const STATUS_ICON: Record<WarrantyContractStatus, React.ComponentType<{ className?: string }>> = {
  Active: ShieldCheck,
  Expired: ShieldOff,
  Cancelled: ShieldOff,
  Suspended: ShieldAlert,
};

const formatDate = (iso: string, locale: string): string =>
  new Date(iso).toLocaleDateString(locale, { year: 'numeric', month: 'short', day: '2-digit' });

export const WarrantyContractCard = ({ contract, onSelect }: Props) => {
  const { t, i18n } = useTranslation();
  const StatusIcon = STATUS_ICON[contract.status] ?? Shield;

  return (
    <button
      type="button"
      onClick={() => onSelect?.(contract)}
      className="flex w-full items-start gap-3 rounded-lg border border-slate-200 bg-white p-4 text-left shadow-sm transition hover:border-primary-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-900 dark:hover:border-primary-500"
    >
      <StatusIcon className="mt-1 h-6 w-6 text-primary-500 dark:text-primary-400" />
      <div className="flex-1">
        <div className="flex items-center justify-between gap-2">
          <span className="font-mono text-sm font-semibold text-slate-800 dark:text-slate-100">
            {contract.number}
          </span>
          <span
            className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_TONE[contract.status]}`}
          >
            {t(`Warranty.Status.${contract.status}`, { defaultValue: contract.status })}
          </span>
        </div>
        <div className="mt-1 text-sm text-slate-600 dark:text-slate-300">
          {t(`Warranty.CoverageType.${contract.coverageType}`, {
            defaultValue: contract.coverageType,
          })}
          {' • '}
          <span>
            {contract.warrantyMonths} {t('Warranty.MonthsShort', { defaultValue: 'mo' })}
          </span>
        </div>
        <div className="mt-2 grid grid-cols-2 gap-2 text-xs text-slate-500 dark:text-slate-400">
          <div>
            <span className="block font-medium text-slate-700 dark:text-slate-200">
              {t('Warranty.StartDate', { defaultValue: 'Start' })}
            </span>
            <span>{formatDate(contract.startDate, i18n.language)}</span>
          </div>
          <div>
            <span className="block font-medium text-slate-700 dark:text-slate-200">
              {t('Warranty.EndDate', { defaultValue: 'End' })}
            </span>
            <span>{formatDate(contract.endDate, i18n.language)}</span>
          </div>
        </div>
      </div>
    </button>
  );
};

export default WarrantyContractCard;
