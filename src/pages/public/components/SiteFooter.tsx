import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Globe, ShieldCheck } from 'lucide-react';
import { Logo } from '@/shared/ui/Logo/Logo';

export const SiteFooter = ({ prefix = '' }: { prefix?: string }) => {
  const { t } = useTranslation();
  const home = prefix || '/';

  const columns = [
    {
      title: t('LandingPage.footer.colProduct'),
      links: [
        { label: t('LandingPage.nav.solutions'), to: `${prefix}/solutions`, anchor: false },
        { label: t('LandingPage.footer.modules'), to: `${home}#modules`, anchor: true },
        { label: t('LandingPage.footer.pricing'), to: `${home}#pricing`, anchor: true },
        { label: t('LandingPage.footer.demo'), to: `${home}#demo`, anchor: true },
      ],
    },
    {
      title: t('LandingPage.footer.colCompany'),
      links: [
        { label: t('LandingPage.nav.about'), to: `${prefix}/about`, anchor: false },
        { label: t('LandingPage.nav.articles'), to: `${prefix}/articles`, anchor: false },
        { label: t('LandingPage.nav.contact'), to: `${prefix}/contact`, anchor: false },
      ],
    },
    {
      title: t('LandingPage.footer.colAccount'),
      links: [
        { label: t('LandingPage.footer.login'), to: '/login', anchor: false },
        { label: t('LandingPage.footer.register'), to: '/register', anchor: false },
      ],
    },
  ];

  return (
    <footer className="relative border-t border-slate-200/70 bg-white/55 px-6 pb-10 pt-16 backdrop-blur-sm sm:px-10 lg:px-16 dark:border-white/5 dark:bg-slate-950/40">
      <div className="mx-auto grid w-full max-w-7xl grid-cols-2 gap-8 md:grid-cols-5">
        <div className="col-span-2">
          <Link to={home} className="flex items-center" aria-label="CoreAlign">
            <Logo size={30} showText />
          </Link>
          <p className="mt-4 max-w-xs text-sm leading-relaxed text-slate-500 dark:text-slate-400">
            {t('LandingPage.footer.tagline')}
          </p>
          <div className="mt-5 flex flex-wrap gap-2">
            <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-200/70 bg-white/60 px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:border-white/10 dark:bg-white/5 dark:text-slate-300">
              <ShieldCheck size={12} className="text-success-500" />
              {t('LandingPage.footer.kvkk')}
            </span>
            <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-200/70 bg-white/60 px-2.5 py-1 text-[11px] font-semibold text-slate-600 dark:border-white/10 dark:bg-white/5 dark:text-slate-300">
              <Globe size={12} className="text-primary-500" />
              {t('LandingPage.footer.languages')}
            </span>
          </div>
        </div>

        {columns.map((col) => (
          <div key={col.title}>
            <h3 className="mb-4 text-[11px] font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500">
              {col.title}
            </h3>
            <ul className="space-y-2.5">
              {col.links.map((link) =>
                link.anchor ? (
                  <li key={link.label}>
                    <a
                      href={link.to}
                      className="text-sm text-slate-600 transition-colors hover:text-primary-600 dark:text-slate-400 dark:hover:text-primary-300"
                    >
                      {link.label}
                    </a>
                  </li>
                ) : (
                  <li key={link.label}>
                    <Link
                      to={link.to}
                      className="text-sm text-slate-600 transition-colors hover:text-primary-600 dark:text-slate-400 dark:hover:text-primary-300"
                    >
                      {link.label}
                    </Link>
                  </li>
                ),
              )}
            </ul>
          </div>
        ))}
      </div>

      <div className="mx-auto mt-12 flex w-full max-w-7xl flex-col items-center justify-between gap-3 border-t border-slate-200/60 pt-6 text-sm text-slate-500 sm:flex-row dark:border-white/5 dark:text-slate-400">
        <span>
          © {new Date().getFullYear()} CoreAlign. {t('LandingPage.footer.rights')}
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-success-400 opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-success-500" />
          </span>
          {t('LandingPage.footer.status')}
        </span>
      </div>
    </footer>
  );
};
