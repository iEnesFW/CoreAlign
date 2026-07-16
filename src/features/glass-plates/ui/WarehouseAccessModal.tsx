import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ShieldCheck } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useUsersQuery } from '@/features/users/hooks/useUsers';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import {
  useAssignUserWarehouses,
  useUserWarehouseAccessQuery,
} from '../hooks/useGlassPlateQueries';
import type { AppUser } from '@/features/users/model/user.types';

interface Props {
  onClose: () => void;
}

const userLabel = (u: AppUser) => {
  const name = [u.firstName, u.lastName].filter(Boolean).join(' ').trim();
  return name ? `${name} (${u.username})` : u.username;
};

export const WarehouseAccessModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const usersQuery = useUsersQuery();
  const warehousesQuery = useWarehousesQuery(true);
  const assignMutation = useAssignUserWarehouses();

  const users = usersQuery.data?.data ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  const [userId, setUserId] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [syncKey, setSyncKey] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const accessQuery = useUserWarehouseAccessQuery(userId || null);
  const currentKey = `${userId}:${accessQuery.dataUpdatedAt}`;
  if (userId && accessQuery.isSuccess && currentKey !== syncKey) {
    setSyncKey(currentKey);
    setSelected(new Set(accessQuery.data ?? []));
  }

  const toggle = (warehouseId: string) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(warehouseId)) next.delete(warehouseId);
      else next.add(warehouseId);
      return next;
    });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userId) {
      toast.error(t('GlassPlates.access.userRequired'));
      return;
    }

    setSubmitting(true);
    const result = await assignMutation
      .mutateAsync({ userId, warehouseIds: [...selected] })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result) {
      toast.success(t('GlassPlates.access.saved'));
      onClose();
    }
  };

  const emptyAccess = userId && selected.size === 0;

  return (
    <Modal
      open={true}
      title={t('GlassPlates.access.title')}
      icon={<ShieldCheck size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button type="submit" form="glass-access-form" isLoading={submitting} disabled={!userId}>
            {t('GlassPlates.actions.save')}
          </Button>
        </>
      }
    >
      <form id="glass-access-form" onSubmit={submit} className="space-y-3">
        <Select
          label={t('GlassPlates.access.user')}
          required
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
        >
          <option value="">{t('GlassPlates.access.selectUser')}</option>
          {users.map((u) => (
            <option key={u.id} value={u.id}>
              {userLabel(u)}
            </option>
          ))}
        </Select>

        <div>
          <p className="mb-2 text-sm font-medium text-slate-700 dark:text-slate-300">
            {t('GlassPlates.access.warehouses')}
          </p>
          <p className="mb-2 text-xs text-slate-500">{t('GlassPlates.access.hint')}</p>
          <div className="space-y-1.5 rounded-lg border border-slate-200 p-3 dark:border-slate-800">
            {warehouses.length === 0 ? (
              <p className="text-sm text-slate-500">{t('GlassPlates.access.noWarehouses')}</p>
            ) : (
              warehouses.map((w) => (
                <label
                  key={w.id}
                  className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300"
                >
                  <input
                    type="checkbox"
                    checked={selected.has(w.id)}
                    onChange={() => toggle(w.id)}
                    disabled={!userId}
                    className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                  />
                  {w.name} ({w.code})
                </label>
              ))
            )}
          </div>
          {emptyAccess && (
            <p className="mt-2 text-xs text-warning-600 dark:text-warning-400">
              {t('GlassPlates.access.emptyMeansNone')}
            </p>
          )}
        </div>
      </form>
    </Modal>
  );
};
