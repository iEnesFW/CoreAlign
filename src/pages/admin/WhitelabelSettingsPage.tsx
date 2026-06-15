import { useTranslation } from 'react-i18next';
import { ThemeEditor } from '@/features/whitelabel/ui/ThemeEditor';

export function WhitelabelSettingsPage() {
  const { t } = useTranslation();
  return (
    <div className="space-y-4 p-4 md:p-6">
      <header>
        <h1 className="text-xl font-semibold text-slate-800 dark:text-slate-100">
          {t('Whitelabel.page.title')}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Whitelabel.page.subtitle')}
        </p>
      </header>
      <ThemeEditor />
    </div>
  );
}

export default WhitelabelSettingsPage;
