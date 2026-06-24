import { registerOfflineExecutor } from '@/shared/offline/offlineFlush';
import { installationAcceptanceApi } from '../api/installationAcceptanceApi';
import type {
  AcceptInstallationInput,
  AddPunchListItemInput,
  CaptureSignatureInput,
  RejectInstallationInput,
  ResolvePunchListItemInput,
  UpdateChecklistItemInput,
  UploadPhotoInput,
} from './installationAcceptance.types';

registerOfflineExecutor('updateChecklist', async (payload) => {
  await installationAcceptanceApi.updateChecklist(payload as UpdateChecklistItemInput);
});
registerOfflineExecutor('addPhoto', async (payload) => {
  await installationAcceptanceApi.addPhoto(payload as UploadPhotoInput);
});
registerOfflineExecutor('captureSignature', async (payload) => {
  await installationAcceptanceApi.captureSignature(payload as CaptureSignatureInput);
});
registerOfflineExecutor('acceptInstallation', async (payload) => {
  await installationAcceptanceApi.accept(payload as AcceptInstallationInput);
});
registerOfflineExecutor('rejectInstallation', async (payload) => {
  await installationAcceptanceApi.reject(payload as RejectInstallationInput);
});
registerOfflineExecutor('addPunchListItem', async (payload) => {
  await installationAcceptanceApi.addPunchListItem(payload as AddPunchListItemInput);
});
registerOfflineExecutor('resolvePunchListItem', async (payload) => {
  await installationAcceptanceApi.resolvePunchListItem(payload as ResolvePunchListItemInput);
});
