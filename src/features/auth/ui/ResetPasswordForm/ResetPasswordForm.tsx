import { useState, useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import styles from './ResetPasswordForm.module.css';
import { Lock, CheckCircle } from 'lucide-react';
import { useResetPassword } from '../../hooks/useAuth';
import type { AxiosError } from 'axios';
import type { ApiResponse } from '../../model/auth.types';

export const ResetPasswordForm = () => {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSent, setIsSent] = useState(false);

  const resetPasswordMutation = useResetPassword();
  const token = searchParams.get('token');

  useEffect(() => {
    if (!token) {
      // Use setTimeout to avoid synchronous setState during render
      const timer = setTimeout(() => {
        setServerError(t('auth.resetPassword.errors.invalidToken'));
      }, 0);
      return () => clearTimeout(timer);
    }
  }, [token, t]);

  const handlePasswordChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setPassword(e.target.value);
  }, []);

  const handleConfirmPasswordChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setConfirmPassword(e.target.value);
  }, []);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setServerError(null);

      if (!token) {
        setServerError(t('auth.resetPassword.errors.invalidToken'));
        return;
      }

      if (password !== confirmPassword) {
        setServerError(t('auth.resetPassword.errors.passwordMismatch'));
        return;
      }

      if (password.length < 8) {
        setServerError(t('auth.resetPassword.errors.passwordLength'));
        return;
      }

      resetPasswordMutation.mutate(
        { token, newPassword: password },
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
    [password, confirmPassword, token, resetPasswordMutation, t],
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
            <h2>{t('auth.resetPassword.success.title')}</h2>
            <p>{t('auth.resetPassword.success.message')}</p>
            <Button onClick={() => navigate('/login')} className={styles.submitButton}>
              {t('auth.resetPassword.success.backToLogin')}
            </Button>
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
        <p className={styles.subtitle}>{t('auth.resetPassword.subtitle')}</p>
      </div>

      {serverError && <div className={styles.errorBanner}>{serverError}</div>}

      <div className={styles.fields}>
        <Input
          label={t('auth.resetPassword.passwordLabel')}
          placeholder={t('auth.resetPassword.passwordPlaceholder')}
          type="password"
          leftIcon={<Lock size={18} />}
          value={password}
          onChange={handlePasswordChange}
          disabled={!token}
        />

        <Input
          label={t('auth.resetPassword.confirmPasswordLabel')}
          placeholder={t('auth.resetPassword.confirmPasswordPlaceholder')}
          type="password"
          leftIcon={<Lock size={18} />}
          value={confirmPassword}
          onChange={handleConfirmPasswordChange}
          disabled={!token}
        />
      </div>

      <div className={styles.actions}>
        <Button
          type="submit"
          isLoading={resetPasswordMutation.isPending}
          className={styles.submitButton}
          disabled={!token}
        >
          {t('auth.resetPassword.submitButton')}
        </Button>
      </div>

      <div className={styles.footer}>
        <Link to="/login" className={styles.link}>
          {t('auth.resetPassword.backToLogin')}
        </Link>
      </div>
    </form>
  );
};
