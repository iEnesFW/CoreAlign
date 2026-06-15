import React from 'react';
import { Tabs, Redirect } from 'expo-router';
import { Text } from 'react-native';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/features/auth/useAuth';

const TabIcon: React.FC<{ glyph: string; focused: boolean }> = ({ glyph, focused }) => (
  <Text style={{ fontSize: focused ? 30 : 26 }}>{glyph}</Text>
);

const TabsLayout: React.FC = () => {
  const { t } = useTranslation();
  const { isAuthenticated, isHydrated } = useAuth();

  if (!isHydrated) return null;
  if (!isAuthenticated) return <Redirect href="/(auth)/login" />;

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarLabelStyle: { fontSize: 13, fontWeight: '600' },
        tabBarStyle: { height: 72, paddingTop: 6, paddingBottom: 12 },
        tabBarActiveTintColor: '#2563EB',
        tabBarInactiveTintColor: '#64748B',
      }}
    >
      <Tabs.Screen
        name="home"
        options={{
          title: t('tabs.home'),
          tabBarIcon: ({ focused }) => <TabIcon glyph="" focused={focused} />,
        }}
      />
      <Tabs.Screen
        name="installations"
        options={{
          title: t('tabs.installations'),
          tabBarIcon: ({ focused }) => <TabIcon glyph="" focused={focused} />,
        }}
      />
      <Tabs.Screen
        name="tickets"
        options={{
          title: t('tabs.tickets'),
          tabBarIcon: ({ focused }) => <TabIcon glyph="" focused={focused} />,
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          title: t('tabs.profile'),
          tabBarIcon: ({ focused }) => <TabIcon glyph="" focused={focused} />,
        }}
      />
    </Tabs>
  );
};

export default TabsLayout;
