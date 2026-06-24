import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Calendar,
  Send,
  Clock,
  ShieldCheck,
  CheckCircle2,
  PhoneCall,
  MonitorPlay,
  Sparkles,
  Lock,
} from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { Section, SectionHeader } from './Section';

const inputClass =
  'mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-xs text-slate-900 outline-none transition focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 dark:border-slate-800 dark:bg-slate-900/80 dark:text-white dark:focus:border-primary-400';

const labelClass =
  'block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400';

export const DemoScheduler = () => {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    company: '',
    module: 'cad',
    date: '',
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.company || !formData.date) return;

    setLoading(true);
    const mockRequest = new Promise((resolve) => {
      setTimeout(() => resolve(true), 1500);
    });

    const [data] = await safeRequestWithNotify(mockRequest, {
      successMessage: t('LandingPage.scheduler.success'),
      showSuccessNotification: true,
    });

    if (data) {
      setFormData({
        name: '',
        email: '',
        company: '',
        module: 'cad',
        date: '',
      });
    }
    setLoading(false);
  };

  const timeline = [
    {
      icon: CheckCircle2,
      title: t('LandingPage.scheduler.step1Title', { defaultValue: 'Talebiniz anında ulaşır' }),
      desc: t('LandingPage.scheduler.step1Desc', {
        defaultValue:
          'Formu gönderdiğiniz an ekibimize iletilir, otomatik bir onay e-postası alırsınız.',
      }),
      tone: 'text-success-600 dark:text-success-400',
      ring: 'border-success-500/30 bg-success-500/10',
    },
    {
      icon: PhoneCall,
      title: t('LandingPage.scheduler.step2Title', { defaultValue: '24 saat içinde dönüş' }),
      desc: t('LandingPage.scheduler.step2Desc', {
        defaultValue: 'Bir uzmanımız uygun saatleri netleştirmek için sizi arar veya yazar.',
      }),
      tone: 'text-primary-600 dark:text-primary-400',
      ring: 'border-primary-500/30 bg-primary-500/10',
    },
    {
      icon: MonitorPlay,
      title: t('LandingPage.scheduler.step3Title', { defaultValue: 'İşinize özel canlı demo' }),
      desc: t('LandingPage.scheduler.step3Desc', {
        defaultValue: 'Seçtiğiniz modül üzerinden 30 dakikalık, senaryonuza göre uyarlanmış sunum.',
      }),
      tone: 'text-accent-600 dark:text-accent-300',
      ring: 'border-accent-500/30 bg-accent-500/10',
    },
  ];

  const reassurances = [
    {
      icon: Clock,
      label: t('LandingPage.scheduler.reassureResponse', { defaultValue: '24 saat içinde yanıt' }),
    },
    {
      icon: ShieldCheck,
      label: t('LandingPage.scheduler.reassureNoCard', {
        defaultValue: 'Kredi kartı veya taahhüt yok',
      }),
    },
    {
      icon: Lock,
      label: t('LandingPage.scheduler.reassurePrivacy', {
        defaultValue: 'Verileriniz yalnızca demo için kullanılır',
      }),
    },
  ];

  return (
    <Section id="demo">
      <SectionHeader
        eyebrow={
          <>
            <Calendar size={12} aria-hidden="true" />
            {t('LandingPage.scheduler.badge', { defaultValue: 'BİREBİR DEMO' })}
          </>
        }
        title={t('LandingPage.scheduler.title')}
        subtitle={t('LandingPage.scheduler.subtitle')}
      />

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-2 lg:items-start">
        <div className="flex flex-col gap-6 animate-fade-up">
          <div className="rounded-3xl border border-slate-200/60 bg-white/40 p-6 backdrop-blur-sm dark:border-slate-800/80 dark:bg-slate-900/40">
            <h3 className="mb-1 flex items-center gap-2 text-base font-bold text-slate-900 dark:text-white">
              <Sparkles
                size={16}
                className="text-primary-600 dark:text-primary-400"
                aria-hidden="true"
              />
              {t('LandingPage.scheduler.expectTitle', { defaultValue: 'Sizi ne bekliyor?' })}
            </h3>
            <p className="mb-6 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
              {t('LandingPage.scheduler.expectSubtitle', {
                defaultValue:
                  'Demo öncesinden sunuma kadar her adımda nerede olduğunuzu bilirsiniz. Süreç şeffaf ve hızlıdır.',
              })}
            </p>

            <ol className="relative space-y-6 pl-2">
              <span
                aria-hidden="true"
                className="absolute left-[22px] top-2 bottom-2 w-px bg-gradient-to-b from-success-500/40 via-primary-500/40 to-accent-500/40"
              />
              {timeline.map((step, idx) => {
                const Icon = step.icon;
                return (
                  <li key={idx} className="relative flex gap-4">
                    <span
                      className={`relative z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl border ${step.ring} ${step.tone}`}
                    >
                      <Icon size={18} aria-hidden="true" />
                      <span
                        aria-hidden="true"
                        className={`absolute inset-0 rounded-2xl ${step.ring} animate-pulse-soft`}
                      />
                    </span>
                    <div className="pt-0.5">
                      <h4 className="text-sm font-semibold text-slate-900 dark:text-white">
                        {step.title}
                      </h4>
                      <p className="mt-0.5 text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                        {step.desc}
                      </p>
                    </div>
                  </li>
                );
              })}
            </ol>
          </div>

          <div
            className="overflow-hidden rounded-3xl border border-slate-200/60 bg-gradient-to-br from-primary-600 to-accent-600 p-6 text-white shadow-lg dark:border-slate-800/80"
            role="img"
            aria-label={t('LandingPage.scheduler.responseAria', {
              defaultValue: 'Ortalama ilk yanıt süresi 24 saatin altında',
            })}
          >
            <div className="flex items-center justify-between">
              <div>
                <div className="text-[10px] font-bold uppercase tracking-wider text-white/70">
                  {t('LandingPage.scheduler.responseLabel', {
                    defaultValue: 'Hedeflenen ilk yanıt süresi',
                  })}
                </div>
                <div className="mt-1 text-3xl font-extrabold">
                  &lt; 24{' '}
                  <span className="text-lg font-bold">
                    {t('LandingPage.scheduler.hours', { defaultValue: 'saat' })}
                  </span>
                </div>
              </div>
              <Clock size={36} className="text-white/80 animate-pulse-soft" aria-hidden="true" />
            </div>
            <svg
              viewBox="0 0 240 40"
              className="mt-4 h-10 w-full"
              preserveAspectRatio="none"
              aria-hidden="true"
            >
              <defs>
                <linearGradient id="demoPulse" x1="0" y1="0" x2="1" y2="0">
                  <stop offset="0%" stopColor="rgba(255,255,255,0.15)" />
                  <stop offset="50%" stopColor="rgba(255,255,255,0.85)" />
                  <stop offset="100%" stopColor="rgba(255,255,255,0.15)" />
                </linearGradient>
              </defs>
              <polyline
                points="0,30 30,30 45,12 60,30 90,30 105,20 120,30 240,30"
                fill="none"
                stroke="rgba(255,255,255,0.35)"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
              <circle r="3" fill="url(#demoPulse)">
                <animateMotion
                  dur="3s"
                  repeatCount="indefinite"
                  path="M0,30 L30,30 L45,12 L60,30 L90,30 L105,20 L120,30 L240,30"
                />
              </circle>
            </svg>
          </div>
        </div>

        <div className="rounded-3xl border border-slate-200/60 bg-white/60 p-8 shadow-xl backdrop-blur-md animate-fade-up dark:border-slate-800/80 dark:bg-surface-deep/65">
          <h3 className="mb-1 text-xl font-extrabold text-slate-900 dark:text-white">
            {t('LandingPage.scheduler.formTitle', { defaultValue: 'Randevu formu' })}
          </h3>
          <p className="mb-6 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
            {t('LandingPage.scheduler.formSubtitle', {
              defaultValue: 'Birkaç detay paylaşın, gerisini biz halledelim.',
            })}
          </p>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label htmlFor="demo-name" className={labelClass}>
                  {t('LandingPage.scheduler.name')}
                </label>
                <input
                  id="demo-name"
                  type="text"
                  required
                  autoComplete="name"
                  placeholder={t('LandingPage.scheduler.namePlaceholder', {
                    defaultValue: 'Ad Soyad',
                  })}
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className={inputClass}
                />
              </div>

              <div>
                <label htmlFor="demo-email" className={labelClass}>
                  {t('LandingPage.scheduler.email')}
                </label>
                <input
                  id="demo-email"
                  type="email"
                  required
                  autoComplete="email"
                  placeholder={t('LandingPage.scheduler.emailPlaceholder', {
                    defaultValue: 'ornek@sirket.com',
                  })}
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className={inputClass}
                />
              </div>
            </div>

            <div>
              <label htmlFor="demo-company" className={labelClass}>
                {t('LandingPage.scheduler.company')}
              </label>
              <input
                id="demo-company"
                type="text"
                required
                autoComplete="organization"
                placeholder={t('LandingPage.scheduler.companyPlaceholder', {
                  defaultValue: 'Şirket adı',
                })}
                value={formData.company}
                onChange={(e) => setFormData({ ...formData, company: e.target.value })}
                className={inputClass}
              />
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label htmlFor="demo-module" className={labelClass}>
                  {t('LandingPage.scheduler.module')}
                </label>
                <select
                  id="demo-module"
                  value={formData.module}
                  onChange={(e) => setFormData({ ...formData, module: e.target.value })}
                  className={inputClass}
                >
                  <option value="cad">{t('LandingPage.scheduler.optCAD')}</option>
                  <option value="mrp">{t('LandingPage.scheduler.optMRP')}</option>
                  <option value="b2b">{t('LandingPage.scheduler.optB2B')}</option>
                  <option value="finance">{t('LandingPage.scheduler.optFinance')}</option>
                </select>
              </div>

              <div>
                <label htmlFor="demo-date" className={labelClass}>
                  {t('LandingPage.scheduler.date')}
                </label>
                <input
                  id="demo-date"
                  type="date"
                  required
                  value={formData.date}
                  onChange={(e) => setFormData({ ...formData, date: e.target.value })}
                  className={inputClass}
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="mt-2 inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary-600 px-6 py-3 font-bold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40 disabled:opacity-50"
            >
              {loading ? t('LandingPage.scheduler.sending') : t('LandingPage.scheduler.submit')}
              {loading ? (
                <Clock size={14} className="animate-spin" aria-hidden="true" />
              ) : (
                <Send size={14} aria-hidden="true" />
              )}
            </button>

            <ul className="mt-5 grid grid-cols-1 gap-2 border-t border-slate-200/70 pt-5 dark:border-slate-800/70">
              {reassurances.map((item, idx) => {
                const Icon = item.icon;
                return (
                  <li
                    key={idx}
                    className="flex items-center gap-2.5 text-xs text-slate-600 dark:text-slate-400"
                  >
                    <Icon
                      size={15}
                      className="shrink-0 text-success-600 dark:text-success-400"
                      aria-hidden="true"
                    />
                    <span>{item.label}</span>
                  </li>
                );
              })}
            </ul>
          </form>
        </div>
      </div>
    </Section>
  );
};
