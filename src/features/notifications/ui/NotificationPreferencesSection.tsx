import { useTranslation } from 'react-i18next';
import { NotificationPreferencesEditor } from './NotificationPreferencesEditor';

export const NotificationPreferencesSection = () => {
  const { t } = useTranslation();
  return (
    <section className="space-y-3">
      <header>
        <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('Notifications.Settings.Title')}
        </h2>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('Notifications.Settings.Description')}
        </p>
      </header>
      <NotificationPreferencesEditor />
    </section>
  );
};
