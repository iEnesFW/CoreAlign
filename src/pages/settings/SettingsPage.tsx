import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Building2,
  Compass,
  Database,
  Hash,
  Mail,
  Network,
  Palette,
  Settings as SettingsIcon,
} from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { CompanyProfileSection } from '@/features/settings/ui/CompanyProfileSection';
import { BrandingSection } from '@/features/settings/ui/BrandingSection';
import { EmailTemplatesSection } from '@/features/settings/ui/EmailTemplatesSection';
import { MasterDataSection } from '@/features/settings/ui/MasterDataSection';
import { NumberFormatSection } from '@/features/settings/ui/NumberFormatSection';
import { GLPostingMapSection } from '@/features/settings/ui/GLPostingMapSection';
import { OnboardingSettingsSection } from '@/features/onboarding/ui/OnboardingSettingsSection';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';

type Tab =
  | 'company'
  | 'branding'
  | 'masterData'
  | 'numberFormat'
  | 'glPostingMap'
  | 'email'
  | 'onboarding';

export const SettingsPage = () => {
  const { t } = useTranslation();
  const isTenantAdmin = useIsTenantAdmin();
  const [tab, setTab] = useState<Tab>('company');

  const TABS: { id: Tab; label: string; icon: typeof Building2 }[] = [
    {
      id: 'company',
      label: t('Settings.tabs.company', { defaultValue: 'Firma Bilgileri' }),
      icon: Building2,
    },
    {
      id: 'branding',
      label: t('Settings.tabs.branding', { defaultValue: 'Marka & Logo' }),
      icon: Palette,
    },
    {
      id: 'masterData',
      label: t('Settings.tabs.masterData', { defaultValue: 'Tanımlar' }),
      icon: Database,
    },
    {
      id: 'numberFormat',
      label: t('Settings.tabs.numberFormat', { defaultValue: 'Sayı Biçimi' }),
      icon: Hash,
    },
    ...(isTenantAdmin
      ? [
          {
            id: 'glPostingMap' as const,
            label: t('Settings.tabs.glPostingMap', { defaultValue: 'GL Eşleştirme' }),
            icon: Network,
          },
        ]
      : []),
    {
      id: 'email',
      label: t('Settings.tabs.email', { defaultValue: 'E-posta Şablonları' }),
      icon: Mail,
    },
    {
      id: 'onboarding',
      label: t('Onboarding.Settings.Title', { defaultValue: 'Onboarding Tur' }),
      icon: Compass,
    },
  ];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<SettingsIcon size={20} />}
          title={t('Settings.title', { defaultValue: 'Yönetim Paneli' })}
          subtitle={t('Settings.subtitle', {
            defaultValue:
              'Firma bilgileri, marka, tanımlar ve bildirim şablonlarını buradan yapılandırın.',
          })}
        />
      }
    >
      <div className="flex flex-wrap gap-1 border-b border-slate-200 dark:border-slate-800">
        {TABS.map((item) => {
          const Icon = item.icon;
          const active = tab === item.id;
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              className={`inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500/40 ${
                active
                  ? 'border-primary-600 text-primary-700 dark:border-primary-400 dark:text-primary-300'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              <Icon size={13} />
              {item.label}
            </button>
          );
        })}
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900 sm:p-5">
        {tab === 'company' && <CompanyProfileSection />}
        {tab === 'branding' && <BrandingSection />}
        {tab === 'masterData' && <MasterDataSection />}
        {tab === 'numberFormat' && <NumberFormatSection />}
        {tab === 'glPostingMap' && isTenantAdmin && <GLPostingMapSection />}
        {tab === 'email' && <EmailTemplatesSection />}
        {tab === 'onboarding' && <OnboardingSettingsSection />}
      </div>
    </ListPageTemplate>
  );
};

export default SettingsPage;
