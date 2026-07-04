import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { FileText, Plus, ShieldCheck, Trash2, Wallet } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import { newOperationId } from '@/shared/lib/operationId';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import { usePurchaseOrdersQuery } from '../hooks/usePurchaseOrders';
import {
  useCreateVendorBill,
  useCreateVendorPayment,
  useUpdateVendorBill,
  useVendorBillAction,
} from '../hooks/useVendorBilling';
import { usePurchasingApprove } from '../hooks/usePurchasingApprove';
import type { PurchaseOrder, PurchaseOrderLine } from '../model/purchaseOrder.types';
import type { VendorBill, VendorBillLineInput } from '../model/vendorBilling.types';

const todayIso = () => new Date().toISOString().slice(0, 10);

interface BillLineState {
  key: string;
  productId: string;
  quantity: string;
  unitPrice: string;
  taxRatePercent: string;
  purchaseOrderLineId: string;
  poUnitCost: number | null;
}

const newBillLine = (): BillLineState => ({
  key: crypto.randomUUID(),
  productId: '',
  quantity: '1',
  unitPrice: '',
  taxRatePercent: '',
  purchaseOrderLineId: '',
  poUnitCost: null,
});

const billToLineState = (line: NonNullable<VendorBill['lines']>[number]): BillLineState => ({
  key: crypto.randomUUID(),
  productId: line.productId,
  quantity: String(line.quantity),
  unitPrice: line.unitPrice ? String(line.unitPrice) : '',
  taxRatePercent:
    line.lineSubtotal > 0 ? String(round2((line.taxAmount / line.lineSubtotal) * 100)) : '',
  purchaseOrderLineId: line.purchaseOrderLineId ?? '',
  poUnitCost: line.poUnitCost || null,
});

const round2 = (n: number) => Math.round(n * 100) / 100;

const cellInputClass =
  'w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

export const VendorBillFormModal = ({
  bill,
  onClose,
}: {
  bill?: VendorBill | null;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const isEdit = Boolean(bill);
  const editable = !bill || bill.status === 'Draft' || bill.status === 'PendingApproval';
  const canApprove = usePurchasingApprove();

  const vendorsQuery = useVendorsQuery({ page: 1, pageSize: 200 });
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const createMutation = useCreateVendorBill();
  const updateMutation = useUpdateVendorBill();
  const billAction = useVendorBillAction();
  const vendors = vendorsQuery.data?.data?.items ?? [];
  const products = productsQuery.data?.data?.items ?? [];

  const [vendorId, setVendorId] = useState(bill?.vendorId ?? '');
  const [billNumber, setBillNumber] = useState(bill?.billNumber ?? '');
  const [billDate, setBillDate] = useState(bill?.billDate?.slice(0, 10) ?? todayIso());
  const [dueDate, setDueDate] = useState(bill?.dueDate?.slice(0, 10) ?? '');
  const [currency, setCurrency] = useState(bill?.currency ?? 'TRY');
  const [subtotal, setSubtotal] = useState(
    bill && !bill.lines?.length ? String(bill.subtotal) : '',
  );
  const [taxAmount, setTaxAmount] = useState(
    bill && !bill.lines?.length ? String(bill.taxAmount) : '',
  );
  const [notes, setNotes] = useState(bill?.notes ?? '');
  const [purchaseOrderId, setPurchaseOrderId] = useState(bill?.purchaseOrderId ?? '');
  const [lineMode, setLineMode] = useState(Boolean(bill?.lines?.length));
  const [lines, setLines] = useState<BillLineState[]>(
    bill?.lines?.length ? bill.lines.map(billToLineState) : [newBillLine()],
  );

  const posQuery = usePurchaseOrdersQuery({ vendorId: vendorId || undefined, pageSize: 100 });
  const purchaseOrders = useMemo(() => posQuery.data?.data?.items ?? [], [posQuery.data]);
  const selectedPo = useMemo<PurchaseOrder | undefined>(
    () => purchaseOrders.find((p) => p.id === purchaseOrderId),
    [purchaseOrders, purchaseOrderId],
  );
  const poLines = selectedPo?.lines ?? [];

  const updateLine = (key: string, patch: Partial<BillLineState>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  const addLine = () => setLines((prev) => [...prev, newBillLine()]);
  const removeLine = (key: string) =>
    setLines((prev) => (prev.length === 1 ? prev : prev.filter((l) => l.key !== key)));

  const selectPoLine = (key: string, poLineId: string) => {
    const match = poLines.find((p) => p.id === poLineId);
    updateLine(key, {
      purchaseOrderLineId: poLineId,
      productId: match ? match.productId : '',
      poUnitCost: match ? match.unitCost : null,
      unitPrice: match ? String(match.unitCost) : '',
      taxRatePercent: match?.taxRatePercent ? String(match.taxRatePercent) : '',
    });
  };

  const onSelectProduct = (key: string, productId: string) => {
    const derived = derivePoLine(poLines, productId);
    updateLine(key, {
      productId,
      purchaseOrderLineId: derived?.id ?? '',
      poUnitCost: derived?.unitCost ?? null,
    });
  };

  const loadFromPo = () => {
    if (poLines.length === 0) return;
    setLineMode(true);
    setLines(
      poLines.map((p) => ({
        key: crypto.randomUUID(),
        productId: p.productId,
        quantity: String(
          p.quantityRemainingToReceive > 0 ? p.quantityRemainingToReceive : p.quantity,
        ),
        unitPrice: String(p.unitCost),
        taxRatePercent: p.taxRatePercent ? String(p.taxRatePercent) : '',
        purchaseOrderLineId: p.id,
        poUnitCost: p.unitCost,
      })),
    );
  };

  const lineTotals = useMemo(() => {
    let sub = 0;
    let tax = 0;
    for (const l of lines) {
      const net = (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0);
      sub += net;
      tax += net * ((Number(l.taxRatePercent) || 0) / 100);
    }
    return { subtotal: round2(sub), tax: round2(tax), total: round2(sub + tax) };
  }, [lines]);

  const headerSubtotal = lineMode ? lineTotals.subtotal : Number(subtotal) || 0;
  const headerTax = lineMode ? lineTotals.tax : Number(taxAmount) || 0;
  const total = headerSubtotal + headerTax;

  const buildLines = (): VendorBillLineInput[] =>
    lines
      .filter((l) => l.productId && Number(l.quantity) > 0)
      .map((l) => ({
        productId: l.productId,
        quantity: Number(l.quantity),
        unitPrice: Number(l.unitPrice) || 0,
        taxRatePercent: Number(l.taxRatePercent) || 0,
        purchaseOrderLineId: l.purchaseOrderLineId || null,
      }));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editable) return;
    if (!vendorId || !billNumber.trim()) {
      toast.error(t('ap.bill.invalid', { defaultValue: 'Tedarikçi, fatura no ve tutar giriniz.' }));
      return;
    }
    const billLines = lineMode ? buildLines() : [];
    if (lineMode && billLines.length === 0) {
      toast.error(t('ap.bill.linesRequired', { defaultValue: 'En az bir geçerli satır ekleyin.' }));
      return;
    }
    if (!lineMode && (Number(subtotal) || 0) < 0) {
      toast.error(t('ap.bill.invalid', { defaultValue: 'Tedarikçi, fatura no ve tutar giriniz.' }));
      return;
    }
    const payload = {
      billNumber: billNumber.trim(),
      billDate: new Date(billDate).toISOString(),
      dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      currency: currency.toUpperCase(),
      subtotal: headerSubtotal,
      taxAmount: headerTax,
      purchaseOrderId: purchaseOrderId || null,
      notes: notes.trim() || null,
      lines: lineMode ? billLines : undefined,
    };
    try {
      if (bill) {
        await updateMutation.mutateAsync({ id: bill.id, ...payload });
        toast.success(t('ap.bill.updated', { defaultValue: 'Tedarikçi faturası güncellendi.' }));
      } else {
        await createMutation.mutateAsync({ vendorId, ...payload });
        toast.success(t('ap.bill.created', { defaultValue: 'Tedarikçi faturası oluşturuldu.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const approve = async () => {
    if (!bill) return;
    try {
      await billAction.mutateAsync({ id: bill.id, action: 'approve' });
      toast.success(t('ap.actionDone', { defaultValue: 'İşlem tamamlandı.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const pending = createMutation.isPending || updateMutation.isPending;
  const title = isEdit
    ? `${t('ap.bill.editTitle', { defaultValue: 'Tedarikçi Faturası' })} ${bill?.billNumber}`
    : t('ap.bill.newTitle', { defaultValue: 'Yeni Tedarikçi Faturası' });

  return (
    <Modal
      open={true}
      title={title}
      icon={<FileText size={18} />}
      onClose={onClose}
      size={lineMode ? '2xl' : 'xl'}
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          {editable && (
            <Button type="submit" form="vendor-bill-form" isLoading={pending}>
              {pending
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('common.save', { defaultValue: 'Kaydet' })}
            </Button>
          )}
        </>
      }
    >
      <form id="vendor-bill-form" onSubmit={submit} className="dense-form space-y-3">
        {bill?.status === 'PendingApproval' && (
          <div className="flex flex-wrap items-center justify-between gap-2 rounded border border-warning-200 bg-warning-50 px-3 py-2 text-xs text-warning-800 dark:border-warning-500/30 dark:bg-warning-500/10 dark:text-warning-300">
            <span className="inline-flex items-center gap-1.5">
              <ShieldCheck size={13} />
              {bill.holdReason
                ? t('ap.bill.holdReason', {
                    defaultValue: 'Onay bekliyor — {{r}}',
                    r: bill.holdReason,
                  })
                : t('ap.bill.pendingApproval', {
                    defaultValue: 'Bu fatura onay bekliyor.',
                  })}
            </span>
            {canApprove && (
              <button
                type="button"
                onClick={approve}
                disabled={billAction.isPending}
                className="inline-flex items-center gap-1 rounded bg-warning-600 px-2.5 py-1 text-[11px] font-semibold text-white hover:bg-warning-700 disabled:opacity-50"
              >
                <ShieldCheck size={12} />
                {t('ap.actions.approve', { defaultValue: 'Onayla ve muhasebeleştir' })}
              </button>
            )}
          </div>
        )}

        <div className="grid grid-cols-2 gap-3">
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.vendor', { defaultValue: 'Tedarikçi' })} *
            </label>
            <select
              value={vendorId}
              onChange={(e) => {
                setVendorId(e.target.value);
                setPurchaseOrderId('');
              }}
              disabled={!editable || isEdit}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            >
              <option value="">{t('ap.bill.selectVendor', { defaultValue: 'Seçiniz…' })}</option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.purchaseOrder', { defaultValue: 'Satınalma Siparişi' })}
            </label>
            <div className="mt-1 flex gap-2">
              <select
                value={purchaseOrderId}
                onChange={(e) => setPurchaseOrderId(e.target.value)}
                disabled={!editable || !vendorId}
                className={fieldBaseClasses(false)}
              >
                <option value="">
                  {t('ap.bill.noPurchaseOrder', { defaultValue: 'Sipariş bağlama (opsiyonel)' })}
                </option>
                {purchaseOrders.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.poNumber} · {formatCurrency(p.total, locale, p.currency)}
                  </option>
                ))}
              </select>
              {editable && selectedPo && poLines.length > 0 && (
                <button
                  type="button"
                  onClick={loadFromPo}
                  className="shrink-0 self-end rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                  {t('ap.bill.loadFromPo', { defaultValue: 'Siparişten doldur' })}
                </button>
              )}
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.number', { defaultValue: 'Fatura No' })} *
            </label>
            <input
              value={billNumber}
              onChange={(e) => setBillNumber(e.target.value)}
              maxLength={64}
              disabled={!editable}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.currency', { defaultValue: 'Para Birimi' })}
            </label>
            <input
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              maxLength={3}
              disabled={!editable}
              className={`mt-1 uppercase ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.date', { defaultValue: 'Fatura Tarihi' })}
            </label>
            <input
              type="date"
              value={billDate}
              onChange={(e) => setBillDate(e.target.value)}
              disabled={!editable}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.bill.due', { defaultValue: 'Vade Tarihi' })}
            </label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              disabled={!editable}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
          {!lineMode && (
            <>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('ap.bill.subtotal', { defaultValue: 'Ara Toplam' })} *
                </label>
                <input
                  type="number"
                  min={0}
                  step="any"
                  value={subtotal}
                  onChange={(e) => setSubtotal(e.target.value)}
                  disabled={!editable}
                  className={`mt-1 text-right ${fieldBaseClasses(false)}`}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('ap.bill.tax', { defaultValue: 'KDV Tutarı' })}
                </label>
                <input
                  type="number"
                  min={0}
                  step="any"
                  value={taxAmount}
                  onChange={(e) => setTaxAmount(e.target.value)}
                  disabled={!editable}
                  className={`mt-1 text-right ${fieldBaseClasses(false)}`}
                />
              </div>
            </>
          )}
        </div>

        <label className="flex items-center gap-2 text-xs font-medium text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={lineMode}
            onChange={(e) => setLineMode(e.target.checked)}
            disabled={!editable}
            className="rounded border-slate-300 text-primary-600 focus:ring-primary-500 dark:border-slate-600"
          />
          {t('ap.bill.itemized', { defaultValue: 'Kalemli giriş (ürün satırları)' })}
        </label>

        {lineMode && (
          <div className="space-y-2">
            <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                  <tr>
                    <th className="px-2 py-1.5 text-left">
                      {t('ap.bill.lineProduct', { defaultValue: 'Ürün' })}
                    </th>
                    {selectedPo && (
                      <th className="w-44 px-2 py-1.5 text-left">
                        {t('ap.bill.linePoLine', { defaultValue: 'Sipariş Satırı' })}
                      </th>
                    )}
                    <th className="w-24 px-2 py-1.5 text-right">
                      {t('ap.bill.lineQty', { defaultValue: 'Miktar' })}
                    </th>
                    <th className="w-28 px-2 py-1.5 text-right">
                      {t('ap.bill.lineUnitPrice', { defaultValue: 'Birim Fiyat' })}
                    </th>
                    <th className="w-20 px-2 py-1.5 text-right">
                      {t('ap.bill.lineTax', { defaultValue: 'KDV %' })}
                    </th>
                    <th className="w-28 px-2 py-1.5 text-right">
                      {t('ap.bill.lineTotal', { defaultValue: 'Satır Toplamı' })}
                    </th>
                    <th className="w-8 px-2 py-1.5" />
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l) => {
                    const net = (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0);
                    const lineTotal = net * (1 + (Number(l.taxRatePercent) || 0) / 100);
                    return (
                      <tr key={l.key} className="border-t border-slate-100 dark:border-slate-800">
                        <td className="px-2 py-1.5">
                          <ProductPicker
                            products={products}
                            value={l.productId}
                            disabled={!editable}
                            onSelect={(productId) => onSelectProduct(l.key, productId)}
                          />
                        </td>
                        {selectedPo && (
                          <td className="px-2 py-1.5">
                            <select
                              value={l.purchaseOrderLineId}
                              onChange={(e) => selectPoLine(l.key, e.target.value)}
                              disabled={!editable}
                              className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                            >
                              <option value="">
                                {t('ap.bill.noPoLine', { defaultValue: '— eşleşme yok —' })}
                              </option>
                              {poLines.map((p) => (
                                <option key={p.id} value={p.id}>
                                  {poLineLabel(p)}
                                </option>
                              ))}
                            </select>
                          </td>
                        )}
                        <td className="px-2 py-1.5">
                          <input
                            type="number"
                            min={0}
                            step="any"
                            value={l.quantity}
                            onChange={(e) => updateLine(l.key, { quantity: e.target.value })}
                            disabled={!editable}
                            className={cellInputClass}
                          />
                        </td>
                        <td className="px-2 py-1.5">
                          <input
                            type="number"
                            min={0}
                            step="any"
                            value={l.unitPrice}
                            onChange={(e) => updateLine(l.key, { unitPrice: e.target.value })}
                            disabled={!editable}
                            className={cellInputClass}
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
                            disabled={!editable}
                            className={cellInputClass}
                          />
                        </td>
                        <td className="px-2 py-1.5 text-right font-mono text-xs text-slate-700 dark:text-slate-300">
                          {formatCurrency(round2(lineTotal), locale, currency)}
                        </td>
                        <td className="px-2 py-1.5 text-center">
                          <button
                            type="button"
                            onClick={() => removeLine(l.key)}
                            disabled={!editable || lines.length === 1}
                            className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                            aria-label={t('common.delete', { defaultValue: 'Sil' })}
                          >
                            <Trash2 size={13} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {editable && (
              <button
                type="button"
                onClick={addLine}
                className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                <Plus size={12} />
                {t('ap.bill.addLine', { defaultValue: 'Satır ekle' })}
              </button>
            )}
          </div>
        )}

        <div className="space-y-0.5 text-right text-sm">
          <div className="text-slate-500 dark:text-slate-400">
            {t('ap.bill.subtotal', { defaultValue: 'Ara Toplam' })}:{' '}
            {formatCurrency(headerSubtotal, locale, currency)}
          </div>
          <div className="text-slate-500 dark:text-slate-400">
            {t('ap.bill.tax', { defaultValue: 'KDV Tutarı' })}:{' '}
            {formatCurrency(headerTax, locale, currency)}
          </div>
          <div className="font-bold text-slate-900 dark:text-slate-100">
            {t('ap.bill.total', { defaultValue: 'Genel Toplam' })}:{' '}
            {formatCurrency(total, locale, currency)}
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('ap.bill.notes', { defaultValue: 'Açıklama' })}
          </label>
          <input
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={200}
            disabled={!editable}
            className={`mt-1 ${fieldBaseClasses(false)}`}
          />
        </div>
      </form>
    </Modal>
  );
};

const poLineLabel = (p: PurchaseOrderLine) => `${p.productSku} · ${p.quantity} × ${p.unitCost}`;

const derivePoLine = (poLines: PurchaseOrderLine[], productId: string) => {
  if (!productId) return undefined;
  const matches = poLines.filter((p) => p.productId === productId);
  return matches.length === 1 ? matches[0] : undefined;
};

export const VendorPaymentModal = ({
  bill,
  onClose,
}: {
  bill: VendorBill;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const payMutation = useCreateVendorPayment();

  const [amount, setAmount] = useState(String(bill.amountDue));
  const [paymentDate, setPaymentDate] = useState(todayIso());
  const [method, setMethod] = useState('BankTransfer');
  const [notes, setNotes] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amt = Number(amount) || 0;
    if (amt <= 0 || amt > bill.amountDue + 0.0001) {
      toast.error(
        t('ap.pay.invalid', { defaultValue: 'Geçerli bir tutar giriniz (kalan borcu aşamaz).' }),
      );
      return;
    }
    try {
      await payMutation.mutateAsync({
        vendorId: bill.vendorId,
        amount: amt,
        paymentDate: new Date(paymentDate).toISOString(),
        currency: bill.currency,
        method,
        vendorBillId: bill.id,
        notes: notes.trim() || null,
        operationId: newOperationId(),
      });
      toast.success(t('ap.pay.done', { defaultValue: 'Ödeme kaydedildi.' }));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={`${t('ap.pay.title', { defaultValue: 'Tedarikçi Ödemesi' })} — ${bill.billNumber}`}
      icon={<Wallet size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button
            type="submit"
            form="vendor-payment-form"
            variant="primary"
            className="bg-success-600 hover:bg-success-700"
            isLoading={payMutation.isPending}
          >
            {payMutation.isPending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('ap.pay.submit', { defaultValue: 'Ödeme Yap' })}
          </Button>
        </>
      }
    >
      <form id="vendor-payment-form" onSubmit={submit} className="dense-form space-y-3">
        <div className="rounded bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
          {bill.vendorName} · {t('ap.pay.due', { defaultValue: 'Kalan borç' })}:{' '}
          <span className="font-semibold">
            {formatCurrency(bill.amountDue, locale, bill.currency)}
          </span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.amount', { defaultValue: 'Tutar' })} *
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              className={`mt-1 text-right ${fieldBaseClasses(false)}`}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.date', { defaultValue: 'Tarih' })}
            </label>
            <input
              type="date"
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.method', { defaultValue: 'Ödeme Yöntemi' })}
            </label>
            <select
              value={method}
              onChange={(e) => setMethod(e.target.value)}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            >
              <option value="BankTransfer">
                {t('ap.pay.bankTransfer', { defaultValue: 'Havale/EFT' })}
              </option>
              <option value="Cash">{t('ap.pay.cash', { defaultValue: 'Nakit' })}</option>
              <option value="Check">{t('ap.pay.check', { defaultValue: 'Çek' })}</option>
              <option value="Card">{t('ap.pay.card', { defaultValue: 'Kart' })}</option>
            </select>
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('ap.pay.notes', { defaultValue: 'Açıklama' })}
            </label>
            <input
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={200}
              className={`mt-1 ${fieldBaseClasses(false)}`}
            />
          </div>
        </div>
      </form>
    </Modal>
  );
};
