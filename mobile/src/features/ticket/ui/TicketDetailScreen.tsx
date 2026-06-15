import React, { useState } from 'react';
import { Alert, Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { PrimaryButton } from '@/shared/ui/PrimaryButton';
import { useAddTicketComment, useResolveTicket, useTicketDetail } from '../hooks/useTickets';

interface TicketDetailScreenProps {
  ticketId: string;
}

export const TicketDetailScreen: React.FC<TicketDetailScreenProps> = ({ ticketId }) => {
  const { t } = useTranslation();
  const query = useTicketDetail(ticketId);
  const addCommentMutation = useAddTicketComment(ticketId);
  const resolveMutation = useResolveTicket(ticketId);

  const [commentBody, setCommentBody] = useState('');
  const [resolution, setResolution] = useState('');
  const [showResolveForm, setShowResolveForm] = useState(false);

  if (query.isLoading) {
    return (
      <Screen>
        <Text className="text-base text-slate-500">{t('common.loading')}</Text>
      </Screen>
    );
  }

  if (!query.data) {
    return (
      <Screen>
        <Text className="text-base text-danger">{t('ticket.notFound')}</Text>
      </Screen>
    );
  }

  const detail = query.data;

  const handleSubmitComment = async () => {
    const body = commentBody.trim();
    if (!body) return;
    try {
      await addCommentMutation.mutateAsync(body);
      setCommentBody('');
    } catch (err) {
      Alert.alert(t('ticket.commentFailed'), err instanceof Error ? err.message : String(err));
    }
  };

  const handleResolve = async () => {
    const resText = resolution.trim();
    if (!resText) return;
    try {
      await resolveMutation.mutateAsync({ resolution: resText, closeImmediately: true });
      setShowResolveForm(false);
      setResolution('');
    } catch (err) {
      Alert.alert(t('ticket.resolveFailed'), err instanceof Error ? err.message : String(err));
    }
  };

  const canResolve = detail.status !== 'Resolved' && detail.status !== 'Closed';

  return (
    <Screen>
      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="mb-4 rounded-2xl bg-white dark:bg-brand-900 p-4">
          <Text className="text-xs text-slate-500 dark:text-slate-300">#{detail.ticketNumber}</Text>
          <Text className="text-2xl font-bold text-brand-900 dark:text-white mt-1">
            {detail.title}
          </Text>
          <Text className="text-base text-slate-600 dark:text-slate-300 mt-3">
            {detail.description}
          </Text>
          <View className="flex-row mt-3">
            <View className="mr-3 px-3 py-1 rounded-full bg-surface-muted">
              <Text className="text-xs text-brand-900">{t(`ticket.status.${detail.status}`)}</Text>
            </View>
            <View className="px-3 py-1 rounded-full bg-warning-soft">
              <Text className="text-xs text-warning">{detail.priority}</Text>
            </View>
          </View>
        </View>

        <View className="mb-4">
          <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
            {t('ticket.comments')}
          </Text>
          {detail.comments.length === 0 ? (
            <Text className="text-sm text-slate-500">{t('ticket.noComments')}</Text>
          ) : (
            detail.comments.map((c) => (
              <View key={c.id} className="mb-2 rounded-xl bg-surface-muted p-3">
                <Text className="text-sm font-semibold text-brand-900">{c.authorName}</Text>
                <Text className="text-sm text-brand-900 mt-1">{c.body}</Text>
                <Text className="text-xs text-slate-400 mt-1">
                  {new Date(c.createdAt).toLocaleString()}
                </Text>
              </View>
            ))
          )}
          <View className="flex-row mt-2">
            <TextInput
              value={commentBody}
              onChangeText={setCommentBody}
              placeholder={t('ticket.commentPlaceholder')}
              multiline
              className="flex-1 mr-2 min-h-touch rounded-xl bg-surface-muted px-4 py-2 text-base text-brand-900"
            />
            <Pressable
              accessibilityRole="button"
              onPress={() => void handleSubmitComment()}
              className="min-h-touch px-4 rounded-xl bg-brand-600 items-center justify-center"
            >
              <Text className="text-white text-base font-bold">
                {'💬'} {t('common.save')}
              </Text>
            </Pressable>
          </View>
        </View>

        {canResolve ? (
          showResolveForm ? (
            <View className="mb-4 rounded-2xl bg-white dark:bg-brand-900 p-4">
              <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
                {t('ticket.resolution')}
              </Text>
              <TextInput
                value={resolution}
                onChangeText={setResolution}
                multiline
                placeholder={t('ticket.resolutionPlaceholder')}
                className="min-h-touch-xl rounded-xl bg-surface-muted px-4 py-3 text-base text-brand-900 mb-3"
              />
              <PrimaryButton
                label={t('ticket.markResolved')}
                onPress={() => void handleResolve()}
                loading={resolveMutation.isPending}
                variant="success"
                icon="✓"
              />
            </View>
          ) : (
            <PrimaryButton
              label={t('ticket.updateStatus')}
              onPress={() => setShowResolveForm(true)}
              variant="primary"
              icon="🛠️"
            />
          )
        ) : null}
        <View className="h-12" />
      </ScrollView>
    </Screen>
  );
};
