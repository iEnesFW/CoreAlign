import { useState, useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { Lock, ShieldCheck, ArrowLeft, AlertCircle } from 'lucide-react';
import type { AxiosError } from 'axios';
import { Button } from '@/shared/ui/Button/Button';
import { PasswordInput } from '@/shared/ui/Input/PasswordInput';
import { PasswordStrength } from '@/shared/ui/Input/PasswordStrength';
import { useResetPassword } from '../../hooks/useAuth';
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
          onSuccess: () => setIsSent(true),
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
      <div className="flex flex-col items-center text-center">
        <div className="grid h-16 w-16 place-items-center rounded-full bg-success-50 text-success-600 dark:bg-success-500/15 dark:text-success-400">
          <ShieldCheck size={32} strokeWidth={1.6} />
        </div>
        <h1 className="mt-6 text-[26px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.resetPassword.success.title')}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] leading-relaxed text-slate-500 dark:text-slate-400">
          {t('auth.resetPassword.success.message')}
        </p>
        <Button onClick={() => navigate('/login')} size="lg" className="mt-7 w-full max-w-[280px]">
          {t('auth.resetPassword.success.backToLogin')}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex w-full flex-col">
      <div className="mb-8 text-center">
        <h1 className="m-0 text-[30px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.resetPassword.title', { defaultValue: 'Yeni şifre belirleyin' })}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] text-slate-500 dark:text-slate-400">
          {t('auth.resetPassword.subtitle')}
        </p>
      </div>

      {serverError && (
        <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-3.5 py-3 text-[13px] text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300">
          <AlertCircle size={16} className="mt-0.5 shrink-0" />
          <span>{serverError}</span>
        </div>
      )}

      <form className="flex flex-col gap-[18px]" onSubmit={handleSubmit} noValidate>
        <div>
          <PasswordInput
            label={t('auth.resetPassword.passwordLabel')}
            placeholder={t('auth.resetPassword.passwordPlaceholder')}
            autoComplete="new-password"
            leftIcon={<Lock size={18} />}
            value={password}
            onChange={handlePasswordChange}
            disabled={!token}
          />
          <PasswordStrength value={password} />
        </div>

        <PasswordInput
          label={t('auth.resetPassword.confirmPasswordLabel')}
          placeholder={t('auth.resetPassword.confirmPasswordPlaceholder')}
          autoComplete="new-password"
          leftIcon={<Lock size={18} />}
          value={confirmPassword}
          onChange={handleConfirmPasswordChange}
          disabled={!token}
        />

        <Button
          type="submit"
          isLoading={resetPasswordMutation.isPending}
          size="lg"
          className="mt-1 w-full"
          disabled={!token}
        >
          {t('auth.resetPassword.submitButton')}
        </Button>
      </form>

      <div className="mt-7 text-center">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-[14px] font-semibold text-slate-500 transition-colors hover:text-slate-800 dark:text-slate-400 dark:hover:text-white"
        >
          <ArrowLeft size={16} />
          {t('auth.resetPassword.backToLogin')}
        </Link>
      </div>
    </div>
  );
};
