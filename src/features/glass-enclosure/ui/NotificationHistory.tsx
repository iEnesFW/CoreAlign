import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Check, Mail, MessageSquare, Phone, Smartphone } from 'lucide-react';
import type { NotificationLogDto } from '../api/glassProjectsApi';

interface NotificationHistoryProps {
  logs: NotificationLogDto[];
  isLoading: boolean;
}

const CHANNEL_ICON: Record<string, React.ReactNode> = {
  Email: <Mail size={14} />,
  Sms: <Phone size={14} />,
  WhatsApp: <MessageSquare size={14} />,
  InApp: <Smartphone size={14} />,
};

const STATUS_BADGE: Record<string, string> = {
  Pending: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-200',
  Sent: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  Delivered: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  Read: 'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  Failed: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
};

export function NotificationHistory({ logs, isLoading }: NotificationHistoryProps) {
  const { t, i18n } = useTranslation();
  const dateFormatter = useMemo(
    () => new Intl.DateTimeFormat(i18n.language, { dateStyle: 'short', timeStyle: 'short' }),
    [i18n.language],
  );

  if (isLoading) {
    return <p className="text-xs text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }

  if (logs.length === 0) {
    return (
      <p className="text-xs text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Notifications.None')}
      </p>
    );
  }

  return (
    <ul className="space-y-1.5">
      {logs.map((log) => (
        <li
          key={log.id}
          className="rounded border border-slate-200 bg-white p-2 text-xs dark:border-slate-700 dark:bg-slate-800"
        >
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1.5 text-slate-700 dark:text-slate-300">
              <span className="text-slate-500">{CHANNEL_ICON[log.channel] ?? null}</span>
              <span className="font-medium">{log.eventCode}</span>
              <span className="text-slate-400">·</span>
              <span className="font-mono text-[10px]">{log.recipientAddress}</span>
            </div>
            <span
              className={`rounded px-1.5 py-0.5 text-[10px] font-medium ${
                STATUS_BADGE[log.status] ?? STATUS_BADGE.Pending
              }`}
            >
              {log.status === 'Sent' || log.status === 'Delivered' || log.status === 'Read' ? (
                <Check size={10} className="mr-0.5 inline" />
              ) : log.status === 'Failed' ? (
                <AlertTriangle size={10} className="mr-0.5 inline" />
              ) : null}
              {log.status}
            </span>
          </div>
          <div className="mt-1 flex items-center justify-between text-[10px] text-slate-500 dark:text-slate-400">
            <span>{dateFormatter.format(new Date(log.createdAtUtc))}</span>
            {log.retryCount > 0 && <span>retry: {log.retryCount}</span>}
          </div>
          {log.errorMessage && (
            <p
              className="mt-1 truncate text-[10px] text-red-600 dark:text-red-400"
              title={log.errorMessage}
            >
              {log.errorMessage}
            </p>
          )}
        </li>
      ))}
    </ul>
  );
}
