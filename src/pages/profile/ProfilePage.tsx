import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Lock, User as UserIcon } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useAuthStore } from '@/shared/lib/store/authStore';
import {
  changePasswordSchema,
  profileSchema,
  type ChangePasswordFormValues,
  type ProfileFormValues,
} from '@/features/auth/model/profileSchemas';
import { useChangePassword, useUpdateProfile } from '@/features/auth/hooks/useAuth';

type Tab = 'profile' | 'security';

export const ProfilePage = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const [tab, setTab] = useState<Tab>('profile');

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('profile.title')}
        </h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {user?.email} · {user?.tenantName}
        </p>
      </div>

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <TabButton active={tab === 'profile'} onClick={() => setTab('profile')}>
          <UserIcon size={14} />
          {t('profile.tabs.profile')}
        </TabButton>
        <TabButton active={tab === 'security'} onClick={() => setTab('security')}>
          <Lock size={14} />
          {t('profile.tabs.security')}
        </TabButton>
      </div>

      {tab === 'profile' ? <ProfileForm /> : <SecurityForm />}
    </div>
  );
};

interface TabButtonProps {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}

const TabButton = ({ active, onClick, children }: TabButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    className={`-mb-px flex items-center gap-2 border-b-2 px-3 py-2 text-sm font-medium transition ${
      active
        ? 'border-primary-500 text-primary-600 dark:text-primary-400'
        : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
    }`}
  >
    {children}
  </button>
);

const ProfileForm = () => {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const updateMutation = useUpdateProfile();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: user?.firstName ?? '',
      lastName: user?.lastName ?? '',
      phoneNumber: '',
      avatarUrl: user?.avatarUrl ?? '',
    },
  });

  useEffect(() => {
    if (user) {
      reset({
        firstName: user.firstName ?? '',
        lastName: user.lastName ?? '',
        phoneNumber: '',
        avatarUrl: user.avatarUrl ?? '',
      });
    }
  }, [user, reset]);

  const onSubmit = handleSubmit((values) => {
    updateMutation.mutate(
      {
        firstName: values.firstName || null,
        lastName: values.lastName || null,
        phoneNumber: values.phoneNumber || null,
        avatarUrl: values.avatarUrl || null,
      },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('profile.toast.profileUpdated'));
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className="max-w-2xl space-y-3 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900"
    >
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Input
          label={t('profile.fields.firstName')}
          placeholder={t('profile.fields.firstNamePlaceholder')}
          autoComplete="given-name"
          error={translateError(errors.firstName?.message)}
          {...register('firstName')}
        />
        <Input
          label={t('profile.fields.lastName')}
          placeholder={t('profile.fields.lastNamePlaceholder')}
          autoComplete="family-name"
          error={translateError(errors.lastName?.message)}
          {...register('lastName')}
        />
      </div>
      <Input
        label={t('profile.fields.phoneNumber')}
        placeholder={t('profile.fields.phonePlaceholder')}
        autoComplete="tel"
        error={translateError(errors.phoneNumber?.message)}
        {...register('phoneNumber')}
      />
      <Input
        label={t('profile.fields.avatarUrl')}
        placeholder="https://..."
        error={translateError(errors.avatarUrl?.message)}
        {...register('avatarUrl')}
      />
      <div className="flex justify-end pt-1">
        <Button
          type="submit"
          isLoading={isSubmitting || updateMutation.isPending}
          disabled={!isDirty}
        >
          {t('common.save')}
        </Button>
      </div>
    </form>
  );
};

const SecurityForm = () => {
  const { t } = useTranslation();
  const changeMutation = useChangePassword();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  });

  const onSubmit = handleSubmit((values) => {
    changeMutation.mutate(
      { currentPassword: values.currentPassword, newPassword: values.newPassword },
      {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('profile.toast.passwordChanged'));
            reset();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      },
    );
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  return (
    <form
      onSubmit={onSubmit}
      noValidate
      className="max-w-md space-y-3 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900"
    >
      <p className="text-xs text-slate-500 dark:text-slate-400">{t('profile.security.note')}</p>
      <Input
        label={t('profile.security.currentPassword')}
        type="password"
        autoComplete="current-password"
        leftIcon={<Lock size={16} />}
        error={translateError(errors.currentPassword?.message)}
        {...register('currentPassword')}
      />
      <Input
        label={t('profile.security.newPassword')}
        type="password"
        autoComplete="new-password"
        leftIcon={<Lock size={16} />}
        error={translateError(errors.newPassword?.message)}
        {...register('newPassword')}
      />
      <Input
        label={t('profile.security.confirmPassword')}
        type="password"
        autoComplete="new-password"
        leftIcon={<Lock size={16} />}
        error={translateError(errors.confirmPassword?.message)}
        {...register('confirmPassword')}
      />
      <div className="flex justify-end pt-1">
        <Button type="submit" isLoading={isSubmitting || changeMutation.isPending}>
          {t('profile.security.submit')}
        </Button>
      </div>
    </form>
  );
};
