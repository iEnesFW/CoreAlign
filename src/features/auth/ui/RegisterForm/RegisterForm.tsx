import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link } from 'react-router-dom';
import { Building2, Lock, Mail, User, ArrowRight, ArrowLeft, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { PasswordInput } from '@/shared/ui/Input/PasswordInput';
import { PasswordStrength } from '@/shared/ui/Input/PasswordStrength';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useRegister } from '../../hooks/useAuth';
import { registerSchema, type RegisterFormValues } from '../../model/registerSchema';

export const RegisterForm = () => {
  const { t } = useTranslation();
  const registerMutation = useRegister();
  const { executeRecaptcha } = useGoogleReCaptcha();
  const [isRegistered, setIsRegistered] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      organizationName: '',
      firstName: '',
      lastName: '',
      username: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
  });

  const passwordValue = useWatch({ control, name: 'password' });

  const onSubmit = handleSubmit(async (values) => {
    const captchaToken = await executeRecaptcha?.('register');

    registerMutation.mutate(
      {
        organizationName: values.organizationName,
        username: values.username,
        email: values.email,
        password: values.password,
        firstName: values.firstName || undefined,
        lastName: values.lastName || undefined,
        captchaToken: captchaToken ?? undefined,
      },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            setIsRegistered(true);
            return;
          }
          toast.error(response.errors[0] ?? t('auth.register.errors.registerFailed'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (isRegistered) {
    return (
      <div className="flex flex-col items-center text-center">
        <div className="grid h-16 w-16 place-items-center rounded-full bg-success-50 text-success-600 dark:bg-success-500/15 dark:text-success-400">
          <CheckCircle2 size={34} strokeWidth={1.6} />
        </div>
        <h1 className="mt-6 text-[26px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.register.success.title')}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] leading-relaxed text-slate-500 dark:text-slate-400">
          {t('auth.register.success.message')}
        </p>
        <Link
          to="/login"
          className="mt-7 inline-flex items-center gap-2 text-[14px] font-semibold text-primary-600 hover:underline dark:text-primary-300"
        >
          <ArrowLeft size={16} />
          {t('auth.register.success.backToLogin')}
        </Link>
      </div>
    );
  }

  const isBusy = isSubmitting || registerMutation.isPending;

  return (
    <div className="flex w-full flex-col">
      <div className="mb-8 text-center">
        <h1 className="m-0 text-[30px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
          {t('auth.register.title', { defaultValue: 'Hesabınızı oluşturun' })}
        </h1>
        <p className="mx-auto mt-2.5 max-w-[340px] text-[15px] text-slate-500 dark:text-slate-400">
          {t('auth.register.subtitle')}
        </p>
      </div>

      <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
        <Input
          label={t('auth.register.organizationNameLabel')}
          placeholder={t('auth.register.organizationNamePlaceholder')}
          type="text"
          autoComplete="organization"
          leftIcon={<Building2 size={18} />}
          error={translateError(errors.organizationName?.message)}
          {...register('organizationName')}
        />

        <div className="grid grid-cols-2 gap-3">
          <Input
            label={t('auth.register.firstNameLabel')}
            placeholder={t('auth.register.firstNamePlaceholder')}
            type="text"
            autoComplete="given-name"
            error={translateError(errors.firstName?.message)}
            {...register('firstName')}
          />
          <Input
            label={t('auth.register.lastNameLabel')}
            placeholder={t('auth.register.lastNamePlaceholder')}
            type="text"
            autoComplete="family-name"
            error={translateError(errors.lastName?.message)}
            {...register('lastName')}
          />
        </div>

        <Input
          label={t('auth.register.usernameLabel')}
          placeholder={t('auth.register.usernamePlaceholder')}
          type="text"
          autoComplete="username"
          leftIcon={<User size={18} />}
          error={translateError(errors.username?.message)}
          {...register('username')}
        />

        <Input
          label={t('auth.register.emailLabel')}
          placeholder={t('auth.register.emailPlaceholder')}
          type="email"
          autoComplete="email"
          leftIcon={<Mail size={18} />}
          error={translateError(errors.email?.message)}
          {...register('email')}
        />

        <div>
          <PasswordInput
            label={t('auth.register.passwordLabel')}
            placeholder={t('auth.register.passwordPlaceholder')}
            autoComplete="new-password"
            leftIcon={<Lock size={18} />}
            error={translateError(errors.password?.message)}
            {...register('password')}
          />
          <PasswordStrength value={passwordValue} />
        </div>

        <PasswordInput
          label={t('auth.register.confirmPasswordLabel')}
          placeholder={t('auth.register.confirmPasswordPlaceholder')}
          autoComplete="new-password"
          leftIcon={<Lock size={18} />}
          error={translateError(errors.confirmPassword?.message)}
          {...register('confirmPassword')}
        />

        <Button type="submit" isLoading={isBusy} size="lg" className="mt-1 w-full">
          {t('auth.register.submitButton')}
          <ArrowRight size={18} />
        </Button>
      </form>

      <p className="mt-7 text-center text-[14px] text-slate-500 dark:text-slate-400">
        {t('auth.register.haveAccountText')}{' '}
        <Link
          to="/login"
          className="font-bold text-slate-900 hover:text-primary-600 dark:text-white dark:hover:text-primary-300"
        >
          {t('auth.register.loginLinkText')}
        </Link>
      </p>
    </div>
  );
};
