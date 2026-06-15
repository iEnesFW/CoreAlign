import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const DEFAULT_DPO_EMAIL = 'dpo@corealign.com';

export const Footer = () => {
  const { t } = useTranslation();
  const year = new Date().getFullYear();
  const links: Array<{ to: string; label: string }> = [
    { to: '/legal/aydinlatma-metni', label: t('legal.aydinlatmaMetni') },
    { to: '/legal/gizlilik-politikasi', label: t('legal.gizlilikPolitikasi') },
    { to: '/legal/kullanim-kosullari', label: t('legal.kullanimKosullari') },
    { to: '/legal/cerez-politikasi', label: t('legal.cerezPolitikasi') },
    { to: '/legal/kvkk-basvuru-formu', label: t('legal.kvkkBasvuruFormu') },
  ];

  return (
    <footer className="border-t border-slate-200/60 bg-white py-4 dark:border-slate-800/60 dark:bg-slate-950">
      <div className="mx-auto flex max-w-7xl flex-col gap-3 px-4 text-xs text-slate-500 dark:text-slate-400 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between sm:px-6 lg:px-10">
        <p>
          &copy; {year} CoreAlign · {t('legal.dpoLabel')}:{' '}
          <a
            href={`mailto:${DEFAULT_DPO_EMAIL}`}
            className="text-indigo-600 hover:underline dark:text-indigo-400"
          >
            {DEFAULT_DPO_EMAIL}
          </a>
        </p>
        <nav aria-label={t('legal.footerNavAria')}>
          <ul className="flex flex-wrap gap-x-4 gap-y-1">
            {links.map((link) => (
              <li key={link.to}>
                <Link to={link.to} className="hover:underline">
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      </div>
    </footer>
  );
};
