import { apiClient } from '@/shared/api/apiClient';

export type ForwardableDocumentType = 'Invoice' | 'Order';

export interface ForwardDocumentInput {
  documentType: ForwardableDocumentType;
  documentId: string;
  recipientEmail: string;
  idempotencyKey: string;
}

export interface ForwardDocumentResult {
  queued: boolean;
  status: string;
}

export const portalForwardApi = {
  forward: async (input: ForwardDocumentInput): Promise<ForwardDocumentResult> => {
    const { data } = await apiClient.post<ForwardDocumentResult>(
      '/customer-portal/documents/forward',
      {
        documentType: input.documentType,
        documentId: input.documentId,
        recipientEmail: input.recipientEmail,
      },
      { headers: { 'Idempotency-Key': input.idempotencyKey } },
    );
    return data;
  },
};
