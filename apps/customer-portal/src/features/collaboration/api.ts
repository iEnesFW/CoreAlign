import { apiClient } from '@/shared/api/apiClient';
import type { CommentDto } from './types';

const base = (orderId: string) => `/customer-portal/orders/${orderId}/comments`;

export const collaborationApi = {
  listOrderComments: async (orderId: string): Promise<CommentDto[]> => {
    const { data } = await apiClient.get<CommentDto[]>(base(orderId));
    return data;
  },
  postOrderComment: async (orderId: string, body: string): Promise<CommentDto> => {
    const { data } = await apiClient.post<CommentDto>(base(orderId), { body });
    return data;
  },
};
