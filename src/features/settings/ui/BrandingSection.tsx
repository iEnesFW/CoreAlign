import { useState } from 'react';
import { toast } from 'sonner';
import { Image as ImageIcon } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCompanyProfileQuery, useUpdateCompanyProfile } from '../hooks/useSettingsQueries';

export const BrandingSection = () => {
  const profile = useCompanyProfileQuery();
  const update = useUpdateCompanyProfile();
  const data = profile.data?.data;

  if (profile.isPending) return <div className="p-6 text-sm text-slate-500">Yükleniyor…</div>;
  if (!data) return <div className="p-6 text-sm text-slate-500">Yüklenemedi.</div>;

  return <BrandingForm key={data.id} initial={data} update={update} />;
};

const BrandingForm = ({
  initial,
  update,
}: {
  initial: NonNullable<NonNullable<ReturnType<typeof useCompanyProfileQuery>['data']>['data']>;
  update: ReturnType<typeof useUpdateCompanyProfile>;
}) => {
  const [logoUrl, setLogoUrl] = useState(initial.logoUrl ?? '');
  const [primaryColor, setPrimaryColor] = useState(initial.primaryColor ?? '#4f46e5');
  const [secondaryColor, setSecondaryColor] = useState(initial.secondaryColor ?? '#0ea5e9');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      // Branding edits reuse the company-profile update; we send the full
      // existing profile plus the branding changes so nothing else is wiped.
      await update.mutateAsync({
        name: initial.name,
        legalName: initial.legalName ?? null,
        tradeName: initial.tradeName ?? null,
        taxNumber: initial.taxNumber ?? null,
        taxOffice: initial.taxOffice ?? null,
        nationalId: initial.nationalId ?? null,
        mersisNumber: initial.mersisNumber ?? null,
        tradeRegistryNumber: initial.tradeRegistryNumber ?? null,
        sector: initial.sector ?? null,
        foundedOn: initial.foundedOn ?? null,
        logoUrl: logoUrl || null,
        addressLine1: initial.addressLine1 ?? null,
        addressLine2: initial.addressLine2 ?? null,
        city: initial.city ?? null,
        stateProvince: initial.stateProvince ?? null,
        postalCode: initial.postalCode ?? null,
        country: initial.country ?? null,
        phone: initial.phone ?? null,
        fax: initial.fax ?? null,
        email: initial.email ?? null,
        website: initial.website ?? null,
        defaultCurrency: initial.defaultCurrency,
        reportingCurrency: initial.reportingCurrency ?? null,
        localeCode: initial.localeCode,
        timeZoneId: initial.timeZoneId,
        fiscalYearStartMonth: initial.fiscalYearStartMonth,
        primaryColor: primaryColor || null,
        secondaryColor: secondaryColor || null,
      });
      toast.success('Marka ayarları kaydedildi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <form onSubmit={submit} className="space-y-5">
      <div>
        <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
          Logo URL
        </label>
        <input
          type="url"
          value={logoUrl}
          onChange={(e) => setLogoUrl(e.target.value)}
          placeholder="https://…/logo.png"
          maxLength={500}
          className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
        />
        <div className="mt-3 flex h-24 w-48 items-center justify-center rounded border border-dashed border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/40">
          {logoUrl ? (
            <img src={logoUrl} alt="Logo önizleme" className="max-h-20 max-w-44 object-contain" />
          ) : (
            <span className="flex flex-col items-center gap-1 text-[10px] text-slate-400">
              <ImageIcon size={20} />
              Logo önizleme
            </span>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:max-w-md">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            Birincil Renk
          </label>
          <div className="mt-1 flex items-center gap-2">
            <input
              type="color"
              value={primaryColor}
              onChange={(e) => setPrimaryColor(e.target.value)}
              className="h-9 w-12 rounded border border-slate-300 dark:border-slate-700"
            />
            <input
              type="text"
              value={primaryColor}
              onChange={(e) => setPrimaryColor(e.target.value)}
              maxLength={16}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            İkincil Renk
          </label>
          <div className="mt-1 flex items-center gap-2">
            <input
              type="color"
              value={secondaryColor}
              onChange={(e) => setSecondaryColor(e.target.value)}
              className="h-9 w-12 rounded border border-slate-300 dark:border-slate-700"
            />
            <input
              type="text"
              value={secondaryColor}
              onChange={(e) => setSecondaryColor(e.target.value)}
              maxLength={16}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
        </div>
      </div>

      <p className="text-xs text-slate-500 dark:text-slate-400">
        Bu renkler fatura/sipariş çıktılarında ve raporlarda firma markası olarak kullanılacaktır.
      </p>

      <div className="flex justify-end border-t border-slate-200 pt-3 dark:border-slate-800">
        <button
          type="submit"
          disabled={update.isPending}
          className="rounded bg-indigo-600 px-4 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {update.isPending ? 'Kaydediliyor…' : 'Kaydet'}
        </button>
      </div>
    </form>
  );
};
