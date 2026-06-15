import { LogOut, Menu, Store } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { useAuthStore } from '@/features/auth/authStore';
import { cn } from '@/shared/lib/cn';

interface TopbarProps {
  onOpenSidebar: () => void;
}

const LANG_STORAGE_KEY = 'corealign.b2b.lang';

export const Topbar = ({ onOpenSidebar }: TopbarProps) => {
  const { t, i18n } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);

  const onLogout = () => {
    clearAuth();
    toast.success(t('b2b.auth.loggedOut'));
    navigate('/login', { replace: true });
  };

  const switchLanguage = async (lng: 'tr' | 'en') => {
    if (i18n.language === lng) return;
    await i18n.changeLanguage(lng);
    window.localStorage.setItem(LANG_STORAGE_KEY, lng);
  };

  const initials = (user?.firstName?.[0] ?? user?.email?.[0] ?? '?').toUpperCase();
  const fullName =
    [user?.firstName, user?.lastName].filter(Boolean).join(' ').trim() || user?.email || '';

  return (
    <header className="sticky top-0 z-20 flex h-16 items-center justify-between gap-3 border-b border-slate-100 bg-white/80 px-4 backdrop-blur lg:px-8 dark:border-slate-800 dark:bg-slate-950/80">
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={onOpenSidebar}
          className="rounded-lg p-2 text-slate-500 hover:bg-slate-100 lg:hidden dark:hover:bg-slate-800"
          aria-label={t('b2b.common.openMenu')}
        >
          <Menu size={18} />
        </button>
        <div className="flex items-center gap-2">
          <span className="inline-flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-amber-500 to-rose-600 text-white shadow-md shadow-amber-500/20">
            <Store size={16} />
          </span>
          <div className="hidden flex-col sm:flex">
            <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t('b2b.app.name')}
            </span>
            <span className="text-xs text-slate-500 dark:text-slate-400">
              {t('b2b.app.tagline')}
            </span>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <div
          className="inline-flex overflow-hidden rounded-xl border border-slate-200 text-xs font-medium dark:border-slate-700"
          aria-label={t('b2b.common.language')}
        >
          {(['tr', 'en'] as const).map((lng) => (
            <button
              key={lng}
              type="button"
              onClick={() => void switchLanguage(lng)}
              className={cn(
                'px-3 py-1.5 uppercase transition',
                i18n.language === lng || i18n.language?.startsWith(lng)
                  ? 'bg-amber-600 text-white'
                  : 'bg-white text-slate-600 hover:bg-slate-50 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800',
              )}
            >
              {lng}
            </button>
          ))}
        </div>

        <div className="relative">
          <button
            type="button"
            onClick={() => setMenuOpen((prev) => !prev)}
            className="flex items-center gap-2 rounded-xl bg-slate-100 px-2.5 py-1.5 text-sm text-slate-700 transition hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
            aria-haspopup="menu"
            aria-expanded={menuOpen}
          >
            <span className="inline-flex h-7 w-7 items-center justify-center rounded-full bg-amber-600 text-xs font-semibold text-white">
              {initials}
            </span>
            <span className="hidden text-sm font-medium sm:inline">{fullName}</span>
          </button>
          {menuOpen ? (
            <div
              className="absolute right-0 mt-2 w-56 overflow-hidden rounded-xl border border-slate-100 bg-white shadow-lg dark:border-slate-800 dark:bg-slate-900"
              role="menu"
              onClick={() => setMenuOpen(false)}
            >
              <div className="border-b border-slate-100 px-4 py-3 text-xs text-slate-500 dark:border-slate-800 dark:text-slate-400">
                {user?.email}
              </div>
              <button
                type="button"
                onClick={onLogout}
                className="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/30"
                role="menuitem"
              >
                <LogOut size={14} />
                {t('b2b.common.logout')}
              </button>
            </div>
          ) : null}
        </div>
      </div>
    </header>
  );
};
