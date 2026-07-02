import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link } from 'react-router-dom';
import { Mail, MailCheck, ArrowLeft, AlertCircle } from 'lucide-react';
import type { AxiosError } from 'axios';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { useForgotPassword } from '../../hooks/useAuth';
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
    [email, forgotPasswordMutation, t, executeRecaptcha],
  );

  if (isSent) {
    return (
      <div className="flex flex-col items-center text-center">
        <div className="grid h-16 w-16 place-items-center rounded-full bg-success-50 text-success-600 dark:bg-success-500/15 dark:text-success-400">
          <MailCheck size={32} strokeWidth={1.6} />
        </div>
        <h1 className="mt-6 text-[26px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.forgotPassword.success.title')}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] leading-relaxed text-slate-500 dark:text-slate-400">
          {t('auth.forgotPassword.success.message')}
        </p>
        <Link
          to="/login"
          className="mt-7 inline-flex items-center gap-2 text-[14px] font-semibold text-primary-600 hover:underline dark:text-primary-300"
        >
          <ArrowLeft size={16} />
          {t('auth.forgotPassword.success.backToLogin')}
        </Link>
      </div>
    );
  }

  return (
    <div className="flex w-full flex-col">
      <div className="mb-8 text-center">
        <h1 className="m-0 text-[30px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.forgotPassword.title', { defaultValue: 'Şifrenizi mi unuttunuz?' })}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] text-slate-500 dark:text-slate-400">
          {t('auth.forgotPassword.subtitle')}
        </p>
      </div>

      {serverError && (
        <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-3.5 py-3 text-[13px] text-danger-700 dark:border-danger-500/30 dark:bg-danger-500/10 dark:text-danger-300">
          <AlertCircle size={16} className="mt-0.5 shrink-0" />
          <span>{serverError}</span>
        </div>
      )}

      <form className="flex flex-col gap-[18px]" onSubmit={handleSubmit} noValidate>
        <Input
          label={t('auth.forgotPassword.emailLabel')}
          placeholder={t('auth.forgotPassword.emailPlaceholder')}
          type="email"
          autoComplete="email"
          leftIcon={<Mail size={18} />}
          value={email}
          onChange={handleEmailChange}
        />

        <Button
          type="submit"
          isLoading={forgotPasswordMutation.isPending}
          size="lg"
          className="mt-1 w-full"
        >
          {t('auth.forgotPassword.submitButton')}
        </Button>
      </form>

      <div className="mt-7 text-center">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-[14px] font-semibold text-slate-500 transition-colors hover:text-slate-800 dark:text-slate-400 dark:hover:text-white"
        >
          <ArrowLeft size={16} />
          {t('auth.forgotPassword.backToLogin')}
        </Link>
      </div>
    </div>
  );
};
