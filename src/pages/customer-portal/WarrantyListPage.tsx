import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyWarrantiesQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const WarrantyListPage = () => {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useMyWarrantiesQuery();

  const items = data?.data ?? [];

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">{t('CustomerPortal.Warranty.ListTitle')}</h1>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError ? (
        <div className="text-sm text-red-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : items.length === 0 ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Warranty.Empty')}</div>
      ) : (
        <ul className="grid grid-cols-1 md:grid-cols-2 gap-3">
          {items.map((w) => (
            <li
              key={w.id}
              className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
            >
              <div className="flex items-start justify-between gap-3 mb-2">
                <Link
                  to={`/customer-portal/warranties/${w.id}`}
                  className="font-medium text-blue-600 hover:underline truncate"
                >
                  {w.number}
                </Link>
                <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 shrink-0">
                  {t(`CustomerPortal.Warranty.Status.${w.status}`)}
                </span>
              </div>
              <div className="text-xs text-slate-500 space-y-0.5">
                <div>
                  {t('CustomerPortal.Warranty.CoverageLabel')}:{' '}
                  {t(`CustomerPortal.Warranty.CoverageType.${w.coverageType}`)}
                </div>
                <div>
                  {t('CustomerPortal.Warranty.Period')}:{' '}
                  {new Date(w.startDate).toLocaleDateString()} -{' '}
                  {new Date(w.endDate).toLocaleDateString()}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default WarrantyListPage;
