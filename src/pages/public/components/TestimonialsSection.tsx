import { useTranslation } from 'react-i18next';
import { Quote, Star } from 'lucide-react';

export const TestimonialsSection = () => {
  const { t } = useTranslation();

  const reviews = [
    {
      quote: t('LandingPage.testimonials.t1Quote'),
      author: t('LandingPage.testimonials.t1Author'),
      role: t('LandingPage.testimonials.t1Role'),
      initials: 'MŞ',
      color: 'bg-indigo-500/10 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400',
    },
    {
      quote: t('LandingPage.testimonials.t2Quote'),
      author: t('LandingPage.testimonials.t2Author'),
      role: t('LandingPage.testimonials.t2Role'),
      initials: 'YS',
      color: 'bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400',
    },
  ];

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Quote size={12} />
            BAŞARI HİKAYELERİ
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.testimonials.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.testimonials.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-8 md:grid-cols-2">
          {reviews.map((rev, idx) => (
            <div
              key={idx}
              className="flex flex-col justify-between rounded-3xl border border-slate-200 bg-white/40 p-8 shadow-sm backdrop-blur-sm transition-all duration-300 hover:border-indigo-500/30 dark:border-slate-800 dark:bg-slate-900/40"
            >
              <div>
                <div className="mb-6 flex gap-1 text-amber-500">
                  <Star size={16} fill="currentColor" />
                  <Star size={16} fill="currentColor" />
                  <Star size={16} fill="currentColor" />
                  <Star size={16} fill="currentColor" />
                  <Star size={16} fill="currentColor" />
                </div>
                <p className="text-sm font-medium leading-relaxed italic text-slate-700 dark:text-slate-300 mb-8">
                  "{rev.quote}"
                </p>
              </div>

              <div className="flex items-center gap-4 border-t border-slate-100/50 pt-6 dark:border-slate-800/60">
                <div
                  className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full font-bold text-xs uppercase ${rev.color}`}
                >
                  {rev.initials}
                </div>
                <div>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white">{rev.author}</h4>
                  <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{rev.role}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
