import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Bell } from 'lucide-react';
import {
  useProfileNotificationPreferencesQuery,
  useUpdateProfileNotificationPreferences,
} from '@/features/auth/hooks/useProfileNotifications';
import type { ProfileNotificationPreference } from '@/features/auth/api/profileNotificationsApi';

type PreferenceOverride = { email: boolean; inApp: boolean };
type OverrideMap = Record<string, PreferenceOverride>;

export const NotificationPreferencesSection = () => {
  const { t } = useTranslation();
  const query = useProfileNotificationPreferencesQuery();
  const updateMutation = useUpdateProfileNotificationPreferences();

  const [overrides, setOverrides] = useState<OverrideMap>({});

  const baseMap = useMemo<OverrideMap>(() => {
    const map: OverrideMap = {};
    for (const item of query.data ?? []) {
      map[item.notificationKind] = { email: item.emailEnabled, inApp: item.inAppEnabled };
    }
    return map;
  }, [query.data]);

  const kinds = useMemo(
    () => (query.data ?? []).map((item) => item.notificationKind),
    [query.data],
  );

  const resolve = (kind: string): PreferenceOverride =>
    overrides[kind] ?? baseMap[kind] ?? { email: true, inApp: true };

  const toggle = (kind: string, channel: 'email' | 'inApp', value: boolean) => {
    const current = resolve(kind);
    setOverrides((prev) => ({
      ...prev,
      [kind]: {
        email: channel === 'email' ? value : current.email,
        inApp: channel === 'inApp' ? value : current.inApp,
      },
    }));
  };

  const handleSave = async () => {
    const items: ProfileNotificationPreference[] = kinds.map((kind) => {
      const row = resolve(kind);
      return {
        notificationKind: kind,
        emailEnabled: row.email,
        inAppEnabled: row.inApp,
      };
    });
    try {
      await updateMutation.mutateAsync({ items });
      setOverrides({});
      toast.success(
        t('profile.notifications.saved', { defaultValue: 'Notification preferences saved.' }),
      );
    } catch {
      toast.error(
        t('profile.notifications.saveFailed', { defaultValue: 'Could not save preferences.' }),
      );
    }
  };

  return (
    <section
      className="space-y-3 rounded-[6px] border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
      data-testid="notification-preferences-section"
    >
      <header className="flex items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Bell className="h-4 w-4 text-slate-500 mt-0.5" />
          <div>
            <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t('profile.notifications.title', { defaultValue: 'Notification preferences' })}
            </h2>
            <p className="text-[11px] text-slate-500 dark:text-slate-400">
              {t('profile.notifications.subtitle', {
                defaultValue: 'Choose which events trigger emails or in-app alerts.',
              })}
            </p>
          </div>
        </div>
      </header>

      {query.isLoading && (
        <p className="text-[11px] text-slate-500">
          {t('profile.notifications.loading', { defaultValue: 'Loading preferences…' })}
        </p>
      )}

      {!query.isLoading && kinds.length > 0 && (
        <div className="overflow-x-auto rounded-[5px] border border-slate-100 dark:border-slate-800">
          <table className="min-w-full text-[12px]">
            <thead className="bg-slate-50 dark:bg-slate-800/40 text-slate-600 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-start font-semibold">&nbsp;</th>
                <th className="px-3 py-2 text-center font-semibold">
                  {t('profile.notifications.channel.email', { defaultValue: 'Email' })}
                </th>
                <th className="px-3 py-2 text-center font-semibold">
                  {t('profile.notifications.channel.inApp', { defaultValue: 'In-app' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {kinds.map((kind) => {
                const row = resolve(kind);
                return (
                  <tr
                    key={kind}
                    data-testid={`notification-pref-row-${kind}`}
                    className="border-t border-slate-100 dark:border-slate-800"
                  >
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-200">
                      {t(`profile.notifications.kinds.${kind}`, { defaultValue: kind })}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <input
                        type="checkbox"
                        aria-label={`${kind} ${t('profile.notifications.channel.email', { defaultValue: 'Email' })}`}
                        checked={row.email}
                        onChange={(e) => toggle(kind, 'email', e.target.checked)}
                        className="h-3.5 w-3.5 rounded-[2px] border-slate-300"
                      />
                    </td>
                    <td className="px-3 py-2 text-center">
                      <input
                        type="checkbox"
                        aria-label={`${kind} ${t('profile.notifications.channel.inApp', { defaultValue: 'In-app' })}`}
                        checked={row.inApp}
                        onChange={(e) => toggle(kind, 'inApp', e.target.checked)}
                        className="h-3.5 w-3.5 rounded-[2px] border-slate-300"
                      />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-end">
        <button
          type="button"
          onClick={() => void handleSave()}
          disabled={query.isLoading || updateMutation.isPending}
          className="rounded-[5px] bg-indigo-600 text-white text-[12px] font-semibold px-3 py-1.5 hover:bg-indigo-500 disabled:opacity-60"
        >
          {t('profile.notifications.save', { defaultValue: 'Save preferences' })}
        </button>
      </div>
    </section>
  );
};

export default NotificationPreferencesSection;
