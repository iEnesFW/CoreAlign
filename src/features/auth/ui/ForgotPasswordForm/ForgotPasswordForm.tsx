import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import styles from './ForgotPasswordForm.module.css';
import { Mail, CheckCircle } from 'lucide-react';
import { useForgotPassword } from '../../hooks/useAuth';
import { Link } from 'react-router-dom';
import type { AxiosError } from 'axios';
import type { ApiResponse } from '../../model/auth.types';

export const ForgotPasswordForm = () => {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSent, setIsSent] = useState(false);
  const forgotPasswordMutation = useForgotPassword();
  const { executeRecaptcha } = useGoogleReCaptcha();

  const handleEmailChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setEmail(e.target.value);
  }, []);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setServerError(null);

      const captchaToken = await executeRecaptcha?.('forgot_password');

      forgotPasswordMutation.mutate(
        { email, captchaToken: captchaToken ?? undefined },
        {
          onSuccess: () => {
            setIsSent(true);
          },
          onError: (error: Error) => {
            const axiosError = error as AxiosError<ApiResponse<unknown>>;
            const message =
              axiosError.response?.data?.errors?.[0] || t('auth.common.unexpectedError');
            setServerError(message);
          },
        },
      );
    },
    [email, forgotPasswordMutation, t, executeRecaptcha],
  );

  if (isSent) {
    return (
      <div className={styles.form}>
        <div className={styles.header}>
          <div className={styles.logoWrapper}>
            <Logo size={42} />
          </div>
          <div className={styles.successMessage}>
            <CheckCircle size={48} strokeWidth={1.5} />
            <h2>{t('auth.forgotPassword.success.title')}</h2>
            <p>{t('auth.forgotPassword.success.message')}</p>
            <Link to="/login" className={styles.link}>
              {t('auth.forgotPassword.success.backToLogin')}
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <div className={styles.header}>
        <div className={styles.logoWrapper}>
          <Logo size={42} />
        </div>
        <p className={styles.subtitle}>{t('auth.forgotPassword.subtitle')}</p>
      </div>

      {serverError && <div className={styles.errorBanner}>{serverError}</div>}

      <div className={styles.fields}>
        <Input
          label={t('auth.forgotPassword.emailLabel')}
          placeholder={t('auth.forgotPassword.emailPlaceholder')}
          type="email"
          leftIcon={<Mail size={18} />}
          value={email}
          onChange={handleEmailChange}
        />
      </div>

      <div className={styles.actions}>
        <Button
          type="submit"
          isLoading={forgotPasswordMutation.isPending}
          className={styles.submitButton}
        >
          {t('auth.forgotPassword.submitButton')}
        </Button>
      </div>

      <div className={styles.footer}>
        <Link to="/login" className={styles.link}>
          {t('auth.forgotPassword.backToLogin')}
        </Link>
      </div>
    </form>
  );
};
