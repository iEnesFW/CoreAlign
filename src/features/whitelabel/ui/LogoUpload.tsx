import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TenantThemeAssetKind } from '../model/whitelabel.types';
import { useUploadThemeAsset } from '../hooks/useTenantTheme';

interface LogoUploadProps {
  kind: TenantThemeAssetKind;
  currentUrl?: string | null;
  label: string;
  acceptHint?: string;
  maxBytes?: number;
}

export const LogoUpload = ({
  kind,
  currentUrl,
  label,
  acceptHint,
  maxBytes = 2 * 1024 * 1024,
}: LogoUploadProps) => {
  const { t } = useTranslation();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [error, setError] = useState<string | null>(null);
  const uploadAsset = useUploadThemeAsset();

  const handleSelect = (file: File | null) => {
    if (!file) return;
    if (file.size > maxBytes) {
      setError(t('Whitelabel.upload.tooLarge', { max: Math.floor(maxBytes / 1024) }));
      return;
    }
    setError(null);
    uploadAsset.mutate({ kind, file });
  };

  return (
    <div className="flex flex-col gap-2 rounded-md border border-slate-200 p-3 dark:border-slate-700">
      <span className="text-sm font-medium text-slate-700 dark:text-slate-200">{label}</span>
      <div className="flex flex-wrap items-center gap-3">
        {currentUrl ? (
          <img
            src={currentUrl}
            alt={label}
            className="h-12 w-auto rounded border border-slate-200 bg-white object-contain p-1 dark:border-slate-600 dark:bg-slate-700"
          />
        ) : (
          <div className="flex h-12 w-24 items-center justify-center rounded border border-dashed border-slate-300 text-xs text-slate-400 dark:border-slate-600">
            {t('Whitelabel.upload.empty')}
          </div>
        )}
        <input
          ref={inputRef}
          type="file"
          accept="image/png,image/jpeg,image/svg+xml,image/x-icon,image/webp"
          className="hidden"
          onChange={(e) => handleSelect(e.target.files?.[0] ?? null)}
        />
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={uploadAsset.isPending}
          className="rounded bg-info-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-info-700 disabled:opacity-60"
        >
          {uploadAsset.isPending ? t('Whitelabel.upload.uploading') : t('Whitelabel.upload.choose')}
        </button>
      </div>
      {acceptHint ? (
        <span className="text-xs text-slate-500 dark:text-slate-400">{acceptHint}</span>
      ) : null}
      {error ? <span className="text-xs text-danger-600 dark:text-danger-400">{error}</span> : null}
    </div>
  );
};
