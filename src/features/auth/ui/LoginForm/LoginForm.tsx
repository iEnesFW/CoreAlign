import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Lock, Mail, KeyRound } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import { generateDeviceFingerprint } from '@/shared/lib/deviceFingerprint';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useLogin } from '../../hooks/useAuth';
import { loginSchema, type LoginFormValues } from '../../model/loginSchema';
import { SsoLoginFormView } from './SsoLoginFormView';
import styles from './LoginForm.module.css';

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
    <div className={styles.formWrapper}>
      <div className={styles.header}>
        <div className={styles.logoWrapper}>
          <Logo size={42} />
        </div>
        <p className={styles.subtitle}>{t('auth.login.subtitle')}</p>
      </div>

      <div className="relative overflow-hidden">
        {view === 'email' ? (
          <div className="animate-in fade-in slide-in-from-left-4 duration-300">
            <form className={styles.form} onSubmit={onSubmit} noValidate>
              <div className={styles.fields}>
                <Input
                  label={t('auth.login.emailLabel')}
                  placeholder={t('auth.login.emailPlaceholder')}
                  type="email"
                  autoComplete="email"
                  leftIcon={<Mail size={18} />}
                  error={translateError(errors.email?.message)}
                  {...register('email')}
                />

                <Input
                  label={t('auth.login.passwordLabel')}
                  placeholder={t('auth.login.passwordPlaceholder')}
                  type="password"
                  autoComplete="current-password"
                  leftIcon={<Lock size={18} />}
                  error={translateError(errors.password?.message)}
                  {...register('password')}
                />
              </div>

              <div className={styles.actions}>
                <Link to="/forgot-password" className={styles.forgotPassword}>
                  {t('auth.login.forgotPassword')}
                </Link>
                <Button type="submit" isLoading={isBusy} className={styles.submitButton}>
                  {t('auth.login.submitButton')}
                </Button>
              </div>
            </form>

            <div className={styles.divider}>
              <span className={styles.dividerText}>{t('auth.login.orDivider')}</span>
            </div>

            <Button
              type="button"
              variant="secondary"
              className="w-full"
              onClick={() => setView('sso')}
            >
              <KeyRound size={16} className="mr-2 opacity-70" />
              {t('auth.login.ssoContinue')}
            </Button>
          </div>
        ) : (
          <SsoLoginFormView onBack={() => setView('email')} />
        )}
      </div>

      {view === 'email' && (
        <div className={styles.footer}>
          {t('auth.login.noAccount')}{' '}
          <Link to="/register" className={styles.link}>
            {t('auth.login.registerLink')}
          </Link>
        </div>
      )}
    </div>
  );
};
