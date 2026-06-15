import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Bell, LogOut, ShieldCheck, UserCog } from 'lucide-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Input } from '@/shared/ui/Input';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { apiClient } from '@/shared/api/apiClient';
import { useAuthStore } from '@/features/auth/authStore';
import { useDealerProfile } from '@/features/portal/hooks';

type Tab = 'profile' | 'security' | 'notifications';

export const ProfilePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const user = useAuthStore((s) => s.user);
  const [tab, setTab] = useState<Tab>('profile');

  if (!user) return null;

  const onLogout = () => {
    clearAuth();
    toast.success(t('b2b.auth.loggedOut'));
    navigate('/login', { replace: true });
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t('b2b.profile.title')} subtitle={t('b2b.profile.subtitle')} />

      <div className="flex flex-wrap gap-2">
        <TabButton
          active={tab === 'profile'}
          onClick={() => setTab('profile')}
          icon={<UserCog size={14} />}
        >
          {t('b2b.profile.sectionProfile')}
        </TabButton>
        <TabButton
          active={tab === 'security'}
          onClick={() => setTab('security')}
          icon={<ShieldCheck size={14} />}
        >
          {t('b2b.profile.sectionSecurity')}
        </TabButton>
        <TabButton
          active={tab === 'notifications'}
          onClick={() => setTab('notifications')}
          icon={<Bell size={14} />}
        >
          {t('b2b.profile.sectionNotifications')}
        </TabButton>
      </div>

      {tab === 'profile' && <ProfileSection onLogout={onLogout} />}
      {tab === 'security' && <SecuritySection />}
      {tab === 'notifications' && <NotificationsSection />}
    </div>
  );
};

const TabButton = ({
  active,
  onClick,
  icon,
  children,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`inline-flex items-center gap-2 rounded-xl border px-3 py-1.5 text-sm font-medium transition ${
      active
        ? 'border-amber-500 bg-amber-50 text-amber-700 dark:border-amber-400 dark:bg-amber-500/10 dark:text-amber-300'
        : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
    }`}
  >
    {icon}
    {children}
  </button>
);

interface UpdateProfileRequest {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  avatarUrl?: string | null;
}

const ProfileSection = ({ onLogout }: { onLogout: () => void }) => {
  const { t } = useTranslation();
  const profile = useDealerProfile();
  const queryClient = useQueryClient();

  if (profile.isLoading || !profile.data) {
    return (
      <Card>
        <CardBody>
          <div className="flex items-center gap-2 text-sm text-slate-500">
            <Spinner /> {t('b2b.common.loading')}
          </div>
        </CardBody>
      </Card>
    );
  }

  return <ProfileForm profile={profile.data} onLogout={onLogout} queryClient={queryClient} t={t} />;
};

interface ProfileFormProps {
  profile: NonNullable<ReturnType<typeof useDealerProfile>['data']>;
  onLogout: () => void;
  queryClient: ReturnType<typeof useQueryClient>;
  t: ReturnType<typeof useTranslation>['t'];
}

const ProfileForm = ({ profile, onLogout, queryClient, t }: ProfileFormProps) => {
  const [firstName, setFirstName] = useState(profile.firstName ?? '');
  const [lastName, setLastName] = useState(profile.lastName ?? '');
  const [phone, setPhone] = useState(profile.phoneNumber ?? '');

  const update = useMutation({
    mutationFn: async (input: UpdateProfileRequest) => {
      await apiClient.put('/auth/profile', input);
    },
    onSuccess: () => {
      toast.success(t('b2b.profile.savedToast'));
      queryClient.invalidateQueries({ queryKey: ['dealer', 'profile'] });
    },
    onError: () => toast.error(t('b2b.common.errorGeneric')),
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    update.mutate({
      firstName: firstName.trim() || null,
      lastName: lastName.trim() || null,
      phoneNumber: phone.trim() || null,
    });
  };

  const fullName =
    [profile.firstName, profile.lastName].filter(Boolean).join(' ').trim() || profile.email;

  return (
    <Card>
      <CardHeader title={fullName} subtitle={profile.email} />
      <CardBody>
        <dl className="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
          <Field label={t('b2b.profile.tenant')} value={profile.tenantName} />
          <Field label={t('b2b.profile.dealer')} value={profile.dealerName} />
          <Field label={t('b2b.profile.dealerCode')} value={profile.dealerCode} />
          <Field label={t('b2b.profile.persona')} value={t('b2b.profile.personaDealer')} />
        </dl>

        <form onSubmit={submit} className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <FieldGroup label={t('b2b.profile.firstName')}>
            <Input
              type="text"
              value={firstName}
              onChange={(event) => setFirstName(event.target.value)}
            />
          </FieldGroup>
          <FieldGroup label={t('b2b.profile.lastName')}>
            <Input
              type="text"
              value={lastName}
              onChange={(event) => setLastName(event.target.value)}
            />
          </FieldGroup>
          <FieldGroup label={t('b2b.profile.phone')}>
            <Input type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} />
          </FieldGroup>
          <div className="sm:col-span-2 flex flex-wrap items-center justify-between gap-2">
            <Button variant="danger" type="button" onClick={onLogout}>
              <LogOut size={14} /> {t('b2b.common.logout')}
            </Button>
            <Button type="submit" disabled={update.isPending}>
              {update.isPending ? t('b2b.common.submitting') : t('b2b.profile.save')}
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
};

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

const SecuritySection = () => {
  const { t } = useTranslation();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');

  const change = useMutation({
    mutationFn: async (input: ChangePasswordRequest) => {
      await apiClient.post('/auth/change-password', input);
    },
    onSuccess: () => {
      toast.success(t('b2b.profile.passwordChanged'));
      setCurrent('');
      setNext('');
      setConfirm('');
    },
    onError: () => toast.error(t('b2b.profile.passwordChangeFailed')),
  });

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (next !== confirm) {
      toast.error(t('b2b.profile.passwordMismatch'));
      return;
    }
    if (next.length < 8) {
      toast.error(t('b2b.profile.passwordTooShort'));
      return;
    }
    change.mutate({ currentPassword: current, newPassword: next });
  };

  return (
    <Card>
      <CardHeader
        title={t('b2b.profile.sectionSecurity')}
        subtitle={t('b2b.profile.securitySubtitle')}
      />
      <CardBody>
        <form onSubmit={submit} className="grid grid-cols-1 gap-4 sm:max-w-md">
          <FieldGroup label={t('b2b.profile.currentPassword')}>
            <Input
              type="password"
              value={current}
              onChange={(event) => setCurrent(event.target.value)}
              required
            />
          </FieldGroup>
          <FieldGroup label={t('b2b.profile.newPassword')}>
            <Input
              type="password"
              value={next}
              onChange={(event) => setNext(event.target.value)}
              required
              minLength={8}
            />
          </FieldGroup>
          <FieldGroup label={t('b2b.profile.confirmPassword')}>
            <Input
              type="password"
              value={confirm}
              onChange={(event) => setConfirm(event.target.value)}
              required
              minLength={8}
            />
          </FieldGroup>
          <div className="flex justify-end">
            <Button type="submit" disabled={change.isPending}>
              {change.isPending ? t('b2b.common.submitting') : t('b2b.profile.changePassword')}
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
};

const NotificationsSection = () => {
  const { t } = useTranslation();
  return (
    <Card>
      <CardHeader
        title={t('b2b.profile.sectionNotifications')}
        subtitle={t('b2b.profile.notificationsSubtitle')}
      />
      <CardBody>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('b2b.profile.notificationsComingSoon')}
        </p>
        <ul className="mt-4 space-y-3 text-sm text-slate-600 dark:text-slate-300">
          <li className="flex items-center justify-between rounded-xl border border-slate-100 px-4 py-3 dark:border-slate-800">
            <span>{t('b2b.profile.notifNewOrderApproval')}</span>
            <span className="text-xs text-slate-400">{t('b2b.profile.notifOnByDefault')}</span>
          </li>
          <li className="flex items-center justify-between rounded-xl border border-slate-100 px-4 py-3 dark:border-slate-800">
            <span>{t('b2b.profile.notifShipmentUpdate')}</span>
            <span className="text-xs text-slate-400">{t('b2b.profile.notifOnByDefault')}</span>
          </li>
          <li className="flex items-center justify-between rounded-xl border border-slate-100 px-4 py-3 dark:border-slate-800">
            <span>{t('b2b.profile.notifCommissionPosted')}</span>
            <span className="text-xs text-slate-400">{t('b2b.profile.notifOnByDefault')}</span>
          </li>
        </ul>
      </CardBody>
    </Card>
  );
};

const Field = ({ label, value }: { label: string; value: React.ReactNode }) => (
  <div>
    <dt className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="mt-1 text-sm font-medium text-slate-900 dark:text-slate-100">{value}</dd>
  </div>
);

const FieldGroup = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="flex flex-col gap-1">
    <span className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </span>
    {children}
  </label>
);
