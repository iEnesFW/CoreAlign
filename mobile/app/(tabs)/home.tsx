import React from 'react';
import { Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { useAuth } from '@/features/auth/useAuth';

const HomeScreen: React.FC = () => {
  const { t } = useTranslation();
  const { user } = useAuth();

  return (
    <Screen>
      <View className="gap-4">
        <Text className="text-3xl font-bold text-brand-900 dark:text-white">
          {t('home.greeting', { name: user?.fullName ?? '' })}
        </Text>
        <View className="rounded-2xl bg-surface-muted p-5">
          <Text className="text-lg text-slate-700">{t('home.pendingInstallations')}</Text>
        </View>
        <View className="rounded-2xl bg-surface-muted p-5">
          <Text className="text-lg text-slate-700">{t('home.openTickets')}</Text>
        </View>
      </View>
    </Screen>
  );
};

export default HomeScreen;
