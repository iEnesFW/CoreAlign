import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { useMyWarrantyQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const WarrantyDetailPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useMyWarrantyQuery(id);

  const warranty = data?.data;

  return (
    <div className="space-y-4">
      <Link
        to="/customer-portal/warranties"
        className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError || !warranty ? (
        <div className="text-sm text-red-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : (
        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-3">
          <div className="flex items-start justify-between gap-3">
            <h1 className="text-xl font-semibold">{warranty.number}</h1>
            <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
              {t(`CustomerPortal.Warranty.Status.${warranty.status}`)}
            </span>
          </div>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
            <div>
              <dt className="text-slate-500 text-xs">
                {t('CustomerPortal.Warranty.CoverageLabel')}
              </dt>
              <dd>{t(`CustomerPortal.Warranty.CoverageType.${warranty.coverageType}`)}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Warranty.Months')}</dt>
              <dd>{warranty.warrantyMonths}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Warranty.StartDate')}</dt>
              <dd>{new Date(warranty.startDate).toLocaleDateString()}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Warranty.EndDate')}</dt>
              <dd>{new Date(warranty.endDate).toLocaleDateString()}</dd>
            </div>
          </dl>
          {warranty.notes ? (
            <div className="text-sm">
              <div className="text-slate-500 text-xs mb-1">
                {t('CustomerPortal.Warranty.Notes')}
              </div>
              <p>{warranty.notes}</p>
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
};

export default WarrantyDetailPage;
