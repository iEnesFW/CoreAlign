import { useTranslation } from 'react-i18next';
import { CircleDollarSign, Check, PhoneCall } from 'lucide-react';

export const PricingSection = () => {
  const { t } = useTranslation();

  const plan1Features = t('LandingPage.pricing.plan1Features', { returnObjects: true });
  const plan2Features = t('LandingPage.pricing.plan2Features', { returnObjects: true });
  const plan3Features = t('LandingPage.pricing.plan3Features', { returnObjects: true });

  const f1 = Array.isArray(plan1Features) ? plan1Features : [];
  const f2 = Array.isArray(plan2Features) ? plan2Features : [];
  const f3 = Array.isArray(plan3Features) ? plan3Features : [];

  const plans = [
    {
      title: t('LandingPage.pricing.plan1Title'),
      desc: t('LandingPage.pricing.plan1Desc'),
      features: f1,
      isPopular: false,
    },
    {
      title: t('LandingPage.pricing.plan2Title'),
      desc: t('LandingPage.pricing.plan2Desc'),
      features: f2,
      isPopular: true,
    },
    {
      title: t('LandingPage.pricing.plan3Title'),
      desc: t('LandingPage.pricing.plan3Desc'),
      features: f3,
      isPopular: false,
    },
  ];

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <CircleDollarSign size={12} />
            FİYATLANDIRMA
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.pricing.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.pricing.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-8 md:grid-cols-3 items-stretch">
          {plans.map((plan, idx) => (
            <div
              key={idx}
              className={`relative flex flex-col justify-between rounded-3xl border p-8 transition-all duration-300 ${
                plan.isPopular
                  ? 'border-indigo-500 bg-white shadow-xl dark:border-indigo-400 dark:bg-slate-900/80 scale-[1.03] z-10'
                  : 'border-slate-200 bg-white/40 backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/40'
              }`}
            >
              {plan.isPopular && (
                <span className="absolute -top-3.5 left-1/2 -translate-x-1/2 rounded-full bg-indigo-600 px-3.5 py-1 text-[9px] font-bold tracking-widest text-white uppercase shadow-sm">
                  {t('LandingPage.pricing.recommended')}
                </span>
              )}

              <div>
                <h3 className="text-lg font-extrabold text-slate-900 dark:text-white mb-2">
                  {plan.title}
                </h3>
                <p className="text-xs text-slate-500 dark:text-slate-400 mb-6 leading-relaxed">
                  {plan.desc}
                </p>

                <div className="h-px bg-slate-100 dark:bg-slate-800 mb-6" />

                <ul className="space-y-3">
                  {plan.features.map((feat, fIdx) => (
                    <li key={fIdx} className="flex items-start gap-2.5">
                      <span className="inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-indigo-500/10 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400 mt-0.5">
                        <Check size={10} />
                      </span>
                      <span className="text-xs font-semibold text-slate-750 dark:text-slate-300">
                        {feat}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>

              <div className="mt-8">
                <a
                  href="#demo"
                  className={`inline-flex w-full items-center justify-center gap-2 rounded-xl py-3 text-xs font-bold transition shadow-sm ${
                    plan.isPopular
                      ? 'bg-indigo-600 text-white hover:bg-indigo-700 hover:shadow-md'
                      : 'bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-750'
                  }`}
                >
                  <PhoneCall size={12} />
                  {t('LandingPage.pricing.contactUs')}
                </a>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
