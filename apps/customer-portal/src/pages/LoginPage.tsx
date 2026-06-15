import { Sparkles } from 'lucide-react';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation } from 'react-router-dom';
import { toast } from 'sonner';
import { LoginForm } from '@/features/auth/LoginForm';
import { useAuthStore } from '@/features/auth/authStore';

export const LoginPage = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);

  const wrongPersona = (location.state as { wrongPersona?: boolean } | null)?.wrongPersona;
  useEffect(() => {
    if (wrongPersona) toast.error(t('auth.wrongPersona'));
  }, [wrongPersona, t]);

  if (isAuthenticated && user?.persona === 'customer') {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="grid min-h-screen grid-cols-1 bg-slate-50 lg:grid-cols-2 dark:bg-slate-950">
      <section className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm space-y-8">
          <div className="flex items-center gap-3">
            <span className="inline-flex h-10 w-10 items-center justify-center rounded-2xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white shadow-lg shadow-sky-500/30">
              <Sparkles size={18} />
            </span>
            <div>
              <p className="text-sm font-medium text-slate-500 dark:text-slate-400">
                {t('app.tagline')}
              </p>
              <p className="text-lg font-semibold text-slate-900 dark:text-slate-100">
                {t('app.name')}
              </p>
            </div>
          </div>

          <div>
            <h1 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-slate-100">
              {t('auth.loginTitle')}
            </h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              {t('auth.loginSubtitle')}
            </p>
          </div>

          <LoginForm />
        </div>
      </section>

      <aside className="relative hidden overflow-hidden bg-gradient-to-br from-sky-600 via-indigo-700 to-slate-900 p-12 text-white lg:flex lg:flex-col lg:justify-end">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,_rgba(255,255,255,0.18),_transparent_55%)]" />
        <div className="relative max-w-md space-y-3">
          <p className="text-sm uppercase tracking-widest text-sky-200">CoreAlign</p>
          <h2 className="text-3xl font-semibold leading-tight">{t('app.tagline')}</h2>
          <p className="text-sm text-sky-100/80">{t('auth.loginSubtitle')}</p>
        </div>
      </aside>
    </div>
  );
};
