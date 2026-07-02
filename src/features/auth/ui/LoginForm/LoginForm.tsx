import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Lock, Mail, KeyRound, ArrowRight } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { PasswordInput } from '@/shared/ui/Input/PasswordInput';
import { generateDeviceFingerprint } from '@/shared/lib/deviceFingerprint';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useLogin } from '../../hooks/useAuth';
import { loginSchema, type LoginFormValues } from '../../model/loginSchema';
import { SsoLoginFormView } from './SsoLoginFormView';

export const LoginForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const loginMutation = useLogin();
  const { executeRecaptcha } = useGoogleReCaptcha();
  const [view, setView] = useState<'email' | 'sso'>(() =>
    new URLSearchParams(window.location.search).get('error') ? 'sso' : 'email',
  );

  useEffect(() => {
    const error = searchParams.get('error');
    if (error) {
      if (error === 'InvalidSsoProviderOrTenant') {
        toast.error(t('auth.login.errors.invalidSsoProviderOrTenant'));
      } else if (error === 'SsoConfigurationError') {
        toast.error(t('auth.login.errors.ssoConfigurationError'));
      } else {
        toast.error(t('auth.login.errors.loginFailed'));
      }

      searchParams.delete('error');
      setSearchParams(searchParams, { replace: true });
    }
  }, [searchParams, setSearchParams, t]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '', rememberMe: false },
  });

  const onSubmit = handleSubmit(async (values) => {
    const [captchaToken, deviceFingerprint] = await Promise.all([
      executeRecaptcha?.('login'),
      generateDeviceFingerprint(),
    ]);

    loginMutation.mutate(
      {
        email: values.email,
        password: values.password,
        rememberMe: values.rememberMe,
        captchaToken: captchaToken ?? undefined,
        deviceFingerprint,
      },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            navigate('/dashboard');
            return;
          }
          toast.error(response.errors[0] ?? t('auth.login.errors.loginFailed'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  const isBusy = isSubmitting || loginMutation.isPending;

  return (
    <div className="flex w-full flex-col">
      {/* heading */}
      <div className="mb-8 text-center">
        <h1 className="m-0 text-[30px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.login.title')}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[320px] text-[15px] text-slate-500 dark:text-slate-400">
          {t('auth.login.subtitle')}
        </p>
      </div>

      <div className="relative overflow-hidden">
        {view === 'email' ? (
          <div className="animate-in fade-in slide-in-from-left-4 duration-300">
            <form className="flex flex-col gap-[18px]" onSubmit={onSubmit} noValidate>
              <Input
                label={t('auth.login.emailLabel')}
                placeholder={t('auth.login.emailPlaceholder')}
                type="email"
                autoComplete="email"
                leftIcon={<Mail size={18} />}
                error={translateError(errors.email?.message)}
                {...register('email')}
              />

              <PasswordInput
                label={t('auth.login.passwordLabel')}
                placeholder={t('auth.login.passwordPlaceholder')}
                autoComplete="current-password"
                leftIcon={<Lock size={18} />}
                error={translateError(errors.password?.message)}
                {...register('password')}
              />

              <div className="flex items-center justify-between">
                <label className="flex cursor-pointer select-none items-center gap-2 text-[13.5px] text-slate-600 dark:text-slate-300">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 accent-primary-600 dark:border-slate-600"
                    {...register('rememberMe')}
                  />
                  {t('auth.login.rememberMe')}
                </label>
                <Link
                  to="/forgot-password"
                  className="text-[13.5px] font-semibold text-primary-600 hover:underline dark:text-primary-300"
                >
                  {t('auth.login.forgotPassword')}
                </Link>
              </div>

              <Button type="submit" isLoading={isBusy} size="lg" className="mt-1 w-full">
                {t('auth.login.submitButton')}
                <ArrowRight size={18} />
              </Button>
            </form>

            <div className="my-6 flex items-center gap-3.5">
              <span className="h-px flex-1 bg-slate-200 dark:bg-white/10" />
              <span className="text-[11.5px] font-semibold uppercase tracking-[0.12em] text-slate-400 dark:text-slate-500">
                {t('auth.login.orDivider')}
              </span>
              <span className="h-px flex-1 bg-slate-200 dark:bg-white/10" />
            </div>

            <Button
              type="button"
              variant="secondary"
              size="lg"
              className="w-full"
              onClick={() => setView('sso')}
            >
              <KeyRound size={16} className="opacity-70" />
              {t('auth.login.ssoContinue')}
            </Button>

            <p className="mt-7 text-center text-[14px] text-slate-500 dark:text-slate-400">
              {t('auth.login.noAccount')}{' '}
              <Link
                to="/register"
                className="font-bold text-slate-900 hover:text-primary-600 dark:text-white dark:hover:text-primary-300"
              >
                {t('auth.login.registerLink')}
              </Link>
            </p>
            <p className="mx-auto mt-4 max-w-[330px] text-center text-[11.5px] leading-relaxed text-slate-400 dark:text-slate-500">
              {t('auth.login.legalNotice', {
                defaultValue:
                  "Giriş yaparak Kullanım Koşulları ve Gizlilik Politikası'nı kabul etmiş olursunuz.",
              })}
            </p>
          </div>
        ) : (
          <SsoLoginFormView onBack={() => setView('email')} />
        )}
      </div>
    </div>
  );
};
