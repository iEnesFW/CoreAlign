import * as FileSystem from 'expo-file-system';

const ensureDir = async (path: string): Promise<void> => {
  const info = await FileSystem.getInfoAsync(path);
  if (!info.exists) {
    await FileSystem.makeDirectoryAsync(path, { intermediates: true });
  }
};

export const acceptancePhotoDir = (): string =>
  `${FileSystem.documentDirectory ?? ''}corealign/acceptance-photos/`;

export const signatureDir = (): string =>
  `${FileSystem.documentDirectory ?? ''}corealign/signatures/`;

export const persistImageToAppDir = async (
  sourceUri: string,
  installationId: string,
): Promise<string> => {
  const dir = `${acceptancePhotoDir()}${installationId}/`;
  await ensureDir(dir);
  const ext = sourceUri.includes('.') ? sourceUri.slice(sourceUri.lastIndexOf('.')) : '.jpg';
  const filename = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}${ext}`;
  const target = `${dir}${filename}`;
  await FileSystem.copyAsync({ from: sourceUri, to: target });
  return target;
};

export const persistSignatureBase64 = async (
  base64Png: string,
  installationId: string,
): Promise<string> => {
  await ensureDir(signatureDir());
  const cleaned = base64Png.replace(/^data:image\/\w+;base64,/, '');
  const target = `${signatureDir()}${installationId}-${Date.now()}.png`;
  await FileSystem.writeAsStringAsync(target, cleaned, {
    encoding: FileSystem.EncodingType.Base64,
  });
  return target;
};

export const readBase64 = async (uri: string): Promise<string> => {
  return FileSystem.readAsStringAsync(uri, {
    encoding: FileSystem.EncodingType.Base64,
  });
};

export const removeFile = async (uri: string): Promise<void> => {
  const info = await FileSystem.getInfoAsync(uri);
  if (info.exists) {
    await FileSystem.deleteAsync(uri, { idempotent: true });
  }
};

export const fileSize = async (uri: string): Promise<number> => {
  const info = await FileSystem.getInfoAsync(uri);
  if (!info.exists) return 0;
  return info.size ?? 0;
};
