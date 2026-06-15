import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, Trash2 } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import {
  useCreateOrderTemplateMutation,
  useOrderTemplateQuery,
  useUpdateOrderTemplateMutation,
} from '@/features/orderTemplates/hooks/useOrderTemplateQueries';
import type {
  OrderFrequency,
  OrderTemplate,
  OrderTemplateLineInput,
} from '@/features/orderTemplates/model/orderTemplate.types';

const FREQUENCIES: OrderFrequency[] = [
  'None',
  'Daily',
  'Weekly',
  'BiWeekly',
  'Monthly',
  'Quarterly',
];

interface InitialState {
  name: string;
  customerId: string;
  currency: string;
  frequency: OrderFrequency;
  nextRunAtUtc: string;
  notes: string;
  isActive: boolean;
  lines: OrderTemplateLineInput[];
}

const buildInitial = (tpl: OrderTemplate | null | undefined): InitialState => ({
  name: tpl?.name ?? '',
  customerId: tpl?.customerId ?? '',
  currency: tpl?.currency ?? 'TRY',
  frequency: tpl?.frequency ?? 'Weekly',
  nextRunAtUtc: tpl?.nextRunAtUtc ? tpl.nextRunAtUtc.slice(0, 16) : '',
  notes: tpl?.notes ?? '',
  isActive: tpl?.isActive ?? true,
  lines:
    tpl && tpl.lines.length > 0
      ? tpl.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          notes: l.notes ?? undefined,
        }))
      : [{ productId: '', quantity: 1, unitPrice: 0 }],
});

export const OrderTemplateFormPage = () => {
  const { id } = useParams<{ id?: string }>();
  const isEdit = Boolean(id);
  const existing = useOrderTemplateQuery(id);

  if (isEdit && existing.isPending) {
    return null;
  }

  return (
    <OrderTemplateFormInner key={id ?? 'new'} id={id} initial={buildInitial(existing.data?.data)} />
  );
};

const OrderTemplateFormInner = ({
  id,
  initial,
}: {
  id: string | undefined;
  initial: InitialState;
}) => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isEdit = Boolean(id);
  const createMut = useCreateOrderTemplateMutation();
  const updateMut = useUpdateOrderTemplateMutation();

  const customers = useCustomersQuery({ page: 1, pageSize: 100 });
  const products = useProductsQuery({ page: 1, pageSize: 200 });

  const [name, setName] = useState(initial.name);
  const [customerId, setCustomerId] = useState(initial.customerId);
  const [currency, setCurrency] = useState(initial.currency);
  const [frequency, setFrequency] = useState<OrderFrequency>(initial.frequency);
  const [nextRunAtUtc, setNextRunAtUtc] = useState(initial.nextRunAtUtc);
  const [notes, setNotes] = useState(initial.notes);
  const [isActive, setIsActive] = useState(initial.isActive);
  const [lines, setLines] = useState<OrderTemplateLineInput[]>(initial.lines);

  const customerOptions = customers.data?.data?.items ?? [];
  const productOptions = useMemo(() => products.data?.data?.items ?? [], [products.data]);

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!name.trim() || !customerId || lines.some((l) => !l.productId)) {
      toast.error(
        t('common.fillRequired', {
          defaultValue: 'Lütfen tüm zorunlu alanları doldurun.',
        }),
      );
      return;
    }
    const payloadLines = lines.map((l) => ({
      productId: l.productId,
      quantity: Number(l.quantity),
      unitPrice: Number(l.unitPrice),
    }));
    const firstRunIso = nextRunAtUtc ? new Date(nextRunAtUtc).toISOString() : null;

    if (isEdit && id) {
      const [data] = await safeRequestWithNotify(
        updateMut.mutateAsync({
          id,
          name,
          customerId,
          currency,
          frequency,
          nextRunAtUtc: firstRunIso,
          priceListId: null,
          notes,
          isActive,
          lines: payloadLines,
        }),
      );
      if (data) {
        toast.success(t('OrderTemplates.Form.Saved'));
        navigate('/order-templates');
      }
    } else {
      const [data] = await safeRequestWithNotify(
        createMut.mutateAsync({
          name,
          customerId,
          currency,
          frequency,
          firstRunAtUtc: firstRunIso,
          priceListId: null,
          notes,
          lines: payloadLines,
        }),
      );
      if (data) {
        toast.success(t('OrderTemplates.Form.Saved'));
        navigate('/order-templates');
      }
    }
  };

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
        {isEdit ? t('OrderTemplates.Form.TitleEdit') : t('OrderTemplates.Form.TitleNew')}
      </h1>

      <form
        onSubmit={onSubmit}
        className="space-y-4 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900"
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <Field label={t('OrderTemplates.Form.Name')}>
            <input
              className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </Field>
          <Field label={t('OrderTemplates.Form.Customer')}>
            <select
              className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              required
            >
              <option value="">—</option>
              {customerOptions.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('OrderTemplates.Form.Currency')}>
            <input
              className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              maxLength={3}
              required
            />
          </Field>
          <Field label={t('OrderTemplates.Form.Frequency')}>
            <select
              className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              value={frequency}
              onChange={(e) => setFrequency(e.target.value as OrderFrequency)}
            >
              {FREQUENCIES.map((f) => (
                <option key={f} value={f}>
                  {t(
                    `OrderTemplates.Form.FrequencyOptions.${f}` as 'OrderTemplates.Form.FrequencyOptions.None',
                  )}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('OrderTemplates.Form.NextRunAt')}>
            <input
              type="datetime-local"
              className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              value={nextRunAtUtc}
              onChange={(e) => setNextRunAtUtc(e.target.value)}
              disabled={frequency === 'None'}
            />
          </Field>
          {isEdit && (
            <Field label={t('OrderTemplates.Form.Active')}>
              <label className="flex items-center gap-2 pt-1.5">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                />
                <span className="text-sm">{isActive ? '✓' : '—'}</span>
              </label>
            </Field>
          )}
        </div>
        <Field label={t('OrderTemplates.Form.Notes')}>
          <textarea
            className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
          />
        </Field>

        <div>
          <div className="mb-2 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-200">
              {t('OrderTemplates.Form.Lines')}
            </h2>
            <button
              type="button"
              onClick={() =>
                setLines((prev) => [...prev, { productId: '', quantity: 1, unitPrice: 0 }])
              }
              className="inline-flex items-center gap-1 rounded border border-slate-200 px-2 py-1 text-xs text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300"
            >
              <Plus size={12} />
              {t('OrderTemplates.Form.AddLine')}
            </button>
          </div>
          <div className="space-y-2">
            {lines.map((line, idx) => (
              <div
                key={idx}
                className="grid grid-cols-1 gap-2 rounded border border-slate-200 p-2 sm:grid-cols-12 dark:border-slate-700"
              >
                <div className="sm:col-span-6">
                  <select
                    className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                    value={line.productId}
                    onChange={(e) => {
                      const productId = e.target.value;
                      const product = productOptions.find((p) => p.id === productId);
                      setLines((prev) => {
                        const next = [...prev];
                        next[idx] = {
                          ...next[idx],
                          productId,
                          unitPrice: product?.price ?? next[idx].unitPrice,
                        };
                        return next;
                      });
                    }}
                    required
                  >
                    <option value="">—</option>
                    {productOptions.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.sku} — {p.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="sm:col-span-2">
                  <input
                    type="number"
                    min={0.0001}
                    step={0.0001}
                    className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                    value={line.quantity}
                    onChange={(e) =>
                      setLines((prev) => {
                        const next = [...prev];
                        next[idx] = { ...next[idx], quantity: Number(e.target.value) };
                        return next;
                      })
                    }
                  />
                </div>
                <div className="sm:col-span-3">
                  <input
                    type="number"
                    min={0}
                    step={0.01}
                    className="w-full rounded border border-slate-200 px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                    value={line.unitPrice}
                    onChange={(e) =>
                      setLines((prev) => {
                        const next = [...prev];
                        next[idx] = { ...next[idx], unitPrice: Number(e.target.value) };
                        return next;
                      })
                    }
                  />
                </div>
                <div className="flex items-center justify-end sm:col-span-1">
                  <button
                    type="button"
                    onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                    disabled={lines.length === 1}
                    className="rounded border border-rose-200 p-1 text-rose-600 hover:bg-rose-50 disabled:opacity-40 dark:border-rose-800 dark:text-rose-400"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={createMut.isPending || updateMut.isPending}
            className="rounded-md bg-indigo-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {t('OrderTemplates.Form.Submit')}
          </button>
        </div>
      </form>
    </div>
  );
};

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="block text-xs font-medium text-slate-600 dark:text-slate-400">
    <span className="mb-1 block">{label}</span>
    {children}
  </label>
);

export default OrderTemplateFormPage;
