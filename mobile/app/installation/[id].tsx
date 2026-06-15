import React from 'react';
import { useLocalSearchParams } from 'expo-router';
import { AcceptanceFormScreen } from '@/features/installation/ui/AcceptanceFormScreen';

const InstallationDetailScreen: React.FC = () => {
  const { id } = useLocalSearchParams<{ id: string }>();
  if (!id) return null;
  return <AcceptanceFormScreen installationId={id} />;
};

export default InstallationDetailScreen;
