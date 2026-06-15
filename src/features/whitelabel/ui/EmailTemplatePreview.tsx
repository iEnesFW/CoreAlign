import { useTranslation } from 'react-i18next';

interface EmailTemplatePreviewProps {
  brandName?: string | null;
  logoUrl?: string | null;
  primaryColor: string;
  bodyMarkdown: string;
}

export const EmailTemplatePreview = ({
  brandName,
  logoUrl,
  primaryColor,
  bodyMarkdown,
}: EmailTemplatePreviewProps) => {
  const { t } = useTranslation();
  return (
    <div className="rounded-md border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
      <div
        className="mb-3 flex items-center gap-3 border-b pb-2"
        style={{ borderColor: primaryColor }}
      >
        {logoUrl ? (
          <img src={logoUrl} alt={brandName ?? 'logo'} className="h-8 w-auto object-contain" />
        ) : (
          <div className="h-8 w-24 rounded bg-slate-100 dark:bg-slate-700" />
        )}
        <span className="text-sm font-semibold" style={{ color: primaryColor }}>
          {brandName ?? t('Whitelabel.preview.defaultBrand')}
        </span>
      </div>
      <pre className="whitespace-pre-wrap font-sans text-xs text-slate-700 dark:text-slate-200">
        {bodyMarkdown || t('Whitelabel.preview.empty')}
      </pre>
    </div>
  );
};
