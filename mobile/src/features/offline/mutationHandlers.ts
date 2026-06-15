import {
  installationApi,
  type AcceptInstallationRequest,
  type RejectInstallationRequest,
} from '@/features/installation/api/installationApi';
import { ticketApi, type CreateTicketRequest } from '@/features/ticket/api/ticketApi';
import { registerMutationHandler, type MutationRecord } from './syncQueue';

export const MUTATION_TYPES = Object.freeze({
  INSTALLATION_ACCEPT: 'installation.accept',
  INSTALLATION_REJECT: 'installation.reject',
  TICKET_CREATE: 'ticket.create',
});

interface InstallationAcceptPayload extends AcceptInstallationRequest {}
interface InstallationRejectPayload extends RejectInstallationRequest {}
interface TicketCreatePayload extends CreateTicketRequest {}

let registered = false;

export const registerCoreMutationHandlers = (): void => {
  if (registered) return;
  registered = true;

  registerMutationHandler<InstallationAcceptPayload>(
    MUTATION_TYPES.INSTALLATION_ACCEPT,
    async (record: MutationRecord<InstallationAcceptPayload>) => {
      if (!record.refId) throw new Error('Installation accept mutation missing refId');
      await installationApi.accept(record.refId, record.payload);
    },
  );

  registerMutationHandler<InstallationRejectPayload>(
    MUTATION_TYPES.INSTALLATION_REJECT,
    async (record: MutationRecord<InstallationRejectPayload>) => {
      if (!record.refId) throw new Error('Installation reject mutation missing refId');
      await installationApi.reject(record.refId, record.payload);
    },
  );

  registerMutationHandler<TicketCreatePayload>(
    MUTATION_TYPES.TICKET_CREATE,
    async (record: MutationRecord<TicketCreatePayload>) => {
      await ticketApi.create(record.payload);
    },
  );
};
