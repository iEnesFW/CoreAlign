import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link, useNavigate } from 'react-router-dom';
import { Lock, Mail } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import { generateDeviceFingerprint } from '@/shared/lib/deviceFingerprint';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useLogin } from '../../hooks/useAuth';
import { loginSchema, type LoginFormValues } from '../../model/loginSchema';
import styles from './LoginForm.module.css';

export const LoginForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const loginMutation = useLogin();
  const { executeRecaptcha } = useGoogleReCaptcha();

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
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      <div className={styles.header}>
        <div className={styles.logoWrapper}>
          <Logo size={42} />
        </div>
        <p className={styles.subtitle}>{t('auth.login.subtitle')}</p>
      </div>

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

      <div className={styles.footer}>
        {t('auth.login.noAccount')}{' '}
        <Link to="/register" className={styles.link}>
          {t('auth.login.registerLink')}
        </Link>
      </div>
    </form>
  );
};
