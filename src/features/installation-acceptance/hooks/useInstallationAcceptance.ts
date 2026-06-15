import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { installationAcceptanceApi } from '../api/installationAcceptanceApi';
import { offlineQueueDb, type OfflineMutationType } from '@/shared/offline/offlineQueueDb';
import type {
  AcceptInstallationInput,
  AddPunchListItemInput,
  CaptureSignatureInput,
  InstallationAcceptanceStatus,
  PunchListItemStatus,
  RejectInstallationInput,
  ResolvePunchListItemInput,
  StartAcceptanceInput,
  UpdateChecklistItemInput,
  UploadPhotoInput,
} from '../model/installationAcceptance.types';

const QK = {
  root: ['installation-acceptances'] as const,
  byWorkOrder: (workOrderId: string) =>
    ['installation-acceptances', 'workOrder', workOrderId] as const,
  forInspector: (inspectorUserId: string, status?: InstallationAcceptanceStatus) =>
    ['installation-acceptances', 'inspector', inspectorUserId, status ?? 'all'] as const,
  detail: (id: string) => ['installation-acceptances', 'detail', id] as const,
  punchByStatus: (status: PunchListItemStatus) =>
    ['installation-acceptances', 'punch-list', status] as const,
};

export interface OfflineQueuedResult<TPayload> {
  queued: true;
  payload: TPayload;
}

const isOffline = (): boolean => typeof navigator !== 'undefined' && navigator.onLine === false;

const newIdempotencyKey = (): string => {
  const cryptoSource: Crypto | undefined = typeof crypto !== 'undefined' ? crypto : undefined;
  if (cryptoSource?.randomUUID) {
    return cryptoSource.randomUUID();
  }
  const rand = () =>
    Math.floor(Math.random() * 0xffff)
      .toString(16)
      .padStart(4, '0');
  return `${rand()}${rand()}-${rand()}-${rand()}-${rand()}-${rand()}${rand()}${rand()}`;
};

interface QueueOptions {
  idempotencyKey?: string;
  tempFileId?: string;
  tempFileField?: string;
}

const runOrQueue = async <TPayload, TResult>(
  type: OfflineMutationType,
  payload: TPayload,
  online: () => Promise<TResult>,
  options?: QueueOptions,
): Promise<TResult | OfflineQueuedResult<TPayload>> => {
  if (isOffline()) {
    await offlineQueueDb.add({
      type,
      payload,
      idempotencyKey: options?.idempotencyKey,
      tempFileId: options?.tempFileId,
      tempFileField: options?.tempFileField,
    });
    return { queued: true, payload };
  }
  return online();
};

export const useAcceptanceByWorkOrderQuery = (workOrderId: string | undefined) =>
  useQuery({
    queryKey: workOrderId
      ? QK.byWorkOrder(workOrderId)
      : ['installation-acceptances', 'workOrder', 'noop'],
    queryFn: () => installationAcceptanceApi.listByWorkOrder(workOrderId!),
    enabled: Boolean(workOrderId),
    staleTime: 30 * 1000,
  });

export const useAcceptancesForInspectorQuery = (
  inspectorUserId: string | undefined,
  status?: InstallationAcceptanceStatus,
) =>
  useQuery({
    queryKey: inspectorUserId
      ? QK.forInspector(inspectorUserId, status)
      : ['installation-acceptances', 'inspector', 'noop'],
    queryFn: () => installationAcceptanceApi.listForInspector(inspectorUserId!, status),
    enabled: Boolean(inspectorUserId),
    staleTime: 30 * 1000,
  });

export const useAcceptanceDetailQuery = (id: string | undefined) =>
  useQuery({
    queryKey: id ? QK.detail(id) : ['installation-acceptances', 'detail', 'noop'],
    queryFn: () => installationAcceptanceApi.getById(id!),
    enabled: Boolean(id),
    staleTime: 15 * 1000,
  });

export const usePunchListByStatusQuery = (status: PunchListItemStatus) =>
  useQuery({
    queryKey: QK.punchByStatus(status),
    queryFn: () => installationAcceptanceApi.listPunchByStatus(status),
    staleTime: 30 * 1000,
  });

export const useStartAcceptance = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: StartAcceptanceInput) => installationAcceptanceApi.start(input),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useUpdateChecklistItem = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateChecklistItemInput) =>
      runOrQueue('updateChecklist', input, () => installationAcceptanceApi.updateChecklist(input)),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useUploadAcceptancePhoto = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: UploadPhotoInput) =>
      runOrQueue('addPhoto', input, () => installationAcceptanceApi.addPhoto(input), {
        tempFileId: input.fileId,
        tempFileField: 'fileId',
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useCaptureCustomerSignature = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: CaptureSignatureInput) =>
      runOrQueue(
        'captureSignature',
        input,
        () => installationAcceptanceApi.captureSignature(input),
        { tempFileId: input.fileId, tempFileField: 'fileId' },
      ),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useAcceptInstallation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { acceptanceId: string; idempotencyKey?: string }) => {
      const payload: AcceptInstallationInput = {
        acceptanceId: input.acceptanceId,
        idempotencyKey: input.idempotencyKey ?? newIdempotencyKey(),
      };
      return runOrQueue(
        'acceptInstallation',
        payload,
        () => installationAcceptanceApi.accept(payload),
        { idempotencyKey: payload.idempotencyKey },
      );
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useRejectInstallation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: RejectInstallationInput) =>
      runOrQueue('rejectInstallation', input, () => installationAcceptanceApi.reject(input)),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useAddPunchListItem = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: AddPunchListItemInput) =>
      runOrQueue('addPunchListItem', input, () =>
        installationAcceptanceApi.addPunchListItem(input),
      ),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export const useResolvePunchListItem = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: ResolvePunchListItemInput) =>
      runOrQueue('resolvePunchListItem', input, () =>
        installationAcceptanceApi.resolvePunchListItem(input),
      ),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK.root }),
  });
};

export { newIdempotencyKey };
