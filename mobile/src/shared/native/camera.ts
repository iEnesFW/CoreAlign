import * as ImagePicker from 'expo-image-picker';
import { Camera } from 'expo-camera';
import { persistImageToAppDir } from './fileSystem';

export interface CapturedPhoto {
  uri: string;
  width: number;
  height: number;
  fileSize: number | null;
}

export const ensureCameraPermission = async (): Promise<boolean> => {
  const existing = await Camera.getCameraPermissionsAsync();
  if (existing.status === 'granted') return true;
  const next = await Camera.requestCameraPermissionsAsync();
  return next.status === 'granted';
};

export const ensureMediaLibraryPermission = async (): Promise<boolean> => {
  const existing = await ImagePicker.getMediaLibraryPermissionsAsync();
  if (existing.status === 'granted') return true;
  const next = await ImagePicker.requestMediaLibraryPermissionsAsync();
  return next.status === 'granted';
};

const DEFAULT_QUALITY = 0.7;

export const captureFromCamera = async (installationId: string): Promise<CapturedPhoto | null> => {
  const ok = await ensureCameraPermission();
  if (!ok) return null;
  const result = await ImagePicker.launchCameraAsync({
    quality: DEFAULT_QUALITY,
    exif: false,
    base64: false,
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    allowsEditing: false,
  });
  if (result.canceled || result.assets.length === 0) return null;
  const asset = result.assets[0];
  if (!asset) return null;
  const persistedUri = await persistImageToAppDir(asset.uri, installationId);
  return {
    uri: persistedUri,
    width: asset.width,
    height: asset.height,
    fileSize: asset.fileSize ?? null,
  };
};

export const pickFromLibrary = async (installationId: string): Promise<CapturedPhoto | null> => {
  const ok = await ensureMediaLibraryPermission();
  if (!ok) return null;
  const result = await ImagePicker.launchImageLibraryAsync({
    quality: DEFAULT_QUALITY,
    exif: false,
    base64: false,
    mediaTypes: ImagePicker.MediaTypeOptions.Images,
    allowsEditing: false,
  });
  if (result.canceled || result.assets.length === 0) return null;
  const asset = result.assets[0];
  if (!asset) return null;
  const persistedUri = await persistImageToAppDir(asset.uri, installationId);
  return {
    uri: persistedUri,
    width: asset.width,
    height: asset.height,
    fileSize: asset.fileSize ?? null,
  };
};

export const buildPhotoFormData = (
  uri: string,
  caption: string | null,
  checklistItemId: string | null,
): FormData => {
  const form = new FormData();
  const filename = uri.split('/').pop() ?? `photo-${Date.now()}.jpg`;
  const extMatch = /\.(\w+)$/.exec(filename);
  const ext = extMatch?.[1]?.toLowerCase() ?? 'jpg';
  const mime = ext === 'png' ? 'image/png' : 'image/jpeg';
  form.append('file', {
    uri,
    name: filename,
    type: mime,
  } as unknown as Blob);
  if (caption) form.append('caption', caption);
  if (checklistItemId) form.append('checklistItemId', checklistItemId);
  return form;
};
