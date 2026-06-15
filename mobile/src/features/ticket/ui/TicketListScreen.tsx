import React from 'react';
import { FlatList, Pressable, RefreshControl, Text, View } from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { useAssignedTickets } from '../hooks/useTickets';
import type { ServiceTicketListItem, TicketPriority, TicketStatus } from '../api/ticketApi';

const PRIORITY_ICON: Record<TicketPriority, string> = {
  Critical: '🚨',
  High: '⚠️',
  Normal: 'ℹ️',
  Low: '🌿',
};

const STATUS_COLOR: Record<TicketStatus, string> = {
  Open: 'bg-warning',
  Assigned: 'bg-brand-600',
  InProgress: 'bg-brand-500',
  Resolved: 'bg-success',
  Closed: 'bg-slate-400',
};

interface TicketListScreenProps {
  onCreatePress?: () => void;
}

export const TicketListScreen: React.FC<TicketListScreenProps> = ({ onCreatePress }) => {
  const { t } = useTranslation();
  const router = useRouter();
  const query = useAssignedTickets();

  const renderItem = ({ item }: { item: ServiceTicketListItem }) => (
    <Pressable
      accessibilityRole="button"
      onPress={() => router.push(`/ticket/${item.id}`)}
      className="mb-3 rounded-2xl bg-white dark:bg-brand-900 p-4"
    >
      <View className="flex-row items-center justify-between mb-1">
        <Text className="text-xs text-slate-500 dark:text-slate-300">#{item.ticketNumber}</Text>
        <View className={`px-3 py-1 rounded-full ${STATUS_COLOR[item.status]}`}>
          <Text className="text-xs text-white font-semibold">
            {t(`ticket.status.${item.status}`)}
          </Text>
        </View>
      </View>
      <Text className="text-lg font-bold text-brand-900 dark:text-white mb-1">
        {PRIORITY_ICON[item.priority]} {item.title}
      </Text>
      <Text className="text-sm text-slate-500 dark:text-slate-300">{item.customerName}</Text>
      {item.scheduledAt ? (
        <Text className="text-xs text-slate-400 mt-1">
          {'📅'} {new Date(item.scheduledAt).toLocaleString()}
        </Text>
      ) : null}
    </Pressable>
  );

  return (
    <Screen>
      <View className="flex-row items-center justify-between mb-3">
        <Text className="text-2xl font-bold text-brand-900 dark:text-white">
          {t('ticket.listTitle')}
        </Text>
        {onCreatePress ? (
          <Pressable
            accessibilityRole="button"
            onPress={onCreatePress}
            className="min-h-touch px-4 rounded-2xl bg-brand-600 items-center justify-center flex-row"
          >
            <Text className="text-white text-base font-bold">
              {'➕'} {t('ticket.new')}
            </Text>
          </Pressable>
        ) : null}
      </View>
      <FlatList
        data={query.data ?? []}
        keyExtractor={(item) => item.id}
        renderItem={renderItem}
        refreshControl={
          <RefreshControl
            refreshing={query.isFetching && !query.isLoading}
            onRefresh={() => void query.refetch()}
          />
        }
        ListEmptyComponent={
          <Text className="text-base text-slate-500 dark:text-slate-300 text-center mt-12">
            {query.isLoading ? t('common.loading') : t('ticket.empty')}
          </Text>
        }
      />
    </Screen>
  );
};
