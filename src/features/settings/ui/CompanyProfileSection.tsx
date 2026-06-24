import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCompanyProfileQuery, useUpdateCompanyProfile } from '../hooks/useSettingsQueries';
import type { UpdateCompanyProfileRequest } from '../model/settings.types';

const TextField = ({
  label,
  value,
  onChange,
  type = 'text',
  maxLength,
  required,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  maxLength?: number;
  required?: boolean;
  placeholder?: string;
}) => (
  <div>
    <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">{label}</label>
    <input
      type={type}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      maxLength={maxLength}
      required={required}
      placeholder={placeholder}
      className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
    />
  </div>
);

export const CompanyProfileSection = () => {
  const { t } = useTranslation();
  const profile = useCompanyProfileQuery();
  const updateMutation = useUpdateCompanyProfile();
  const data = profile.data?.data;

  if (profile.isPending)
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('CompanyProfile.Loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  if (!data)
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('CompanyProfile.LoadFailed', { defaultValue: 'Firma bilgisi yüklenemedi.' })}
      </div>
    );

  return <CompanyProfileForm key={data.id} initial={data} onSave={updateMutation} />;
};

const CompanyProfileForm = ({
  initial,
  onSave,
}: {
  initial: ReturnType<typeof useCompanyProfileQuery>['data'] extends infer T
    ? NonNullable<T extends { data: infer D } ? D : never>
    : never;
  onSave: ReturnType<typeof useUpdateCompanyProfile>;
}) => {
  const { t } = useTranslation();
  const [form, setForm] = useState<UpdateCompanyProfileRequest>({
    name: initial.name,
    legalName: initial.legalName ?? '',
    tradeName: initial.tradeName ?? '',
    taxNumber: initial.taxNumber ?? '',
    taxOffice: initial.taxOffice ?? '',
    nationalId: initial.nationalId ?? '',
    mersisNumber: initial.mersisNumber ?? '',
    tradeRegistryNumber: initial.tradeRegistryNumber ?? '',
    sector: initial.sector ?? '',
    foundedOn: initial.foundedOn ?? null,
    logoUrl: initial.logoUrl ?? '',
    addressLine1: initial.addressLine1 ?? '',
    addressLine2: initial.addressLine2 ?? '',
    city: initial.city ?? '',
    stateProvince: initial.stateProvince ?? '',
    postalCode: initial.postalCode ?? '',
    country: initial.country ?? '',
    phone: initial.phone ?? '',
    fax: initial.fax ?? '',
    email: initial.email ?? '',
    website: initial.website ?? '',
    defaultCurrency: initial.defaultCurrency,
    reportingCurrency: initial.reportingCurrency ?? '',
    localeCode: initial.localeCode,
    timeZoneId: initial.timeZoneId,
    fiscalYearStartMonth: initial.fiscalYearStartMonth,
    primaryColor: initial.primaryColor ?? '',
    secondaryColor: initial.secondaryColor ?? '',
  });

  const set = <K extends keyof UpdateCompanyProfileRequest>(
    key: K,
    value: UpdateCompanyProfileRequest[K],
  ) => setForm((prev) => ({ ...prev, [key]: value }));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await onSave.mutateAsync({
        ...form,
        reportingCurrency: form.reportingCurrency || null,
        foundedOn: form.foundedOn || null,
      });
      toast.success(
        t('CompanyProfile.SaveSuccess', { defaultValue: 'Firma bilgileri kaydedildi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <form onSubmit={submit} className="space-y-5">
      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('CompanyProfile.IdentitySection', { defaultValue: 'Kimlik' })}
        </h3>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <TextField
            label={t('CompanyProfile.CompanyName', { defaultValue: 'Firma Adı *' })}
            value={form.name}
            onChange={(v) => set('name', v)}
            required
            maxLength={150}
          />
          <TextField
            label={t('CompanyProfile.LegalName', { defaultValue: 'Ticari Ünvan' })}
            value={form.legalName ?? ''}
            onChange={(v) => set('legalName', v)}
            maxLength={200}
          />
          <TextField
            label={t('CompanyProfile.TradeName', { defaultValue: 'Marka Adı' })}
            value={form.tradeName ?? ''}
            onChange={(v) => set('tradeName', v)}
            maxLength={200}
          />
          <TextField
            label={t('CompanyProfile.TaxNumber', { defaultValue: 'Vergi No (VKN)' })}
            value={form.taxNumber ?? ''}
            onChange={(v) => set('taxNumber', v)}
            maxLength={50}
          />
          <TextField
            label={t('CompanyProfile.TaxOffice', { defaultValue: 'Vergi Dairesi' })}
            value={form.taxOffice ?? ''}
            onChange={(v) => set('taxOffice', v)}
            maxLength={100}
          />
          <TextField
            label={t('CompanyProfile.NationalId', { defaultValue: 'TC Kimlik No' })}
            value={form.nationalId ?? ''}
            onChange={(v) => set('nationalId', v)}
            maxLength={32}
          />
          <TextField
            label={t('CompanyProfile.MersisNumber', { defaultValue: 'MERSIS No' })}
            value={form.mersisNumber ?? ''}
            onChange={(v) => set('mersisNumber', v)}
            maxLength={32}
          />
          <TextField
            label={t('CompanyProfile.TradeRegistryNumber', { defaultValue: 'Ticaret Sicil No' })}
            value={form.tradeRegistryNumber ?? ''}
            onChange={(v) => set('tradeRegistryNumber', v)}
            maxLength={64}
          />
          <TextField
            label={t('CompanyProfile.Sector', { defaultValue: 'Sektör' })}
            value={form.sector ?? ''}
            onChange={(v) => set('sector', v)}
            maxLength={100}
          />
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('CompanyProfile.AddressSection', { defaultValue: 'Adres' })}
        </h3>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="sm:col-span-3">
            <TextField
              label={t('CompanyProfile.AddressLine1', { defaultValue: 'Adres Satırı 1' })}
              value={form.addressLine1 ?? ''}
              onChange={(v) => set('addressLine1', v)}
              maxLength={200}
            />
          </div>
          <div className="sm:col-span-3">
            <TextField
              label={t('CompanyProfile.AddressLine2', { defaultValue: 'Adres Satırı 2' })}
              value={form.addressLine2 ?? ''}
              onChange={(v) => set('addressLine2', v)}
              maxLength={200}
            />
          </div>
          <TextField
            label={t('CompanyProfile.City', { defaultValue: 'İl/Şehir' })}
            value={form.city ?? ''}
            onChange={(v) => set('city', v)}
            maxLength={100}
          />
          <TextField
            label={t('CompanyProfile.StateProvince', { defaultValue: 'İlçe/Eyalet' })}
            value={form.stateProvince ?? ''}
            onChange={(v) => set('stateProvince', v)}
            maxLength={100}
          />
          <TextField
            label={t('CompanyProfile.PostalCode', { defaultValue: 'Posta Kodu' })}
            value={form.postalCode ?? ''}
            onChange={(v) => set('postalCode', v)}
            maxLength={20}
          />
          <TextField
            label={t('CompanyProfile.Country', { defaultValue: 'Ülke' })}
            value={form.country ?? ''}
            onChange={(v) => set('country', v)}
            maxLength={100}
          />
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('CompanyProfile.ContactSection', { defaultValue: 'İletişim' })}
        </h3>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
          <TextField
            label={t('CompanyProfile.Phone', { defaultValue: 'Telefon' })}
            value={form.phone ?? ''}
            onChange={(v) => set('phone', v)}
            maxLength={30}
          />
          <TextField
            label={t('CompanyProfile.Fax', { defaultValue: 'Faks' })}
            value={form.fax ?? ''}
            onChange={(v) => set('fax', v)}
            maxLength={30}
          />
          <TextField
            label={t('CompanyProfile.Email', { defaultValue: 'E-posta' })}
            value={form.email ?? ''}
            onChange={(v) => set('email', v)}
            type="email"
            maxLength={256}
          />
          <TextField
            label={t('CompanyProfile.Website', { defaultValue: 'Web Sitesi' })}
            value={form.website ?? ''}
            onChange={(v) => set('website', v)}
            maxLength={500}
          />
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('CompanyProfile.LocaleFinancialSection', {
            defaultValue: 'Yerel ve Finansal Varsayılanlar',
          })}
        </h3>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
          <TextField
            label={t('CompanyProfile.DefaultCurrency', { defaultValue: 'Varsayılan Para Birimi' })}
            value={form.defaultCurrency}
            onChange={(v) => set('defaultCurrency', v.toUpperCase())}
            maxLength={3}
            required
          />
          <TextField
            label={t('CompanyProfile.ReportingCurrency', { defaultValue: 'Raporlama Para Birimi' })}
            value={form.reportingCurrency ?? ''}
            onChange={(v) => set('reportingCurrency', v.toUpperCase())}
            maxLength={3}
            placeholder={t('CompanyProfile.OptionalPlaceholder', { defaultValue: '(opsiyonel)' })}
          />
          <TextField
            label={t('CompanyProfile.LocaleCode', { defaultValue: 'Yerel Kod' })}
            value={form.localeCode}
            onChange={(v) => set('localeCode', v)}
            maxLength={10}
          />
          <TextField
            label={t('CompanyProfile.TimeZone', { defaultValue: 'Saat Dilimi' })}
            value={form.timeZoneId}
            onChange={(v) => set('timeZoneId', v)}
            maxLength={64}
          />
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('CompanyProfile.FiscalYearStartMonth', { defaultValue: 'Mali Yıl Başı (Ay)' })}
            </label>
            <input
              type="number"
              min={1}
              max={12}
              value={form.fiscalYearStartMonth}
              onChange={(e) => set('fiscalYearStartMonth', parseInt(e.target.value, 10) || 1)}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
        </div>
      </section>

      <div className="flex justify-end border-t border-slate-200 pt-3 dark:border-slate-800">
        <button
          type="submit"
          disabled={onSave.isPending}
          className="rounded bg-primary-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-primary-700 disabled:opacity-50"
        >
          {onSave.isPending
            ? t('CompanyProfile.Saving', { defaultValue: 'Kaydediliyor…' })
            : t('CompanyProfile.Save', { defaultValue: 'Kaydet' })}
        </button>
      </div>
    </form>
  );
};
