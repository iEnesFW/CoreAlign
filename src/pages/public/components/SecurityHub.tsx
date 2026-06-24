import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import {
  Shield,
  Lock,
  Server,
  ShieldCheck,
  KeyRound,
  ScrollText,
  Users,
  Activity,
  CheckCircle2,
  Target,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

type SecurityTab = 'crypto' | 'tenant' | 'audit' | 'cert';

type SpecRow = { key: string; value: string };

const SpecCard = ({ title, rows }: { title: string; rows: SpecRow[] }) => (
  <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
    <div className="rounded-xl border border-slate-100 bg-slate-50/50 p-3 dark:border-slate-800 dark:bg-slate-900/40">
      <div className="mb-2 font-bold text-slate-700 dark:text-white">{title}</div>
      {rows.map((row, idx) => (
        <div
          key={row.key}
          className={`flex justify-between gap-3 ${
            idx < rows.length - 1 ? 'border-b border-slate-100 dark:border-slate-800' : ''
          } ${idx === 0 ? 'pb-1.5' : idx === rows.length - 1 ? 'pt-1.5' : 'py-1.5'}`}
        >
          <span className="shrink-0">{row.key}</span>
          <span className="text-right font-bold">{row.value}</span>
        </div>
      ))}
    </div>
  </div>
);

const ShieldTelemetry = ({ label }: { label: string }) => (
  <div
    className="relative mx-auto flex aspect-square w-full max-w-[260px] items-center justify-center"
    aria-hidden="true"
  >
    <svg viewBox="0 0 200 200" className="h-full w-full">
      <defs>
        <radialGradient id="ca-sec-core" cx="50%" cy="42%" r="60%">
          <stop offset="0%" stopColor="var(--color-primary-400)" stopOpacity="0.9" />
          <stop offset="100%" stopColor="var(--color-primary-700)" stopOpacity="0.85" />
        </radialGradient>
        <linearGradient id="ca-sec-sweep" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="var(--color-accent-400)" stopOpacity="0" />
          <stop offset="100%" stopColor="var(--color-accent-400)" stopOpacity="0.55" />
        </linearGradient>
      </defs>

      <circle
        cx="100"
        cy="100"
        r="80"
        fill="none"
        stroke="var(--color-primary-400)"
        strokeWidth="1"
        strokeOpacity="0.25"
      >
        <animate attributeName="r" values="60;88;60" dur="3.6s" repeatCount="indefinite" />
        <animate
          attributeName="stroke-opacity"
          values="0.35;0;0.35"
          dur="3.6s"
          repeatCount="indefinite"
        />
      </circle>
      <circle
        cx="100"
        cy="100"
        r="70"
        fill="none"
        stroke="var(--color-accent-400)"
        strokeWidth="1"
        strokeOpacity="0.2"
      >
        <animate
          attributeName="r"
          values="55;82;55"
          dur="3.6s"
          begin="1.2s"
          repeatCount="indefinite"
        />
        <animate
          attributeName="stroke-opacity"
          values="0.3;0;0.3"
          dur="3.6s"
          begin="1.2s"
          repeatCount="indefinite"
        />
      </circle>

      <g className="origin-center" style={{ transformBox: 'fill-box' }}>
        <path
          d="M100 20 L60 100 A60 60 0 0 0 100 140 A60 60 0 0 0 140 100 Z"
          fill="url(#ca-sec-sweep)"
        >
          <animateTransform
            attributeName="transform"
            type="rotate"
            from="0 100 100"
            to="360 100 100"
            dur="4s"
            repeatCount="indefinite"
          />
        </path>
      </g>

      <path
        d="M100 36 L150 56 V104 C150 134 128 156 100 166 C72 156 50 134 50 104 V56 Z"
        fill="url(#ca-sec-core)"
        stroke="var(--color-primary-300)"
        strokeWidth="1.5"
        strokeOpacity="0.6"
      />
      <path
        d="M82 100 L95 114 L120 84"
        fill="none"
        stroke="#ffffff"
        strokeWidth="6"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeDasharray="60"
        strokeDashoffset="60"
      >
        <animate
          attributeName="stroke-dashoffset"
          values="60;0"
          dur="1s"
          begin="0.3s"
          fill="freeze"
        />
      </path>

      {[0, 1, 2, 3, 4, 5].map((i) => {
        const angle = (i / 6) * Math.PI * 2;
        const cx = 100 + Math.cos(angle) * 86;
        const cy = 100 + Math.sin(angle) * 86;
        return (
          <circle key={i} cx={cx} cy={cy} r="3" fill="var(--color-success-400)">
            <animate
              attributeName="opacity"
              values="0.2;1;0.2"
              dur="2.4s"
              begin={`${i * 0.4}s`}
              repeatCount="indefinite"
            />
          </circle>
        );
      })}
    </svg>
    <span className="absolute -bottom-1 left-1/2 -translate-x-1/2 whitespace-nowrap rounded-full border border-success-500/30 bg-success-500/10 px-2.5 py-1 text-[10px] font-semibold text-success-700 dark:text-success-300">
      {label}
    </span>
  </div>
);

const buildTabs = (t: TFunction) => [
  {
    id: 'crypto' as SecurityTab,
    icon: <Lock size={16} />,
    label: t('LandingPage.security.tabCrypto'),
    desc: t('LandingPage.security.tabCryptoDesc'),
    details: (
      <SpecCard
        title={t('LandingPage.security.cryptoSpecTitle', {
          defaultValue: 'Şifreleme anahtar yönetimi',
        })}
        rows={[
          {
            key: t('LandingPage.security.cryptoRow1Key', { defaultValue: 'ALGORİTMA' }),
            value: t('LandingPage.security.cryptoRow1Val', {
              defaultValue: 'AES-256-GCM (durağan + iletim)',
            }),
          },
          {
            key: t('LandingPage.security.cryptoRow2Key', { defaultValue: 'ANAHTAR DÖNGÜSÜ' }),
            value: t('LandingPage.security.cryptoRow2Val', {
              defaultValue: 'Key Vault ile periyodik rotasyon',
            }),
          },
          {
            key: t('LandingPage.security.cryptoRow3Key', { defaultValue: 'ŞİFRELİ ALANLAR' }),
            value: t('LandingPage.security.cryptoRow3Val', {
              defaultValue: 'Vergi no, kredi limiti, banka bilgisi, API anahtarı',
            }),
          },
        ]}
      />
    ),
  },
  {
    id: 'tenant' as SecurityTab,
    icon: <Server size={16} />,
    label: t('LandingPage.security.tabTenant'),
    desc: t('LandingPage.security.tabTenantDesc'),
    details: (
      <SpecCard
        title={t('LandingPage.security.tenantSpecTitle', {
          defaultValue: 'Kiracı izolasyon katmanları',
        })}
        rows={[
          {
            key: t('LandingPage.security.tenantRow1Key', { defaultValue: 'STRATEJİ' }),
            value: t('LandingPage.security.tenantRow1Val', {
              defaultValue: 'Mantıksal kiracı kısıtları',
            }),
          },
          {
            key: t('LandingPage.security.tenantRow2Key', { defaultValue: 'SORGU FİLTRESİ' }),
            value: t('LandingPage.security.tenantRow2Val', {
              defaultValue: 'Her sorguda otomatik TenantId filtresi',
            }),
          },
          {
            key: t('LandingPage.security.tenantRow3Key', { defaultValue: 'SIZINTI ÖNLEME' }),
            value: t('LandingPage.security.tenantRow3Val', {
              defaultValue: 'Kiracıya özel önbellek + RLS savunma derinliği',
            }),
          },
        ]}
      />
    ),
  },
  {
    id: 'audit' as SecurityTab,
    icon: <Shield size={16} />,
    label: t('LandingPage.security.tabAudit'),
    desc: t('LandingPage.security.tabAuditDesc'),
    details: (
      <SpecCard
        title={t('LandingPage.security.auditSpecTitle', {
          defaultValue: 'Denetim izi tasarımı',
        })}
        rows={[
          {
            key: t('LandingPage.security.auditRow1Key', { defaultValue: 'KAYIT' }),
            value: t('LandingPage.security.auditRow1Val', {
              defaultValue: 'Değişiklik takibi + aktör/kiracı damgası',
            }),
          },
          {
            key: t('LandingPage.security.auditRow2Key', { defaultValue: 'BÜTÜNLÜK' }),
            value: t('LandingPage.security.auditRow2Val', {
              defaultValue: 'Yalnız-ekleme (append-only) + hash zinciri',
            }),
          },
          {
            key: t('LandingPage.security.auditRow3Key', { defaultValue: 'SAKLAMA' }),
            value: t('LandingPage.security.auditRow3Val', {
              defaultValue: 'Yapılandırılabilir saklama + soğuk arşiv',
            }),
          },
        ]}
      />
    ),
  },
  {
    id: 'cert' as SecurityTab,
    icon: <ShieldCheck size={16} />,
    label: t('LandingPage.security.tabCert'),
    desc: t('LandingPage.security.tabCertDesc'),
    details: (
      <div className="space-y-4 font-mono text-[10px] text-slate-500 dark:text-slate-400">
        <div className="rounded-xl border border-slate-100 bg-slate-50/50 p-3 dark:border-slate-800 dark:bg-slate-900/40">
          <div className="mb-2 font-bold text-slate-700 dark:text-white">
            {t('LandingPage.security.certSpecTitle', {
              defaultValue: 'Hedeflenen uyum çerçevesi',
            })}
          </div>
          <div className="grid grid-cols-2 gap-2 text-center text-[9px] font-bold">
            {[
              t('LandingPage.security.certIso', { defaultValue: 'ISO 27001 HEDEF' }),
              t('LandingPage.security.certGdpr', { defaultValue: 'GDPR & KVKK İLKELERİ' }),
              t('LandingPage.security.certSoc', { defaultValue: 'SOC 2 HAZIRLIK' }),
              t('LandingPage.security.certSla', { defaultValue: 'YÜKSEK ERİŞİLEBİLİRLİK SLA' }),
            ].map((badge) => (
              <span
                key={badge}
                className="rounded bg-primary-500/10 p-2 text-primary-600 dark:text-primary-300"
              >
                {badge}
              </span>
            ))}
          </div>
        </div>
      </div>
    ),
  },
];

const buildControls = (t: TFunction) => [
  {
    icon: KeyRound,
    title: t('LandingPage.security.ctrlCryptoTitle', { defaultValue: 'Uçtan uca şifreleme' }),
    desc: t('LandingPage.security.ctrlCryptoDesc', {
      defaultValue:
        'Hassas finansal ve kişisel alanlar AES-256 ile durağan ve iletim halinde şifrelenir; anahtarlar uygulamadan ayrı tutulur.',
    }),
    color: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-300',
  },
  {
    icon: Server,
    title: t('LandingPage.security.ctrlTenantTitle', { defaultValue: 'Kiracı izolasyonu' }),
    desc: t('LandingPage.security.ctrlTenantDesc', {
      defaultValue:
        'Her sorgu otomatik TenantId filtresine tabidir; satır seviyesi güvenlik (RLS) ikinci bir savunma katmanı olarak hedeflenir.',
    }),
    color: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
  },
  {
    icon: ScrollText,
    title: t('LandingPage.security.ctrlAuditTitle', { defaultValue: 'Değişmez denetim izi' }),
    desc: t('LandingPage.security.ctrlAuditDesc', {
      defaultValue:
        'Para, stok ve yetki değişiklikleri kim/ne zaman/eski-yeni olarak yalnız-ekleme bir günlüğe kaydedilir.',
    }),
    color: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-300',
  },
  {
    icon: Users,
    title: t('LandingPage.security.ctrlRbacTitle', { defaultValue: 'Rol tabanlı erişim (RBAC)' }),
    desc: t('LandingPage.security.ctrlRbacDesc', {
      defaultValue:
        'Yetkiler policy tabanlı tanımlanır; her uç nokta varsayılan olarak korumalıdır, en az ayrıcalık ilkesi uygulanır.',
    }),
    color: 'bg-warning-500/10 text-warning-600 dark:bg-warning-500/20 dark:text-warning-300',
  },
];

export const SecurityHub = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<SecurityTab>('crypto');

  const tabs = buildTabs(t);
  const controls = buildControls(t);
  const currentTab = tabs.find((tab) => tab.id === activeTab) || tabs[0];

  return (
    <Section>
      <div className="animate-fade-up">
        <SectionHeader
          eyebrow={
            <>
              <Shield size={12} />
              {t('LandingPage.security.badge', { defaultValue: 'GÜVENLİK' })}
            </>
          }
          title={t('LandingPage.security.title')}
          subtitle={t('LandingPage.security.subtitle')}
        />
        <p className="mb-10 inline-flex max-w-2xl items-center gap-2 rounded-xl border border-info-500/30 bg-info-500/10 px-3.5 py-2 text-xs font-medium text-info-700 dark:text-info-300">
          <Target size={14} className="shrink-0" />
          {t('LandingPage.security.preLaunchNote', {
            defaultValue:
              'CoreAlign lansman öncesindedir. Aşağıdaki kontroller hedeflediğimiz güvenlik standartlarını ve mimari ilkeleri tanımlar; sertifikasyon süreçleri devam etmektedir.',
          })}
        </p>

        <div className="mb-10 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4 ca-stagger">
          {controls.map((control) => {
            const Icon = control.icon;
            return (
              <div
                key={control.title}
                className="group flex flex-col rounded-2xl border border-slate-200 bg-white/50 p-5 shadow-sm backdrop-blur-sm transition-all duration-300 hover:-translate-y-0.5 hover:border-primary-500/40 hover:shadow-md dark:border-slate-800 dark:bg-slate-900/40"
              >
                <div
                  className={`mb-4 inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110 ${control.color}`}
                >
                  <Icon size={18} />
                </div>
                <h3 className="mb-1.5 text-sm font-bold text-slate-900 dark:text-white">
                  {control.title}
                </h3>
                <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                  {control.desc}
                </p>
              </div>
            );
          })}
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
          <div className="flex flex-col items-center justify-center rounded-3xl border border-slate-200 bg-white/60 p-6 shadow-sm backdrop-blur-sm dark:border-slate-800/80 dark:bg-surface-deep/65 lg:col-span-4">
            <ShieldTelemetry
              label={t('LandingPage.security.shieldLabel', {
                defaultValue: 'İzolasyon aktif',
              })}
            />
            <div className="mt-6 grid w-full grid-cols-2 gap-3 text-center">
              <div className="rounded-xl border border-slate-100 bg-slate-50/60 p-3 dark:border-slate-800 dark:bg-slate-900/40">
                <div className="flex items-center justify-center gap-1 text-base font-extrabold text-success-600 dark:text-success-400">
                  <Activity size={14} className="animate-pulse-soft" />
                  AES-256
                </div>
                <div className="mt-0.5 text-[10px] font-medium text-slate-500 dark:text-slate-400">
                  {t('LandingPage.security.kpiEncryption', { defaultValue: 'Veri şifreleme' })}
                </div>
              </div>
              <div className="rounded-xl border border-slate-100 bg-slate-50/60 p-3 dark:border-slate-800 dark:bg-slate-900/40">
                <div className="flex items-center justify-center gap-1 text-base font-extrabold text-primary-600 dark:text-primary-400">
                  <CheckCircle2 size={14} />
                  RLS
                </div>
                <div className="mt-0.5 text-[10px] font-medium text-slate-500 dark:text-slate-400">
                  {t('LandingPage.security.kpiIsolation', { defaultValue: 'Satır seviyesi' })}
                </div>
              </div>
            </div>
          </div>

          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800/80 dark:bg-surface-deep/65 lg:col-span-8">
            <div
              className="flex flex-wrap gap-2 border-b border-slate-100 pb-4 dark:border-slate-800"
              role="tablist"
              aria-label={t('LandingPage.security.tablistLabel', {
                defaultValue: 'Güvenlik kontrolleri',
              })}
            >
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  aria-selected={activeTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-semibold transition-all duration-300 ${
                    activeTab === tab.id
                      ? 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400'
                      : 'text-slate-500 hover:bg-slate-50 dark:hover:bg-slate-900/60'
                  }`}
                >
                  {tab.icon}
                  {tab.label}
                </button>
              ))}
            </div>

            <div className="grid grid-cols-1 items-start gap-8 pt-6 md:grid-cols-12">
              <div key={currentTab.id} className="space-y-4 animate-fade-in md:col-span-7">
                <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                  {currentTab.label}
                </h3>
                <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                  {currentTab.desc}
                </p>
              </div>
              <div className="md:col-span-5">
                <div className="rounded-2xl border border-slate-100 bg-slate-50/50 p-4 dark:border-slate-800 dark:bg-[#0a0f18]/60">
                  <span className="mb-3 inline-block text-[9px] font-extrabold uppercase tracking-widest text-primary-500">
                    {t('LandingPage.security.specLabel', {
                      defaultValue: 'Mimari tasarım ilkeleri',
                    })}
                  </span>
                  {currentTab.details}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Section>
  );
};

export default SecurityHub;
