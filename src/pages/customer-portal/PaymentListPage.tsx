import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyPaymentsQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const PaymentListPage = () => {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useMyPaymentsQuery();
  const items = data?.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-xl font-semibold">{t('CustomerPortal.Payment.ListTitle')}</h1>
        <Link
          to="/customer-portal/payments/initiate"
          className="inline-flex items-center px-3 py-2 rounded-md bg-primary-600 text-white text-sm hover:bg-primary-700"
        >
          {t('CustomerPortal.Payment.New')}
        </Link>
      </div>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError ? (
        <div className="text-sm text-danger-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : items.length === 0 ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Payment.Empty')}</div>
      ) : (
        <ul className="space-y-2">
          {items.map((p) => (
            <li
              key={p.id}
              className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-3 sm:p-4"
            >
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1">
                <span className="font-medium">{p.paymentNumber}</span>
                <div className="flex items-center gap-2 text-xs">
                  <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
                    {t(`CustomerPortal.Payment.Status.${p.status}`)}
                  </span>
                  <span className="px-2 py-0.5 rounded-full bg-primary-100 text-primary-800 dark:bg-primary-900/30 dark:text-primary-300">
                    {t(`CustomerPortal.Payment.Method.${p.method}`)}
                  </span>
                  <span className="font-medium">
                    {p.amount.toLocaleString(undefined, { maximumFractionDigits: 2 })} {p.currency}
                  </span>
                </div>
              </div>
              <div className="text-xs text-slate-500 mt-1">
                {new Date(p.paymentDate).toLocaleString()}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default PaymentListPage;
