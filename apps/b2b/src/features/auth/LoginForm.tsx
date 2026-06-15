import { useMutation } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Spinner } from '@/shared/ui/Spinner';
import { login } from './loginApi';
import { useAuthStore } from './authStore';

export const LoginForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);
  const clearAuth = useAuthStore((s) => s.clearAuth);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const loginMutation = useMutation({
    mutationFn: () => login(email.trim(), password),
    onSuccess: (response) => {
      if (response.user.persona !== 'dealer') {
        clearAuth();
        const message = t('b2b.auth.wrongPersona');
        setError(message);
        toast.error(message);
        return;
      }
      setAuth(response.accessToken, response.expiresAt, response.user);
      navigate('/', { replace: true });
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; message?: string; status?: number };
      const message =
        err.status === 401
          ? t('b2b.auth.loginFailed')
          : (err.normalizedMessage ?? err.message ?? t('b2b.auth.loginFailed'));
      setError(message);
      toast.error(message);
    },
  });

  const submitting = loginMutation.isPending;

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting) return;
    setError(null);
    loginMutation.mutate();
  };

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <Input
        label={t('b2b.auth.email')}
        type="email"
        autoComplete="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        required
        disabled={submitting}
      />
      <Input
        label={t('b2b.auth.password')}
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
        {submitting ? t('b2b.auth.submitting') : t('b2b.auth.submit')}
      </Button>
    </form>
  );
};
