import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield, History } from 'lucide-react';
import { SubmitRequestForm } from '@/features/privacy/ui/SubmitRequestForm';
import { useAuthStore } from '@/features/auth/model/authStore';

export const PrivacyRequestsPage = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const [lastSubmittedId, setLastSubmittedId] = useState<string | null>(null);

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 sm:p-6">
      <header className="flex items-center gap-3">
        <Shield className="text-indigo-600 dark:text-indigo-400" size={20} />
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('Privacy.Portal.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Privacy.Portal.Subtitle')}
          </p>
        </div>
      </header>

      <SubmitRequestForm defaultEmail={user?.email} onSubmitted={(id) => setLastSubmittedId(id)} />

      {lastSubmittedId && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900 dark:border-emerald-700/50 dark:bg-emerald-900/20 dark:text-emerald-200">
          <div className="flex items-center gap-2 font-semibold">
            <History size={14} />
            {t('Privacy.Portal.LastSubmitted')}
          </div>
          <div className="mt-1 break-all font-mono text-xs">{lastSubmittedId}</div>
        </div>
      )}
    </div>
  );
};

export default PrivacyRequestsPage;
