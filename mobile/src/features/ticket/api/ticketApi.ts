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
  installationId: string | null;
  projectId: string | null;
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

export interface ServiceTicketDetail extends ServiceTicketListItem {
  description: string;
  resolution: string | null;
  attachments: TicketAttachment[];
  comments: TicketComment[];
  assignedTo: string | null;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: TicketPriority;
  customerId?: string | null;
  installationId?: string | null;
  projectId?: string | null;
}

export interface AssignTicketRequest {
  userId: string;
  note?: string;
}

export interface ResolveTicketRequest {
  resolution: string;
  closeImmediately?: boolean;
}

export const ticketApi = {
  async list(): Promise<ServiceTicketListItem[]> {
    const { data } = await apiClient.get<ServiceTicketListItem[]>('/api/v1/service-tickets');
    return data;
  },

  async listAssignedToMe(): Promise<ServiceTicketListItem[]> {
    const { data } = await apiClient.get<ServiceTicketListItem[]>(
      '/api/v1/service-tickets/assigned',
    );
    return data;
  },

  async getById(id: string): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.get<ServiceTicketDetail>(`/api/v1/service-tickets/${id}`);
    return data;
  },

  async create(body: CreateTicketRequest): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.post<ServiceTicketDetail>('/api/v1/service-tickets', body);
    return data;
  },

  async assign(id: string, body: AssignTicketRequest): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.post<ServiceTicketDetail>(
      `/api/v1/service-tickets/${id}/assign`,
      body,
    );
    return data;
  },

  async resolve(id: string, body: ResolveTicketRequest): Promise<ServiceTicketDetail> {
    const { data } = await apiClient.post<ServiceTicketDetail>(
      `/api/v1/service-tickets/${id}/resolve`,
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
