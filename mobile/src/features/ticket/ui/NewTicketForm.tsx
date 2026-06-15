import React, { useState } from 'react';
import { Alert, Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { PrimaryButton } from '@/shared/ui/PrimaryButton';
import { useCreateTicket } from '../hooks/useTickets';
import type { TicketPriority } from '../api/ticketApi';

interface NewTicketFormProps {
  installationId?: string | null;
  projectId?: string | null;
  onCreated: (ticketId: string | null) => void;
  onCancel: () => void;
}

const PRIORITY_CHOICES: { value: TicketPriority; emoji: string }[] = [
  { value: 'Low', emoji: '🌿' },
  { value: 'Normal', emoji: 'ℹ️' },
  { value: 'High', emoji: '⚠️' },
  { value: 'Critical', emoji: '🚨' },
];

const priorityButtonClass = (current: TicketPriority, target: TicketPriority): string => {
  if (current !== target) return 'bg-surface-muted';
  if (target === 'Critical') return 'bg-danger';
  if (target === 'High') return 'bg-warning';
  if (target === 'Normal') return 'bg-brand-600';
  return 'bg-success';
};

export const NewTicketForm: React.FC<NewTicketFormProps> = ({
  installationId = null,
  projectId = null,
  onCreated,
  onCancel,
}) => {
  const { t } = useTranslation();
  const createMutation = useCreateTicket();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<TicketPriority>('Normal');

  const handleSubmit = async () => {
    if (!title.trim() || !description.trim()) {
      Alert.alert(t('ticket.fillRequired'));
      return;
    }
    try {
      const result = await createMutation.mutateAsync({
        title: title.trim(),
        description: description.trim(),
        priority,
        installationId,
        projectId,
      });
      if (result.queued) {
        Alert.alert(t('ticket.queuedOffline'));
        onCreated(null);
      } else if (result.detail) {
        onCreated(result.detail.id);
      } else {
        onCreated(null);
      }
    } catch (err) {
      Alert.alert(t('ticket.createFailed'), err instanceof Error ? err.message : String(err));
    }
  };

  return (
    <Screen>
      <ScrollView showsVerticalScrollIndicator={false}>
        <Text className="text-2xl font-bold text-brand-900 dark:text-white mb-4">
          {t('ticket.newTitle')}
        </Text>

        <Text className="text-sm font-semibold text-slate-600 dark:text-slate-300 mb-1">
          {t('ticket.title')}
        </Text>
        <TextInput
          value={title}
          onChangeText={setTitle}
          placeholder={t('ticket.titlePlaceholder')}
          className="min-h-touch rounded-xl bg-surface-muted px-4 text-base text-brand-900 mb-3"
        />

        <Text className="text-sm font-semibold text-slate-600 dark:text-slate-300 mb-1">
          {t('ticket.description')}
        </Text>
        <TextInput
          value={description}
          onChangeText={setDescription}
          multiline
          placeholder={t('ticket.descriptionPlaceholder')}
          className="min-h-touch-xl rounded-xl bg-surface-muted px-4 py-3 text-base text-brand-900 mb-3"
        />

        <Text className="text-sm font-semibold text-slate-600 dark:text-slate-300 mb-2">
          {t('ticket.priority')}
        </Text>
        <View className="flex-row mb-4">
          {PRIORITY_CHOICES.map((choice) => (
            <Pressable
              key={choice.value}
              accessibilityRole="button"
              onPress={() => setPriority(choice.value)}
              className={`flex-1 mr-2 min-h-touch rounded-xl items-center justify-center ${priorityButtonClass(priority, choice.value)}`}
            >
              <Text className="text-sm font-bold text-white">
                {choice.emoji} {choice.value}
              </Text>
            </Pressable>
          ))}
        </View>

        <PrimaryButton
          label={t('ticket.submit')}
          onPress={() => void handleSubmit()}
          loading={createMutation.isPending}
          variant="success"
          icon="✓"
        />
        <View className="h-3" />
        <PrimaryButton label={t('common.cancel')} onPress={onCancel} variant="danger" icon="✕" />
        <View className="h-12" />
      </ScrollView>
    </Screen>
  );
};
