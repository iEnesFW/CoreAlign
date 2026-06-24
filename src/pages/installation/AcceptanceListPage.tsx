import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { ClipboardCheck } from 'lucide-react';
import { usePunchListByStatusQuery } from '@/features/installation-acceptance/hooks/useInstallationAcceptance';

export const AcceptanceListPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = usePunchListByStatusQuery('Open');
  const items = data?.data ?? [];

  return (
    <div className="flex flex-col gap-4 px-4 py-4 md:px-6">
      <header className="flex items-center gap-3">
        <ClipboardCheck className="size-6 text-primary-600" />
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('InstallationAcceptance.Title')}
        </h1>
      </header>

      <section className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
        <h2 className="mb-3 text-sm font-medium text-slate-700 dark:text-slate-200">
          {t('InstallationAcceptance.OpenPunchListItems')}
        </h2>
        {isLoading && <p className="text-sm text-slate-500">{t('Common.Loading')}</p>}
        {!isLoading && items.length === 0 && (
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('InstallationAcceptance.PunchList.Empty')}
          </p>
        )}
        <ul className="flex flex-col gap-2">
          {items.map((item) => (
            <li
              key={item.id}
              className="flex items-center justify-between gap-2 rounded border border-slate-100 px-3 py-2 text-sm dark:border-slate-800"
            >
              <span>{item.description}</span>
              <Link
                to={`/dashboard/installation/acceptances/${item.acceptanceId}`}
                className="text-xs text-primary-600 hover:underline dark:text-primary-300"
              >
                {t('InstallationAcceptance.OpenForm')}
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
};

export default AcceptanceListPage;
