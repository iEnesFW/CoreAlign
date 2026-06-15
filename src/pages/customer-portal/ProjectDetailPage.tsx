import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { useMyProjectInstallationStatusQuery } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const ProjectDetailPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading, isError } = useMyProjectInstallationStatusQuery(id);
  const project = data?.data;

  return (
    <div className="space-y-4">
      <Link
        to="/customer-portal/projects"
        className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      {isLoading ? (
        <div className="text-sm text-slate-500">{t('CustomerPortal.Common.Loading')}</div>
      ) : isError || !project ? (
        <div className="text-sm text-red-600">{t('CustomerPortal.Common.LoadError')}</div>
      ) : (
        <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-3">
          <div className="flex items-start justify-between gap-3">
            <h1 className="text-xl font-semibold">{project.projectName}</h1>
            <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
              {t(`CustomerPortal.Project.Status.${project.status}`)}
            </span>
          </div>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Project.Code')}</dt>
              <dd className="font-mono">{project.code}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Project.SiteCity')}</dt>
              <dd>{project.siteCity ?? '-'}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Project.SiteDistrict')}</dt>
              <dd>{project.siteDistrict ?? '-'}</dd>
            </div>
            <div>
              <dt className="text-slate-500 text-xs">{t('CustomerPortal.Project.LastUpdated')}</dt>
              <dd>{new Date(project.updatedAtUtc).toLocaleString()}</dd>
            </div>
            {project.validUntilDate ? (
              <div>
                <dt className="text-slate-500 text-xs">{t('CustomerPortal.Project.ValidUntil')}</dt>
                <dd>{new Date(project.validUntilDate).toLocaleDateString()}</dd>
              </div>
            ) : null}
          </dl>
          <div className="text-xs text-slate-500 pt-2 border-t border-slate-200 dark:border-slate-800">
            {t('CustomerPortal.Project.ReadOnlyNotice')}
          </div>
        </div>
      )}
    </div>
  );
};

export default ProjectDetailPage;
