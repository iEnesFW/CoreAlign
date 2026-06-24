import { useTranslation } from 'react-i18next';
import { useAuthStore } from '@/shared/lib/store/authStore';

export const ProfilePage = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);

  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || user?.email;

  return (
    <div className="space-y-4 max-w-2xl">
      <h1 className="text-xl font-semibold">{t('CustomerPortal.Profile.Title')}</h1>

      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-3">
        <dl className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
          <div>
            <dt className="text-slate-500 text-xs">{t('CustomerPortal.Profile.Name')}</dt>
            <dd>{displayName ?? '-'}</dd>
          </div>
          <div>
            <dt className="text-slate-500 text-xs">{t('CustomerPortal.Profile.Email')}</dt>
            <dd>{user?.email ?? '-'}</dd>
          </div>
          <div>
            <dt className="text-slate-500 text-xs">{t('CustomerPortal.Profile.Username')}</dt>
            <dd>{user?.username ?? '-'}</dd>
          </div>
          <div>
            <dt className="text-slate-500 text-xs">{t('CustomerPortal.Profile.Tenant')}</dt>
            <dd>{user?.tenantName ?? '-'}</dd>
          </div>
        </dl>
      </div>

      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-3">
        <h2 className="text-sm font-semibold">
          {t('CustomerPortal.Profile.NotificationPrefsTitle')}
        </h2>
        <p className="text-sm text-slate-500">
          {t('CustomerPortal.Profile.NotificationPrefsHint')}
        </p>
      </div>
    </div>
  );
};

export default ProfilePage;
