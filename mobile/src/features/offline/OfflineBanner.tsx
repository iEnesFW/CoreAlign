import React, { useCallback } from 'react';
import { Pressable, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useNetworkStatus } from './useNetworkStatus';
import { usePendingMutations } from './usePendingMutations';
import { flushMutations } from './syncQueue';

const buildLabel = (
  t: (key: string, opts?: Record<string, unknown>) => string,
  isOnline: boolean,
  total: number,
  failed: number,
): string | null => {
  if (!isOnline) {
    if (total <= 0) return t('offline.bannerOffline');
    const key =
      total === 1 ? 'offline.bannerOfflineWithPending' : 'offline.bannerOfflineWithPendingPlural';
    return t(key, { count: total });
  }
  if (failed > 0) {
    const key = failed === 1 ? 'offline.bannerFailed' : 'offline.bannerFailedPlural';
    return t(key, { count: failed });
  }
  if (total > 0) {
    const key = total === 1 ? 'offline.bannerOnlinePending' : 'offline.bannerOnlinePendingPlural';
    return t(key, { count: total });
  }
  return null;
};

const tone = (isOnline: boolean, failed: number): string => {
  if (!isOnline) return 'bg-red-600';
  if (failed > 0) return 'bg-amber-600';
  return 'bg-blue-600';
};

export interface OfflineBannerProps {
  className?: string;
}

export const OfflineBanner: React.FC<OfflineBannerProps> = ({ className }) => {
  const { t } = useTranslation();
  const { isOnline } = useNetworkStatus({
    onReconnect: async () => {
      await flushMutations();
    },
  });
  const { summary, refresh } = usePendingMutations();

  const handleSyncNow = useCallback(async (): Promise<void> => {
    if (!isOnline) return;
    await flushMutations();
    await refresh();
  }, [isOnline, refresh]);

  const label = buildLabel(t, isOnline, summary.total, summary.failed);
  if (!label) return null;

  const containerTone = tone(isOnline, summary.failed);
  const showSyncButton = isOnline && summary.total > 0;

  return (
    <View
      className={`${containerTone} px-4 py-2 flex-row items-center justify-between ${className ?? ''}`.trim()}
      accessibilityRole="alert"
      accessibilityLiveRegion="polite"
    >
      <Text className="text-white font-semibold text-base flex-1" numberOfLines={2}>
        {label}
      </Text>
      {showSyncButton ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={t('offline.syncNow')}
          onPress={() => {
            void handleSyncNow();
          }}
          className="ml-3 bg-white/20 active:bg-white/30 rounded-md px-3 py-1"
        >
          <Text className="text-white font-semibold text-sm">{t('offline.syncNow')}</Text>
        </Pressable>
      ) : null}
    </View>
  );
};
