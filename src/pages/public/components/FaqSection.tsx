import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ChevronDown,
  HelpCircle,
  Plug,
  PenTool,
  ShieldCheck,
  Cpu,
  Wallet,
  DatabaseBackup,
  Languages,
  ServerCog,
  MessageCircleQuestion,
  ArrowRight,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { Section, SectionHeader } from './Section';

type FaqCategory = {
  key: string;
  label: string;
  color: string;
};

type FaqItem = {
  q: string;
  a: string;
  icon: LucideIcon;
  category: FaqCategory;
};

export const FaqSection = () => {
  const { t } = useTranslation();
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  const categories = {
    integration: {
      key: 'integration',
      label: t('LandingPage.faq.catIntegration', { defaultValue: 'Entegrasyon' }),
      color: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-300',
    },
    design: {
      key: 'design',
      label: t('LandingPage.faq.catDesign', { defaultValue: 'Tasarım & Üretim' }),
      color: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
    },
    security: {
      key: 'security',
      label: t('LandingPage.faq.catSecurity', { defaultValue: 'Güvenlik' }),
      color: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-300',
    },
    commercial: {
      key: 'commercial',
      label: t('LandingPage.faq.catCommercial', { defaultValue: 'Ticari' }),
      color: 'bg-info-500/10 text-info-600 dark:bg-info-500/20 dark:text-info-300',
    },
    data: {
      key: 'data',
      label: t('LandingPage.faq.catData', { defaultValue: 'Veri & Taşıma' }),
      color: 'bg-warning-500/10 text-warning-600 dark:bg-warning-500/20 dark:text-warning-300',
    },
  } as const;

  const faqItems: FaqItem[] = [
    {
      q: t('LandingPage.faq.q5', {
        defaultValue: 'Fiyatlandırma modeli nasıl işliyor? Gizli maliyet var mı?',
      }),
      a: t('LandingPage.faq.a5', {
        defaultValue:
          'CoreAlign abonelik (SaaS) modeliyle çalışır: aktif kullanıcı ve kullandığınız modüller üzerinden aylık veya yıllık ödenir. Kurulum, bakım ve sürüm güncellemeleri abonelik ücretine dahildir; donanım veya lisans sunucusu satın almanız gerekmez. Modülleri ihtiyaç duydukça açar, büyüdükçe ölçeklersiniz. Sözleşmede satır satır şeffaf kalemler yer alır — sürpriz kurulum veya kullanım bedeli çıkarmıyoruz.',
      }),
      icon: Wallet,
      category: categories.commercial,
    },
    {
      q: t('LandingPage.faq.a_dataOwnerQ', {
        defaultValue: 'Verilerimin sahibi kim? Aboneliği bırakırsam verilerime ne olur?',
      }),
      a: t('LandingPage.faq.a_dataOwnerA', {
        defaultValue:
          'Verileriniz tümüyle size aittir; CoreAlign yalnızca işleyen taraftır. İstediğiniz an müşteri, ürün, sipariş, fatura ve muhasebe kayıtlarınızı standart formatlarda (CSV, Excel ve yapısal JSON) dışa aktarabilirsiniz. Aboneliğinizi sonlandırırsanız, tanımlı bir geçiş penceresinde tam veri dışa aktarımı sağlanır; saklama süresi dolduğunda veriler kalıcı olarak silinir. Verinizi rehin alan bir kilitlenme (vendor lock-in) yaratmıyoruz.',
      }),
      icon: DatabaseBackup,
      category: categories.data,
    },
    {
      q: t('LandingPage.faq.a_migrationQ', {
        defaultValue: 'Mevcut sistemimizdeki verileri CoreAlign’e taşımak ne kadar sürer?',
      }),
      a: t('LandingPage.faq.a_migrationA', {
        defaultValue:
          'Geçiş yapılandırılmış bir süreçtir: önce cari hesap, ürün/stok kartları, açık siparişler ve açılış bakiyeleri eşlenir, ardından test ortamında doğrulanıp canlıya alınır. Hazır içe aktarma şablonları ve doğrulama kontrolleri sayesinde, hatalı veya eksik kayıtlar daha aktarım anında yakalanır. Onboarding ekibimiz eşleştirme ve mutabakatı sizinle birlikte yürütür; canlıya geçişi hafta sonu gibi düşük yoğunluklu bir pencerede planlarız.',
      }),
      icon: ArrowRight,
      category: categories.data,
    },
    {
      q: t('LandingPage.faq.a_langQ', {
        defaultValue: 'Hangi dilleri destekliyor? Yurt dışı bayilerimizle kullanabilir miyiz?',
      }),
      a: t('LandingPage.faq.a_langA', {
        defaultValue:
          'Arayüz Türkçe, İngilizce, Almanca, Rusça ve Arapça olarak sunulur; Arapça için sağdan-sola (RTL) yerleşim desteklenir. Her kullanıcı kendi dilini seçer, böylece yurt dışı bayileriniz kendi dillerinde sipariş girerken siz Türkçe çalışmaya devam edersiniz. Tarih, sayı ve para birimi biçimleri kullanıcının yereline göre gösterilir; veriler arka planda UTC ve standart birimlerle tutulduğu için raporlama tutarlı kalır.',
      }),
      icon: Languages,
      category: categories.integration,
    },
    {
      q: t('LandingPage.faq.a_deployQ', {
        defaultValue: 'Buluttan mı yoksa kendi sunucumuzda (on-premise) mı kullanmalıyız?',
      }),
      a: t('LandingPage.faq.a_deployA', {
        defaultValue:
          'İki seçenek de mevcut. Çoğu işletme için önerimiz buluttur: yedekleme, ölçekleme ve güncellemeler tarafımızca yönetilir, ek BT yükü olmaz. Düzenleyici gereksinim veya kurumsal politika nedeniyle verinin sizde kalması gerekiyorsa, Türkiye sınırları içindeki özel sunucularda barındırma ya da kendi veri merkezinizde (on-premise) kurulum sağlıyoruz. Her iki dağıtımda da aynı kod tabanı ve aynı çok-kiracılı izolasyon mimarisi çalışır.',
      }),
      icon: ServerCog,
      category: categories.commercial,
    },
    {
      q: t('LandingPage.faq.q3'),
      a: t('LandingPage.faq.a3'),
      icon: ShieldCheck,
      category: categories.security,
    },
    {
      q: t('LandingPage.faq.q1'),
      a: t('LandingPage.faq.a1'),
      icon: Plug,
      category: categories.integration,
    },
    {
      q: t('LandingPage.faq.q4'),
      a: t('LandingPage.faq.a4'),
      icon: Cpu,
      category: categories.design,
    },
    {
      q: t('LandingPage.faq.q2'),
      a: t('LandingPage.faq.a2'),
      icon: PenTool,
      category: categories.design,
    },
  ];

  const toggleIndex = (idx: number) => {
    setOpenIndex(openIndex === idx ? null : idx);
  };

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <HelpCircle size={12} aria-hidden="true" />
            {t('LandingPage.faq.badge', { defaultValue: 'S.S.S.' })}
          </>
        }
        title={t('LandingPage.faq.title')}
        subtitle={t('LandingPage.faq.subtitle')}
      />

      {renderTrustStrip(t)}

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-[1.65fr_1fr] lg:items-start">
        <div className="ca-stagger space-y-4">
          {faqItems.map((item, idx) => {
            const isOpen = openIndex === idx;
            const Icon = item.icon;
            const panelId = `faq-panel-${idx}`;
            const buttonId = `faq-button-${idx}`;
            return (
              <div
                key={idx}
                className={`overflow-hidden rounded-2xl border bg-white/40 shadow-sm backdrop-blur-sm transition-all duration-300 dark:bg-slate-900/40 ${
                  isOpen
                    ? 'border-primary-500/40 shadow-md shadow-primary-500/5 dark:border-primary-500/40'
                    : 'border-slate-200 hover:border-primary-500/30 dark:border-slate-800'
                }`}
              >
                <h3>
                  <button
                    id={buttonId}
                    onClick={() => toggleIndex(idx)}
                    aria-expanded={isOpen}
                    aria-controls={panelId}
                    className="flex w-full items-center gap-4 p-5 text-left sm:p-6"
                  >
                    <span
                      className={`inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-xl transition-colors duration-300 ${item.category.color}`}
                    >
                      <Icon size={18} aria-hidden="true" />
                    </span>
                    <span className="flex-1">
                      <span className="block text-base font-bold text-slate-900 dark:text-slate-100">
                        {item.q}
                      </span>
                      <span
                        className={`mt-1 inline-block rounded-md px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide ${item.category.color}`}
                      >
                        {item.category.label}
                      </span>
                    </span>
                    <span
                      className={`shrink-0 rounded-xl bg-slate-100 p-2 text-slate-500 transition-transform duration-300 dark:bg-slate-800 dark:text-slate-400 ${
                        isOpen ? 'rotate-180' : 'rotate-0'
                      }`}
                    >
                      <ChevronDown size={16} aria-hidden="true" />
                    </span>
                  </button>
                </h3>

                <div
                  id={panelId}
                  role="region"
                  aria-labelledby={buttonId}
                  className={`grid transition-all duration-300 ease-out ${
                    isOpen ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0'
                  }`}
                >
                  <div className="overflow-hidden">
                    <p className="border-t border-slate-100 px-5 pb-6 pt-4 text-sm leading-relaxed text-slate-600 dark:border-slate-800/60 dark:text-slate-400 sm:px-6 sm:pl-20">
                      {item.a}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        <aside className="space-y-6 lg:sticky lg:top-24">
          {renderResolutionVisual(t)}
          {renderSupportCard(t)}
        </aside>
      </div>
    </Section>
  );
};

const renderTrustStrip = (t: (key: string, options?: { defaultValue: string }) => string) => {
  const stats = [
    {
      value: t('LandingPage.faq.statUptimeValue', { defaultValue: '%99.9' }),
      label: t('LandingPage.faq.statUptimeLabel', { defaultValue: 'Hedeflenen erişilebilirlik' }),
      color: 'text-success-600 dark:text-success-400',
    },
    {
      value: 'AES-256',
      label: t('LandingPage.faq.statEncLabel', { defaultValue: 'Sunucu tarafı şifreleme' }),
      color: 'text-primary-600 dark:text-primary-300',
    },
    {
      value: t('LandingPage.faq.statLangValue', { defaultValue: '5 dil' }),
      label: t('LandingPage.faq.statLangLabel', { defaultValue: 'TR · EN · DE · RU · AR' }),
      color: 'text-accent-600 dark:text-accent-300',
    },
    {
      value: t('LandingPage.faq.statDeployValue', { defaultValue: 'Bulut / On-prem' }),
      label: t('LandingPage.faq.statDeployLabel', { defaultValue: 'Esnek dağıtım' }),
      color: 'text-info-600 dark:text-info-300',
    },
  ];

  return (
    <div className="ca-stagger mb-10 grid grid-cols-2 gap-3 sm:grid-cols-4 sm:gap-4">
      {stats.map((stat, idx) => (
        <div
          key={idx}
          className="rounded-2xl border border-slate-200 bg-white/40 p-4 text-center shadow-sm backdrop-blur-sm transition-colors duration-300 hover:border-primary-500/30 dark:border-slate-800 dark:bg-slate-900/40"
        >
          <div className={`text-xl font-extrabold tracking-tight ${stat.color}`}>{stat.value}</div>
          <div className="mt-1 text-[11px] font-medium leading-tight text-slate-500 dark:text-slate-400">
            {stat.label}
          </div>
        </div>
      ))}
    </div>
  );
};

const renderResolutionVisual = (t: (key: string, options?: { defaultValue: string }) => string) => (
  <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white/40 p-6 shadow-sm backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/40">
    <h3 className="mb-1 text-sm font-bold text-slate-900 dark:text-white">
      {t('LandingPage.faq.visualTitle', { defaultValue: 'Sorudan yanıta giden yol' })}
    </h3>
    <p className="mb-5 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
      {t('LandingPage.faq.visualSubtitle', {
        defaultValue:
          'Her talep planlı bir akıştan geçer: değerlendirme, onboarding ekibiyle eşleştirme ve net yanıt.',
      })}
    </p>

    <svg
      viewBox="0 0 240 132"
      className="w-full"
      role="img"
      aria-label={t('LandingPage.faq.visualAria', {
        defaultValue: 'Soru sinyalinin destek katmanlarından geçip yanıta ulaşmasını gösteren şema',
      })}
    >
      <defs>
        <linearGradient id="faqFlow" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" className="[stop-color:var(--color-primary-500)]" />
          <stop offset="100%" className="[stop-color:var(--color-accent-500)]" />
        </linearGradient>
      </defs>

      <path
        d="M28 66 H120 M120 66 C150 66 150 36 188 36 M120 66 C150 66 150 96 188 96"
        fill="none"
        className="stroke-slate-200 dark:stroke-slate-700"
        strokeWidth="2"
        strokeLinecap="round"
      />
      <path
        d="M28 66 H120 M120 66 C150 66 150 36 188 36 M120 66 C150 66 150 96 188 96"
        fill="none"
        stroke="url(#faqFlow)"
        strokeWidth="2"
        strokeLinecap="round"
        strokeDasharray="7 215"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="222"
          to="0"
          dur="3.2s"
          repeatCount="indefinite"
        />
      </path>

      <g className="fill-primary-600 dark:fill-primary-400">
        <circle cx="28" cy="66" r="9" className="animate-pulse-soft" />
        <text x="28" y="69" textAnchor="middle" className="fill-white text-[9px] font-bold">
          ?
        </text>
      </g>

      <g>
        <circle
          cx="120"
          cy="66"
          r="11"
          className="fill-accent-500/15 stroke-accent-500 dark:fill-accent-500/25"
          strokeWidth="1.5"
        />
        <circle cx="120" cy="66" r="4" className="fill-accent-600 dark:fill-accent-400" />
      </g>

      <g
        className="fill-success-500/15 stroke-success-500 dark:fill-success-500/25"
        strokeWidth="1.5"
      >
        <circle cx="188" cy="36" r="10" />
        <circle cx="188" cy="96" r="10" />
      </g>
      <path
        d="M184 36 l3 3 l5 -6 M184 96 l3 3 l5 -6"
        fill="none"
        className="stroke-success-600 dark:stroke-success-400"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>

    <div className="mt-4 flex items-center justify-between text-[11px] font-semibold text-slate-500 dark:text-slate-400">
      <span>{t('LandingPage.faq.visualStep1', { defaultValue: 'Talep' })}</span>
      <span>{t('LandingPage.faq.visualStep2', { defaultValue: 'Değerlendirme' })}</span>
      <span>{t('LandingPage.faq.visualStep3', { defaultValue: 'Çözüm' })}</span>
    </div>
  </div>
);

const renderSupportCard = (t: (key: string, options?: { defaultValue: string }) => string) => (
  <div className="rounded-3xl border border-primary-500/30 bg-primary-500/5 p-6 shadow-sm backdrop-blur-sm dark:border-primary-500/30 dark:bg-primary-500/10">
    <span className="mb-4 inline-flex h-10 w-10 items-center justify-center rounded-2xl bg-primary-500/15 text-primary-600 dark:bg-primary-500/25 dark:text-primary-300">
      <MessageCircleQuestion size={20} aria-hidden="true" />
    </span>
    <h3 className="mb-1 text-base font-bold text-slate-900 dark:text-white">
      {t('LandingPage.faq.supportTitle', { defaultValue: 'Aradığınız yanıt burada değil mi?' })}
    </h3>
    <p className="mb-5 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
      {t('LandingPage.faq.supportDesc', {
        defaultValue:
          'Sektöre özel senaryonuzu birlikte konuşalım. Ekibimiz işleyişinizi dinler ve CoreAlign’in nasıl uyarlanacağını adım adım gösterir.',
      })}
    </p>
    <a
      href="#demo"
      className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary-600 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40"
    >
      {t('LandingPage.faq.supportCta', { defaultValue: 'Uzmanla demo planlayın' })}
      <ArrowRight size={16} aria-hidden="true" />
    </a>
  </div>
);
