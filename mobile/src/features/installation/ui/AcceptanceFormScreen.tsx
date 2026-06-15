import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, ScrollView, Text, TextInput, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { Screen } from '@/shared/ui/Screen';
import { PrimaryButton } from '@/shared/ui/PrimaryButton';
import { buildPhotoFormData } from '@/shared/native/camera';
import {
  installationApi,
  type ChecklistItemStatus,
  type InstallationChecklistItem,
} from '../api/installationApi';
import {
  useAcceptInstallation,
  useAddPunchItem,
  useInstallationDetail,
  useResolvePunchItem,
  useStartInstallation,
  useUpdateChecklistItem,
} from '../hooks/useInstallations';
import { useInstallationStore } from '../store/installationStore';
import { ChecklistCategoryCard } from './ChecklistCategoryCard';
import { PhotoCapture } from './PhotoCapture';
import { SignaturePad } from './SignaturePad';
import { PunchListItem } from './PunchListItem';

interface AcceptanceFormScreenProps {
  installationId: string;
}

export const AcceptanceFormScreen: React.FC<AcceptanceFormScreenProps> = ({ installationId }) => {
  const { t } = useTranslation();
  const detailQuery = useInstallationDetail(installationId);
  const startMutation = useStartInstallation(installationId);
  const checklistMutation = useUpdateChecklistItem(installationId);
  const acceptMutation = useAcceptInstallation(installationId);
  const addPunchMutation = useAddPunchItem(installationId);
  const resolvePunchMutation = useResolvePunchItem(installationId);

  const draft = useInstallationStore((s) => s.getDraft(installationId));
  const setNotes = useInstallationStore((s) => s.setNotes);
  const setOverride = useInstallationStore((s) => s.setOverride);
  const addPhoto = useInstallationStore((s) => s.addPhoto);
  const removePhoto = useInstallationStore((s) => s.removePhoto);
  const setSignature = useInstallationStore((s) => s.setSignature);
  const markPhotoUploaded = useInstallationStore((s) => s.markPhotoUploaded);
  const markStarted = useInstallationStore((s) => s.markStarted);
  const resetDraft = useInstallationStore((s) => s.reset);

  const [punchInput, setPunchInput] = useState('');

  useEffect(() => {
    if (!detailQuery.data) return;
    if (detailQuery.data.status === 'Pending' && !draft.startedAt) {
      markStarted(installationId);
      startMutation.mutate();
    }
  }, [detailQuery.data, draft.startedAt, installationId, markStarted, startMutation]);

  const completion = useMemo(() => {
    if (!detailQuery.data) return { passed: 0, total: 0 };
    let total = 0;
    let passed = 0;
    for (const cat of detailQuery.data.categories) {
      for (const item of cat.items) {
        total += 1;
        const status = draft.overrides[item.id]?.status ?? item.status;
        if (status === 'Pass' || status === 'NotApplicable') passed += 1;
      }
    }
    return { passed, total };
  }, [detailQuery.data, draft.overrides]);

  const allPhotosUploaded = useMemo(() => draft.photos.every((p) => p.uploaded), [draft.photos]);

  const canSubmit =
    completion.total > 0 &&
    completion.passed === completion.total &&
    draft.signature !== null &&
    allPhotosUploaded;

  const handleChecklistChange = (item: InstallationChecklistItem, status: ChecklistItemStatus) => {
    setOverride(installationId, item.id, { status, notes: null });
    checklistMutation.mutate({ itemId: item.id, status });
  };

  const handlePhotoUpload = useCallback(
    async (localId: string) => {
      const current = useInstallationStore.getState().getDraft(installationId);
      const photo = current.photos.find((p) => p.localId === localId);
      if (!photo || photo.uploaded) return;
      try {
        const form = buildPhotoFormData(photo.uri, photo.caption, photo.checklistItemId);
        const remote = await installationApi.uploadPhoto(installationId, form);
        markPhotoUploaded(installationId, localId, remote);
      } catch (err) {
        Alert.alert(
          t('installation.photoUploadFailed'),
          err instanceof Error ? err.message : String(err),
        );
      }
    },
    [installationId, markPhotoUploaded, t],
  );

  const pendingPhotoKey = useMemo(
    () =>
      draft.photos
        .filter((p) => !p.uploaded)
        .map((p) => p.localId)
        .join('|'),
    [draft.photos],
  );

  useEffect(() => {
    if (!pendingPhotoKey) return;
    for (const localId of pendingPhotoKey.split('|')) {
      if (localId) void handlePhotoUpload(localId);
    }
  }, [pendingPhotoKey, handlePhotoUpload]);

  const handleAddPunch = () => {
    const desc = punchInput.trim();
    if (!desc) return;
    addPunchMutation.mutate({ description: desc, severity: 'Minor' });
    setPunchInput('');
  };

  const handleSubmit = async () => {
    if (!draft.signature) return;
    const uploadedIds = draft.photos.filter((p) => p.uploaded && p.remote).map((p) => p.remote!.id);
    try {
      const result = await acceptMutation.mutateAsync({
        signerName: draft.signature.signerName,
        signatureBase64: draft.signature.base64,
        notes: draft.notes || null,
        photoIds: uploadedIds,
      });
      if (result.queued) {
        Alert.alert(t('installation.submittedOffline'));
      } else {
        Alert.alert(t('installation.submitted'));
      }
      resetDraft(installationId);
    } catch (err) {
      Alert.alert(t('installation.submitFailed'), err instanceof Error ? err.message : String(err));
    }
  };

  if (detailQuery.isLoading) {
    return (
      <Screen>
        <Text className="text-base text-slate-500">{t('common.loading')}</Text>
      </Screen>
    );
  }

  if (!detailQuery.data) {
    return (
      <Screen>
        <Text className="text-base text-danger">{t('installation.notFound')}</Text>
      </Screen>
    );
  }

  const detail = detailQuery.data;

  return (
    <Screen>
      <ScrollView showsVerticalScrollIndicator={false}>
        <View className="mb-4">
          <Text className="text-2xl font-bold text-brand-900 dark:text-white">
            {detail.customerName}
          </Text>
          <Text className="text-base text-slate-500 dark:text-slate-300 mt-1">
            {detail.siteAddress}
          </Text>
          <Text className="text-sm text-slate-400 mt-1">
            {detail.projectCode} · {detail.totalGlassCount} {'🪟'}
          </Text>
          <View className="mt-3 h-2 rounded-full bg-surface-muted overflow-hidden">
            <View
              className="h-full bg-success"
              style={{
                width:
                  completion.total === 0
                    ? '0%'
                    : `${Math.round((completion.passed / completion.total) * 100)}%`,
              }}
            />
          </View>
          <Text className="mt-1 text-sm text-slate-500">
            {completion.passed} / {completion.total}
          </Text>
        </View>

        {detail.categories.map((category) => (
          <ChecklistCategoryCard
            key={category.code}
            category={category}
            overrides={draft.overrides}
            onChangeStatus={handleChecklistChange}
          />
        ))}

        <PhotoCapture
          installationId={installationId}
          photos={draft.photos}
          onCaptured={(photo) => addPhoto(installationId, photo)}
          onRemove={(localId) => removePhoto(installationId, localId)}
        />

        <View className="mb-4">
          <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
            {t('installation.punchList')}
          </Text>
          <View className="flex-row mb-2">
            <TextInput
              value={punchInput}
              onChangeText={setPunchInput}
              placeholder={t('installation.punchPlaceholder')}
              className="flex-1 mr-2 min-h-touch rounded-xl bg-surface-muted dark:bg-brand-700 px-4 text-base text-brand-900 dark:text-white"
            />
            <PrimaryButton label={t('installation.addPunch')} onPress={handleAddPunch} icon="➕" />
          </View>
          {detail.punchList.map((item) => (
            <PunchListItem
              key={item.id}
              item={item}
              onResolve={(p) => resolvePunchMutation.mutate(p.id)}
            />
          ))}
        </View>

        <View className="mb-4">
          <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
            {t('installation.notes')}
          </Text>
          <TextInput
            value={draft.notes}
            onChangeText={(value) => setNotes(installationId, value)}
            multiline
            placeholder={t('installation.notesPlaceholder')}
            className="min-h-touch-xl rounded-xl bg-surface-muted dark:bg-brand-700 px-4 py-3 text-base text-brand-900 dark:text-white"
          />
        </View>

        <SignaturePad
          installationId={installationId}
          signature={draft.signature}
          onCaptured={(sig) => setSignature(installationId, sig)}
          onClear={() => setSignature(installationId, null)}
        />

        <PrimaryButton
          label={t('installation.submit')}
          onPress={() => void handleSubmit()}
          loading={acceptMutation.isPending}
          disabled={!canSubmit}
          variant="success"
          icon="✓"
        />
        <View className="h-12" />
      </ScrollView>
    </Screen>
  );
};
