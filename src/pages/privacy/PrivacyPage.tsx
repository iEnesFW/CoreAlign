import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, ShieldAlert, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '@/features/auth/model/authStore';
import { privacyApi } from '@/features/privacy/api/privacyApi';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { isApiError } from '@/shared/api/ApiError';

export const PrivacyPage = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const [isExporting, setIsExporting] = useState(false);
  const [isErasing, setIsErasing] = useState(false);
  const [confirmText, setConfirmText] = useState('');
  const [showConfirm, setShowConfirm] = useState(false);

  const handleExport = async () => {
    setIsExporting(true);
    const [response] = await safeRequestWithNotify(privacyApi.exportMyData(), {
      successMessage: t('Privacy.Export.Success'),
    });
    if (response?.data) {
      const blob = new Blob([JSON.stringify(response.data, null, 2)], {
        type: 'application/json',
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      const safeUsername = response.data.profile.username.replace(/[^a-z0-9._-]/gi, '_') || 'user';
      link.download = `corealign-personal-data-${safeUsername}-${new Date()
        .toISOString()
        .slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }
    setIsExporting(false);
  };

  const handleErase = async () => {
    if (!user) return;
    if (confirmText !== user.username) {
      queueToast({
        dedupeKey: 'privacy:erase-mismatch',
        description: t('Privacy.Erase.MismatchError'),
        variant: 'error',
      });
      return;
    }
    setIsErasing(true);
    const [response, error] = await safeRequestWithNotify(privacyApi.eraseMyAccount(confirmText), {
      successMessage: t('Privacy.Erase.Success'),
    });
    setIsErasing(false);
    const sessionGone = isApiError(error) && error.statusCode === 401;
    if (response || sessionGone) {
      clearAuth();
      window.location.href = '/login';
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-4 sm:p-6">
      <header>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('Privacy.Title')}
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{t('Privacy.Subtitle')}</p>
      </header>

      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <div className="mb-3 flex items-center gap-2">
          <ShieldCheck className="text-emerald-600 dark:text-emerald-400" size={18} />
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('Privacy.Export.Title')}
          </h2>
        </div>
        <p className="mb-4 text-sm text-slate-600 dark:text-slate-300">
          {t('Privacy.Export.Description')}
        </p>
        <p className="mb-4 text-xs text-slate-500 dark:text-slate-400">
          {t('Privacy.ExportIncludes')}
        </p>
        <button
          type="button"
          onClick={handleExport}
          disabled={isExporting}
          className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
        >
          <Download size={14} />
          {isExporting ? t('Privacy.Export.Preparing') : t('Privacy.Export.Button')}
        </button>
      </section>

      <section className="rounded-lg border border-red-200 bg-red-50/40 p-5 shadow-sm dark:border-red-900/40 dark:bg-red-950/20">
        <div className="mb-3 flex items-center gap-2">
          <ShieldAlert className="text-red-600 dark:text-red-400" size={18} />
          <h2 className="text-base font-semibold text-red-900 dark:text-red-200">
            {t('Privacy.Erase.Title')}
          </h2>
        </div>
        <p className="mb-4 text-sm text-red-800/90 dark:text-red-200/90">
          {t('Privacy.Erase.Description')}
        </p>
        {!showConfirm ? (
          <button
            type="button"
            onClick={() => setShowConfirm(true)}
            disabled={!user}
            className="inline-flex items-center gap-2 rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            <ShieldAlert size={14} />
            {t('Privacy.Erase.Button')}
          </button>
        ) : (
          <div className="space-y-3">
            <label className="block text-sm text-red-900 dark:text-red-200">
              {t('Privacy.Erase.ConfirmPrompt')}
              <input
                type="text"
                value={confirmText}
                onChange={(e) => setConfirmText(e.target.value)}
                autoComplete="off"
                className="mt-1 block w-full rounded-md border border-red-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-red-500 focus:ring-1 focus:ring-red-500 dark:border-red-800 dark:bg-slate-900 dark:text-slate-100"
                placeholder={user?.username}
              />
            </label>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={handleErase}
                disabled={isErasing || confirmText !== user?.username}
                className="inline-flex items-center gap-2 rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
              >
                <ShieldAlert size={14} />
                {isErasing ? t('Privacy.Erase.Processing') : t('Privacy.Erase.Button')}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowConfirm(false);
                  setConfirmText('');
                }}
                disabled={isErasing}
                className="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                {t('Common.Cancel')}
              </button>
            </div>
          </div>
        )}
      </section>
    </div>
  );
};

export default PrivacyPage;
