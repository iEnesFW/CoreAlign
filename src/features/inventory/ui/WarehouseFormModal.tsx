import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Warehouse as WarehouseIcon } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateWarehouse, useUpdateWarehouse } from '@/shared/master-data/hooks/useMasterData';
import type { Warehouse, WarehouseType } from '@/shared/master-data/model/masterData.types';

interface Props {
  mode: 'create' | 'edit';
  warehouse?: Warehouse;
  onClose: () => void;
}

const WAREHOUSE_TYPES: WarehouseType[] = ['Main', 'Transit', 'Return', 'Damaged', 'Quarantine'];

export const WarehouseFormModal = ({ mode, warehouse, onClose }: Props) => {
  const { t } = useTranslation();

  const typeLabel: Record<WarehouseType, string> = {
    Main: t('Warehouse.TypeMain', { defaultValue: 'Ana Depo' }),
    Transit: t('Warehouse.TypeTransit', { defaultValue: 'Transit' }),
    Return: t('Warehouse.TypeReturn', { defaultValue: 'İade' }),
    Damaged: t('Warehouse.TypeDamaged', { defaultValue: 'Hasarlı' }),
    Quarantine: t('Warehouse.TypeQuarantine', { defaultValue: 'Karantina' }),
  };
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
    <Modal
      open={true}
      title={
        mode === 'create'
          ? t('inventory.warehouse.new', { defaultValue: 'Yeni Depo' })
          : t('inventory.warehouse.edit', { defaultValue: 'Depoyu Düzenle' })
      }
      icon={<WarehouseIcon size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="warehouse-form" isLoading={isPending}>
            {isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="warehouse-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <Input
            label={t('inventory.warehouse.code', { defaultValue: 'Kod' })}
            type="text"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            maxLength={32}
            className="font-mono"
            placeholder="MERKEZ"
          />
          <Select
            label={t('inventory.warehouse.type', { defaultValue: 'Tip' })}
            value={type}
            onChange={(e) => setType(e.target.value as WarehouseType)}
          >
            {WAREHOUSE_TYPES.map((tp) => (
              <option key={tp} value={tp}>
                {typeLabel[tp]}
              </option>
            ))}
          </Select>
        </div>
        <Input
          label={t('inventory.warehouse.name', { defaultValue: 'İsim' })}
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={200}
        />

        {mode === 'edit' && (
          <>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label={t('inventory.warehouse.address', { defaultValue: 'Adres' })}
                type="text"
                value={addressLine1}
                onChange={(e) => setAddressLine1(e.target.value)}
                maxLength={200}
              />
              <Input
                label={t('inventory.warehouse.city', { defaultValue: 'Şehir' })}
                type="text"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                maxLength={100}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label={t('inventory.warehouse.country', { defaultValue: 'Ülke' })}
                type="text"
                value={country}
                onChange={(e) => setCountry(e.target.value)}
                maxLength={100}
              />
              <Input
                label={t('inventory.warehouse.phone', { defaultValue: 'Telefon' })}
                type="text"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                maxLength={40}
              />
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
      </form>
    </Modal>
  );
};
