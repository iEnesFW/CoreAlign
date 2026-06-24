import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useTenantThemeQuery, useUpdateTenantTheme } from '../hooks/useTenantTheme';
import type { UpdateTenantThemeInput } from '../model/whitelabel.types';
import { ColorPicker } from './ColorPicker';
import { LogoUpload } from './LogoUpload';
import { EmailTemplatePreview } from './EmailTemplatePreview';

const DEFAULT_FORM: UpdateTenantThemeInput = {
  primaryColor: '#0EA5E9',
  accentColor: '#22D3EE',
  brandName: '',
  customSubdomain: '',
  customDomain: '',
  emailFromName: 'CoreAlign',
  emailFromAddress: '',
  loginHeadingMd: '',
};

export const ThemeEditor = () => {
  const { t } = useTranslation();
  const themeQuery = useTenantThemeQuery();
  const updateTheme = useUpdateTenantTheme();
  const [form, setForm] = useState<UpdateTenantThemeInput>(DEFAULT_FORM);
  const [syncedData, setSyncedData] = useState(themeQuery.data);

  if (themeQuery.data && themeQuery.data !== syncedData) {
    setSyncedData(themeQuery.data);
    setForm({
      primaryColor: themeQuery.data.primaryColor,
      accentColor: themeQuery.data.accentColor,
      brandName: themeQuery.data.brandName ?? '',
      customSubdomain: themeQuery.data.customSubdomain ?? '',
      customDomain: themeQuery.data.customDomain ?? '',
      emailFromName: themeQuery.data.emailFromName,
      emailFromAddress: themeQuery.data.emailFromAddress ?? '',
      loginHeadingMd: themeQuery.data.loginHeadingMd ?? '',
    });
  }

  const update = <K extends keyof UpdateTenantThemeInput>(
    key: K,
    value: UpdateTenantThemeInput[K],
  ) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    updateTheme.mutate(form);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <section className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <LogoUpload
          kind="Logo"
          currentUrl={themeQuery.data?.logoUrl}
          label={t('Whitelabel.fields.logo')}
          acceptHint={t('Whitelabel.upload.acceptImage')}
        />
        <LogoUpload
          kind="Favicon"
          currentUrl={themeQuery.data?.faviconUrl}
          label={t('Whitelabel.fields.favicon')}
          acceptHint={t('Whitelabel.upload.acceptFavicon')}
        />
        <LogoUpload
          kind="LoginBackground"
          currentUrl={themeQuery.data?.loginBackgroundUrl}
          label={t('Whitelabel.fields.loginBackground')}
          acceptHint={t('Whitelabel.upload.acceptImage')}
        />
      </section>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <ColorPicker
          label={t('Whitelabel.fields.primaryColor')}
          value={form.primaryColor}
          onChange={(v) => update('primaryColor', v)}
        />
        <ColorPicker
          label={t('Whitelabel.fields.accentColor')}
          value={form.accentColor}
          onChange={(v) => update('accentColor', v)}
        />
      </section>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.brandName')}
          </span>
          <input
            type="text"
            value={form.brandName ?? ''}
            onChange={(e) => update('brandName', e.target.value)}
            className="h-9 rounded border border-slate-300 px-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            maxLength={200}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.emailFromName')}
          </span>
          <input
            type="text"
            value={form.emailFromName}
            onChange={(e) => update('emailFromName', e.target.value)}
            className="h-9 rounded border border-slate-300 px-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            maxLength={200}
            required
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.emailFromAddress')}
          </span>
          <input
            type="email"
            value={form.emailFromAddress ?? ''}
            onChange={(e) => update('emailFromAddress', e.target.value)}
            className="h-9 rounded border border-slate-300 px-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            maxLength={320}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.customSubdomain')}
          </span>
          <input
            type="text"
            value={form.customSubdomain ?? ''}
            onChange={(e) => update('customSubdomain', e.target.value.toLowerCase())}
            className="h-9 rounded border border-slate-300 px-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            placeholder="tenant"
            maxLength={64}
          />
          <span className="text-xs text-slate-500 dark:text-slate-400">
            {t('Whitelabel.fields.customSubdomainHint')}
          </span>
        </label>

        <label className="flex flex-col gap-1 text-sm md:col-span-2">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.customDomain')}
          </span>
          <input
            type="text"
            value={form.customDomain ?? ''}
            onChange={(e) => update('customDomain', e.target.value.toLowerCase())}
            className="h-9 rounded border border-slate-300 px-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            placeholder="erp.example.com"
            maxLength={255}
          />
          <span className="text-xs text-slate-500 dark:text-slate-400">
            {t('Whitelabel.fields.customDomainHint')}
          </span>
        </label>

        <label className="flex flex-col gap-1 text-sm md:col-span-2">
          <span className="font-medium text-slate-700 dark:text-slate-200">
            {t('Whitelabel.fields.loginHeading')}
          </span>
          <textarea
            value={form.loginHeadingMd ?? ''}
            onChange={(e) => update('loginHeadingMd', e.target.value)}
            className="min-h-[80px] rounded border border-slate-300 p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            maxLength={2000}
          />
        </label>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-200">
          {t('Whitelabel.preview.title')}
        </h3>
        <EmailTemplatePreview
          brandName={form.brandName}
          logoUrl={themeQuery.data?.logoUrl}
          primaryColor={form.primaryColor}
          bodyMarkdown={t('Whitelabel.preview.sampleBody')}
        />
      </section>

      <div className="flex items-center justify-end gap-3">
        {updateTheme.isError ? (
          <span className="text-sm text-danger-600 dark:text-danger-400">
            {t('Whitelabel.save.error')}
          </span>
        ) : null}
        {updateTheme.isSuccess ? (
          <span className="text-sm text-success-600 dark:text-success-400">
            {t('Whitelabel.save.success')}
          </span>
        ) : null}
        <button
          type="submit"
          disabled={updateTheme.isPending}
          className="rounded bg-info-600 px-4 py-2 text-sm font-medium text-white hover:bg-info-700 disabled:opacity-60"
        >
          {updateTheme.isPending ? t('Whitelabel.save.saving') : t('Whitelabel.save.submit')}
        </button>
      </div>
    </form>
  );
};
