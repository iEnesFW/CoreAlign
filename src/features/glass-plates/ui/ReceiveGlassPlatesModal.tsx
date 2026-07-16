import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Boxes, Plus, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useReceiveGlassPlates, useStorageLocationsQuery } from '../hooks/useGlassPlateQueries';

interface Props {
  onClose: () => void;
  initialProductId?: string;
  initialWarehouseId?: string;
}

interface PlateLine {
  key: string;
  plateNumber: string;
  widthMm: string;
  heightMm: string;
  thicknessMm: string;
}

const newLine = (defaults?: Partial<PlateLine>): PlateLine => ({
  key: crypto.randomUUID(),
  plateNumber: '',
  widthMm: defaults?.widthMm ?? '',
  heightMm: defaults?.heightMm ?? '',
  thicknessMm: defaults?.thicknessMm ?? '',
});

export const ReceiveGlassPlatesModal = ({
  onClose,
  initialProductId,
  initialWarehouseId,
}: Props) => {
  const { t } = useTranslation();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const warehousesQuery = useWarehousesQuery(true);
  const receiveMutation = useReceiveGlassPlates();

  const products = productsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  const [productId, setProductId] = useState(initialProductId ?? '');
  const [warehouseId, setWarehouseId] = useState(initialWarehouseId ?? '');
  const [storageLocationId, setStorageLocationId] = useState('');
  const [unitCostPerM2, setUnitCostPerM2] = useState('');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<PlateLine[]>([newLine()]);
  const [submitting, setSubmitting] = useState(false);

  const locationsQuery = useStorageLocationsQuery(warehouseId || undefined);
  const locations = warehouseId ? (locationsQuery.data ?? []) : [];

  const updateLine = (key: string, patch: Partial<PlateLine>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  const addLine = () =>
    setLines((prev) => {
      const last = prev[prev.length - 1];
      return [
        ...prev,
        newLine({
          widthMm: last?.widthMm,
          heightMm: last?.heightMm,
          thicknessMm: last?.thicknessMm,
        }),
      ];
    });
  const removeLine = (key: string) =>
    setLines((prev) => (prev.length === 1 ? prev : prev.filter((l) => l.key !== key)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!productId) {
      toast.error(t('GlassPlates.receiveForm.productRequired'));
      return;
    }
    if (!warehouseId) {
      toast.error(t('GlassPlates.receiveForm.warehouseRequired'));
      return;
    }
    const validLines = lines.filter(
      (l) =>
        l.plateNumber.trim() &&
        Number(l.widthMm) > 0 &&
        Number(l.heightMm) > 0 &&
        Number(l.thicknessMm) > 0,
    );
    if (validLines.length === 0) {
      toast.error(t('GlassPlates.receiveForm.linesRequired'));
      return;
    }

    setSubmitting(true);
    const result = await receiveMutation
      .mutateAsync({
        productId,
        warehouseId,
        storageLocationId: storageLocationId || null,
        unitCostPerM2: Number(unitCostPerM2) || 0,
        notes: notes.trim() || null,
        plates: validLines.map((l) => ({
          plateNumber: l.plateNumber.trim(),
          widthMm: Number(l.widthMm),
          heightMm: Number(l.heightMm),
          thicknessMm: Number(l.thicknessMm),
        })),
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(
        t('GlassPlates.receiveForm.received', {
          count: result.data?.plateCount ?? validLines.length,
        }),
      );
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.receiveForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('GlassPlates.receiveForm.title')}
      icon={<Boxes size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button type="submit" form="glass-receive-form" isLoading={submitting}>
            {t('GlassPlates.actions.save')}
          </Button>
        </>
      }
    >
      <form id="glass-receive-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
              {t('GlassPlates.receiveForm.product')}
            </label>
            <ProductPicker
              products={products}
              value={productId}
              onSelect={(id) => setProductId(id)}
            />
          </div>
          <Select
            label={t('GlassPlates.receiveForm.warehouse')}
            required
            value={warehouseId}
            onChange={(e) => {
              setWarehouseId(e.target.value);
              setStorageLocationId('');
            }}
          >
            <option value="">{t('GlassPlates.locationForm.selectWarehouse')}</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name} ({w.code})
              </option>
            ))}
          </Select>
          <Select
            label={t('GlassPlates.receiveForm.location')}
            value={storageLocationId}
            onChange={(e) => setStorageLocationId(e.target.value)}
            disabled={!warehouseId || locations.length === 0}
          >
            <option value="">{t('GlassPlates.receiveForm.noLocation')}</option>
            {locations.map((l) => (
              <option key={l.id} value={l.id}>
                {l.code} — {l.name}
              </option>
            ))}
          </Select>
          <Input
            label={t('GlassPlates.receiveForm.unitCostPerM2')}
            type="number"
            min={0}
            step="any"
            value={unitCostPerM2}
            onChange={(e) => setUnitCostPerM2(e.target.value)}
          />
        </div>

        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-2 py-1.5 text-left">
                  {t('GlassPlates.receiveForm.plateNumber')}
                </th>
                <th className="w-24 px-2 py-1.5 text-right">
                  {t('GlassPlates.receiveForm.width')}
                </th>
                <th className="w-24 px-2 py-1.5 text-right">
                  {t('GlassPlates.receiveForm.height')}
                </th>
                <th className="w-24 px-2 py-1.5 text-right">
                  {t('GlassPlates.receiveForm.thickness')}
                </th>
                <th className="w-8 px-2 py-1.5"></th>
              </tr>
            </thead>
            <tbody>
              {lines.map((l) => (
                <tr key={l.key} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-2 py-1.5">
                    <input
                      type="text"
                      value={l.plateNumber}
                      onChange={(e) => updateLine(l.key, { plateNumber: e.target.value })}
                      maxLength={60}
                      className={fieldBaseClasses(false)}
                      aria-label={t('GlassPlates.receiveForm.plateNumber')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={l.widthMm}
                      onChange={(e) => updateLine(l.key, { widthMm: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('GlassPlates.receiveForm.width')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={l.heightMm}
                      onChange={(e) => updateLine(l.key, { heightMm: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('GlassPlates.receiveForm.height')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={l.thicknessMm}
                      onChange={(e) => updateLine(l.key, { thicknessMm: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('GlassPlates.receiveForm.thickness')}
                    />
                  </td>
                  <td className="px-2 py-1.5 text-center">
                    <button
                      type="button"
                      onClick={() => removeLine(l.key)}
                      disabled={lines.length === 1}
                      className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                      aria-label={t('GlassPlates.actions.remove')}
                    >
                      <Trash2 size={13} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={addLine}
            className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <Plus size={12} />
            {t('GlassPlates.receiveForm.addPlate')}
          </button>
          <Input
            label={t('GlassPlates.receiveForm.notes')}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={200}
            className="flex-1"
          />
        </div>
      </form>
    </Modal>
  );
};
