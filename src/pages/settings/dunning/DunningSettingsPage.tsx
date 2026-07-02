import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useDunningSettingsQuery,
  useUpsertDunningSetting,
} from '@/features/dunning/hooks/useDunningSettings';
import { useUsersQuery } from '@/features/users/hooks/useUsers';
import { DUNNING_TYPES } from '@/features/dunning/model/dunning.types';
import type { DunningSetting, DunningType } from '@/features/dunning/model/dunning.types';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { DunningTypeRow } from './components/DunningTypeRow';

const emptySetting = (type: DunningType): DunningSetting => ({
  type,
  isEnabled: false,
  sendInApp: true,
  sendEmail: false,
  recipientUserIds: [],
});

export const DunningSettingsPage = () => {
  const { t } = useTranslation();
  const settingsQuery = useDunningSettingsQuery();
  const usersQuery = useUsersQuery();
  const upsert = useUpsertDunningSetting();
  const [savingType, setSavingType] = useState<DunningType | null>(null);

  const settings = settingsQuery.data?.data ?? [];
  const users = usersQuery.data?.data ?? [];
  const byType = (type: DunningType): DunningSetting =>
    settings.find((s) => s.type === type) ?? emptySetting(type);

  const handleSave = async (next: DunningSetting) => {
    setSavingType(next.type);
    await safeRequestWithNotify(upsert.mutateAsync(next), {
      showSuccessNotification: true,
      successMessage: t('Dunning.toast.saved'),
    });
    setSavingType(null);
  };

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-5 p-4 sm:p-6">
      <header>
        <h1 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
          {t('Dunning.title')}
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{t('Dunning.subtitle')}</p>
      </header>

      {settingsQuery.isLoading || usersQuery.isLoading ? (
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('Dunning.loading')}</p>
      ) : settingsQuery.isError ? (
        <p role="alert" className="text-sm text-danger-600 dark:text-danger-400">
          {t('Dunning.loadError')}
        </p>
      ) : (
        <div className="flex flex-col gap-4">
          {DUNNING_TYPES.map((type) => (
            <DunningTypeRow
              key={type}
              setting={byType(type)}
              users={users}
              onSave={handleSave}
              isSaving={savingType === type}
            />
          ))}
        </div>
      )}
    </div>
  );
};
