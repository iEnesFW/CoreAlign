import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Upload, ImageIcon } from 'lucide-react';
import { toast } from 'sonner';
import { useCompanyProfileQuery } from '@/features/settings/hooks/useSettingsQueries';
import { useUploadTenantLogo } from '@/features/settings/hooks/useTenantBranding';

const MAX_BYTES = 1024 * 1024;
const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/svg+xml'];

export const TenantBrandingSection = () => {
  const { t } = useTranslation();
  const companyQuery = useCompanyProfileQuery();
  const uploadMutation = useUploadTenantLogo();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [dragOver, setDragOver] = useState(false);

  const logoUrl = useMemo(() => companyQuery.data?.data?.logoUrl ?? null, [companyQuery.data]);

  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    const file = files[0];
    if (!ALLOWED_TYPES.includes(file.type)) {
      toast.error(
        t('TenantBranding.invalidType', {
          defaultValue: 'Unsupported file type. Use PNG, JPG, or SVG.',
        }),
      );
      return;
    }
    if (file.size > MAX_BYTES) {
      toast.error(
        t('TenantBranding.tooLarge', {
          defaultValue: 'File exceeds {{max}} KB.',
          max: MAX_BYTES / 1024,
        }),
      );
      return;
    }
    try {
      await uploadMutation.mutateAsync(file);
      toast.success(t('TenantBranding.uploaded', { defaultValue: 'Logo updated.' }));
    } catch {
      toast.error(t('TenantBranding.uploadFailed', { defaultValue: 'Logo upload failed.' }));
    }
    if (inputRef.current) inputRef.current.value = '';
  };

  return (
    <section
      className="space-y-3 rounded-[6px] border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4"
      data-testid="tenant-branding-section"
    >
      <header>
        <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('TenantBranding.title', { defaultValue: 'Brand & logo' })}
        </h2>
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('TenantBranding.subtitle', {
            defaultValue:
              'Upload the logo that appears on invoices, the portal header, and email templates.',
          })}
        </p>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="rounded-[5px] border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/40 p-4 flex flex-col items-center justify-center text-center min-h-[120px]">
          <p className="text-[11px] text-slate-500 mb-2">
            {t('TenantBranding.current', { defaultValue: 'Current logo' })}
          </p>
          {logoUrl ? (
            <img src={logoUrl} alt="Tenant logo" className="max-h-20 max-w-full object-contain" />
          ) : (
            <div className="flex flex-col items-center gap-1 text-slate-400">
              <ImageIcon className="h-6 w-6" />
              <span className="text-[11px]">
                {t('TenantBranding.noLogo', { defaultValue: 'No logo uploaded.' })}
              </span>
            </div>
          )}
        </div>

        <div
          onDragOver={(e) => {
            e.preventDefault();
            setDragOver(true);
          }}
          onDragLeave={() => setDragOver(false)}
          onDrop={(e) => {
            e.preventDefault();
            setDragOver(false);
            void handleFiles(e.dataTransfer.files);
          }}
          className={`rounded-[5px] border-2 border-dashed p-4 flex flex-col items-center justify-center gap-2 text-center min-h-[120px] transition ${
            dragOver
              ? 'border-indigo-500 bg-indigo-50/40 dark:bg-indigo-500/10'
              : 'border-slate-200 dark:border-slate-700'
          }`}
        >
          <input
            ref={inputRef}
            type="file"
            accept={ALLOWED_TYPES.join(',')}
            className="hidden"
            onChange={(e) => void handleFiles(e.target.files)}
          />
          <Upload className="h-6 w-6 text-slate-400" />
          <p className="text-[11px] text-slate-500 dark:text-slate-400 max-w-[260px]">
            {t('TenantBranding.dragDropHint', {
              defaultValue: 'Drag a PNG, JPG, or SVG here (max {{max}} KB) or click to browse.',
              max: MAX_BYTES / 1024,
            })}
          </p>
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={uploadMutation.isPending}
            className="rounded-[5px] bg-indigo-600 text-white text-[11px] font-semibold px-3 py-1.5 hover:bg-indigo-500 disabled:opacity-60"
          >
            {uploadMutation.isPending
              ? t('TenantBranding.uploading', { defaultValue: 'Uploading…' })
              : t('TenantBranding.uploadButton', { defaultValue: 'Upload logo' })}
          </button>
        </div>
      </div>
    </section>
  );
};

export default TenantBrandingSection;
