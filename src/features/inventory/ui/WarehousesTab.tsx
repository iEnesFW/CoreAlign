import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Pencil, Plus, Star, Trash2 } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { useDeleteWarehouse, useWarehousesQuery } from '@/features/master-data/hooks/useMasterData';
import type { Warehouse, WarehouseType } from '@/features/master-data/model/masterData.types';
import { WarehouseFormModal } from './WarehouseFormModal';

const TYPE_LABEL: Record<WarehouseType, string> = {
  Main: 'Ana Depo',
  Transit: 'Transit',
  Return: 'İade',
  Damaged: 'Hasarlı',
  Quarantine: 'Karantina',
};

export const WarehousesTab = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const warehousesQuery = useWarehousesQuery();
  const deleteMutation = useDeleteWarehouse();
  const [modal, setModal] = useState<
    { mode: 'create' } | { mode: 'edit'; warehouse: Warehouse } | null
  >(null);

  const warehouses = warehousesQuery.data?.data ?? [];

  const remove = async (w: Warehouse) => {
    const ok = await confirm({
      title: t('inventory.warehouse.deleteTitle', { defaultValue: 'Depoyu Sil' }),
      message: t('inventory.warehouse.deleteConfirm', {
        defaultValue: '{{name}} silinsin mi?',
        name: w.name,
      }),
      confirmLabel: t('common.delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(w.id);
      toast.success(t('inventory.warehouse.deleted', { defaultValue: 'Depo silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('inventory.warehouse.count', {
            defaultValue: '{{count}} depo',
            count: warehouses.length,
          })}
        </span>
        <button
          type="button"
          onClick={() => setModal({ mode: 'create' })}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={12} />
          {t('inventory.warehouse.new', { defaultValue: 'Yeni Depo' })}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        {warehousesQuery.isPending ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : warehouses.length === 0 ? (
          <div className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('inventory.warehouse.empty', { defaultValue: 'Henüz depo tanımlanmadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('inventory.warehouse.code', { defaultValue: 'Kod' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.warehouse.name', { defaultValue: 'İsim' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.warehouse.type', { defaultValue: 'Tip' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('inventory.warehouse.location', { defaultValue: 'Konum' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('inventory.warehouse.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {warehouses.map((w) => (
                <tr key={w.id} className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {w.code}
                  </td>
                  <td className="px-3 py-2">
                    <span className="inline-flex items-center gap-1.5 font-medium text-slate-800 dark:text-slate-100">
                      {w.isDefault && <Star size={12} className="text-amber-500" />}
                      {w.name}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
                    {TYPE_LABEL[w.type]}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
                    {[w.city, w.country].filter(Boolean).join(', ') || '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
                        w.isActive
                          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
                          : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {w.isActive
                        ? t('common.active', { defaultValue: 'Aktif' })
                        : t('common.inactive', { defaultValue: 'Pasif' })}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-right">
                    <div className="inline-flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => setModal({ mode: 'edit', warehouse: w })}
                        className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                        title={t('common.edit', { defaultValue: 'Düzenle' })}
                      >
                        <Pencil size={13} />
                      </button>
                      <button
                        type="button"
                        onClick={() => remove(w)}
                        className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
                        title={t('common.delete', { defaultValue: 'Sil' })}
                      >
                        <Trash2 size={13} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal && (
        <WarehouseFormModal
          key={modal.mode === 'edit' ? modal.warehouse.id : 'new'}
          mode={modal.mode}
          warehouse={modal.mode === 'edit' ? modal.warehouse : undefined}
          onClose={() => setModal(null)}
        />
      )}
    </div>
  );
};
