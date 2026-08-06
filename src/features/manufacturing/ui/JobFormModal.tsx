import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { toast } from 'sonner';
import { useCreateJob } from '@/features/manufacturing/hooks/useManufacturingQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { UnitOfMeasureSelect } from '@/shared/ui/form/UnitOfMeasureSelect';
import { useRoutingsQuery } from '@/features/manufacturing/hooks/useManufacturingQueries';
import { Textarea } from '@/shared/ui/Textarea/Textarea';

interface Props {
  onClose: () => void;
}

export const JobFormModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const { mutateAsync: createJob, isPending } = useCreateJob();
  const { data: products } = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const { data: warehouses } = useWarehousesQuery(true);
  const { data: routings } = useRoutingsQuery('Active');

  const [formData, setFormData] = useState({
    productId: '',
    plannedQuantity: 1,
    unitOfMeasure: 'ADET',
    warehouseId: '',
    routingId: '',
    notes: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createJob({
        productId: formData.productId,
        plannedQuantity: formData.plannedQuantity,
        unitOfMeasure: formData.unitOfMeasure,
        warehouseId: formData.warehouseId || undefined,
        routingId: formData.routingId || undefined,
        notes: formData.notes,
      });
      toast.success(t('ProductionJobs.create_success'));
      onClose();
    } catch (error) {
      toastApiError(error, t('ProductionJobs.create_error'));
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 sm:p-0">
      <div className="w-full max-w-md rounded-2xl bg-white shadow-xl dark:bg-slate-900 overflow-hidden flex flex-col max-h-full">
        <div className="flex items-center justify-between border-b border-slate-100 p-6 dark:border-slate-800">
          <h2 className="text-xl font-semibold text-slate-900 dark:text-white">
            {t('ProductionJobs.actions.new_job')}
          </h2>
          <button
            onClick={onClose}
            className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="overflow-y-auto p-6">
          <form id="jobForm" onSubmit={handleSubmit} className="space-y-4">
            <Select
              label={t('ProductionJobs.fields.product')}
              value={formData.productId}
              onChange={(e) => setFormData({ ...formData, productId: e.target.value })}
              required
            >
              <option value="">{t('Common.Select', 'Seçiniz')}</option>
              {products?.data?.items?.map((p: { id: string; name: string; sku: string }) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.sku})
                </option>
              ))}
            </Select>

            <div className="grid grid-cols-2 gap-4">
              <Input
                type="number"
                label={t('ProductionJobs.fields.qty')}
                value={formData.plannedQuantity}
                onChange={(e) =>
                  setFormData({ ...formData, plannedQuantity: Number(e.target.value) })
                }
                min="0.01"
                step="0.01"
                required
              />
              <div>
                <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('ProductionJobs.fields.uom')}
                </label>
                <UnitOfMeasureSelect
                  value={formData.unitOfMeasure}
                  onChange={(code) => setFormData({ ...formData, unitOfMeasure: code })}
                />
              </div>
            </div>

            <Select
              label={t('ProductionJobs.fields.routing')}
              value={formData.routingId}
              onChange={(e) => setFormData({ ...formData, routingId: e.target.value })}
            >
              <option value="">{t('Common.Select', 'Seçiniz')}</option>
              {routings?.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name} ({r.code})
                </option>
              ))}
            </Select>

            <Select
              label={t('ProductionJobs.fields.warehouse')}
              value={formData.warehouseId}
              onChange={(e) => setFormData({ ...formData, warehouseId: e.target.value })}
            >
              <option value="">{t('Common.Select', 'Seçiniz')}</option>
              {warehouses?.data?.map((w: { id: string; name: string; code: string }) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </Select>

            <Textarea
              label={t('ProductionJobs.fields.notes')}
              value={formData.notes || ''}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                setFormData((p) => ({ ...p, notes: e.target.value }))
              }
              rows={3}
            />
          </form>
        </div>

        <div className="border-t border-slate-100 bg-slate-50/50 p-6 dark:border-slate-800 dark:bg-slate-800/50 flex justify-end gap-3 mt-auto">
          <Button variant="secondary" onClick={onClose}>
            {t('Common.Cancel', 'İptal')}
          </Button>
          <Button type="submit" form="jobForm" disabled={isPending}>
            {isPending ? t('Common.Saving', 'Kaydediliyor...') : t('Common.Save', 'Kaydet')}
          </Button>
        </div>
      </div>
    </div>
  );
};
