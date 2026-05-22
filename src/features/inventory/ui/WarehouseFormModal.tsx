import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateWarehouse, useUpdateWarehouse } from '@/features/master-data/hooks/useMasterData';
import type { Warehouse, WarehouseType } from '@/features/master-data/model/masterData.types';

interface Props {
  mode: 'create' | 'edit';
  warehouse?: Warehouse;
  onClose: () => void;
}

const WAREHOUSE_TYPES: WarehouseType[] = ['Main', 'Transit', 'Return', 'Damaged', 'Quarantine'];

const TYPE_LABEL: Record<WarehouseType, string> = {
  Main: 'Ana Depo',
  Transit: 'Transit',
  Return: 'İade',
  Damaged: 'Hasarlı',
  Quarantine: 'Karantina',
};

export const WarehouseFormModal = ({ mode, warehouse, onClose }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateWarehouse();
  const updateMutation = useUpdateWarehouse();

  const initial =
    mode === 'edit' && warehouse
      ? {
          code: warehouse.code,
          name: warehouse.name,
          type: warehouse.type,
          isDefault: warehouse.isDefault,
          isActive: warehouse.isActive,
          addressLine1: warehouse.addressLine1 ?? '',
          city: warehouse.city ?? '',
          country: warehouse.country ?? '',
          phone: warehouse.phone ?? '',
        }
      : {
          code: '',
          name: '',
          type: 'Main' as WarehouseType,
          isDefault: false,
          isActive: true,
          addressLine1: '',
          city: '',
          country: '',
          phone: '',
        };

  const [code, setCode] = useState(initial.code);
  const [name, setName] = useState(initial.name);
  const [type, setType] = useState<WarehouseType>(initial.type);
  const [isDefault, setIsDefault] = useState(initial.isDefault);
  const [isActive, setIsActive] = useState(initial.isActive);
  const [addressLine1, setAddressLine1] = useState(initial.addressLine1);
  const [city, setCity] = useState(initial.city);
  const [country, setCountry] = useState(initial.country);
  const [phone, setPhone] = useState(initial.phone);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (mode === 'create') {
        await createMutation.mutateAsync({
          code: code.trim(),
          name: name.trim(),
          type,
          isDefault,
        });
        toast.success(t('inventory.warehouse.created', { defaultValue: 'Depo oluşturuldu.' }));
      } else if (warehouse) {
        await updateMutation.mutateAsync({
          id: warehouse.id,
          code: code.trim(),
          name: name.trim(),
          type,
          addressLine1: addressLine1.trim() || null,
          addressLine2: warehouse.addressLine2,
          city: city.trim() || null,
          state: warehouse.state,
          postalCode: warehouse.postalCode,
          country: country.trim() || null,
          phone: phone.trim() || null,
          managerUserId: null,
          isDefault,
          isActive,
        });
        toast.success(t('inventory.warehouse.updated', { defaultValue: 'Depo güncellendi.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-lg rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {mode === 'create'
              ? t('inventory.warehouse.new', { defaultValue: 'Yeni Depo' })
              : t('inventory.warehouse.edit', { defaultValue: 'Depoyu Düzenle' })}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('common.close', { defaultValue: 'Kapat' })}
          >
            <X size={16} />
          </button>
        </div>
        <form onSubmit={submit} className="space-y-3 p-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('inventory.warehouse.code', { defaultValue: 'Kod' })}
              </label>
              <input
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                required
                maxLength={32}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 font-mono text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                placeholder="MERKEZ"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('inventory.warehouse.type', { defaultValue: 'Tip' })}
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as WarehouseType)}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              >
                {WAREHOUSE_TYPES.map((tp) => (
                  <option key={tp} value={tp}>
                    {TYPE_LABEL[tp]}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('inventory.warehouse.name', { defaultValue: 'İsim' })}
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={200}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            />
          </div>

          {mode === 'edit' && (
            <>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('inventory.warehouse.address', { defaultValue: 'Adres' })}
                  </label>
                  <input
                    type="text"
                    value={addressLine1}
                    onChange={(e) => setAddressLine1(e.target.value)}
                    maxLength={200}
                    className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('inventory.warehouse.city', { defaultValue: 'Şehir' })}
                  </label>
                  <input
                    type="text"
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    maxLength={100}
                    className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('inventory.warehouse.country', { defaultValue: 'Ülke' })}
                  </label>
                  <input
                    type="text"
                    value={country}
                    onChange={(e) => setCountry(e.target.value)}
                    maxLength={100}
                    className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                    {t('inventory.warehouse.phone', { defaultValue: 'Telefon' })}
                  </label>
                  <input
                    type="text"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    maxLength={40}
                    className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                  />
                </div>
              </div>
            </>
          )}

          <div className="flex flex-wrap gap-4">
            <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isDefault}
                onChange={(e) => setIsDefault(e.target.checked)}
              />
              {t('inventory.warehouse.isDefault', { defaultValue: 'Varsayılan depo' })}
            </label>
            {mode === 'edit' && (
              <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                />
                {t('inventory.warehouse.isActive', { defaultValue: 'Aktif' })}
              </label>
            )}
          </div>

          <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel', { defaultValue: 'İptal' })}
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {isPending
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('common.save', { defaultValue: 'Kaydet' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
