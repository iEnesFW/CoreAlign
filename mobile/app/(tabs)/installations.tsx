import React from 'react';
import { Text } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';

const InstallationsScreen: React.FC = () => {
  const { t } = useTranslation();
  return (
    <Screen>
      <Text className="text-2xl font-bold text-brand-900 dark:text-white">
        {t('installation.listTitle')}
      </Text>
      <Text className="mt-4 text-base text-slate-600 dark:text-slate-300">
        {t('installation.empty')}
      </Text>
    </Screen>
  );
};

export default InstallationsScreen;
