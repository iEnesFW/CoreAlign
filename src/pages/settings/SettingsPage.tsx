import { useState } from 'react';
import { Building2, Database, Hash, Mail, Palette } from 'lucide-react';
import { CompanyProfileSection } from '@/features/settings/ui/CompanyProfileSection';
import { BrandingSection } from '@/features/settings/ui/BrandingSection';
import { EmailTemplatesSection } from '@/features/settings/ui/EmailTemplatesSection';
import { MasterDataSection } from '@/features/settings/ui/MasterDataSection';
import { NumberFormatSection } from '@/features/settings/ui/NumberFormatSection';

type Tab = 'company' | 'branding' | 'masterData' | 'numberFormat' | 'email';

const TABS: { id: Tab; label: string; icon: typeof Building2 }[] = [
  { id: 'company', label: 'Firma Bilgileri', icon: Building2 },
  { id: 'branding', label: 'Marka & Logo', icon: Palette },
  { id: 'masterData', label: 'Tanımlar', icon: Database },
  { id: 'numberFormat', label: 'Sayı Biçimi', icon: Hash },
  { id: 'email', label: 'E-posta Şablonları', icon: Mail },
];

export const SettingsPage = () => {
  const [tab, setTab] = useState<Tab>('company');

  return (
    <div className="space-y-4 p-4">
      <div>
        <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">Yönetim Paneli</h1>
        <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
          Firma bilgileri, marka, tanımlar ve bildirim şablonlarını buradan yapılandırın.
        </p>
      </div>

      <div className="flex flex-wrap gap-1 border-b border-slate-200 dark:border-slate-800">
        {TABS.map((t) => {
          const Icon = t.icon;
          const active = tab === t.id;
          return (
            <button
              key={t.id}
              type="button"
              onClick={() => setTab(t.id)}
              className={`inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition ${
                active
                  ? 'border-indigo-600 text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              <Icon size={13} />
              {t.label}
            </button>
          );
        })}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        {tab === 'company' && <CompanyProfileSection />}
        {tab === 'branding' && <BrandingSection />}
        {tab === 'masterData' && <MasterDataSection />}
        {tab === 'numberFormat' && <NumberFormatSection />}
        {tab === 'email' && <EmailTemplatesSection />}
      </div>
    </div>
  );
};

export default SettingsPage;
