import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import {
  useNotificationPreferencesQuery,
  useUpsertNotificationPreference,
} from '../hooks/useNotificationPreferences';
import type { NotificationChannel } from '../model/notifications.types';

const CATEGORIES: string[] = ['Warranty', 'Installation', 'Payment', 'Mrp', 'ServiceTicket'];
const CHANNELS: NotificationChannel[] = ['InApp', 'Email', 'Sms', 'Push', 'WhatsApp'];

export const NotificationPreferencesEditor = () => {
  const { t } = useTranslation();
  const prefsQuery = useNotificationPreferencesQuery();
  const upsert = useUpsertNotificationPreference();

  const lookup = useMemo(() => {
    const map = new Map<string, boolean>();
    for (const p of prefsQuery.data ?? []) {
      map.set(`${p.categoryKey}|${p.channel}`, p.isEnabled);
    }
    return map;
  }, [prefsQuery.data]);

  const toggle = (categoryKey: string, channel: NotificationChannel, current: boolean) => {
    upsert.mutate({ categoryKey, channel, isEnabled: !current });
  };

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
      <h3 className="mb-3 text-sm font-semibold text-slate-900 dark:text-slate-100">
        {t('Notifications.Preferences.Title')}
      </h3>
      <table className="w-full text-xs">
        <thead>
          <tr className="border-b border-slate-200 text-left text-slate-500 dark:border-slate-700 dark:text-slate-400">
            <th className="px-2 py-1.5">{t('Notifications.Preferences.ChannelMatrix')}</th>
            {CHANNELS.map((c) => (
              <th key={c} className="px-2 py-1.5 text-center">
                {t(`Notifications.Channel.${c}`)}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {CATEGORIES.map((cat) => (
            <tr key={cat} className="border-b border-slate-100 dark:border-slate-700/60">
              <td className="px-2 py-1.5 font-medium text-slate-800 dark:text-slate-200">
                {t(`Notifications.Category.${cat}`)}
              </td>
              {CHANNELS.map((channel) => {
                const enabled = lookup.get(`${cat}|${channel}`) ?? true;
                return (
                  <td key={channel} className="px-2 py-1.5 text-center">
                    <input
                      type="checkbox"
                      checked={enabled}
                      onChange={() => toggle(cat, channel, enabled)}
                      disabled={upsert.isPending}
                      className="h-3.5 w-3.5 rounded text-indigo-600"
                    />
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
};
