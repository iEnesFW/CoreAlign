import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  MapPin,
  Phone,
  Mail,
  Clock,
  Send,
  Inbox,
  Route,
  UserCheck,
  MessageSquareReply,
  ShieldCheck,
  CalendarClock,
} from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { Section, SectionHeader } from './Section';

const SupportPipeline = ({ stationLabels }: { stationLabels: string[] }) => {
  const stations = [
    { icon: Inbox, x: 40 },
    { icon: Route, x: 150 },
    { icon: UserCheck, x: 260 },
    { icon: MessageSquareReply, x: 370 },
  ];

  return (
    <svg
      viewBox="0 0 410 130"
      className="h-auto w-full"
      role="img"
      aria-label={stationLabels.join(' → ')}
    >
      <defs>
        <linearGradient id="ca-contact-flow" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" className="[stop-color:var(--color-primary-400)]" />
          <stop offset="100%" className="[stop-color:var(--color-accent-400)]" />
        </linearGradient>
      </defs>

      <line
        x1="40"
        y1="48"
        x2="370"
        y2="48"
        stroke="url(#ca-contact-flow)"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeDasharray="2 8"
        className="opacity-70"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="20"
          to="0"
          dur="1.1s"
          repeatCount="indefinite"
        />
      </line>

      <circle r="4.5" fill="url(#ca-contact-flow)">
        <animateMotion path="M40,48 H370" dur="3.2s" repeatCount="indefinite" />
        <animate attributeName="opacity" values="0;1;1;0" dur="3.2s" repeatCount="indefinite" />
      </circle>
      <circle r="3" className="fill-accent-400">
        <animateMotion path="M40,48 H370" dur="3.2s" begin="1.6s" repeatCount="indefinite" />
        <animate
          attributeName="opacity"
          values="0;1;1;0"
          dur="3.2s"
          begin="1.6s"
          repeatCount="indefinite"
        />
      </circle>

      {stations.map((station, idx) => (
        <g key={idx}>
          <circle
            cx={station.x}
            cy="48"
            r="17"
            className="fill-white stroke-primary-300 dark:fill-slate-900 dark:stroke-primary-700"
            strokeWidth="2"
          />
          <circle
            cx={station.x}
            cy="48"
            r="17"
            className="fill-primary-500/5 dark:fill-primary-400/10"
          />
          <foreignObject x={station.x - 9} y="39" width="18" height="18">
            <div className="flex h-[18px] w-[18px] items-center justify-center text-primary-600 dark:text-primary-300">
              <station.icon size={13} aria-hidden="true" />
            </div>
          </foreignObject>
          <text
            x={station.x}
            y="84"
            textAnchor="middle"
            className="fill-slate-500 text-[9px] font-semibold dark:fill-slate-400"
          >
            {stationLabels[idx]}
          </text>
          <circle cx={station.x} cy="100" r="2.5" className="fill-success-500">
            <animate
              attributeName="opacity"
              values="0.25;1;0.25"
              dur="1.8s"
              begin={`${idx * 0.4}s`}
              repeatCount="indefinite"
            />
          </circle>
        </g>
      ))}
    </svg>
  );
};

export const ContactSection = () => {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: '',
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.message) return;

    setLoading(true);
    const mockRequest = new Promise((resolve) => {
      setTimeout(() => resolve(true), 1500);
    });

    const [data] = await safeRequestWithNotify(mockRequest, {
      successMessage: t('LandingPage.contact.success'),
      showSuccessNotification: true,
    });

    if (data) {
      setFormData({
        name: '',
        email: '',
        subject: '',
        message: '',
      });
    }
    setLoading(false);
  };

  const channels = [
    {
      icon: MapPin,
      label: t('LandingPage.contact.address'),
      hint: t('LandingPage.contact.addressHint', {
        defaultValue: 'Ekibimiz Teknopark İstanbul ofisinde; toplantı için randevu alın.',
      }),
    },
    {
      icon: Phone,
      label: t('LandingPage.contact.phone'),
      hint: t('LandingPage.contact.phoneHint', {
        defaultValue: 'Örnek hat — canlı destek için mesai saatleri içinde arayın.',
      }),
    },
    {
      icon: Mail,
      label: t('LandingPage.contact.emailLabel'),
      hint: t('LandingPage.contact.emailHint', {
        defaultValue: 'Genel sorular için örnek adres; talebiniz doğru ekibe yönlendirilir.',
      }),
    },
    {
      icon: Clock,
      label: t('LandingPage.contact.hours'),
      hint: t('LandingPage.contact.hoursHint', {
        defaultValue: 'Form üzerinden gelen talepleri 7/24 alır, mesai başında yanıtlarız.',
      }),
    },
  ];

  const stats = [
    {
      icon: CalendarClock,
      value: t('LandingPage.contact.statResponseValue', { defaultValue: '< 1 iş günü' }),
      label: t('LandingPage.contact.statResponseLabel', { defaultValue: 'Hedef ilk yanıt süresi' }),
      tone: 'text-primary-600 dark:text-primary-300',
    },
    {
      icon: UserCheck,
      value: t('LandingPage.contact.statOwnerValue', { defaultValue: 'Tek temas' }),
      label: t('LandingPage.contact.statOwnerLabel', { defaultValue: 'Size atanmış uzman' }),
      tone: 'text-accent-600 dark:text-accent-300',
    },
    {
      icon: ShieldCheck,
      value: t('LandingPage.contact.statPrivacyValue', { defaultValue: 'KVKK uyumlu' }),
      label: t('LandingPage.contact.statPrivacyLabel', { defaultValue: 'Verileriniz güvende' }),
      tone: 'text-success-600 dark:text-success-300',
    },
  ];

  return (
    <Section id="contact">
      <SectionHeader
        className="animate-fade-up"
        eyebrow={
          <>
            <MessageSquareReply size={12} aria-hidden="true" />
            {t('LandingPage.contact.badge', { defaultValue: 'İLETİŞİM' })}
          </>
        }
        title={t('LandingPage.contact.title')}
        subtitle={t('LandingPage.contact.subtitle')}
      />

      <div className="mb-10 grid grid-cols-1 gap-4 ca-stagger sm:grid-cols-3">
        {stats.map((stat, idx) => {
          const Icon = stat.icon;
          return (
            <div
              key={idx}
              className="flex items-center gap-4 rounded-2xl border border-slate-200/60 bg-white/50 p-5 shadow-sm transition-all duration-300 hover:border-primary-500/30 dark:border-slate-800/60 dark:bg-slate-800/40"
            >
              <span
                className={`shrink-0 rounded-xl bg-slate-100 p-3 dark:bg-slate-900/60 ${stat.tone}`}
              >
                <Icon size={20} aria-hidden="true" />
              </span>
              <div>
                <p className={`text-lg font-extrabold leading-tight ${stat.tone}`}>{stat.value}</p>
                <p className="text-xs text-slate-500 dark:text-slate-400">{stat.label}</p>
              </div>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 gap-12 md:grid-cols-2">
        <div className="rounded-3xl border border-slate-200/60 bg-white/50 p-8 shadow-sm animate-fade-up dark:border-slate-800/60 dark:bg-slate-800/50">
          <h3 className="mb-1 text-lg font-bold text-slate-900 dark:text-slate-100">
            {t('LandingPage.contact.formTitle', { defaultValue: 'Bize mesaj bırakın' })}
          </h3>
          <p className="mb-5 text-sm text-slate-500 dark:text-slate-400">
            {t('LandingPage.contact.formHint', {
              defaultValue: 'Kısa bir özet bırakın; ihtiyacınıza uygun uzmanımız size geri dönsün.',
            })}
          </p>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label
                htmlFor="ca-contact-name"
                className="block text-xs font-semibold text-slate-700 dark:text-slate-300"
              >
                {t('LandingPage.contact.name')}
              </label>
              <input
                id="ca-contact-name"
                type="text"
                required
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-primary-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-primary-400"
              />
            </div>
            <div>
              <label
                htmlFor="ca-contact-email"
                className="block text-xs font-semibold text-slate-700 dark:text-slate-300"
              >
                {t('LandingPage.contact.email')}
              </label>
              <input
                id="ca-contact-email"
                type="email"
                required
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-primary-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-primary-400"
              />
            </div>
            <div>
              <label
                htmlFor="ca-contact-subject"
                className="block text-xs font-semibold text-slate-700 dark:text-slate-300"
              >
                {t('LandingPage.contact.subject')}
              </label>
              <input
                id="ca-contact-subject"
                type="text"
                value={formData.subject}
                onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-primary-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-primary-400"
              />
            </div>
            <div>
              <label
                htmlFor="ca-contact-message"
                className="block text-xs font-semibold text-slate-700 dark:text-slate-300"
              >
                {t('LandingPage.contact.message')}
              </label>
              <textarea
                id="ca-contact-message"
                required
                rows={4}
                value={formData.message}
                onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-primary-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-primary-400"
              />
            </div>
            <button
              type="submit"
              disabled={loading}
              className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-primary-600 px-6 py-3 font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40 disabled:opacity-50"
            >
              {loading ? t('LandingPage.contact.sending') : t('LandingPage.contact.submit')}
              <Send size={16} aria-hidden="true" />
            </button>
            <p className="text-center text-[11px] text-slate-400 dark:text-slate-500">
              {t('LandingPage.contact.formFootnote', {
                defaultValue: 'Bilgileriniz yalnızca talebinize yanıt vermek için kullanılır.',
              })}
            </p>
          </form>
        </div>

        <div className="flex flex-col gap-8 py-2">
          <div className="animate-fade-up">
            <h3 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">
              {t('LandingPage.contact.infoTitle')}
            </h3>
            <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">
              {t('LandingPage.contact.infoSubtitle', {
                defaultValue:
                  'Aşağıdaki kanallardan bize ulaşabilirsiniz. Henüz lansman öncesi olduğumuz için iletişim bilgileri örnek niteliğindedir.',
              })}
            </p>
            <ul className="space-y-3">
              {channels.map((channel, idx) => {
                const Icon = channel.icon;
                return (
                  <li
                    key={idx}
                    className="flex items-start gap-4 rounded-2xl border border-slate-200/50 bg-white/40 p-3 transition-colors hover:border-primary-500/30 dark:border-slate-800/50 dark:bg-slate-900/30"
                  >
                    <span className="shrink-0 rounded-xl bg-primary-500/10 p-3 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400">
                      <Icon size={18} aria-hidden="true" />
                    </span>
                    <div>
                      <p className="text-sm font-medium leading-relaxed text-slate-700 dark:text-slate-200">
                        {channel.label}
                      </p>
                      <p className="mt-0.5 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
                        {channel.hint}
                      </p>
                    </div>
                  </li>
                );
              })}
            </ul>
          </div>

          <div className="overflow-hidden rounded-3xl border border-slate-200/60 bg-gradient-to-br from-slate-50 to-slate-100 p-6 shadow-sm animate-fade-up dark:border-slate-800/60 dark:from-slate-900/60 dark:to-slate-800/40">
            <h3 className="mb-1 text-sm font-bold text-slate-900 dark:text-slate-100">
              {t('LandingPage.contact.pipelineTitle', { defaultValue: 'Mesajınıza ne oluyor?' })}
            </h3>
            <p className="mb-4 text-xs text-slate-500 dark:text-slate-400">
              {t('LandingPage.contact.pipelineSubtitle', {
                defaultValue:
                  'Talebiniz tek bir akışta toplanır, doğru uzmana yönlendirilir ve yanıtlanır.',
              })}
            </p>
            <SupportPipeline
              stationLabels={[
                t('LandingPage.contact.stageReceived', { defaultValue: 'Alındı' }),
                t('LandingPage.contact.stageRouted', { defaultValue: 'Yönlendirildi' }),
                t('LandingPage.contact.stageExpert', { defaultValue: 'Uzman' }),
                t('LandingPage.contact.stageReply', { defaultValue: 'Yanıt' }),
              ]}
            />
          </div>
        </div>
      </div>
    </Section>
  );
};
