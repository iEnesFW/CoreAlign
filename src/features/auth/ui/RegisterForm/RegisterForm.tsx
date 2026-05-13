import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useGoogleReCaptcha } from 'react-google-recaptcha-v3';
import { Link } from 'react-router-dom';
import { Building2, Lock, Mail, User, UserCheck } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Logo } from '@/shared/ui/Logo/Logo';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useRegister } from '../../hooks/useAuth';
import { registerSchema, type RegisterFormValues } from '../../model/registerSchema';
import styles from './RegisterForm.module.css';

export const RegisterForm = () => {
  const { t } = useTranslation();
  const registerMutation = useRegister();
  const { executeRecaptcha } = useGoogleReCaptcha();
  const [isRegistered, setIsRegistered] = useState(false);

  const {
    register,
    handleSubmit,
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
      <div className={styles.form}>
        <div className={styles.header}>
          <div className={styles.logoWrapper}>
            <Logo size={42} />
          </div>
          <div className={styles.successMessage}>
            <UserCheck size={48} strokeWidth={1.5} />
            <h2>{t('auth.register.success.title')}</h2>
            <p>{t('auth.register.success.message')}</p>
            <Link to="/login" className={styles.link}>
              {t('auth.register.success.backToLogin')}
            </Link>
          </div>
        </div>
      </div>
    );
  }

  const isBusy = isSubmitting || registerMutation.isPending;

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      <div className={styles.header}>
        <div className={styles.logoWrapper}>
          <Logo size={42} />
        </div>
        <p className={styles.subtitle}>{t('auth.register.subtitle')}</p>
      </div>

      <div className={styles.fields}>
        <Input
          label={t('auth.register.organizationNameLabel')}
          placeholder={t('auth.register.organizationNamePlaceholder')}
          type="text"
          autoComplete="organization"
          leftIcon={<Building2 size={18} />}
          error={translateError(errors.organizationName?.message)}
          {...register('organizationName')}
        />

        <div className={styles.row}>
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

        <Input
          label={t('auth.register.passwordLabel')}
          placeholder={t('auth.register.passwordPlaceholder')}
          type="password"
          autoComplete="new-password"
          leftIcon={<Lock size={18} />}
          error={translateError(errors.password?.message)}
          {...register('password')}
        />

        <Input
          label={t('auth.register.confirmPasswordLabel')}
          placeholder={t('auth.register.confirmPasswordPlaceholder')}
          type="password"
          autoComplete="new-password"
          leftIcon={<Lock size={18} />}
          error={translateError(errors.confirmPassword?.message)}
          {...register('confirmPassword')}
        />
      </div>

      <div className={styles.actions}>
        <Button type="submit" isLoading={isBusy} className={styles.submitButton}>
          {t('auth.register.submitButton')}
        </Button>
      </div>

      <div className={styles.footer}>
        {t('auth.register.haveAccountText')}{' '}
        <Link to="/login" className={styles.link}>
          {t('auth.register.loginLinkText')}
        </Link>
      </div>
    </form>
  );
};
