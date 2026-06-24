import { useMutation } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Spinner } from '@/shared/ui/Spinner';
import { completeTwoFactorChallenge, login, type LoginResponse } from './loginApi';
import { useAuthStore } from './authStore';

export const LoginForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);
  const clearAuth = useAuthStore((s) => s.clearAuth);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [challengeToken, setChallengeToken] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [useBackupCode, setUseBackupCode] = useState(false);

  const finishLogin = (response: LoginResponse) => {
    if (!response.user || response.user.persona !== 'customer') {
      clearAuth();
      const message = t('auth.wrongPersona');
      setError(message);
      toast.error(message);
      return;
    }
    setAuth(response.accessToken, response.expiresAt, response.user);
    navigate('/', { replace: true });
  };

  const errorMessage = (caught: unknown) => {
    const err = caught as { normalizedMessage?: string; message?: string; status?: number };
    return err.status === 401
      ? t('auth.loginFailed')
      : (err.normalizedMessage ?? err.message ?? t('auth.loginFailed'));
  };

  const loginMutation = useMutation({
    mutationFn: () => login(email.trim(), password),
    onSuccess: (response) => {
      if (response.requiresTwoFactor && response.twoFactorChallengeToken) {
        setChallengeToken(response.twoFactorChallengeToken);
        setError(null);
        return;
      }
      finishLogin(response);
    },
    onError: (caught: unknown) => {
      const message = errorMessage(caught);
      setError(message);
      toast.error(message);
    },
  });

  const challengeMutation = useMutation({
    mutationFn: () =>
      completeTwoFactorChallenge(
        challengeToken!,
        useBackupCode ? { backupCode: code.trim() } : { code: code.trim() },
      ),
    onSuccess: finishLogin,
    onError: (caught: unknown) => {
      const err = caught as { status?: number };
      const message = err.status === 401 ? t('auth.twoFactorInvalid') : errorMessage(caught);
      setError(message);
      toast.error(message);
    },
  });

  const submitting = loginMutation.isPending || challengeMutation.isPending;

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting) return;
    setError(null);
    if (challengeToken) {
      challengeMutation.mutate();
    } else {
      loginMutation.mutate();
    }
  };

  const backToLogin = () => {
    setChallengeToken(null);
    setCode('');
    setUseBackupCode(false);
    setError(null);
  };

  const toggleBackupCode = () => {
    setUseBackupCode((prev) => !prev);
    setCode('');
    setError(null);
  };

  if (challengeToken) {
    return (
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <div className="text-sm text-slate-600 dark:text-slate-300">
          {useBackupCode ? t('auth.twoFactorBackupHint') : t('auth.twoFactorHint')}
        </div>
        <Input
          label={useBackupCode ? t('auth.twoFactorBackupCode') : t('auth.twoFactorCode')}
          type="text"
          inputMode={useBackupCode ? 'text' : 'numeric'}
          autoComplete="one-time-code"
          maxLength={useBackupCode ? 16 : 6}
          value={code}
          onChange={(event) =>
            setCode(
              useBackupCode ? event.target.value.trim() : event.target.value.replace(/\D/g, ''),
            )
          }
          required
          disabled={submitting}
          error={error ?? undefined}
        />
        <Button type="submit" size="lg" disabled={submitting} className="mt-2 w-full">
          {submitting ? <Spinner size={16} className="text-white" /> : null}
          {submitting ? t('auth.submitting') : t('auth.twoFactorSubmit')}
        </Button>
        <button
          type="button"
          onClick={toggleBackupCode}
          disabled={submitting}
          className="text-sm text-slate-500 underline hover:text-slate-700 disabled:opacity-50 dark:text-slate-400 dark:hover:text-slate-200"
        >
          {useBackupCode ? t('auth.twoFactorUseApp') : t('auth.twoFactorUseBackup')}
        </button>
        <button
          type="button"
          onClick={backToLogin}
          disabled={submitting}
          className="text-sm text-slate-500 underline hover:text-slate-700 disabled:opacity-50 dark:text-slate-400 dark:hover:text-slate-200"
        >
          {t('auth.twoFactorBack')}
        </button>
      </form>
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <Input
        label={t('auth.email')}
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        required
        disabled={submitting}
      />
      <Input
        label={t('auth.password')}
        type="password"
        autoComplete="current-password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        required
        disabled={submitting}
        error={error ?? undefined}
      />
      <Button type="submit" size="lg" disabled={submitting} className="mt-2 w-full">
        {submitting ? <Spinner size={16} className="text-white" /> : null}
        {submitting ? t('auth.submitting') : t('auth.submit')}
      </Button>
    </form>
  );
};
