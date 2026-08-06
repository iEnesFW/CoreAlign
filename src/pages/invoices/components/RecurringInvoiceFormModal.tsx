import { useMemo, useState } from 'react';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { useTranslation } from 'react-i18next';
import { Plus, Repeat, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import {
  useCreateRecurringInvoice,
  useUpdateRecurringInvoice,
} from '@/features/invoices/hooks/useRecurringInvoiceQueries';
import type {
  RecurrenceFrequency,
  RecurringInvoiceTemplate,
} from '@/features/invoices/model/recurringInvoice.types';

interface Props {
  open: boolean;
  onClose: () => void;
  template?: RecurringInvoiceTemplate | null;
  onSaved?: () => void;
}

interface LineDraft {
  key: string;
  name: string;
  quantity: number;
  unitPrice: number;
  taxRatePercent: number;
}

const FREQUENCIES: RecurrenceFrequency[] = ['Weekly', 'Monthly', 'Quarterly', 'Yearly'];

const newLine = (): LineDraft => ({
  key: crypto.randomUUID(),
  name: '',
  quantity: 1,
  unitPrice: 0,
  taxRatePercent: 20,
});

const todayIso = () => new Date().toISOString().substring(0, 10);

const addMonthsClamped = (from: Date, months: number, anchor: number | null): Date => {
  const total = from.getUTCMonth() + months;
  const targetYear = from.getUTCFullYear() + Math.floor(total / 12);
  const targetMonth = ((total % 12) + 12) % 12;
  const desired = anchor ?? from.getUTCDate();
  const lastDay = new Date(Date.UTC(targetYear, targetMonth + 1, 0)).getUTCDate();
  return new Date(Date.UTC(targetYear, targetMonth, Math.min(desired, lastDay)));
};

const computeNextPreview = (
  frequency: RecurrenceFrequency,
  interval: number,
  anchorDom: number | null,
  fromIso: string,
): string => {
  const from = new Date(`${fromIso}T00:00:00Z`);
  if (Number.isNaN(from.getTime())) return '';
  const step = interval < 1 ? 1 : interval;
  let next: Date;
  if (frequency === 'Weekly') {
    next = new Date(from.getTime() + step * 7 * 86_400_000);
  } else if (frequency === 'Quarterly') {
    next = addMonthsClamped(from, step * 3, anchorDom);
  } else if (frequency === 'Yearly') {
    next = addMonthsClamped(from, step * 12, anchorDom);
  } else {
    next = addMonthsClamped(from, step, anchorDom);
  }
  return next.toISOString().slice(0, 10);
};

export const RecurringInvoiceFormModal = ({ open, onClose, template, onSaved }: Props) => {
  const { t, i18n } = useTranslation();
  const isEdit = Boolean(template);
  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100 });
  const createMutation = useCreateRecurringInvoice();
  const updateMutation = useUpdateRecurringInvoice();
  const customers = customersQuery.data?.data?.items ?? [];

  const [name, setName] = useState(template?.name ?? '');
  const [customerId, setCustomerId] = useState(template?.customerId ?? '');
  const [currency, setCurrency] = useState(template?.currency ?? 'TRY');
  const [frequency, setFrequency] = useState<RecurrenceFrequency>(template?.frequency ?? 'Monthly');
  const [intervalCount, setIntervalCount] = useState(template?.intervalCount ?? 1);
  const [anchorDayOfMonth, setAnchorDayOfMonth] = useState<number | ''>(
    template?.anchorDayOfMonth ?? '',
  );
  const [startDate, setStartDate] = useState(template?.startDate?.slice(0, 10) ?? todayIso());
  const [endDate, setEndDate] = useState(template?.endDate?.slice(0, 10) ?? '');
  const [maxOccurrences, setMaxOccurrences] = useState<number | ''>(template?.maxOccurrences ?? '');
  const [dueDays, setDueDays] = useState(template?.dueDays ?? 30);
  const [publicNotes, setPublicNotes] = useState(template?.publicNotes ?? '');
  const [internalNotes, setInternalNotes] = useState(template?.internalNotes ?? '');
  const [lines, setLines] = useState<LineDraft[]>(
    template?.lines?.length
      ? template.lines.map((l) => ({
          key: crypto.randomUUID(),
          name: l.productName,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          taxRatePercent: l.taxRatePercent,
        }))
      : [newLine()],
  );

  const pending = createMutation.isPending || updateMutation.isPending;
  const showAnchor = frequency !== 'Weekly';

  const totals = useMemo(() => {
    let subtotal = 0;
    let tax = 0;
    lines.forEach((l) => {
      const lineSub = l.quantity * l.unitPrice;
      subtotal += lineSub;
      tax += (lineSub * l.taxRatePercent) / 100;
    });
    return { subtotal, tax, total: subtotal + tax };
  }, [lines]);

  const nextRunPreview = useMemo(() => {
    const anchor = anchorDayOfMonth === '' ? null : Number(anchorDayOfMonth);
    const second = computeNextPreview(frequency, intervalCount, anchor, startDate);
    const fmt = (iso: string) => {
      if (!iso) return '';
      try {
        return new Intl.DateTimeFormat(i18n.language, {
          dateStyle: 'medium',
          timeZone: 'UTC',
        }).format(new Date(`${iso}T00:00:00Z`));
      } catch {
        return iso;
      }
    };
    return { first: fmt(startDate), second: fmt(second) };
  }, [frequency, intervalCount, anchorDayOfMonth, startDate, i18n.language]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      toast.error(
        t('RecurringInvoices.validation.nameRequired', { defaultValue: 'Şablon adı girin.' }),
      );
      return;
    }
    if (!customerId) {
      toast.error(
        t('RecurringInvoices.validation.customerRequired', { defaultValue: 'Müşteri seçin.' }),
      );
      return;
    }
    const validLines = lines.filter((l) => l.name.trim() && l.quantity > 0);
    if (validLines.length === 0) {
      toast.error(
        t('RecurringInvoices.validation.lineRequired', {
          defaultValue: 'En az bir geçerli satır ekleyin.',
        }),
      );
      return;
    }

    const payload = {
      name: name.trim(),
      customerId,
      currency,
      frequency,
      intervalCount: intervalCount < 1 ? 1 : intervalCount,
      anchorDayOfMonth: showAnchor && anchorDayOfMonth !== '' ? Number(anchorDayOfMonth) : null,
      startDate,
      endDate: endDate || null,
      maxOccurrences: maxOccurrences === '' ? null : Number(maxOccurrences),
      dueDays,
      publicNotes: publicNotes.trim() || null,
      internalNotes: internalNotes.trim() || null,
      lines: validLines.map((l) => ({
        productId: null,
        productName: l.name.trim(),
        description: null,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        taxRatePercent: l.taxRatePercent,
      })),
    };

    const onDone = () => {
      toast.success(
        isEdit
          ? t('RecurringInvoices.toast.updated', {
              defaultValue: 'Tekrarlayan fatura güncellendi.',
            })
          : t('RecurringInvoices.toast.created', {
              defaultValue: 'Tekrarlayan fatura oluşturuldu.',
            }),
      );
      onSaved?.();
      onClose();
    };

    if (isEdit && template) {
      updateMutation.mutate(
        { id: template.id, ...payload },
        { onSuccess: onDone, onError: (err: unknown) => toastApiError(err) },
      );
    } else {
      createMutation.mutate(payload, {
        onSuccess: onDone,
        onError: (err: unknown) => toastApiError(err),
      });
    }
  };

  return (
    <Modal
      open={open}
      title={
        isEdit
          ? t('RecurringInvoices.editTitle', { defaultValue: 'Tekrarlayan Faturayı Düzenle' })
          : t('RecurringInvoices.newTitle', { defaultValue: 'Yeni Tekrarlayan Fatura' })
      }
      subtitle={t('RecurringInvoices.subtitle', {
        defaultValue: 'Belirli aralıklarla otomatik fatura üretir.',
      })}
      icon={<Repeat size={18} />}
      onClose={onClose}
      size="2xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose} type="button">
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="recurring-invoice-form" isLoading={pending}>
            {pending
              ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
              : t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </div>
      }
    >
      <form id="recurring-invoice-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field
            label={t('RecurringInvoices.fields.name', { defaultValue: 'Şablon adı' })}
            required
          >
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={200}
              className={fieldCls}
              required
            />
          </Field>
          <Field
            label={t('RecurringInvoices.fields.customer', { defaultValue: 'Müşteri' })}
            required
          >
            <select
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              className={fieldCls}
              required
            >
              <option value="">
                {t('RecurringInvoices.fields.selectCustomer', { defaultValue: 'Seçiniz…' })}
              </option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} {c.code ? `(${c.code})` : ''}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('RecurringInvoices.fields.frequency', { defaultValue: 'Sıklık' })}>
            <select
              value={frequency}
              onChange={(e) => setFrequency(e.target.value as RecurrenceFrequency)}
              className={fieldCls}
            >
              {FREQUENCIES.map((f) => (
                <option key={f} value={f}>
                  {t(`RecurringInvoices.frequency.${f}` as const, { defaultValue: f })}
                </option>
              ))}
            </select>
          </Field>
          <Field
            label={t('RecurringInvoices.fields.intervalCount', { defaultValue: 'Aralık (her N)' })}
          >
            <input
              type="number"
              min={1}
              max={60}
              value={intervalCount}
              onChange={(e) => setIntervalCount(Number.parseInt(e.target.value, 10) || 1)}
              className={fieldCls}
            />
          </Field>
          {showAnchor && (
            <Field
              label={t('RecurringInvoices.fields.anchorDayOfMonth', {
                defaultValue: 'Ayın günü (1-31, ops.)',
              })}
            >
              <input
                type="number"
                min={1}
                max={31}
                value={anchorDayOfMonth}
                onChange={(e) =>
                  setAnchorDayOfMonth(
                    e.target.value === '' ? '' : Number.parseInt(e.target.value, 10),
                  )
                }
                className={fieldCls}
              />
            </Field>
          )}
          <Field label={t('RecurringInvoices.fields.currency', { defaultValue: 'Para birimi' })}>
            <CurrencySelect value={currency} onChange={setCurrency} className={fieldCls} />
          </Field>
          <Field label={t('RecurringInvoices.fields.startDate', { defaultValue: 'Başlangıç' })}>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className={fieldCls}
            />
          </Field>
          <Field label={t('RecurringInvoices.fields.endDate', { defaultValue: 'Bitiş (ops.)' })}>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className={fieldCls}
            />
          </Field>
          <Field
            label={t('RecurringInvoices.fields.maxOccurrences', {
              defaultValue: 'Maks. tekrar (ops.)',
            })}
          >
            <input
              type="number"
              min={1}
              value={maxOccurrences}
              onChange={(e) =>
                setMaxOccurrences(e.target.value === '' ? '' : Number.parseInt(e.target.value, 10))
              }
              className={fieldCls}
            />
          </Field>
          <Field label={t('RecurringInvoices.fields.dueDays', { defaultValue: 'Vade (gün)' })}>
            <input
              type="number"
              min={0}
              max={365}
              value={dueDays}
              onChange={(e) => setDueDays(Number.parseInt(e.target.value, 10) || 0)}
              className={fieldCls}
            />
          </Field>
        </div>

        <div className="rounded-md border border-primary-200 bg-primary-50/50 px-3 py-2 text-[11px] text-primary-700 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300">
          {t('RecurringInvoices.nextRunPreview', {
            defaultValue: 'İlk fatura: {{first}} · sonraki: {{second}}',
            first: nextRunPreview.first,
            second: nextRunPreview.second,
          })}
        </div>

        <div className="rounded-lg border border-slate-200 dark:border-slate-800">
          <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2 dark:border-slate-800">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('RecurringInvoices.lines.header', { defaultValue: 'Satırlar' })}
            </span>
            <button
              type="button"
              onClick={() => setLines((prev) => [...prev, newLine()])}
              className="inline-flex items-center gap-1 rounded-md border border-slate-300 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <Plus size={12} />
              {t('RecurringInvoices.lines.add', { defaultValue: 'Satır ekle' })}
            </button>
          </div>
          <div className="divide-y divide-slate-100 dark:divide-slate-800">
            {lines.map((line, idx) => (
              <div
                key={line.key}
                className="grid grid-cols-1 gap-2 px-3 py-2 sm:grid-cols-[3fr_1fr_1fr_1fr_auto]"
              >
                <input
                  type="text"
                  value={line.name}
                  placeholder={t('RecurringInvoices.lines.name', {
                    defaultValue: 'Açıklama / kalem',
                  })}
                  onChange={(e) =>
                    setLines((prev) =>
                      prev.map((l, i) => (i === idx ? { ...l, name: e.target.value } : l)),
                    )
                  }
                  className={cellCls}
                />
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={Number.isFinite(line.quantity) ? line.quantity : 0}
                  aria-label={t('RecurringInvoices.lines.quantity', { defaultValue: 'Miktar' })}
                  onChange={(e) =>
                    setLines((prev) =>
                      prev.map((l, i) =>
                        i === idx ? { ...l, quantity: Number.parseFloat(e.target.value) || 0 } : l,
                      ),
                    )
                  }
                  className={cellCls}
                />
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={Number.isFinite(line.unitPrice) ? line.unitPrice : 0}
                  aria-label={t('RecurringInvoices.lines.unitPrice', {
                    defaultValue: 'Birim fiyat',
                  })}
                  onChange={(e) =>
                    setLines((prev) =>
                      prev.map((l, i) =>
                        i === idx ? { ...l, unitPrice: Number.parseFloat(e.target.value) || 0 } : l,
                      ),
                    )
                  }
                  className={cellCls}
                />
                <input
                  type="number"
                  min={0}
                  max={100}
                  step={0.1}
                  value={Number.isFinite(line.taxRatePercent) ? line.taxRatePercent : 0}
                  aria-label={t('RecurringInvoices.lines.taxRate', { defaultValue: 'KDV %' })}
                  onChange={(e) =>
                    setLines((prev) =>
                      prev.map((l, i) =>
                        i === idx
                          ? { ...l, taxRatePercent: Number.parseFloat(e.target.value) || 0 }
                          : l,
                      ),
                    )
                  }
                  className={cellCls}
                />
                <button
                  type="button"
                  onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                  className="self-center rounded-md p-1.5 text-danger-600 hover:bg-danger-50 dark:text-danger-300 dark:hover:bg-danger-900/40"
                  aria-label={t('RecurringInvoices.lines.remove', { defaultValue: 'Satırı sil' })}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
          <div className="flex flex-col gap-1 border-t border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-800/40 dark:text-slate-200 sm:flex-row sm:justify-end sm:gap-6">
            <span>
              {t('RecurringInvoices.lines.subtotal', { defaultValue: 'Ara toplam' })}:{' '}
              <strong className="tabular-nums">{totals.subtotal.toFixed(2)}</strong>
            </span>
            <span>
              {t('RecurringInvoices.lines.tax', { defaultValue: 'KDV' })}:{' '}
              <strong className="tabular-nums">{totals.tax.toFixed(2)}</strong>
            </span>
            <span>
              {t('RecurringInvoices.lines.total', { defaultValue: 'Genel toplam' })}:{' '}
              <strong className="tabular-nums">
                {totals.total.toFixed(2)} {currency}
              </strong>
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field
            label={t('RecurringInvoices.fields.publicNotes', { defaultValue: 'Müşteri notu' })}
          >
            <textarea
              value={publicNotes}
              onChange={(e) => setPublicNotes(e.target.value)}
              rows={2}
              className={fieldCls}
            />
          </Field>
          <Field label={t('RecurringInvoices.fields.internalNotes', { defaultValue: 'İç not' })}>
            <textarea
              value={internalNotes}
              onChange={(e) => setInternalNotes(e.target.value)}
              rows={2}
              className={fieldCls}
            />
          </Field>
        </div>
      </form>
    </Modal>
  );
};

const fieldCls =
  'w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const cellCls =
  'w-full rounded-md border border-slate-300 bg-white px-2 py-1 text-xs text-slate-800 placeholder:text-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

const Field = ({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) => (
  <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
    <span>
      {label}
      {required ? <span className="text-danger-500"> *</span> : null}
    </span>
    {children}
  </label>
);
