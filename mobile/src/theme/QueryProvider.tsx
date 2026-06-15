import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const buildClient = (): QueryClient =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: 2,
        staleTime: 30_000,
        refetchOnWindowFocus: false,
      },
      mutations: { retry: 0 },
    },
  });

export const QueryProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [client] = useState<QueryClient>(() => buildClient());
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
};
