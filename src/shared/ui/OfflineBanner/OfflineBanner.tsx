import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { CloudOff, RefreshCw } from 'lucide-react';
import { toast } from 'sonner';
import { useOfflineSync } from '@/shared/offline/useOfflineSync';

export const OfflineBanner = () => {
  const { t } = useTranslation();
  const { isOnline, queueSize, isFlushing, lastFlush, flush } = useOfflineSync();
  const wasOfflineRef = useRef(!isOnline);

  useEffect(() => {
    if (isOnline && wasOfflineRef.current && lastFlush) {
      if (lastFlush.flushed > 0) {
        toast.success(t('Common.Offline.Synced', { count: lastFlush.flushed }));
      }
      if (lastFlush.failed > 0) {
        toast.error(t('Common.Offline.Error', { count: lastFlush.failed }));
      }
    }
    wasOfflineRef.current = !isOnline;
  }, [isOnline, lastFlush, t]);

  if (isOnline && queueSize === 0) {
    return null;
  }

  const banner = isOnline ? (
    <div
      role="status"
      className="sticky top-0 z-50 flex w-full items-center justify-between gap-3 bg-warning-500 px-4 py-2 text-sm font-medium text-white shadow"
    >
      <span className="flex items-center gap-2">
        <RefreshCw className={isFlushing ? 'h-4 w-4 animate-spin' : 'h-4 w-4'} />
        {t('Common.Offline.Queued', { count: queueSize })}
      </span>
      <button
        type="button"
        onClick={() => void flush()}
        disabled={isFlushing}
        className="rounded bg-white/20 px-2 py-1 text-xs hover:bg-white/30 disabled:opacity-50"
      >
        {t('Common.Offline.SyncNow')}
      </button>
    </div>
  ) : (
    <div
      role="alert"
      className="sticky top-0 z-50 flex w-full items-center gap-3 bg-slate-800 px-4 py-2 text-sm font-medium text-white shadow"
    >
      <CloudOff className="h-4 w-4" />
      <span>{t('Common.Offline.Banner')}</span>
      {queueSize > 0 ? (
        <span className="ml-auto rounded bg-white/20 px-2 py-0.5 text-xs">
          {t('Common.Offline.Queued', { count: queueSize })}
        </span>
      ) : null}
    </div>
  );

  return banner;
};
