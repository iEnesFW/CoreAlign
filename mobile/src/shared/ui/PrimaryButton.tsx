import React from 'react';
import { ActivityIndicator, Pressable, Text } from 'react-native';

interface PrimaryButtonProps {
  label: string;
  onPress: () => void;
  loading?: boolean;
  disabled?: boolean;
  variant?: 'primary' | 'success' | 'danger';
  icon?: string;
}

const VARIANT_STYLES: Record<NonNullable<PrimaryButtonProps['variant']>, string> = {
  primary: 'bg-brand-600 active:bg-brand-700',
  success: 'bg-success active:bg-emerald-700',
  danger: 'bg-danger active:bg-red-700',
};

export const PrimaryButton: React.FC<PrimaryButtonProps> = ({
  label,
  onPress,
  loading = false,
  disabled = false,
  variant = 'primary',
  icon,
}) => {
  const isDisabled = disabled || loading;
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      disabled={isDisabled}
      className={`min-h-touch-xl rounded-2xl px-6 items-center justify-center flex-row ${VARIANT_STYLES[variant]} ${isDisabled ? 'opacity-50' : ''}`}
    >
      {loading ? (
        <ActivityIndicator color="#FFFFFF" />
      ) : (
        <>
          {icon ? <Text className="text-white text-2xl mr-3">{icon}</Text> : null}
          <Text className="text-white text-btn-xl">{label}</Text>
        </>
      )}
    </Pressable>
  );
};
