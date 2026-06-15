import React, { useState } from 'react';
import { KeyboardAvoidingView, Platform, Text, TextInput, View } from 'react-native';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { PrimaryButton } from '@/shared/ui/PrimaryButton';
import { useAuth } from '@/features/auth/useAuth';

const LoginScreen: React.FC = () => {
  const { t } = useTranslation();
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (): Promise<void> => {
    if (!email || !password) return;
    setSubmitting(true);
    setError(null);
    try {
      await login({ email, password });
      router.replace('/(tabs)/home');
    } catch {
      setError(t('auth.loginError'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Screen>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        className="flex-1 justify-center"
      >
        <View className="gap-6">
          <Text className="text-4xl font-bold text-brand-900 dark:text-white">
            {t('auth.loginTitle')}
          </Text>

          <View className="gap-2">
            <Text className="text-lg text-slate-700 dark:text-slate-200">{t('auth.email')}</Text>
            <TextInput
              accessibilityLabel={t('auth.email')}
              autoCapitalize="none"
              autoComplete="email"
              keyboardType="email-address"
              value={email}
              onChangeText={setEmail}
              className="min-h-touch-lg rounded-2xl bg-surface-muted px-4 text-lg text-slate-900"
            />
          </View>

          <View className="gap-2">
            <Text className="text-lg text-slate-700 dark:text-slate-200">{t('auth.password')}</Text>
            <TextInput
              accessibilityLabel={t('auth.password')}
              secureTextEntry
              autoComplete="password"
              value={password}
              onChangeText={setPassword}
              className="min-h-touch-lg rounded-2xl bg-surface-muted px-4 text-lg text-slate-900"
            />
          </View>

          {error ? <Text className="text-danger">{error}</Text> : null}

          <PrimaryButton
            label={t('auth.loginButton')}
            onPress={handleSubmit}
            loading={submitting}
            icon=""
          />
        </View>
      </KeyboardAvoidingView>
    </Screen>
  );
};

export default LoginScreen;
