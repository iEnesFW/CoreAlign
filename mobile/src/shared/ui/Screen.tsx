import React from 'react';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

interface ScreenProps {
  children: React.ReactNode;
  scroll?: boolean;
}

export const Screen: React.FC<ScreenProps> = ({ children }) => {
  return (
    <SafeAreaView className="flex-1 bg-surface-light dark:bg-surface-dark">
      <View className="flex-1 px-4 py-3">{children}</View>
    </SafeAreaView>
  );
};
