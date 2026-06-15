import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield, Lock, Server, ShieldCheck } from 'lucide-react';

type SecurityTab = 'crypto' | 'tenant' | 'audit' | 'cert';

export const SecurityHub = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<SecurityTab>('crypto');

  const tabs = [
    {
      id: 'crypto' as SecurityTab,
      icon: <Lock size={16} />,
      label: t('LandingPage.security.tabCrypto'),
      desc: t('LandingPage.security.tabCryptoDesc'),
      details: (
        <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
          <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 dark:border-slate-800 dark:bg-slate-900/40">
            <div className="font-bold text-slate-700 dark:text-white mb-2">
              Cryptographic Keys Normalization
            </div>
            <div className="flex justify-between border-b border-slate-100 pb-1.5 dark:border-slate-800">
              <span>ALGORITHM:</span>
              <span className="font-bold">AES-256-GCM (En-Route & At-Rest)</span>
            </div>
            <div className="flex justify-between border-b border-slate-100 py-1.5 dark:border-slate-800">
              <span>KEY_ROTATION:</span>
              <span className="font-bold">Auto 90-Day Cycle via Key Vault</span>
            </div>
            <div className="flex justify-between pt-1.5">
              <span>ENCRYPTED_FIELDS:</span>
              <span className="font-bold">TaxNumber, CreditLimit, BankDetails, ApiKeys</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'tenant' as SecurityTab,
      icon: <Server size={16} />,
      label: t('LandingPage.security.tabTenant'),
      desc: t('LandingPage.security.tabTenantDesc'),
      details: (
        <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
          <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 dark:border-slate-800 dark:bg-slate-900/40">
            <div className="font-bold text-slate-700 dark:text-white mb-2">
              Database Schema Tenant Separation
            </div>
            <div className="flex justify-between border-b border-slate-100 pb-1.5 dark:border-slate-800">
              <span>ISOLATION_STRATEGY:</span>
              <span className="font-bold">Logical Tenant Filter Constraints</span>
            </div>
            <div className="flex justify-between border-b border-slate-100 py-1.5 dark:border-slate-800">
              <span>QUERY_FILTER:</span>
              <span className="font-bold">
                EF Core Global Query Filters (TenantId = CurrentTenant)
              </span>
            </div>
            <div className="flex justify-between pt-1.5">
              <span>CROSS_LEAK_PREVENTION:</span>
              <span className="font-bold">Tenant-scoped Memory Cache & IDP Isolation</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'audit' as SecurityTab,
      icon: <Shield size={16} />,
      label: t('LandingPage.security.tabAudit'),
      desc: t('LandingPage.security.tabAuditDesc'),
      details: (
        <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
          <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 dark:border-slate-800 dark:bg-slate-900/40">
            <div className="font-bold text-slate-700 dark:text-white mb-2">
              SOC 2 Administrative Activity Logs
            </div>
            <div className="flex justify-between border-b border-slate-100 pb-1.5 dark:border-slate-800">
              <span>AUDIT_LOGGER:</span>
              <span className="font-bold">DbChangeTracker Audit Store</span>
            </div>
            <div className="flex justify-between border-b border-slate-100 py-1.5 dark:border-slate-800">
              <span>MUTABILITY:</span>
              <span className="font-bold">Write-Once-Read-Many (WORM) Storage Logs</span>
            </div>
            <div className="flex justify-between pt-1.5">
              <span>RETENTION_POLICY:</span>
              <span className="font-bold">7 Years Cold Storage (Archive Backups)</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'cert' as SecurityTab,
      icon: <ShieldCheck size={16} />,
      label: t('LandingPage.security.tabCert'),
      desc: t('LandingPage.security.tabCertDesc'),
      details: (
        <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
          <div className="rounded-xl border border-slate-100 p-3 bg-slate-50/50 dark:border-slate-800 dark:bg-slate-900/40">
            <div className="font-bold text-slate-700 dark:text-white mb-2">
              Compliance Grid & SLAs
            </div>
            <div className="grid grid-cols-2 gap-2 text-center text-[9px] font-bold">
              <span className="rounded bg-indigo-500/10 p-2 text-indigo-650 dark:text-indigo-400">
                ISO 27001 COMPLIANT
              </span>
              <span className="rounded bg-indigo-500/10 p-2 text-indigo-650 dark:text-indigo-400">
                GDPR & KVKK READY
              </span>
              <span className="rounded bg-indigo-500/10 p-2 text-indigo-650 dark:text-indigo-400">
                SOC 2 TYPE II SECURE
              </span>
              <span className="rounded bg-indigo-500/10 p-2 text-indigo-650 dark:text-indigo-400">
                99.99% UPTIME SLA
              </span>
            </div>
          </div>
        </div>
      ),
    },
  ];

  const currentTab = tabs.find((t) => t.id === activeTab) || tabs[0];

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-4xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Shield size={12} />
            GÜVENLİK
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.security.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.security.subtitle')}
          </p>
        </div>

        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800/80 dark:bg-[#0f1524]/65">
          <div className="flex flex-wrap gap-2 border-b border-slate-100 pb-4 dark:border-slate-800">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-semibold transition-all duration-300 ${
                  activeTab === tab.id
                    ? 'bg-indigo-500/10 text-indigo-650 dark:bg-indigo-500/20 dark:text-indigo-400'
                    : 'text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-900/60'
                }`}
              >
                {tab.icon}
                {tab.label}
              </button>
            ))}
          </div>

          <div className="grid grid-cols-1 gap-8 pt-6 md:grid-cols-12 items-start">
            <div className="md:col-span-7 space-y-4">
              <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                {currentTab.label}
              </h3>
              <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                {currentTab.desc}
              </p>
            </div>
            <div className="md:col-span-5">
              <div className="rounded-2xl border border-slate-100 p-4 bg-slate-50/50 dark:border-slate-850 dark:bg-[#0a0f18]/60">
                <span className="mb-3 inline-block text-[9px] font-extrabold uppercase tracking-widest text-indigo-500">
                  Infrastructure Telemetry Specs
                </span>
                {currentTab.details}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
