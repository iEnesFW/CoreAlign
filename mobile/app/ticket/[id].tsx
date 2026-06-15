import React from 'react';
import { useLocalSearchParams } from 'expo-router';
import { TicketDetailScreen } from '@/features/ticket/ui/TicketDetailScreen';

const TicketDetailRoute: React.FC = () => {
  const { id } = useLocalSearchParams<{ id: string }>();
  if (!id) return null;
  return <TicketDetailScreen ticketId={id} />;
};

export default TicketDetailRoute;
