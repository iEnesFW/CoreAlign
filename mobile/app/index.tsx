import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { Redirect } from 'expo-router';
import { useAuth } from '@/features/auth/useAuth';

const IndexRoute: React.FC = () => {
  const { isAuthenticated, isHydrated } = useAuth();

  if (!isHydrated) {
    return (
      <View className="flex-1 items-center justify-center bg-surface-light dark:bg-surface-dark">
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return isAuthenticated ? <Redirect href="/(tabs)/home" /> : <Redirect href="/(auth)/login" />;
};

export default IndexRoute;
