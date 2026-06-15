import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Camera, ImagePlus } from 'lucide-react';

interface Props {
  photoFileIds: string;
  onPhotoSelected: (file: File) => void;
  disabled?: boolean;
}

export const PhotoCapture = ({ photoFileIds, onPhotoSelected, disabled }: Props) => {
  const { t } = useTranslation();
  const cameraInputRef = useRef<HTMLInputElement | null>(null);
  const galleryInputRef = useRef<HTMLInputElement | null>(null);
  const [photoCount] = useState<number>(() => {
    try {
      const arr = JSON.parse(photoFileIds) as unknown[];
      return Array.isArray(arr) ? arr.length : 0;
    } catch {
      return 0;
    }
  });

  const onFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) onPhotoSelected(file);
    e.target.value = '';
  };

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          disabled={disabled}
          onClick={() => cameraInputRef.current?.click()}
          className="flex flex-1 items-center justify-center gap-2 rounded bg-blue-600 px-4 py-3 text-sm font-medium text-white disabled:opacity-50 sm:flex-initial"
        >
          <Camera className="size-4" />
          {t('InstallationAcceptance.PhotoCapture.TakePhoto')}
        </button>
        <button
          type="button"
          disabled={disabled}
          onClick={() => galleryInputRef.current?.click()}
          className="flex flex-1 items-center justify-center gap-2 rounded border border-slate-300 bg-white px-4 py-3 text-sm text-slate-700 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 sm:flex-initial"
        >
          <ImagePlus className="size-4" />
          {t('InstallationAcceptance.PhotoCapture.PickFromGallery')}
        </button>
      </div>
      <p className="text-xs text-slate-600 dark:text-slate-400">
        {t('InstallationAcceptance.PhotoCapture.Counter', { count: photoCount })}
      </p>
      <input
        ref={cameraInputRef}
        type="file"
        accept="image/*"
        capture="environment"
        hidden
        onChange={onFile}
      />
      <input ref={galleryInputRef} type="file" accept="image/*" hidden onChange={onFile} />
    </div>
  );
};
