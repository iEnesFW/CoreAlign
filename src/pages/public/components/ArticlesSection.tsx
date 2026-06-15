import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X, BookOpen } from 'lucide-react';

export const ArticlesSection = () => {
  const { t } = useTranslation();
  const [activeArticleIndex, setActiveArticleIndex] = useState<number | null>(null);

  const articles = [
    {
      category: t('LandingPage.articles.a1Category'),
      title: t('LandingPage.articles.a1Title'),
      text: t('LandingPage.articles.a1Text'),
      fullText: t('LandingPage.articles.a1FullText'),
      author: t('LandingPage.articles.a1Author'),
      role: t('LandingPage.articles.a1Role'),
      initials: 'AY',
      badgeColor: 'bg-indigo-500/10 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400',
    },
    {
      category: t('LandingPage.articles.a2Category'),
      title: t('LandingPage.articles.a2Title'),
      text: t('LandingPage.articles.a2Text'),
      fullText: t('LandingPage.articles.a2FullText'),
      author: t('LandingPage.articles.a2Author'),
      role: t('LandingPage.articles.a2Role'),
      initials: 'EK',
      badgeColor: 'bg-purple-500/10 text-purple-600 dark:bg-purple-500/20 dark:text-purple-400',
    },
    {
      category: t('LandingPage.articles.a3Category'),
      title: t('LandingPage.articles.a3Title'),
      text: t('LandingPage.articles.a3Text'),
      fullText: t('LandingPage.articles.a3FullText'),
      author: t('LandingPage.articles.a3Author'),
      role: t('LandingPage.articles.a3Role'),
      initials: 'CD',
      badgeColor: 'bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400',
    },
    {
      category: t('LandingPage.articles.a4Category'),
      title: t('LandingPage.articles.a4Title'),
      text: t('LandingPage.articles.a4Text'),
      fullText: t('LandingPage.articles.a4FullText'),
      author: t('LandingPage.articles.a4Author'),
      role: t('LandingPage.articles.a4Role'),
      initials: 'MÇ',
      badgeColor: 'bg-blue-500/10 text-blue-600 dark:bg-blue-500/20 dark:text-blue-400',
    },
  ];

  const currentArticle = activeArticleIndex !== null ? articles[activeArticleIndex] : null;

  return (
    <section className="px-8 py-16 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-4xl">
        <div className="mb-12 text-center">
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.articles.title')}
          </h2>
          <p className="text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.articles.subtitle')}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-8">
          {articles.map((art, index) => (
            <div
              key={index}
              className="flex flex-col overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-xl shadow-slate-200/50 dark:border-slate-800/80 dark:bg-[#0f1524]/60 dark:shadow-none md:flex-row hover:border-indigo-500/30 transition-all duration-300"
            >
              <div className="flex-1 p-8 md:p-10 flex flex-col justify-between">
                <div>
                  <span
                    className={`mb-4 inline-block rounded-full px-3 py-1 text-xs font-extrabold tracking-widest ${art.badgeColor}`}
                  >
                    {art.category}
                  </span>
                  <h3 className="mb-4 text-2xl font-bold leading-tight text-slate-900 dark:text-white">
                    {art.title}
                  </h3>
                  <p className="mb-6 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                    {art.text}
                  </p>
                </div>
                <div className="flex items-center justify-between mt-4 border-t border-slate-100 pt-4 dark:border-slate-800/60">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 font-bold text-slate-700 dark:bg-slate-800 dark:text-slate-300">
                      {art.initials}
                    </div>
                    <div>
                      <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                        {art.author}
                      </div>
                      <div className="text-xs text-slate-500 dark:text-slate-400">{art.role}</div>
                    </div>
                  </div>
                  <button
                    onClick={() => setActiveArticleIndex(index)}
                    className="inline-flex items-center gap-1.5 rounded-xl border border-indigo-500/20 bg-indigo-500/5 px-4 py-2 text-xs font-bold text-indigo-600 transition hover:bg-indigo-600 hover:text-white dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-400 dark:hover:bg-indigo-650"
                  >
                    <BookOpen size={14} />
                    {t('LandingPage.articles.readMore')}
                  </button>
                </div>
              </div>
              <div className="hidden w-1/4 bg-gradient-to-br from-indigo-500 to-purple-600 md:block opacity-90" />
            </div>
          ))}
        </div>
      </div>

      {currentArticle && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-md">
          <div className="w-full max-w-2xl rounded-3xl bg-white p-6 shadow-2xl dark:bg-[#0f1524] border border-slate-200/60 dark:border-slate-800/60 overflow-y-auto max-h-[85vh] relative animate-in fade-in zoom-in-95 duration-200">
            <button
              onClick={() => setActiveArticleIndex(null)}
              className="absolute right-4 top-4 p-2 rounded-xl text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800/65 transition"
              aria-label={t('LandingPage.articles.close')}
            >
              <X size={20} />
            </button>

            <div className="mb-6">
              <span
                className={`inline-block rounded-full px-3 py-1 text-xs font-extrabold tracking-widest ${currentArticle.badgeColor} mb-3`}
              >
                {currentArticle.category}
              </span>
              <h3 className="text-2xl font-extrabold leading-tight text-slate-900 dark:text-white">
                {currentArticle.title}
              </h3>
            </div>

            <div className="mb-6 text-sm leading-relaxed text-slate-600 dark:text-slate-300 space-y-4">
              <p className="font-semibold text-slate-700 dark:text-slate-200 bg-slate-50 dark:bg-slate-900/40 p-4 rounded-2xl italic">
                {currentArticle.text}
              </p>
              <p className="pt-2 whitespace-pre-wrap">{currentArticle.fullText}</p>
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 pt-4 dark:border-slate-800/65">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 font-bold text-slate-700 dark:bg-slate-800 dark:text-slate-300">
                  {currentArticle.initials}
                </div>
                <div>
                  <div className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {currentArticle.author}
                  </div>
                  <div className="text-xs text-slate-500 dark:text-slate-400">
                    {currentArticle.role}
                  </div>
                </div>
              </div>
              <button
                onClick={() => setActiveArticleIndex(null)}
                className="rounded-xl bg-slate-900 px-5 py-2 text-xs font-bold text-white transition hover:bg-slate-800 dark:bg-slate-800 dark:hover:bg-slate-705"
              >
                {t('LandingPage.articles.close')}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
};
