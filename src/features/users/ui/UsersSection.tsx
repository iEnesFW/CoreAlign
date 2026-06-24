import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, Power, PowerOff, ShieldCheck, UserPlus } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Label } from '@/shared/ui/Label/Label';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useInviteUser,
  useRolesQuery,
  useSetUserActive,
  useUpdateUserRoles,
  useUsersQuery,
} from '../hooks/useUsers';
import type { AppUser, Role } from '../model/user.types';

const fmtDate = (iso: string | null, locale: string) => formatDateTime(iso, locale);

const RoleChecklist = ({
  roles,
  selected,
  onToggle,
}: {
  roles: Role[];
  selected: Set<number>;
  onToggle: (id: number) => void;
}) => (
  <div className="space-y-1">
    {roles.map((r) => (
      <label
        key={r.id}
        className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300"
      >
        <input type="checkbox" checked={selected.has(r.id)} onChange={() => onToggle(r.id)} />
        <span className="font-medium">{r.name}</span>
        {r.description && <span className="text-slate-400">— {r.description}</span>}
      </label>
    ))}
  </div>
);

export const UsersSection = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const confirm = useConfirm();
  const usersQuery = useUsersQuery();
  const rolesQuery = useRolesQuery();
  const inviteMutation = useInviteUser();
  const rolesMutation = useUpdateUserRoles();
  const activeMutation = useSetUserActive();

  const users = usersQuery.data?.data ?? [];
  const roles = rolesQuery.data?.data ?? [];

  const [inviteOpen, setInviteOpen] = useState(false);
  const [editRolesUser, setEditRolesUser] = useState<AppUser | null>(null);

  const toggleActive = async (u: AppUser) => {
    const ok = await confirm({
      title: u.isActive
        ? t('users.deactivateTitle', { defaultValue: 'Kullanıcıyı Pasifleştir' })
        : t('users.activateTitle', { defaultValue: 'Kullanıcıyı Aktifleştir' }),
      message: u.isActive
        ? t('users.deactivateConfirm', {
            defaultValue: '{{user}} giriş yapamayacak. Devam edilsin mi?',
            user: u.username,
          })
        : t('users.activateConfirm', {
            defaultValue: '{{user}} tekrar giriş yapabilecek.',
            user: u.username,
          }),
      confirmLabel: u.isActive
        ? t('common.deactivate', { defaultValue: 'Pasifleştir' })
        : t('common.activate', { defaultValue: 'Aktifleştir' }),
      tone: u.isActive ? 'danger' : 'default',
    });
    if (!ok) return;
    try {
      await activeMutation.mutateAsync({ id: u.id, isActive: !u.isActive });
      toast.success(t('users.statusUpdated', { defaultValue: 'Kullanıcı durumu güncellendi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('users.count', { defaultValue: '{{count}} kullanıcı', count: users.length })}
        </span>
        <button
          type="button"
          onClick={() => setInviteOpen(true)}
          className="inline-flex items-center gap-1.5 rounded bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700"
        >
          <Plus size={12} />
          {t('users.invite', { defaultValue: 'Kullanıcı Ekle' })}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('users.user', { defaultValue: 'Kullanıcı' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('users.roles', { defaultValue: 'Roller' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('users.lastLogin', { defaultValue: 'Son Giriş' })}
              </th>
              <th className="px-3 py-2 text-center">
                {t('users.status', { defaultValue: 'Durum' })}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                <td className="px-3 py-2">
                  <div className="font-medium text-slate-800 dark:text-slate-100">
                    {[u.firstName, u.lastName].filter(Boolean).join(' ') || u.username}
                  </div>
                  <div className="text-[11px] text-slate-500 dark:text-slate-400">{u.email}</div>
                </td>
                <td className="px-3 py-2">
                  <div className="flex flex-wrap gap-1">
                    {u.roles.length === 0 ? (
                      <span className="text-[11px] text-slate-400">—</span>
                    ) : (
                      u.roles.map((r) => (
                        <span
                          key={r}
                          className="rounded bg-primary-100 px-1.5 py-0.5 text-[10px] font-semibold text-primary-700 dark:bg-primary-500/20 dark:text-primary-300"
                        >
                          {r}
                        </span>
                      ))
                    )}
                  </div>
                </td>
                <td className="px-3 py-2 text-[11px] text-slate-500 dark:text-slate-400">
                  {fmtDate(u.lastLoginAtUtc, locale)}
                </td>
                <td className="px-3 py-2 text-center">
                  <span
                    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
                      u.isActive
                        ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300'
                        : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                    }`}
                  >
                    {u.isActive
                      ? t('common.active', { defaultValue: 'Aktif' })
                      : t('common.inactive', { defaultValue: 'Pasif' })}
                  </span>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="inline-flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => setEditRolesUser(u)}
                      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                      title={t('users.editRoles', { defaultValue: 'Rolleri Düzenle' })}
                    >
                      <ShieldCheck size={14} />
                    </button>
                    <button
                      type="button"
                      onClick={() => toggleActive(u)}
                      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                      title={
                        u.isActive
                          ? t('common.deactivate', { defaultValue: 'Pasifleştir' })
                          : t('common.activate', { defaultValue: 'Aktifleştir' })
                      }
                    >
                      {u.isActive ? <PowerOff size={14} /> : <Power size={14} />}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {users.length === 0 && !usersQuery.isPending && (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-4 text-center text-xs text-slate-500 dark:text-slate-400"
                >
                  {t('users.empty', { defaultValue: 'Kullanıcı bulunamadı.' })}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {inviteOpen && (
        <InviteUserModal
          roles={roles}
          onClose={() => setInviteOpen(false)}
          onSubmit={async (input) => {
            await inviteMutation.mutateAsync(input);
            toast.success(t('users.invited', { defaultValue: 'Kullanıcı oluşturuldu.' }));
            setInviteOpen(false);
          }}
          pending={inviteMutation.isPending}
        />
      )}

      {editRolesUser && (
        <EditRolesModal
          user={editRolesUser}
          roles={roles}
          onClose={() => setEditRolesUser(null)}
          onSubmit={async (roleIds) => {
            await rolesMutation.mutateAsync({ id: editRolesUser.id, roleIds });
            toast.success(t('users.rolesUpdated', { defaultValue: 'Roller güncellendi.' }));
            setEditRolesUser(null);
          }}
          pending={rolesMutation.isPending}
        />
      )}
    </div>
  );
};

const InviteUserModal = ({
  roles,
  onClose,
  onSubmit,
  pending,
}: {
  roles: Role[];
  onClose: () => void;
  onSubmit: (input: {
    username: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    password: string;
    roleIds: number[];
  }) => Promise<void>;
  pending: boolean;
}) => {
  const { t } = useTranslation();
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [selected, setSelected] = useState<Set<number>>(new Set());

  const toggle = (id: number) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !email.trim() || password.length < 8) {
      toast.error(
        t('users.inviteInvalid', {
          defaultValue: 'Kullanıcı adı, e-posta ve en az 8 karakterli parola gerekli.',
        }),
      );
      return;
    }
    try {
      await onSubmit({
        username: username.trim(),
        email: email.trim(),
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        password,
        roleIds: [...selected],
      });
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('users.invite', { defaultValue: 'Kullanıcı Ekle' })}
      icon={<UserPlus size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="invite-user-form" isLoading={pending} disabled={pending}>
            {pending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('users.create', { defaultValue: 'Oluştur' })}
          </Button>
        </>
      }
    >
      <form id="invite-user-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <Input
            label={`${t('users.username', { defaultValue: 'Kullanıcı Adı' })} *`}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <Input
            label={`${t('users.email', { defaultValue: 'E-posta' })} *`}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <Input
            label={t('users.firstName', { defaultValue: 'Ad' })}
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
          />
          <Input
            label={t('users.lastName', { defaultValue: 'Soyad' })}
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
          />
        </div>
        <div>
          <Input
            label={`${t('users.tempPassword', { defaultValue: 'Geçici Parola' })} *`}
            type="text"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="font-mono"
          />
          <p className="mt-0.5 text-[10px] text-slate-400">
            {t('users.tempPasswordHint', {
              defaultValue: 'En az 8 karakter. Kullanıcı ilk girişte değiştirmeli.',
            })}
          </p>
        </div>
        <div>
          <Label className="mb-1 block">{t('users.roles', { defaultValue: 'Roller' })}</Label>
          <RoleChecklist roles={roles} selected={selected} onToggle={toggle} />
        </div>
      </form>
    </Modal>
  );
};

const EditRolesModal = ({
  user,
  roles,
  onClose,
  onSubmit,
  pending,
}: {
  user: AppUser;
  roles: Role[];
  onClose: () => void;
  onSubmit: (roleIds: number[]) => Promise<void>;
  pending: boolean;
}) => {
  const { t } = useTranslation();
  const [selected, setSelected] = useState<Set<number>>(new Set(user.roleIds));

  const toggle = (id: number) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await onSubmit([...selected]);
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={`${t('users.editRoles', { defaultValue: 'Rolleri Düzenle' })} — ${user.username}`}
      icon={<ShieldCheck size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="edit-roles-form" isLoading={pending} disabled={pending}>
            {pending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="edit-roles-form" onSubmit={submit} className="space-y-3">
        <RoleChecklist roles={roles} selected={selected} onToggle={toggle} />
      </form>
    </Modal>
  );
};
