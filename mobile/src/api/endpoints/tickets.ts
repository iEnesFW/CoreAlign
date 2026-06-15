import { apiClient } from '@/api/apiClient';

export type TicketStatus = 'Open' | 'Assigned' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Normal' | 'High' | 'Critical';

export interface ServiceTicketListItem {
  id: string;
  ticketNumber: string;
  title: string;
  customerName: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdAt: string;
  scheduledAt: string | null;
}

export interface ServiceTicketDetail extends ServiceTicketListItem {
  description: string;
  resolution: string | null;
  attachments: TicketAttachment[];
  comments: TicketComment[];
}

export interface TicketAttachment {
  id: string;
  url: string;
  fileName: string;
  uploadedAt: string;
}

export interface TicketComment {
  id: string;
  authorName: string;
  body: string;
  createdAt: string;
}

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
  resolution?: string;
}

export const ticketsApi = {
  async listAssigned(): Promise<ServiceTicketListItem[]> {
    const { data } = await apiClient.get<ServiceTicketListItem[]>(
      '/api/v1/service-tickets/assigned',
    );
    return data;
  },
  async getById(id: string): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.get<ServiceTicketDetail>(`/api/v1/service-tickets/${id}`);
    return data;
  },
  async updateStatus(id: string, body: UpdateTicketStatusRequest): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.patch<ServiceTicketDetail>(
      `/api/v1/service-tickets/${id}/status`,
      body,
    );
    return data;
  },
  async addComment(id: string, body: string): Promise<TicketComment> {
    const { data } = await apiClient.post<TicketComment>(`/api/v1/service-tickets/${id}/comments`, {
      body,
    });
    return data;
  },
};
