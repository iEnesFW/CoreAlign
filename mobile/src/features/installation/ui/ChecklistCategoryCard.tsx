import React, { useMemo } from 'react';
import { Pressable, Text, View } from 'react-native';
import type {
  ChecklistItemStatus,
  InstallationChecklistCategory,
  InstallationChecklistItem,
} from '../api/installationApi';

interface ChecklistCategoryCardProps {
  category: InstallationChecklistCategory;
  overrides: Record<string, { status: ChecklistItemStatus; notes: string | null }>;
  onChangeStatus: (item: InstallationChecklistItem, status: ChecklistItemStatus) => void;
  onCapturePhoto?: (item: InstallationChecklistItem) => void;
}

const CATEGORY_ICONS: Record<InstallationChecklistCategory['code'], string> = {
  Glass: '🪟',
  Frame: '🔧',
  Hardware: '⚙️',
  Sealing: '🎯',
  Cleanup: '🧹',
};

const STATUS_OPTIONS: { value: ChecklistItemStatus; label: string; emoji: string }[] = [
  { value: 'Pass', label: 'OK', emoji: '✅' },
  { value: 'Fail', label: 'Fail', emoji: '❌' },
  { value: 'NotApplicable', label: 'N/A', emoji: '➖' },
];

const statusButtonClass = (current: ChecklistItemStatus, option: ChecklistItemStatus): string => {
  const isActive = current === option;
  if (!isActive) return 'bg-surface-muted';
  if (option === 'Pass') return 'bg-success';
  if (option === 'Fail') return 'bg-danger';
  return 'bg-warning';
};

export const ChecklistCategoryCard: React.FC<ChecklistCategoryCardProps> = ({
  category,
  overrides,
  onChangeStatus,
  onCapturePhoto,
}) => {
  const summary = useMemo(() => {
    const total = category.items.length;
    const passed = category.items.filter((item) => {
      const status = overrides[item.id]?.status ?? item.status;
      return status === 'Pass' || status === 'NotApplicable';
    }).length;
    return { total, passed };
  }, [category.items, overrides]);

  return (
    <View className="mb-4 rounded-2xl bg-white dark:bg-brand-900 p-4 shadow">
      <View className="flex-row items-center justify-between mb-3">
        <View className="flex-row items-center">
          <Text className="text-3xl mr-3">{CATEGORY_ICONS[category.code]}</Text>
          <Text className="text-lg font-bold text-brand-900 dark:text-white">{category.label}</Text>
        </View>
        <Text className="text-sm text-slate-500 dark:text-slate-300">
          {summary.passed}/{summary.total}
        </Text>
      </View>

      {category.items.map((item) => {
        const status = overrides[item.id]?.status ?? item.status;
        return (
          <View key={item.id} className="mb-3 rounded-xl bg-surface-muted dark:bg-brand-700 p-3">
            <Text className="text-base font-medium text-brand-900 dark:text-white mb-2">
              {item.label}
            </Text>
            <View className="flex-row">
              {STATUS_OPTIONS.map((option) => (
                <Pressable
                  key={option.value}
                  accessibilityRole="button"
                  onPress={() => onChangeStatus(item, option.value)}
                  className={`flex-1 mr-2 min-h-touch rounded-xl items-center justify-center ${statusButtonClass(status, option.value)}`}
                >
                  <Text className="text-white text-base font-semibold">
                    {option.emoji} {option.label}
                  </Text>
                </Pressable>
              ))}
            </View>
            {item.requiresPhoto && onCapturePhoto ? (
              <Pressable
                accessibilityRole="button"
                onPress={() => onCapturePhoto(item)}
                className="mt-2 min-h-touch rounded-xl bg-brand-600 items-center justify-center"
              >
                <Text className="text-white text-base font-semibold">{'📷'} Photo required</Text>
              </Pressable>
            ) : null}
          </View>
        );
      })}
    </View>
  );
};
