import React from 'react';
import { Pressable, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import type { PunchListItem as PunchListItemModel } from '../api/installationApi';

interface PunchListItemProps {
  item: PunchListItemModel;
  onResolve?: (item: PunchListItemModel) => void;
}

const severityClass = (severity: PunchListItemModel['severity']): string => {
  if (severity === 'Critical') return 'bg-danger';
  if (severity === 'Major') return 'bg-warning';
  return 'bg-brand-600';
};

const severityIcon = (severity: PunchListItemModel['severity']): string => {
  if (severity === 'Critical') return '🚨';
  if (severity === 'Major') return '⚠️';
  return 'ℹ️';
};

export const PunchListItem: React.FC<PunchListItemProps> = ({ item, onResolve }) => {
  const { t } = useTranslation();
  return (
    <View className="mb-2 rounded-xl bg-white dark:bg-brand-900 p-3 flex-row items-center">
      <View
        className={`w-10 h-10 rounded-full items-center justify-center mr-3 ${severityClass(item.severity)}`}
      >
        <Text className="text-lg">{severityIcon(item.severity)}</Text>
      </View>
      <View className="flex-1">
        <Text
          className={`text-base ${item.resolved ? 'line-through text-slate-400' : 'text-brand-900 dark:text-white'}`}
        >
          {item.description}
        </Text>
        <Text className="text-xs text-slate-500 dark:text-slate-300">
          {new Date(item.createdAt).toLocaleString()}
        </Text>
      </View>
      {!item.resolved && onResolve ? (
        <Pressable
          accessibilityRole="button"
          onPress={() => onResolve(item)}
          className="min-h-touch px-4 rounded-xl bg-success items-center justify-center"
        >
          <Text className="text-white text-sm font-semibold">
            {'✓'} {t('installation.resolve')}
          </Text>
        </Pressable>
      ) : null}
    </View>
  );
};
