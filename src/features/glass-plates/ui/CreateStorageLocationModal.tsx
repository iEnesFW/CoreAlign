import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { LayoutGrid } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useCreateStorageLocation } from '../hooks/useGlassPlateQueries';
import type { StorageLocationKind } from '../model/glassPlate.types';

interface Props {
  onClose: () => void;
}

const KINDS: StorageLocationKind[] = ['Rack', 'Shelf', 'Pallet', 'Floor', 'Zone'];

export const CreateStorageLocationModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const warehousesQuery = useWarehousesQuery(true);
  const createMutation = useCreateStorageLocation();

  const warehouses = warehousesQuery.data?.data ?? [];

  const [warehouseId, setWarehouseId] = useState('');
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [kind, setKind] = useState<StorageLocationKind>('Rack');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!warehouseId) {
      toast.error(t('GlassPlates.locationForm.warehouseRequired'));
      return;
    }
    if (!code.trim() || !name.trim()) {
      toast.error(t('GlassPlates.locationForm.codeNameRequired'));
      return;
    }

    setSubmitting(true);
    const result = await createMutation
      .mutateAsync({
        warehouseId,
        code: code.trim(),
        name: name.trim(),
        kind,
        notes: notes.trim() || null,
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('GlassPlates.locationForm.created'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.locationForm.createFailed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('GlassPlates.locationForm.title')}
      icon={<LayoutGrid size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button type="submit" form="glass-location-form" isLoading={submitting}>
            {t('GlassPlates.actions.save')}
          </Button>
        </>
      }
    >
      <form id="glass-location-form" onSubmit={submit} className="space-y-3">
        <Select
          label={t('GlassPlates.locationForm.warehouse')}
          required
          value={warehouseId}
          onChange={(e) => setWarehouseId(e.target.value)}
        >
          <option value="">{t('GlassPlates.locationForm.selectWarehouse')}</option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name} ({w.code})
            </option>
          ))}
        </Select>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Input
            label={t('GlassPlates.locations.code')}
            value={code}
            onChange={(e) => setCode(e.target.value)}
            maxLength={40}
            required
          />
          <Input
            label={t('GlassPlates.locations.name')}
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={120}
            required
          />
        </div>
        <Select
          label={t('GlassPlates.locations.kind')}
          value={kind}
          onChange={(e) => setKind(e.target.value as StorageLocationKind)}
        >
          {KINDS.map((k) => (
            <option key={k} value={k}>
              {t(`GlassPlates.locationKind.${k}`)}
            </option>
          ))}
        </Select>
        <Input
          label={t('GlassPlates.locationForm.notes')}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={200}
        />
      </form>
    </Modal>
  );
};
