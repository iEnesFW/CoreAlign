import '@/shared/i18n';
import '../global.css';

import React, { useEffect } from 'react';
import { View } from 'react-native';
import { Slot, SplashScreen } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthProvider } from '@/features/auth/AuthProvider';
import { QueryProvider } from '@/theme/QueryProvider';
import { ThemeProvider } from '@/theme/ThemeProvider';
import { OfflineProvider } from '@/features/offline/OfflineProvider';
import { OfflineBanner } from '@/features/offline/OfflineBanner';

void SplashScreen.preventAutoHideAsync();

const RootLayout: React.FC = () => {
  useEffect(() => {
    void SplashScreen.hideAsync();
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <ThemeProvider>
          <QueryProvider>
            <AuthProvider>
              <OfflineProvider>
                <StatusBar style="auto" />
                <View style={{ flex: 1 }}>
                  <OfflineBanner />
                  <View style={{ flex: 1 }}>
                    <Slot />
                  </View>
                </View>
              </OfflineProvider>
            </AuthProvider>
          </QueryProvider>
        </ThemeProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
};

export default RootLayout;
