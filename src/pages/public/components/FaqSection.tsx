import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronDown, ChevronUp, HelpCircle } from 'lucide-react';

export const FaqSection = () => {
  const { t } = useTranslation();
  const [openIndex, setOpenIndex] = useState<number | null>(null);

  const faqItems = [
    {
      q: t('LandingPage.faq.q1'),
      a: t('LandingPage.faq.a1'),
    },
    {
      q: t('LandingPage.faq.q2'),
      a: t('LandingPage.faq.a2'),
    },
    {
      q: t('LandingPage.faq.q3'),
      a: t('LandingPage.faq.a3'),
    },
    {
      q: t('LandingPage.faq.q4'),
      a: t('LandingPage.faq.a4'),
    },
  ];

  const toggleIndex = (idx: number) => {
    setOpenIndex(openIndex === idx ? null : idx);
  };

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-4xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-400">
            <HelpCircle size={12} />
            S.S.S.
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.faq.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.faq.subtitle')}
          </p>
        </div>

        <div className="space-y-4">
          {faqItems.map((item, idx) => {
            const isOpen = openIndex === idx;
            return (
              <div
                key={idx}
                className="rounded-2xl border border-slate-200 bg-white/40 shadow-sm backdrop-blur-sm transition-all duration-300 dark:border-slate-800 dark:bg-slate-900/40"
              >
                <button
                  onClick={() => toggleIndex(idx)}
                  className="flex w-full items-center justify-between p-6 text-left"
                >
                  <span className="text-base font-bold text-slate-900 dark:text-slate-100 pr-4">
                    {item.q}
                  </span>
                  <span className="rounded-xl bg-slate-100 p-2 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                    {isOpen ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                  </span>
                </button>

                <div
                  className={`overflow-hidden transition-all duration-300 ${
                    isOpen
                      ? 'max-h-[300px] border-t border-slate-100 dark:border-slate-800/60'
                      : 'max-h-0'
                  }`}
                >
                  <p className="p-6 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                    {item.a}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
};
