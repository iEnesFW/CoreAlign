import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Home, LogOut, MonitorSmartphone, ShieldCheck, ShieldOff, UserCog } from 'lucide-react';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Input } from '@/shared/ui/Input';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { useAuthStore } from '@/features/auth/authStore';
import { AddressesSection } from '@/features/portal/AddressesSection';
import {
  useChangePassword,
  useDisableTwoFactor,
  useEnrollTwoFactor,
  usePortalNotificationPreferences,
  usePortalProfile,
  usePortalSessions,
  useRegenerateBackupCodes,
  useRevokeAllSessions,
  useUpdateNotificationPreference,
  useUpdatePortalProfile,
  useVerifyTwoFactor,
} from '@/features/portal/profileHooks';
import type { TwoFactorEnrollment } from '@/features/portal/profileApi';

type Tab = 'profile' | 'security' | 'notifications' | 'addresses';

export const ProfilePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const [tab, setTab] = useState<Tab>('profile');

  if (!user) return null;

  const onLogout = () => {
    clearAuth();
    toast.success(t('auth.loggedOut'));
    navigate('/login', { replace: true });
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t('profile.title')} subtitle={t('profile.subtitle')} />

      <div className="flex flex-wrap gap-2">
        <TabButton
          active={tab === 'profile'}
          onClick={() => setTab('profile')}
          icon={<UserCog size={14} />}
        >
          {t('profile.sectionProfile')}
        </TabButton>
        <TabButton
          active={tab === 'security'}
          onClick={() => setTab('security')}
          icon={<ShieldCheck size={14} />}
        >
          {t('profile.sectionSecurity')}
        </TabButton>
        <TabButton
          active={tab === 'notifications'}
          onClick={() => setTab('notifications')}
          icon={<MonitorSmartphone size={14} />}
        >
          {t('profile.sectionNotifications')}
        </TabButton>
        <TabButton
          active={tab === 'addresses'}
          onClick={() => setTab('addresses')}
          icon={<Home size={14} />}
        >
          {t('addresses.title')}
        </TabButton>
      </div>

      {tab === 'profile' && <ProfileSection onLogout={onLogout} />}
      {tab === 'security' && <SecuritySection />}
      {tab === 'notifications' && <NotificationsSection />}
      {tab === 'addresses' && <AddressesSection />}
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
        ? 'border-sky-500 bg-sky-50 text-sky-700 dark:border-sky-400 dark:bg-sky-500/10 dark:text-sky-300'
        : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
    }`}
  >
    {icon}
    {children}
  </button>
);

const ProfileSection = ({ onLogout }: { onLogout: () => void }) => {
  const { t } = useTranslation();
  const profile = usePortalProfile();

  if (profile.isLoading || !profile.data) {
    return (
      <Card>
        <CardBody>
          <Spinner /> {t('common.loading')}
        </CardBody>
      </Card>
    );
  }

  return <ProfileForm profile={profile.data} onLogout={onLogout} key={profile.data.userId} />;
};

interface ProfileFormProps {
  profile: NonNullable<ReturnType<typeof usePortalProfile>['data']>;
  onLogout: () => void;
}

const ProfileForm = ({ profile, onLogout }: ProfileFormProps) => {
  const { t, i18n } = useTranslation();
  const update = useUpdatePortalProfile();
  const [firstName, setFirstName] = useState(profile.firstName ?? '');
  const [lastName, setLastName] = useState(profile.lastName ?? '');
  const [phone, setPhone] = useState(profile.phoneNumber ?? '');
  const [locale, setLocale] = useState(profile.preferredLocale ?? i18n.language ?? 'tr');

  const onSave = async () => {
    try {
      await update.mutateAsync({
        firstName,
        lastName,
        phoneNumber: phone,
        preferredLocale: locale,
      });
      toast.success(t('profile.savedProfile'));
      if (locale && i18n.language !== locale) {
        await i18n.changeLanguage(locale);
      }
    } catch {
      toast.error(t('profile.saveFailed'));
    }
  };

  return (
    <Card>
      <CardHeader title={profile.email} subtitle={profile.tenantName} />
      <CardBody>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <Input
            label={t('profile.firstName')}
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
          <Input
            label={t('profile.lastName')}
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
          <Input
            label={t('profile.phone')}
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
          <div className="flex flex-col gap-1.5">
            <label
              className="text-sm font-medium text-slate-700 dark:text-slate-200"
              htmlFor="profile-locale"
            >
              {t('profile.preferredLocale')}
            </label>
            <select
              id="profile-locale"
              value={locale}
              onChange={(e) => setLocale(e.target.value)}
              className="h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              <option value="tr">{t('profile.localeTr')}</option>
              <option value="en">{t('profile.localeEn')}</option>
            </select>
          </div>
        </div>

        <div className="mt-6 flex items-center justify-between gap-3">
          <Button variant="danger" onClick={onLogout}>
            <LogOut size={16} /> {t('common.logout')}
          </Button>
          <Button variant="primary" onClick={onSave} disabled={update.isPending}>
            {t('profile.saveProfile')}
          </Button>
        </div>
      </CardBody>
    </Card>
  );
};

const SecuritySection = () => {
  const { t } = useTranslation();
  const profile = usePortalProfile();
  const changePassword = useChangePassword();
  const sessions = usePortalSessions();
  const revokeAll = useRevokeAllSessions();
  const enroll = useEnrollTwoFactor();
  const verify = useVerifyTwoFactor();
  const disable = useDisableTwoFactor();
  const regen = useRegenerateBackupCodes();

  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [twoFactorPwd, setTwoFactorPwd] = useState('');
  const [code, setCode] = useState('');
  const [enrollment, setEnrollment] = useState<TwoFactorEnrollment | null>(null);
  const [backupCodes, setBackupCodes] = useState<string[] | null>(null);

  const isTwoFactorEnabled = profile.data?.isTwoFactorEnabled ?? false;

  const onChangePassword = async () => {
    if (next !== confirm) {
      toast.error(t('security.passwordsMustMatch'));
      return;
    }
    try {
      await changePassword.mutateAsync({ currentPassword: current, newPassword: next });
      toast.success(t('security.passwordChanged'));
      setCurrent('');
      setNext('');
      setConfirm('');
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onEnroll = async () => {
    try {
      const data = await enroll.mutateAsync();
      setEnrollment(data);
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onVerify = async () => {
    try {
      const data = await verify.mutateAsync(code);
      setBackupCodes(data.backupCodes);
      setEnrollment(null);
      setCode('');
      toast.success(t('security.twoFactorEnrolled'));
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onDisable = async () => {
    if (!twoFactorPwd) {
      toast.error(t('security.passwordRequired'));
      return;
    }
    try {
      await disable.mutateAsync(twoFactorPwd);
      setTwoFactorPwd('');
      setBackupCodes(null);
      toast.success(t('security.twoFactorDisabledOk'));
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onRegen = async () => {
    if (!twoFactorPwd) {
      toast.error(t('security.passwordRequired'));
      return;
    }
    try {
      const data = await regen.mutateAsync(twoFactorPwd);
      setBackupCodes(data.backupCodes);
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onRevokeAll = async () => {
    try {
      await revokeAll.mutateAsync();
      toast.success(t('security.revokedAll'));
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title={t('security.changePassword')} />
        <CardBody>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Input
              label={t('security.currentPassword')}
              type="password"
              value={current}
              onChange={(e) => setCurrent(e.target.value)}
            />
            <Input
              label={t('security.newPassword')}
              type="password"
              value={next}
              onChange={(e) => setNext(e.target.value)}
            />
            <Input
              label={t('security.confirmPassword')}
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
            />
          </div>
          <div className="mt-4 flex justify-end">
            <Button onClick={onChangePassword} disabled={changePassword.isPending}>
              {t('security.changePassword')}
            </Button>
          </div>
        </CardBody>
      </Card>

      <Card>
        <CardHeader
          title={t('security.twoFactor')}
          subtitle={
            isTwoFactorEnabled ? t('security.twoFactorEnabled') : t('security.twoFactorDisabled')
          }
        />
        <CardBody>
          {!isTwoFactorEnabled && !enrollment && (
            <Button onClick={onEnroll} disabled={enroll.isPending}>
              <ShieldCheck size={14} /> {t('security.enroll')}
            </Button>
          )}
          {enrollment && (
            <div className="space-y-3">
              <p className="text-sm text-slate-600 dark:text-slate-300">{t('security.scanQr')}</p>
              <pre className="overflow-x-auto rounded-lg bg-slate-100 p-3 text-xs dark:bg-slate-800">
                {enrollment.qrCodeUri}
              </pre>
              <p className="text-xs text-slate-500">{t('security.manualKey')}</p>
              <code className="block rounded bg-slate-100 px-2 py-1 text-xs dark:bg-slate-800">
                {enrollment.manualKey}
              </code>
              <div className="flex items-end gap-3">
                <Input
                  label={t('security.twoFactorCode')}
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                />
                <Button onClick={onVerify} disabled={verify.isPending}>
                  {t('security.verify')}
                </Button>
              </div>
            </div>
          )}
          {isTwoFactorEnabled && (
            <div className="space-y-3">
              <Input
                label={t('security.currentPassword')}
                type="password"
                value={twoFactorPwd}
                onChange={(e) => setTwoFactorPwd(e.target.value)}
              />
              <div className="flex flex-wrap gap-2">
                <Button variant="danger" onClick={onDisable} disabled={disable.isPending}>
                  <ShieldOff size={14} /> {t('security.disable')}
                </Button>
                <Button variant="secondary" onClick={onRegen} disabled={regen.isPending}>
                  {t('security.regenerateBackup')}
                </Button>
              </div>
              {backupCodes && (
                <div className="space-y-2 rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-700 dark:bg-amber-900/30">
                  <p className="text-xs text-amber-800 dark:text-amber-200">
                    {t('security.backupCodesIntro')}
                  </p>
                  <ul className="grid grid-cols-2 gap-1 text-xs font-mono text-amber-900 dark:text-amber-100">
                    {backupCodes.map((bc) => (
                      <li key={bc}>{bc}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={t('security.sessions')} />
        <CardBody>
          {sessions.isLoading ? (
            <Spinner />
          ) : (sessions.data?.length ?? 0) === 0 ? (
            <p className="text-sm text-slate-500">{t('security.sessionsEmpty')}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
                <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                  <tr>
                    <th className="px-3 py-2 font-medium">{t('security.device')}</th>
                    <th className="px-3 py-2 font-medium">{t('security.ipAddress')}</th>
                    <th className="px-3 py-2 font-medium">{t('security.lastActivity')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                  {sessions.data?.map((s) => (
                    <tr key={s.id}>
                      <td className="px-3 py-2">{s.deviceInfo ?? '—'}</td>
                      <td className="px-3 py-2 text-slate-500">{s.ipAddress ?? '—'}</td>
                      <td className="px-3 py-2 text-slate-500">
                        {new Date(s.lastActivityAtUtc).toLocaleString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="mt-4 flex justify-end">
            <Button variant="danger" onClick={onRevokeAll} disabled={revokeAll.isPending}>
              {t('security.revokeAll')}
            </Button>
          </div>
        </CardBody>
      </Card>
    </div>
  );
};

const NotificationsSection = () => {
  const { t } = useTranslation();
  const prefs = usePortalNotificationPreferences();
  const update = useUpdateNotificationPreference();

  const onToggle = async (kind: string, field: 'emailEnabled' | 'inAppEnabled', value: boolean) => {
    const existing = prefs.data?.find((p) => p.notificationKind === kind);
    if (!existing) return;
    try {
      await update.mutateAsync({ ...existing, [field]: value });
      toast.success(t('notifications.preferenceSaved'));
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  return (
    <Card>
      <CardHeader title={t('notifications.title')} subtitle={t('notifications.subtitle')} />
      <CardBody>
        {prefs.isLoading ? (
          <Spinner />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th className="px-3 py-2 font-medium">{t('notifications.channel')}</th>
                  <th className="px-3 py-2 text-center font-medium">{t('notifications.email')}</th>
                  <th className="px-3 py-2 text-center font-medium">{t('notifications.inApp')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {prefs.data?.map((p) => (
                  <tr key={p.notificationKind}>
                    <td className="px-3 py-2 text-slate-700 dark:text-slate-200">
                      {t(`notifications.kinds.${p.notificationKind}`, {
                        defaultValue: p.notificationKind,
                      })}
                    </td>
                    <td className="px-3 py-2 text-center">
                      <input
                        type="checkbox"
                        checked={p.emailEnabled}
                        onChange={(e) =>
                          onToggle(p.notificationKind, 'emailEnabled', e.target.checked)
                        }
                        className="h-4 w-4 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
                      />
                    </td>
                    <td className="px-3 py-2 text-center">
                      <input
                        type="checkbox"
                        checked={p.inAppEnabled}
                        onChange={(e) =>
                          onToggle(p.notificationKind, 'inAppEnabled', e.target.checked)
                        }
                        className="h-4 w-4 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardBody>
    </Card>
  );
};
