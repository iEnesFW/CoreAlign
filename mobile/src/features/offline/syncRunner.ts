import * as Network from 'expo-network';
import { acceptanceQueue, ticketQueue, type QueuedRecord } from '@/shared/db/offlineQueue';
import {
  installationApi,
  type AcceptInstallationRequest,
} from '@/features/installation/api/installationApi';
import { ticketApi, type CreateTicketRequest } from '@/features/ticket/api/ticketApi';
import { flushMutations } from './syncQueue';

type PendingAcceptancePayload = AcceptInstallationRequest;

interface PendingTicketEnvelope {
  kind: 'create';
  body: CreateTicketRequest;
}

const MAX_ATTEMPTS = 5;

export const isOnline = async (): Promise<boolean> => {
  const state = await Network.getNetworkStateAsync();
  return Boolean(state.isConnected && state.isInternetReachable !== false);
};

const drainAcceptances = async (
  installationLookup: (record: QueuedRecord<PendingAcceptancePayload>) => string,
): Promise<void> => {
  const records = await acceptanceQueue.list<PendingAcceptancePayload>();
  for (const record of records) {
    if (record.attempts >= MAX_ATTEMPTS) continue;
    try {
      await installationApi.accept(installationLookup(record), record.payload);
      await acceptanceQueue.remove(record.id);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      await acceptanceQueue.markFailure(record.id, msg);
    }
  }
};

const drainTickets = async (): Promise<void> => {
  const records = await ticketQueue.list<PendingTicketEnvelope>();
  for (const record of records) {
    if (record.attempts >= MAX_ATTEMPTS) continue;
    try {
      if (record.payload.kind === 'create') {
        await ticketApi.create(record.payload.body);
      }
      await ticketQueue.remove(record.id);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      await ticketQueue.markFailure(record.id, msg);
    }
  }
};

export const drainOfflineQueues = async (): Promise<void> => {
  if (!(await isOnline())) return;
  await drainAcceptances((record) => record.refId);
  await drainTickets();
  await flushMutations();
};
