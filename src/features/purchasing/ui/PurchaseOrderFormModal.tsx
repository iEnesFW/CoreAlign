import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, ShoppingCart, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { useCreatePurchaseOrder, useUpdatePurchaseOrder } from '../hooks/usePurchaseOrders';
import type { PurchaseOrder } from '../model/purchaseOrder.types';

interface Props {
  order: PurchaseOrder | null;
  onClose: () => void;
}

interface LineState {
  key: string;
  productId: string;
  quantity: string;
  unitCost: string;
  taxRatePercent: string;
}

const newLine = (): LineState => ({
  key: crypto.randomUUID(),
  productId: '',
  quantity: '1',
  unitCost: '',
  taxRatePercent: '',
});

const todayIso = () => new Date().toISOString().slice(0, 10);

export const PurchaseOrderFormModal = ({ order, onClose }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const isEdit = order !== null;

  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const warehousesQuery = useWarehousesQuery(true);
  const createMutation = useCreatePurchaseOrder();
  const updateMutation = useUpdatePurchaseOrder();

  const products = productsQuery.data?.data?.items ?? [];
  const vendors = vendorsQuery.data?.data?.items ?? [];
  const warehouses = warehousesQuery.data?.data ?? [];

  const [vendorId, setVendorId] = useState(order?.vendorId ?? '');
  const [currency, setCurrency] = useState(order?.currency ?? 'TRY');
  const [orderDate, setOrderDate] = useState(order?.orderDate?.slice(0, 10) ?? todayIso());
  const [expectedDate, setExpectedDate] = useState(order?.expectedDate?.slice(0, 10) ?? '');
  const [warehouseId, setWarehouseId] = useState(order?.warehouseId ?? '');
  const [notes, setNotes] = useState(order?.notes ?? '');
  const [lines, setLines] = useState<LineState[]>(
    order && order.lines.length > 0
      ? order.lines.map((l) => ({
          key: l.id,
          productId: l.productId,
          quantity: String(l.quantity),
          unitCost: String(l.unitCost),
          taxRatePercent: l.taxRatePercent ? String(l.taxRatePercent) : '',
        }))
      : [newLine()],
  );

  const updateLine = (key: string, patch: Partial<LineState>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  const addLine = () => setLines((prev) => [...prev, newLine()]);
  const removeLine = (key: string) =>
    setLines((prev) => (prev.length === 1 ? prev : prev.filter((l) => l.key !== key)));

  const totals = useMemo(() => {
    let subtotal = 0;
    let tax = 0;
    for (const l of lines) {
      const net = (Number(l.quantity) || 0) * (Number(l.unitCost) || 0);
      subtotal += net;
      tax += net * ((Number(l.taxRatePercent) || 0) / 100);
    }
    return { subtotal, tax, total: subtotal + tax };
  }, [lines]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vendorId) {
      toast.error(t('po.form.vendorRequired', { defaultValue: 'Tedarikçi seçiniz.' }));
      return;
    }
    const validLines = lines.filter((l) => l.productId && Number(l.quantity) > 0);
    if (validLines.length === 0) {
      toast.error(t('po.form.linesRequired', { defaultValue: 'En az bir geçerli satır ekleyin.' }));
      return;
    }
    const payload = {
      vendorId,
      orderDate: new Date(orderDate).toISOString(),
      currency: currency.toUpperCase(),
      expectedDate: expectedDate ? new Date(expectedDate).toISOString() : null,
      warehouseId: warehouseId || null,
      notes: notes.trim() || null,
      lines: validLines.map((l) => ({
        productId: l.productId,
        quantity: Number(l.quantity),
        unitCost: Number(l.unitCost) || 0,
        taxRatePercent: Number(l.taxRatePercent) || 0,
      })),
    };
    try {
      if (isEdit && order) {
        await updateMutation.mutateAsync({ id: order.id, ...payload });
        toast.success(t('po.form.updated', { defaultValue: 'Satınalma siparişi güncellendi.' }));
      } else {
        await createMutation.mutateAsync(payload);
        toast.success(t('po.form.created', { defaultValue: 'Satınalma siparişi oluşturuldu.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const pending = createMutation.isPending || updateMutation.isPending;
  const numberCellClass = `${fieldBaseClasses(false)} text-right`;

  return (
    <Modal
      open={true}
      title={
        isEdit
          ? `${t('po.form.editTitle', { defaultValue: 'Satınalma Siparişi' })} ${order?.poNumber}`
          : t('po.form.newTitle', { defaultValue: 'Yeni Satınalma Siparişi' })
      }
      icon={<ShoppingCart size={18} />}
      onClose={onClose}
      size="2xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="purchase-order-form" isLoading={pending} disabled={pending}>
            {pending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="purchase-order-form" onSubmit={submit} className="dense-form space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Select
            label={t('po.form.vendor', { defaultValue: 'Tedarikçi' })}
            required
            value={vendorId}
            onChange={(e) => {
              const nextVendorId = e.target.value;
              setVendorId(nextVendorId);
              // New POs inherit the supplier's default currency (mirrors the customer
              // commercial-terms auto-flow); the user can still override.
              if (!isEdit) {
                const vendor = vendors.find((v) => v.id === nextVendorId);
                if (vendor?.defaultCurrency) {
                  setCurrency(vendor.defaultCurrency.toUpperCase());
                }
              }
            }}
          >
            <option value="">{t('po.form.selectVendor', { defaultValue: 'Seçiniz…' })}</option>
            {vendors.map((v) => (
              <option key={v.id} value={v.id}>
                {v.name}
              </option>
            ))}
          </Select>
          <Select
            label={t('po.form.warehouse', { defaultValue: 'Teslim Deposu' })}
            value={warehouseId}
            onChange={(e) => setWarehouseId(e.target.value)}
          >
            <option value="">{t('po.form.selectWarehouse', { defaultValue: 'Seçiniz…' })}</option>
            {warehouses.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name} ({w.code})
              </option>
            ))}
          </Select>
          <Input
            label={t('po.form.currency', { defaultValue: 'Para Birimi' })}
            value={currency}
            onChange={(e) => setCurrency(e.target.value.toUpperCase())}
            maxLength={3}
            className="uppercase"
          />
          <Input
            label={t('po.form.orderDate', { defaultValue: 'Sipariş Tarihi' })}
            type="date"
            value={orderDate}
            onChange={(e) => setOrderDate(e.target.value)}
          />
          <Input
            label={t('po.form.expectedDate', { defaultValue: 'Beklenen Teslim' })}
            type="date"
            value={expectedDate}
            onChange={(e) => setExpectedDate(e.target.value)}
          />
        </div>

        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-2 py-1.5 text-left">
                  {t('po.form.product', { defaultValue: 'Ürün' })}
                </th>
                <th className="w-24 px-2 py-1.5 text-right">
                  {t('po.form.qty', { defaultValue: 'Miktar' })}
                </th>
                <th className="w-28 px-2 py-1.5 text-right">
                  {t('po.form.unitCost', { defaultValue: 'Birim Maliyet' })}
                </th>
                <th className="w-20 px-2 py-1.5 text-right">
                  {t('po.form.tax', { defaultValue: 'KDV %' })}
                </th>
                <th className="w-8 px-2 py-1.5" />
              </tr>
            </thead>
            <tbody>
              {lines.map((l) => (
                <tr key={l.key} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-2 py-1.5">
                    <ProductPicker
                      products={products}
                      value={l.productId}
                      onSelect={(productId) => updateLine(l.key, { productId })}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={l.quantity}
                      onChange={(e) => updateLine(l.key, { quantity: e.target.value })}
                      className={numberCellClass}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={l.unitCost}
                      onChange={(e) => updateLine(l.key, { unitCost: e.target.value })}
                      className={numberCellClass}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      max={100}
                      step="any"
                      value={l.taxRatePercent}
                      onChange={(e) => updateLine(l.key, { taxRatePercent: e.target.value })}
                      className={numberCellClass}
                    />
                  </td>
                  <td className="px-2 py-1.5 text-center">
                    <button
                      type="button"
                      onClick={() => removeLine(l.key)}
                      disabled={lines.length === 1}
                      className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                      aria-label={t('common.delete', { defaultValue: 'Sil' })}
                    >
                      <Trash2 size={13} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between">
          <button
            type="button"
            onClick={addLine}
            className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <Plus size={12} />
            {t('po.form.addLine', { defaultValue: 'Satır ekle' })}
          </button>
          <div className="text-right text-sm">
            <div className="text-slate-500 dark:text-slate-400">
              {t('po.form.subtotal', { defaultValue: 'Ara Toplam' })}:{' '}
              {formatCurrency(totals.subtotal, locale, currency)}
            </div>
            <div className="text-slate-500 dark:text-slate-400">
              {t('po.form.taxTotal', { defaultValue: 'KDV' })}:{' '}
              {formatCurrency(totals.tax, locale, currency)}
            </div>
            <div className="font-bold text-slate-900 dark:text-slate-100">
              {t('po.form.total', { defaultValue: 'Genel Toplam' })}:{' '}
              {formatCurrency(totals.total, locale, currency)}
            </div>
          </div>
        </div>

        <Textarea
          label={t('po.form.notes', { defaultValue: 'Açıklama' })}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          maxLength={2000}
        />
      </form>
    </Modal>
  );
};
