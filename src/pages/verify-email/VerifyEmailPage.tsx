import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AuthLayout } from '@/widgets/Layout/AuthLayout';
import { Loader2, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Logo } from '@/shared/ui/Logo/Logo';
import { useVerifyEmail } from '@/features/auth/hooks/useAuth';
import { isApiError } from '@/shared/api/ApiError';
import styles from '@/features/auth/ui/ResetPasswordForm/ResetPasswordForm.module.css';

export const VerifyEmailPage = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const navigate = useNavigate();
  const { t } = useTranslation();

  const { mutate } = useVerifyEmail();
  const hasVerified = useRef(false);
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>(
    token ? 'loading' : 'idle',
  );
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (!token || hasVerified.current) return;
    hasVerified.current = true;
    const fallback = t('VerifyEmail.failedFallback');
    mutate(
      { token },
      {
        onSuccess: (response) => {
          if (response.isSuccess && response.data) {
            setStatus('success');
            setTimeout(() => navigate('/login'), 4000);
          } else {
            setStatus('error');
            setErrorMessage(response.errors[0] ?? fallback);
          }
        },
        onError: (error) => {
          setStatus('error');
          setErrorMessage(isApiError(error) ? (error.errors[0] ?? fallback) : fallback);
        },
      },
    );
  }, [token, navigate, mutate, t]);

  const renderContent = () => {
    if (!token) {
      return (
        <div className={styles.successMessage}>
          <AlertCircle size={56} strokeWidth={1.5} className="mb-4 text-danger-400" />
          <h2 className="mb-2 text-2xl font-semibold text-white">
            {t('VerifyEmail.invalidLinkTitle')}
          </h2>
          <p className="mb-6 text-gray-400">{t('VerifyEmail.invalidLinkBody')}</p>
          <Button onClick={() => navigate('/login')} className={styles.submitButton}>
            {t('VerifyEmail.backToLogin')}
          </Button>
        </div>
      );
    }

    if (status === 'loading' || status === 'idle') {
      return (
        <div className={styles.successMessage}>
          <Loader2 size={56} strokeWidth={1.5} className="text-primary mb-4 animate-spin" />
          <h2 className="mb-2 text-2xl font-semibold text-white">
            {t('VerifyEmail.verifyingTitle')}
          </h2>
          <p className="text-gray-400">{t('VerifyEmail.verifyingBody')}</p>
        </div>
      );
    }

    if (status === 'success') {
      return (
        <div className={styles.successMessage}>
          <CheckCircle2 size={56} strokeWidth={1.5} className="mb-4 text-success-400" />
          <h2 className="mb-2 text-2xl font-semibold text-white">
            {t('VerifyEmail.successTitle')}
          </h2>
          <p className="mb-6 text-gray-400">{t('VerifyEmail.successBody')}</p>
          <Button onClick={() => navigate('/login')} className={styles.submitButton}>
            {t('VerifyEmail.loginNow')}
          </Button>
        </div>
      );
    }

    return (
      <div className={styles.successMessage}>
        <XCircle size={56} strokeWidth={1.5} className="mb-4 text-danger-400" />
        <h2 className="mb-2 text-2xl font-semibold text-white">{t('VerifyEmail.failedTitle')}</h2>
        <p className="mb-6 text-sm text-danger-300/80">{errorMessage}</p>
        <Button onClick={() => navigate('/login')} className={styles.submitButton}>
          {t('VerifyEmail.backToLogin')}
        </Button>
      </div>
    );
  };

  return (
    <AuthLayout>
      <div
        className={styles.form}
        style={{ border: 'none', background: 'transparent', boxShadow: 'none' }}
      >
        <div className={styles.header}>
          <div className={styles.logoWrapper}>
            <Logo size={42} />
          </div>
        </div>
        {renderContent()}
      </div>
    </AuthLayout>
  );
};
