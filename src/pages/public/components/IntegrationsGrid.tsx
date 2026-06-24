import { useTranslation } from 'react-i18next';
import type { LucideIcon } from 'lucide-react';
import {
  Network,
  Database,
  Cpu,
  Boxes,
  Banknote,
  ShieldCheck,
  ArrowLeftRight,
  Zap,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

type IntegrationCategory = {
  icon: LucideIcon;
  accent: string;
  iconColor: string;
  title: string;
  desc: string;
  tags: string[];
};

const HUB_SPOKES = [
  { cx: 90, cy: 48, color: 'text-primary-500' },
  { cx: 90, cy: 132, color: 'text-success-500' },
  { cx: 90, cy: 216, color: 'text-warning-500' },
  { cx: 510, cy: 48, color: 'text-info-500' },
  { cx: 510, cy: 132, color: 'text-accent-500' },
  { cx: 510, cy: 216, color: 'text-danger-500' },
] as const;

export const IntegrationsGrid = () => {
  const { t } = useTranslation();

  const categories: IntegrationCategory[] = [
    {
      icon: Network,
      accent: 'bg-primary-500/10 dark:bg-primary-500/20',
      iconColor: 'text-primary-600 dark:text-primary-400',
      title: t('LandingPage.integrations.erpLabel'),
      desc: t('LandingPage.integrations.erpDesc'),
      tags: ['Logo Tiger/Go', 'Netsis 3', 'Mikro Fly', 'SAP B1', 'MS Dynamics'],
    },
    {
      icon: Database,
      accent: 'bg-success-500/10 dark:bg-success-500/20',
      iconColor: 'text-success-600 dark:text-success-400',
      title: t('LandingPage.integrations.dbLabel'),
      desc: t('LandingPage.integrations.dbDesc'),
      tags: ['MS SQL', 'PostgreSQL', 'REST API', 'JSON Webhooks', 'OAuth 2.0'],
    },
    {
      icon: Cpu,
      accent: 'bg-warning-500/10 dark:bg-warning-500/20',
      iconColor: 'text-warning-600 dark:text-warning-400',
      title: t('LandingPage.integrations.hwLabel'),
      desc: t('LandingPage.integrations.hwDesc'),
      tags: ['Modbus TCP/IP', 'OPC UA', 'OPC DA', 'Serial RS-485', 'MQTT Broker'],
    },
    {
      icon: Banknote,
      accent: 'bg-info-500/10 dark:bg-info-500/20',
      iconColor: 'text-info-600 dark:text-info-400',
      title: t('LandingPage.integrations.financeLabel', {
        defaultValue: 'e-Belge & Finans',
      }),
      desc: t('LandingPage.integrations.financeDesc', {
        defaultValue:
          'GİB e-Fatura/e-Arşiv özel entegratörleri, banka POS mutabakatı ve cari hesap kapama akışları otomatik eşleşir.',
      }),
      tags: ['e-Fatura', 'e-Arşiv', 'e-İrsaliye', 'Banka POS', 'IBAN Mutabakat'],
    },
    {
      icon: Boxes,
      accent: 'bg-accent-500/10 dark:bg-accent-500/20',
      iconColor: 'text-accent-600 dark:text-accent-300',
      title: t('LandingPage.integrations.logisticsLabel', {
        defaultValue: 'Lojistik & Kataloglar',
      }),
      desc: t('LandingPage.integrations.logisticsDesc', {
        defaultValue:
          'Kargo takibi, tedarikçi fiyat listeleri ve cam/profil üretici katalogları CPQ motoruna canlı beslenir.',
      }),
      tags: ['Kargo API', 'Tedarikçi Katalog', 'Fiyat Listesi', 'Barkod/GTIN', 'CSV/Excel'],
    },
    {
      icon: ShieldCheck,
      accent: 'bg-danger-500/10 dark:bg-danger-500/20',
      iconColor: 'text-danger-600 dark:text-danger-400',
      title: t('LandingPage.integrations.identityLabel', {
        defaultValue: 'Kimlik & Bildirim',
      }),
      desc: t('LandingPage.integrations.identityDesc', {
        defaultValue:
          'Kurumsal SSO ile tek tıkla giriş; SMS, e-posta ve webhook kanallarından gerçek zamanlı saha bildirimleri.',
      }),
      tags: ['SAML 2.0', 'OpenID Connect', 'SMTP/IMAP', 'SMS Gateway', 'Slack/Teams'],
    },
  ];

  const stats = [
    {
      icon: ArrowLeftRight,
      value: t('LandingPage.integrations.statSyncValue', { defaultValue: 'Çift yönlü' }),
      label: t('LandingPage.integrations.statSyncLabel', {
        defaultValue: 'Senkron veri akışı',
      }),
    },
    {
      icon: Zap,
      value: t('LandingPage.integrations.statLatencyValue', { defaultValue: '< 2 sn' }),
      label: t('LandingPage.integrations.statLatencyLabel', {
        defaultValue: 'Webhook tetikleme hedefi',
      }),
    },
    {
      icon: ShieldCheck,
      value: t('LandingPage.integrations.statAuthValue', { defaultValue: 'OAuth 2.0' }),
      label: t('LandingPage.integrations.statAuthLabel', {
        defaultValue: 'Şifreli & yetkilendirilmiş',
      }),
    },
  ];

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Network size={12} aria-hidden="true" />
            {t('LandingPage.integrations.badge', { defaultValue: 'ENTEGRASYON' })}
          </>
        }
        title={t('LandingPage.integrations.title')}
        subtitle={t('LandingPage.integrations.subtitle')}
      />

      {renderHubDiagram(t)}

      <div className="mb-10 grid grid-cols-1 gap-8 ca-stagger sm:grid-cols-2 lg:grid-cols-3">
        {categories.map((cat) => (
          <article
            key={cat.title}
            className="flex flex-col justify-between rounded-3xl border border-slate-200 bg-white/50 p-6 shadow-sm backdrop-blur-sm transition-all duration-300 hover:translate-y-[-4px] hover:border-primary-500/30 hover:shadow-md dark:border-slate-800/60 dark:bg-surface-deep/50 dark:shadow-none"
          >
            <div>
              <div className={`mb-4 inline-flex rounded-2xl p-3 ${cat.accent}`}>
                <cat.icon className={`h-6 w-6 ${cat.iconColor}`} aria-hidden="true" />
              </div>
              <h3 className="mb-2 text-lg font-bold text-slate-900 dark:text-white">{cat.title}</h3>
              <p className="mb-6 text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                {cat.desc}
              </p>
            </div>

            <ul className="flex flex-wrap gap-1.5 border-t border-slate-100/50 pt-4 dark:border-slate-800/60">
              {cat.tags.map((tag) => (
                <li
                  key={tag}
                  className="rounded-lg border border-primary-500/10 bg-primary-500/5 px-2 py-1 text-[10px] font-bold text-primary-700 dark:border-primary-500/10 dark:bg-primary-500/10 dark:text-primary-300"
                >
                  {tag}
                </li>
              ))}
            </ul>
          </article>
        ))}
      </div>

      <div className="grid grid-cols-1 gap-4 rounded-3xl border border-slate-200 bg-white/40 p-6 backdrop-blur-sm sm:grid-cols-3 dark:border-slate-800/60 dark:bg-slate-900/40">
        {stats.map((stat) => (
          <div key={stat.label} className="flex items-center gap-3">
            <div className="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400">
              <stat.icon size={20} aria-hidden="true" />
            </div>
            <div>
              <p className="text-base font-bold text-slate-900 dark:text-white">{stat.value}</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">{stat.label}</p>
            </div>
          </div>
        ))}
      </div>
    </Section>
  );
};

function renderHubDiagram(t: (key: string, opts?: Record<string, string>) => string) {
  return (
    <figure className="mb-14 overflow-hidden rounded-3xl border border-slate-200 bg-white/40 p-4 backdrop-blur-sm animate-zoom-in sm:p-6 dark:border-slate-800/60 dark:bg-slate-900/40">
      <svg
        viewBox="0 0 600 264"
        className="h-auto w-full"
        role="img"
        aria-label={t('LandingPage.integrations.diagramAria', {
          defaultValue:
            'CoreAlign merkez veri yolu, dış sistemlerle çift yönlü veri akışını gösteren şema.',
        })}
      >
        <defs>
          <radialGradient id="ca-hub-glow" cx="50%" cy="50%" r="50%">
            <stop
              offset="0%"
              className="text-primary-500"
              stopColor="currentColor"
              stopOpacity="0.35"
            />
            <stop
              offset="100%"
              className="text-primary-500"
              stopColor="currentColor"
              stopOpacity="0"
            />
          </radialGradient>
        </defs>

        {HUB_SPOKES.map((spoke, idx) => {
          const path = `M ${spoke.cx} ${spoke.cy} C 300 ${spoke.cy}, 300 132, 300 132`;
          return (
            <g key={idx} className={spoke.color} aria-hidden="true">
              <path
                d={path}
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeOpacity="0.35"
                strokeLinecap="round"
              />
              <circle r="3.5" fill="currentColor">
                <animateMotion
                  dur={`${2.4 + idx * 0.35}s`}
                  repeatCount="indefinite"
                  path={path}
                  keyPoints="0;1"
                  keyTimes="0;1"
                />
                <animate
                  attributeName="opacity"
                  values="0;1;1;0"
                  dur={`${2.4 + idx * 0.35}s`}
                  repeatCount="indefinite"
                />
              </circle>
              <circle cx={spoke.cx} cy={spoke.cy} r="9" fill="currentColor" fillOpacity="0.12" />
              <circle
                cx={spoke.cx}
                cy={spoke.cy}
                r="5"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeOpacity="0.8"
              />
            </g>
          );
        })}

        <circle cx="300" cy="132" r="64" fill="url(#ca-hub-glow)" aria-hidden="true" />
        <circle
          cx="300"
          cy="132"
          r="40"
          className="text-primary-600 dark:text-primary-500"
          fill="currentColor"
          aria-hidden="true"
        >
          <animate attributeName="r" values="40;42;40" dur="3s" repeatCount="indefinite" />
        </circle>
        <text x="300" y="129" textAnchor="middle" className="fill-white text-[15px] font-extrabold">
          Core
        </text>
        <text x="300" y="146" textAnchor="middle" className="fill-white text-[15px] font-extrabold">
          Align
        </text>
      </svg>
      <figcaption className="mt-3 text-center text-xs text-slate-500 dark:text-slate-400">
        {t('LandingPage.integrations.diagramCaption', {
          defaultValue:
            'Tüm sistemler tek bir CoreAlign veri yoluna bağlanır; veri her yönde gerçek zamanlı akar.',
        })}
      </figcaption>
    </figure>
  );
}

export default IntegrationsGrid;
