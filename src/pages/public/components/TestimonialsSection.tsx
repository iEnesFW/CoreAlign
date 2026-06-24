import { useTranslation } from 'react-i18next';
import { Rocket, Headset, GitPullRequest, ArrowRight } from 'lucide-react';
import { Section, SectionHeader } from './Section';

export const TestimonialsSection = () => {
  const { t } = useTranslation();

  const pillars = [
    {
      icon: Headset,
      title: t('LandingPage.earlyAccess.p1Title', { defaultValue: 'Öncelikli kurulum & destek' }),
      desc: t('LandingPage.earlyAccess.p1Desc', {
        defaultValue: 'Ekibinizle birebir onboarding, veri taşıma ve canlıya geçiş desteği.',
      }),
      color: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400',
    },
    {
      icon: GitPullRequest,
      title: t('LandingPage.earlyAccess.p2Title', { defaultValue: 'Yol haritasına yön verin' }),
      desc: t('LandingPage.earlyAccess.p2Desc', {
        defaultValue: 'Talepleriniz öncelik kazanır; ürün, iş akışınıza göre şekillenir.',
      }),
      color: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-400',
    },
    {
      icon: Rocket,
      title: t('LandingPage.earlyAccess.p3Title', { defaultValue: 'Kurucu müşteri avantajı' }),
      desc: t('LandingPage.earlyAccess.p3Desc', {
        defaultValue: 'Erken katılan işletmelere özel, esnek başlangıç koşulları.',
      }),
      color: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
    },
  ];

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Rocket size={12} />
            {t('LandingPage.earlyAccess.badge', { defaultValue: 'ERKEN ERİŞİM' })}
          </>
        }
        title={t('LandingPage.earlyAccess.title', {
          defaultValue: 'İlk kullananlar arasında yerinizi alın',
        })}
        subtitle={t('LandingPage.earlyAccess.subtitle', {
          defaultValue:
            'CoreAlign yeni nesil bir ERP. Kurucu müşterilerimizle birlikte büyüyoruz — erken katılın, ürünü kendi süreçlerinize göre şekillendirin.',
        })}
      />

      <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
        {pillars.map((pillar, idx) => {
          const Icon = pillar.icon;
          return (
            <div
              key={idx}
              className="flex flex-col rounded-3xl border border-slate-200 bg-white/40 p-8 shadow-sm backdrop-blur-sm transition-all duration-300 hover:border-primary-500/30 dark:border-slate-800 dark:bg-slate-900/40"
            >
              <div
                className={`mb-5 inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl ${pillar.color}`}
              >
                <Icon size={20} />
              </div>
              <h3 className="mb-2 text-base font-bold text-slate-900 dark:text-white">
                {pillar.title}
              </h3>
              <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                {pillar.desc}
              </p>
            </div>
          );
        })}
      </div>

      <div className="mt-12 flex justify-start">
        <a
          href="#demo"
          className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700 hover:shadow-primary-500/40"
        >
          {t('LandingPage.earlyAccess.cta', { defaultValue: 'Erken erişim için demo planlayın' })}
          <ArrowRight size={16} />
        </a>
      </div>
    </Section>
  );
};
