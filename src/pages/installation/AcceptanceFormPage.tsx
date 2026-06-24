import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { CheckCircle2, ShieldX } from 'lucide-react';
import {
  newIdempotencyKey,
  useAcceptanceDetailQuery,
  useAcceptInstallation,
  useAddPunchListItem,
  useCaptureCustomerSignature,
  useRejectInstallation,
  useResolvePunchListItem,
  useUpdateChecklistItem,
  useUploadAcceptancePhoto,
} from '@/features/installation-acceptance/hooks/useInstallationAcceptance';
import { AcceptanceChecklist } from '@/features/installation-acceptance/ui/AcceptanceChecklist';
import { PhotoCapture } from '@/features/installation-acceptance/ui/PhotoCapture';
import { PunchListEditor } from '@/features/installation-acceptance/ui/PunchListEditor';
import { SignaturePad } from '@/features/installation-acceptance/ui/SignaturePad';
import { offlineQueueDb } from '@/shared/offline/offlineQueueDb';
import { logger } from '@/shared/lib/logger';

const dataUrlToBlob = (dataUrl: string): Blob | null => {
  try {
    const [meta, base64] = dataUrl.split(',');
    if (!meta || !base64) return null;
    const match = /data:(.*?);base64/.exec(meta);
    const mime = match?.[1] ?? 'application/octet-stream';
    const byteString = atob(base64);
    const len = byteString.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i += 1) {
      bytes[i] = byteString.charCodeAt(i);
    }
    return new Blob([bytes], { type: mime });
  } catch (err) {
    logger.error('AcceptanceFormPage.dataUrlToBlob.failed', err);
    return null;
  }
};

const persistTemporaryBlob = async (blob: Blob, filename: string): Promise<string> => {
  const tempFileId = newIdempotencyKey();
  await offlineQueueDb.addBlob({
    tempFileId,
    blob,
    filename,
    contentType: blob.type || 'application/octet-stream',
    createdAt: Date.now(),
  });
  return tempFileId;
};

export const AcceptanceFormPage = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading } = useAcceptanceDetailQuery(id);

  const updateChecklist = useUpdateChecklistItem();
  const uploadPhoto = useUploadAcceptancePhoto();
  const captureSignature = useCaptureCustomerSignature();
  const acceptMutation = useAcceptInstallation();
  const rejectMutation = useRejectInstallation();
  const addPunch = useAddPunchListItem();
  const resolvePunch = useResolvePunchListItem();

  if (isLoading || !data?.data) {
    return (
      <div className="px-4 py-6 text-sm text-slate-500 dark:text-slate-400">
        {t('Common.Loading')}
      </div>
    );
  }

  const { acceptance, punchList } = data.data;
  const isLocked = acceptance.status === 'Accepted' || acceptance.status === 'Rejected';

  const handlePhotoSelected = async (file: File) => {
    const tempFileId = await persistTemporaryBlob(file, file.name);
    uploadPhoto.mutate({ acceptanceId: acceptance.id, fileId: tempFileId });
  };

  const handleSignatureCaptured = async (dataUrl: string, customerName: string) => {
    const blob = dataUrlToBlob(dataUrl);
    if (!blob) {
      logger.warn('AcceptanceFormPage.signature.blob_decode_failed');
      return;
    }
    const tempFileId = await persistTemporaryBlob(blob, 'signature.png');
    captureSignature.mutate({
      acceptanceId: acceptance.id,
      fileId: tempFileId,
      customerName,
    });
  };

  const handleAccept = () => {
    acceptMutation.mutate({
      acceptanceId: acceptance.id,
      idempotencyKey: newIdempotencyKey(),
    });
  };

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-4 py-4 md:px-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
          {t('InstallationAcceptance.Title')}
        </h1>
        <span className="text-xs text-slate-500 dark:text-slate-400">
          {t(`InstallationAcceptance.Status.${acceptance.status}`)}
        </span>
      </header>

      <section data-tour="checklist-section">
        <h2 className="mb-2 text-sm font-medium">
          {t('InstallationAcceptance.Action.UpdateChecklist')}
        </h2>
        <AcceptanceChecklist
          checklistJson={acceptance.checklistJson}
          disabled={isLocked}
          onItemChange={(category, itemKey, result, notes) =>
            updateChecklist.mutate({
              acceptanceId: acceptance.id,
              category,
              itemKey,
              result,
              notes,
            })
          }
        />
      </section>

      <section data-tour="photo-capture">
        <h2 className="mb-2 text-sm font-medium">
          {t('InstallationAcceptance.Action.UploadPhoto')}
        </h2>
        <PhotoCapture
          photoFileIds={acceptance.photoFileIds}
          disabled={isLocked}
          onPhotoSelected={(file) => {
            void handlePhotoSelected(file);
          }}
        />
      </section>

      <section data-tour="punch-list">
        <h2 className="mb-2 text-sm font-medium">{t('InstallationAcceptance.PunchList.Title')}</h2>
        <PunchListEditor
          acceptanceId={acceptance.id}
          items={punchList}
          disabled={isLocked}
          onAdd={(input) => addPunch.mutate(input)}
          onResolve={(input) => resolvePunch.mutate(input)}
        />
      </section>

      <section data-tour="signature-pad">
        <h2 className="mb-2 text-sm font-medium">
          {t('InstallationAcceptance.Action.CaptureSignature')}
        </h2>
        <SignaturePad
          disabled={isLocked}
          onCapture={(dataUrl, customerName) => {
            void handleSignatureCaptured(dataUrl, customerName);
          }}
        />
      </section>

      <section className="flex flex-wrap gap-2 pt-2">
        <button
          type="button"
          disabled={isLocked || acceptance.status !== 'SignedByCustomer'}
          onClick={handleAccept}
          className="flex flex-1 items-center justify-center gap-2 rounded bg-success-600 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50"
        >
          <CheckCircle2 className="size-4" />
          {t('InstallationAcceptance.Action.Accept')}
        </button>
        <button
          type="button"
          disabled={isLocked}
          onClick={() => {
            const reason = window.prompt(t('InstallationAcceptance.Action.RejectReasonPrompt'));
            if (reason && reason.trim().length > 0) {
              rejectMutation.mutate({ acceptanceId: acceptance.id, reason: reason.trim() });
            }
          }}
          className="flex flex-1 items-center justify-center gap-2 rounded border border-danger-300 px-4 py-3 text-sm font-semibold text-danger-700 disabled:opacity-50 dark:border-danger-700 dark:text-danger-300"
        >
          <ShieldX className="size-4" />
          {t('InstallationAcceptance.Action.Reject')}
        </button>
      </section>
    </div>
  );
};

export default AcceptanceFormPage;
