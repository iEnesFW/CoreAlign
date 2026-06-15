import { useMutation, useQuery, useQueryClient, type UseQueryOptions } from '@tanstack/react-query';
import { collaborationApi } from './api';
import type { CommentDto } from './types';

const REFRESH_MS = 30_000;

export const collaborationKeys = {
  orderComments: (orderId: string) => ['collab', 'order-comments', orderId] as const,
};

export const useOrderComments = (
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<CommentDto[]>, 'queryKey' | 'queryFn' | 'enabled'>,
) =>
  useQuery({
    queryKey: collaborationKeys.orderComments(orderId ?? ''),
    queryFn: () => collaborationApi.listOrderComments(orderId!),
    enabled: !!orderId,
    refetchInterval: REFRESH_MS,
    staleTime: 10_000,
    ...options,
  });

export const usePostOrderComment = (orderId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: string) => collaborationApi.postOrderComment(orderId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: collaborationKeys.orderComments(orderId) });
    },
  });
};
