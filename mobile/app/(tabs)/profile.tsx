import React from 'react';
import { Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { PrimaryButton } from '@/shared/ui/PrimaryButton';
import { useAuth } from '@/features/auth/useAuth';

const ProfileScreen: React.FC = () => {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  return (
    <Screen>
      <View className="gap-6">
        <Text className="text-2xl font-bold text-brand-900 dark:text-white">
          {t('profile.title')}
        </Text>
        <View className="rounded-2xl bg-surface-muted p-5">
          <Text className="text-lg font-semibold text-slate-900">{user?.fullName}</Text>
          <Text className="text-base text-slate-600">{user?.email}</Text>
        </View>
        <PrimaryButton label={t('auth.logout')} onPress={logout} variant="danger" icon="" />
      </View>
    </Screen>
  );
};

export default ProfileScreen;
