import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  X,
  BookOpen,
  Clock,
  ArrowRight,
  Library,
  Cpu,
  Network,
  ShieldCheck,
  Sparkles,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { Section, SectionHeader } from './Section';

type ArticleTone = {
  badge: string;
  spine: string;
  icon: string;
};

type Article = {
  category: string;
  title: string;
  text: string;
  fullText: string;
  author: string;
  role: string;
  initials: string;
  icon: LucideIcon;
  readMinutes: number;
  tone: ArticleTone;
};

const TONES: Record<string, ArticleTone> = {
  primary: {
    badge: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-300',
    spine: 'from-primary-500 to-primary-700',
    icon: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-300',
  },
  accent: {
    badge: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
    spine: 'from-accent-500 to-accent-700',
    icon: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
  },
  success: {
    badge: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-300',
    spine: 'from-success-500 to-success-700',
    icon: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-300',
  },
  info: {
    badge: 'bg-info-500/10 text-info-600 dark:bg-info-500/20 dark:text-info-300',
    spine: 'from-info-500 to-info-700',
    icon: 'bg-info-500/10 text-info-600 dark:bg-info-500/20 dark:text-info-300',
  },
};

const KnowledgeGraphVisual = ({ caption }: { caption: string }) => (
  <figure className="relative mx-auto mt-10 max-w-3xl overflow-hidden rounded-3xl border border-slate-200 bg-white/60 p-6 shadow-sm backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/40">
    <div className="ca-grid-mask pointer-events-none absolute inset-0" aria-hidden="true" />
    <svg viewBox="0 0 480 150" className="relative h-auto w-full" aria-hidden="true">
      <defs>
        <linearGradient id="ca-edge" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.05" />
          <stop offset="50%" stopColor="currentColor" stopOpacity="0.45" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0.05" />
        </linearGradient>
      </defs>
      <g
        className="text-primary-500 dark:text-primary-400"
        stroke="url(#ca-edge)"
        strokeWidth="1.5"
      >
        <line x1="60" y1="75" x2="180" y2="40" />
        <line x1="60" y1="75" x2="180" y2="110" />
        <line x1="180" y1="40" x2="300" y2="75" />
        <line x1="180" y1="110" x2="300" y2="75" />
        <line x1="300" y1="75" x2="420" y2="40" />
        <line x1="300" y1="75" x2="420" y2="110" />
      </g>
      <g>
        {[
          { x: 60, y: 75, d: '0s' },
          { x: 180, y: 40, d: '0.4s' },
          { x: 180, y: 110, d: '0.8s' },
          { x: 300, y: 75, d: '1.2s' },
          { x: 420, y: 40, d: '1.6s' },
          { x: 420, y: 110, d: '2s' },
        ].map((n, i) => (
          <g key={i} className="text-primary-600 dark:text-primary-400">
            <circle cx={n.x} cy={n.y} r="11" className="fill-white dark:fill-slate-900" />
            <circle
              cx={n.x}
              cy={n.y}
              r="6"
              className="fill-current"
              style={{ animation: `ca-pulse-soft 1.6s ease-in-out ${n.d} infinite` }}
            />
          </g>
        ))}
      </g>
      <g className="text-accent-500 dark:text-accent-400">
        <circle r="3.5" className="fill-current">
          <animateMotion
            dur="3.6s"
            repeatCount="indefinite"
            path="M60,75 L180,40 L300,75 L420,110"
          />
        </circle>
        <circle r="3.5" className="fill-current">
          <animateMotion
            dur="3.6s"
            begin="1.8s"
            repeatCount="indefinite"
            path="M60,75 L180,110 L300,75 L420,40"
          />
        </circle>
      </g>
    </svg>
    <figcaption className="relative mt-3 text-center text-xs font-medium text-slate-500 dark:text-slate-400">
      {caption}
    </figcaption>
  </figure>
);

export const ArticlesSection = () => {
  const { t } = useTranslation();
  const [activeArticleIndex, setActiveArticleIndex] = useState<number | null>(null);

  const articles = useMemo<Article[]>(
    () => [
      {
        category: t('LandingPage.articles.a1Category'),
        title: t('LandingPage.articles.a1Title'),
        text: t('LandingPage.articles.a1Text'),
        fullText: t('LandingPage.articles.a1FullText'),
        author: t('LandingPage.articles.a1Author'),
        role: t('LandingPage.articles.a1Role'),
        initials: 'AY',
        icon: Network,
        readMinutes: 6,
        tone: TONES.primary,
      },
      {
        category: t('LandingPage.articles.a2Category'),
        title: t('LandingPage.articles.a2Title'),
        text: t('LandingPage.articles.a2Text'),
        fullText: t('LandingPage.articles.a2FullText'),
        author: t('LandingPage.articles.a2Author'),
        role: t('LandingPage.articles.a2Role'),
        initials: 'EK',
        icon: Cpu,
        readMinutes: 8,
        tone: TONES.accent,
      },
      {
        category: t('LandingPage.articles.a3Category'),
        title: t('LandingPage.articles.a3Title'),
        text: t('LandingPage.articles.a3Text'),
        fullText: t('LandingPage.articles.a3FullText'),
        author: t('LandingPage.articles.a3Author'),
        role: t('LandingPage.articles.a3Role'),
        initials: 'CD',
        icon: BookOpen,
        readMinutes: 5,
        tone: TONES.success,
      },
      {
        category: t('LandingPage.articles.a4Category'),
        title: t('LandingPage.articles.a4Title'),
        text: t('LandingPage.articles.a4Text'),
        fullText: t('LandingPage.articles.a4FullText'),
        author: t('LandingPage.articles.a4Author'),
        role: t('LandingPage.articles.a4Role'),
        initials: 'MÇ',
        icon: ShieldCheck,
        readMinutes: 7,
        tone: TONES.info,
      },
    ],
    [t],
  );

  const currentArticle = activeArticleIndex !== null ? articles[activeArticleIndex] : null;

  const minuteLabel = (minutes: number) =>
    t('LandingPage.articles.readingTime', {
      defaultValue: '{{count}} dk okuma',
      count: minutes,
    });

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Library size={12} aria-hidden="true" />
            {t('LandingPage.articles.badge', { defaultValue: 'KAYNAK MERKEZİ' })}
          </>
        }
        title={t('LandingPage.articles.title')}
        subtitle={t('LandingPage.articles.subtitleRich', {
          defaultValue:
            'Bulut ERP, MRP üretim planlama, çoklu kiracı güvenliği ve cam tasarım otomasyonu üzerine eğitici rehberler. Kavramları örneklerle açıklıyor, kurulum kararlarınızı kolaylaştırıyoruz.',
        })}
      />

      <KnowledgeGraphVisual
        caption={t('LandingPage.articles.visualCaption', {
          defaultValue:
            'Sipariş → MRP → stok → muhasebe: tek bir veri akışında birbirine bağlanan modüller.',
        })}
      />

      <div className="ca-stagger mt-12 grid grid-cols-1 gap-6 md:grid-cols-2">
        {articles.map((art, index) => {
          const Icon = art.icon;
          return (
            <article
              key={index}
              className="group relative flex flex-col overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-primary-500/40 hover:shadow-xl hover:shadow-primary-500/10 dark:border-slate-800/80 dark:bg-slate-900/50 dark:shadow-none dark:hover:border-primary-500/40"
            >
              <span
                className={`pointer-events-none absolute inset-y-0 left-0 w-1 bg-gradient-to-b ${art.tone.spine} opacity-70 transition-opacity duration-300 group-hover:opacity-100`}
                aria-hidden="true"
              />
              <div className="flex flex-1 flex-col p-7 md:p-8">
                <div className="mb-4 flex items-center justify-between gap-3">
                  <span
                    className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[11px] font-extrabold tracking-widest ${art.tone.badge}`}
                  >
                    {art.category}
                  </span>
                  <span
                    className={`inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl transition-transform duration-300 group-hover:scale-110 ${art.tone.icon}`}
                    aria-hidden="true"
                  >
                    <Icon size={18} />
                  </span>
                </div>
                <h3 className="mb-3 text-xl font-bold leading-snug text-slate-900 transition-colors duration-300 group-hover:text-primary-600 dark:text-white dark:group-hover:text-primary-300">
                  {art.title}
                </h3>
                <p className="mb-6 line-clamp-3 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                  {art.text}
                </p>
                <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-4 dark:border-slate-800/60">
                  <span className="inline-flex items-center gap-1.5 text-xs font-medium text-slate-500 dark:text-slate-400">
                    <Clock size={13} aria-hidden="true" />
                    {minuteLabel(art.readMinutes)}
                  </span>
                  <button
                    type="button"
                    onClick={() => setActiveArticleIndex(index)}
                    className="inline-flex items-center gap-1.5 rounded-xl border border-primary-500/20 bg-primary-500/5 px-4 py-2 text-xs font-bold text-primary-600 transition hover:bg-primary-600 hover:text-white dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-600 dark:hover:text-white"
                  >
                    <BookOpen size={14} aria-hidden="true" />
                    {t('LandingPage.articles.readMore')}
                    <ArrowRight
                      size={13}
                      aria-hidden="true"
                      className="transition-transform duration-300 group-hover:translate-x-0.5"
                    />
                  </button>
                </div>
              </div>
            </article>
          );
        })}
      </div>

      <div className="mt-12 flex flex-col items-start gap-3 rounded-3xl border border-dashed border-slate-200 bg-slate-50/60 p-6 text-left dark:border-slate-800 dark:bg-slate-900/40 sm:flex-row sm:items-center sm:justify-start">
        <Sparkles size={18} className="text-accent-500 dark:text-accent-400" aria-hidden="true" />
        <p className="text-sm text-slate-600 dark:text-slate-400">
          {t('LandingPage.articles.newsletterPrompt', {
            defaultValue:
              'Yeni rehberler yayınlandıkça haberdar olmak ister misiniz? Erken erişim listesine katılın.',
          })}
        </p>
        <a
          href="#demo"
          className="inline-flex items-center gap-2 rounded-xl bg-primary-600 px-5 py-2.5 text-xs font-semibold text-white shadow-lg shadow-primary-500/30 transition hover:bg-primary-700"
        >
          {t('LandingPage.articles.newsletterCta', { defaultValue: 'Listeye katıl' })}
          <ArrowRight size={14} aria-hidden="true" />
        </a>
      </div>

      {currentArticle && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 backdrop-blur-md"
          role="dialog"
          aria-modal="true"
          aria-label={currentArticle.title}
          onClick={() => setActiveArticleIndex(null)}
        >
          <div
            className="animate-zoom-in relative max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-3xl border border-slate-200/60 bg-white p-6 shadow-2xl dark:border-slate-800/60 dark:bg-slate-900"
            onClick={(event) => event.stopPropagation()}
          >
            <button
              type="button"
              onClick={() => setActiveArticleIndex(null)}
              className="absolute right-4 top-4 rounded-xl p-2 text-slate-500 transition hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
              aria-label={t('LandingPage.articles.close')}
            >
              <X size={20} aria-hidden="true" />
            </button>

            <div className="mb-6 pr-10">
              <div className="mb-3 flex flex-wrap items-center gap-2">
                <span
                  className={`inline-block rounded-full px-3 py-1 text-[11px] font-extrabold tracking-widest ${currentArticle.tone.badge}`}
                >
                  {currentArticle.category}
                </span>
                <span className="inline-flex items-center gap-1.5 text-xs font-medium text-slate-500 dark:text-slate-400">
                  <Clock size={13} aria-hidden="true" />
                  {minuteLabel(currentArticle.readMinutes)}
                </span>
              </div>
              <h3 className="text-2xl font-extrabold leading-tight text-slate-900 dark:text-white">
                {currentArticle.title}
              </h3>
            </div>

            <div className="mb-6 space-y-4 text-sm leading-relaxed text-slate-600 dark:text-slate-300">
              <p className="rounded-2xl bg-slate-50 p-4 font-semibold italic text-slate-700 dark:bg-slate-800/50 dark:text-slate-200">
                {currentArticle.text}
              </p>
              <p className="whitespace-pre-wrap pt-2">{currentArticle.fullText}</p>
            </div>

            <div className="flex items-center justify-between border-t border-slate-100 pt-4 dark:border-slate-800/60">
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
                type="button"
                onClick={() => setActiveArticleIndex(null)}
                className="rounded-xl bg-slate-900 px-5 py-2 text-xs font-bold text-white transition hover:bg-slate-800 dark:bg-slate-800 dark:hover:bg-slate-700"
              >
                {t('LandingPage.articles.close')}
              </button>
            </div>
          </div>
        </div>
      )}
    </Section>
  );
};

export default ArticlesSection;
