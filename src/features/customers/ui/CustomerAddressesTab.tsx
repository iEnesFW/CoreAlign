import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Check, MapPin, Pencil, Plus, Star, Trash2, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useCreateCustomerAddress,
  useCustomerAddressesQuery,
  useDeleteCustomerAddress,
  useUpdateCustomerAddress,
} from '@/features/customers/hooks/useCustomerQueries';
import {
  customerAddressSchema,
  emptyCustomerAddressForm,
  type CustomerAddressFormValues,
} from '@/features/customers/model/customerAddressSchema';
import type { CustomerAddress } from '@/features/customers/model/customer.types';

const toFormValues = (a: CustomerAddress): CustomerAddressFormValues => ({
  label: a.label,
  line1: a.line1,
  line2: a.line2 ?? '',
  city: a.city ?? '',
  state: a.state ?? '',
  postalCode: a.postalCode ?? '',
  country: a.country ?? '',
  isPrimary: a.isPrimary,
});

interface Props {
  customerId: string;
}

export const CustomerAddressesTab = ({ customerId }: Props) => {
  const { t } = useTranslation();
  const query = useCustomerAddressesQuery(customerId);
  const createMutation = useCreateCustomerAddress();
  const updateMutation = useUpdateCustomerAddress();
  const deleteMutation = useDeleteCustomerAddress();
  const confirm = useConfirm();

  const [editing, setEditing] = useState<string | 'new' | null>(null);

  const addresses = query.data?.data ?? [];

  const startNew = () => setEditing('new');
  const startEdit = (a: CustomerAddress) => setEditing(a.id);
  const cancel = () => setEditing(null);

  const initialValues =
    editing && editing !== 'new' ? (addresses.find((a) => a.id === editing) ?? null) : null;

  const handleSubmit = (values: CustomerAddressFormValues) => {
    const payload = {
      customerId,
      label: values.label.trim(),
      line1: values.line1.trim(),
      line2: values.line2?.trim() || null,
      city: values.city?.trim() || null,
      state: values.state?.trim() || null,
      postalCode: values.postalCode?.trim() || null,
      country: values.country?.trim() || null,
      isPrimary: values.isPrimary,
    };

    const onComplete = (msg: string) => {
      toast.success(msg);
      cancel();
    };

    if (editing === 'new') {
      createMutation.mutate(payload, {
        onSuccess: () => onComplete(t('customers.detail.addresses.toast.created')),
        onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
      });
    } else if (editing) {
      updateMutation.mutate(
        { ...payload, id: editing },
        {
          onSuccess: () => onComplete(t('customers.detail.addresses.toast.updated')),
          onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
        },
      );
    }
  };

  const remove = async (a: CustomerAddress) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('customers.detail.addresses.confirmDelete', { label: a.label }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    deleteMutation.mutate(
      { customerId, id: a.id },
      {
        onSuccess: () => toast.success(t('customers.detail.addresses.toast.deleted')),
        onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
      },
    );
  };

  return (
    <div className="space-y-3">
      {editing === null && (
        <button
          type="button"
          onClick={startNew}
          className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 bg-slate-50/50 px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:bg-slate-800/30 dark:text-slate-300 dark:hover:bg-slate-800"
        >
          <Plus size={14} />
          {t('customers.detail.addresses.addNew')}
        </button>
      )}

      {editing !== null && (
        <AddressForm
          key={editing}
          initial={initialValues ? toFormValues(initialValues) : emptyCustomerAddressForm}
          onSubmit={handleSubmit}
          onCancel={cancel}
          saving={createMutation.isPending || updateMutation.isPending}
        />
      )}

      {query.isPending && addresses.length === 0 ? (
        <div className="text-sm text-slate-500">{t('common.loading')}</div>
      ) : addresses.length === 0 && editing === null ? (
        <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
          {t('customers.detail.addresses.empty')}
        </div>
      ) : (
        <ul className="space-y-2">
          {addresses.map((a) => (
            <li
              key={a.id}
              className="flex items-start justify-between gap-2 rounded-lg border border-slate-200 p-3 dark:border-slate-800"
            >
              <div className="min-w-0">
                <div className="flex items-center gap-1.5">
                  <MapPin size={12} className="text-slate-500" />
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {a.label}
                  </span>
                  {a.isPrimary && (
                    <span className="inline-flex items-center gap-0.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-700 dark:bg-amber-500/20 dark:text-amber-300">
                      <Star size={9} fill="currentColor" />
                      {t('customers.detail.addresses.primary')}
                    </span>
                  )}
                </div>
                <div className="mt-0.5 text-xs text-slate-600 dark:text-slate-400">
                  {a.line1}
                  {a.line2 ? `, ${a.line2}` : ''}
                </div>
                <div className="text-[10px] text-slate-500">
                  {[a.city, a.state, a.postalCode, a.country].filter(Boolean).join(', ') || '—'}
                </div>
              </div>
              <div className="flex shrink-0 items-center gap-1">
                <button
                  type="button"
                  onClick={() => startEdit(a)}
                  className="rounded p-1 text-slate-500 hover:bg-slate-100 hover:text-indigo-600 dark:hover:bg-slate-800 dark:hover:text-indigo-400"
                  aria-label={t('common.edit')}
                >
                  <Pencil size={12} />
                </button>
                <button
                  type="button"
                  onClick={() => remove(a)}
                  className="rounded p-1 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                  aria-label={t('common.delete')}
                >
                  <Trash2 size={12} />
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

const AddressForm = ({
  initial,
  onSubmit,
  onCancel,
  saving,
}: {
  initial: CustomerAddressFormValues;
  onSubmit: (values: CustomerAddressFormValues) => void;
  onCancel: () => void;
  saving: boolean;
}) => {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CustomerAddressFormValues>({
    resolver: zodResolver(customerAddressSchema),
    defaultValues: initial,
  });

  useEffect(() => {
    reset(initial);
  }, [initial, reset]);

  const fieldError = (key?: unknown): string | undefined =>
    typeof key === 'string' ? t(key, { defaultValue: key }) : undefined;

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-2 rounded-lg border border-indigo-200 bg-indigo-50/30 p-3 dark:border-indigo-500/30 dark:bg-indigo-500/5"
    >
      <div className="grid grid-cols-2 gap-2">
        <Field
          label={t('customers.detail.addresses.fields.label')}
          error={fieldError(errors.label?.message)}
        >
          <input
            {...register('label')}
            placeholder={t('customers.detail.addresses.fields.labelPlaceholder')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
        <Field
          label={t('customers.detail.addresses.fields.country')}
          error={fieldError(errors.country?.message)}
        >
          <input
            {...register('country')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
      </div>
      <Field
        label={t('customers.detail.addresses.fields.line1')}
        error={fieldError(errors.line1?.message)}
      >
        <input
          {...register('line1')}
          className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </Field>
      <Field
        label={t('customers.detail.addresses.fields.line2')}
        error={fieldError(errors.line2?.message)}
      >
        <input
          {...register('line2')}
          className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </Field>
      <div className="grid grid-cols-3 gap-2">
        <Field
          label={t('customers.detail.addresses.fields.city')}
          error={fieldError(errors.city?.message)}
        >
          <input
            {...register('city')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
        <Field
          label={t('customers.detail.addresses.fields.state')}
          error={fieldError(errors.state?.message)}
        >
          <input
            {...register('state')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
        <Field
          label={t('customers.detail.addresses.fields.postalCode')}
          error={fieldError(errors.postalCode?.message)}
        >
          <input
            {...register('postalCode')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
      </div>
      <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200">
        <input type="checkbox" {...register('isPrimary')} />
        {t('customers.detail.addresses.fields.isPrimary')}
      </label>
      <div className="flex gap-2 pt-1">
        <button
          type="submit"
          disabled={saving}
          className="inline-flex flex-1 items-center justify-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          <Check size={12} />
          {t('common.save')}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex items-center justify-center gap-1.5 rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <X size={12} />
          {t('common.cancel')}
        </button>
      </div>
    </form>
  );
};

const Field = ({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) => (
  <label className="block">
    <span className="mb-0.5 block text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </span>
    {children}
    {error && <span className="mt-0.5 block text-[10px] text-red-500">{error}</span>}
  </label>
);
