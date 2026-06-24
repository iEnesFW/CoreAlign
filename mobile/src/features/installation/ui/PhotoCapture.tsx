import React, { useState } from 'react';
import { Alert, FlatList, Image, Pressable, Text, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import { captureFromCamera, pickFromLibrary } from '@/shared/native/camera';
import type { DraftPhoto } from '../store/installationStore';

interface PhotoCaptureProps {
  installationId: string;
  photos: DraftPhoto[];
  onCaptured: (photo: DraftPhoto) => void;
  onRemove: (localId: string) => void;
  checklistItemId?: string | null;
}

const generateLocalId = (): string =>
  `photo-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

export const PhotoCapture: React.FC<PhotoCaptureProps> = ({
  installationId,
  photos,
  onCaptured,
  onRemove,
  checklistItemId = null,
}) => {
  const { t } = useTranslation();
  const [busy, setBusy] = useState(false);

  const doCapture = async (source: 'camera' | 'library') => {
    if (busy) return;
    setBusy(true);
    try {
      const result =
        source === 'camera'
          ? await captureFromCamera(installationId)
          : await pickFromLibrary(installationId);
      if (!result) return;
      onCaptured({
        localId: generateLocalId(),
        uri: result.uri,
        capturedAt: Date.now(),
        caption: null,
        checklistItemId,
        uploaded: false,
      });
    } catch (err) {
      Alert.alert(
        t('installation.cameraErrorTitle'),
        err instanceof Error ? err.message : String(err),
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <View className="mb-4">
      <Text className="text-base font-semibold text-brand-900 dark:text-white mb-2">
        {t('installation.photos')}
      </Text>
      <View className="flex-row mb-3">
        <Pressable
          accessibilityRole="button"
          onPress={() => void doCapture('camera')}
          disabled={busy}
          className={`flex-1 mr-2 min-h-touch-lg rounded-2xl bg-brand-600 items-center justify-center ${busy ? 'opacity-50' : ''}`}
        >
          <Text className="text-white text-lg font-bold">
            {'📷'} {t('installation.capturePhoto')}
          </Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={() => void doCapture('library')}
          disabled={busy}
          className={`flex-1 min-h-touch-lg rounded-2xl bg-brand-700 items-center justify-center ${busy ? 'opacity-50' : ''}`}
        >
          <Text className="text-white text-lg font-bold">
            {'🖼️'} {t('installation.pickPhoto')}
          </Text>
        </Pressable>
      </View>

      {photos.length === 0 ? (
        <Text className="text-sm text-slate-500 dark:text-slate-300">
          {t('installation.noPhotos')}
        </Text>
      ) : (
        <FlatList
          data={photos}
          horizontal
          keyExtractor={(item) => item.localId}
          renderItem={({ item }) => (
            <View className="mr-3">
              <Image
                source={{ uri: item.uri }}
                className="w-24 h-24 rounded-xl bg-surface-muted"
                resizeMode="cover"
              />
              <Pressable
                accessibilityRole="button"
                onPress={() => onRemove(item.localId)}
                className="absolute top-1 right-1 bg-danger rounded-full w-6 h-6 items-center justify-center"
              >
                <Text className="text-white text-xs font-bold">×</Text>
              </Pressable>
              {item.uploaded ? (
                <Text className="text-xs text-success mt-1">
                  {'✓'} {t('installation.photoSynced')}
                </Text>
              ) : (
                <Text className="text-xs text-warning mt-1">
                  {'…'} {t('installation.photoPending')}
                </Text>
              )}
            </View>
          )}
        />
      )}
    </View>
  );
};
