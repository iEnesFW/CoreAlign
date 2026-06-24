import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Check, Mail, Pencil, Phone, Plus, Star, Trash2, User, X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useCreateCustomerContact,
  useCustomerContactsQuery,
  useDeleteCustomerContact,
  useUpdateCustomerContact,
} from '@/features/customers/hooks/useCustomerQueries';
import {
  customerContactSchema,
  emptyCustomerContactForm,
  type CustomerContactFormValues,
} from '@/features/customers/model/customerContactSchema';
import type { CustomerContact } from '@/features/customers/model/customer.types';

const toFormValues = (c: CustomerContact): CustomerContactFormValues => ({
  name: c.name,
  role: c.role ?? '',
  email: c.email ?? '',
  phone: c.phone ?? '',
  notes: c.notes ?? '',
  isPrimary: c.isPrimary,
});

interface Props {
  customerId: string;
}

export const CustomerContactsTab = ({ customerId }: Props) => {
  const { t } = useTranslation();
  const query = useCustomerContactsQuery(customerId);
  const createMutation = useCreateCustomerContact();
  const updateMutation = useUpdateCustomerContact();
  const deleteMutation = useDeleteCustomerContact();
  const confirm = useConfirm();

  const [editing, setEditing] = useState<string | 'new' | null>(null);
  const contacts = query.data?.data ?? [];

  const startNew = () => setEditing('new');
  const startEdit = (c: CustomerContact) => setEditing(c.id);
  const cancel = () => setEditing(null);

  const initialValues =
    editing && editing !== 'new' ? (contacts.find((c) => c.id === editing) ?? null) : null;

  const handleSubmit = (values: CustomerContactFormValues) => {
    const payload = {
      customerId,
      name: values.name.trim(),
      role: values.role?.trim() || null,
      email: values.email?.trim() || null,
      phone: values.phone?.trim() || null,
      notes: values.notes?.trim() || null,
      isPrimary: values.isPrimary,
    };

    const onComplete = (msg: string) => {
      toast.success(msg);
      cancel();
    };

    if (editing === 'new') {
      createMutation.mutate(payload, {
        onSuccess: () => onComplete(t('customers.detail.contacts.toast.created')),
        onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
      });
    } else if (editing) {
      updateMutation.mutate(
        { ...payload, id: editing },
        {
          onSuccess: () => onComplete(t('customers.detail.contacts.toast.updated')),
          onError: (err) => toastApiError(err, t('auth.common.unexpectedError')),
        },
      );
    }
  };

  const remove = async (c: CustomerContact) => {
    const confirmed = await confirm({
      title: t('common.confirmDelete'),
      message: t('customers.detail.contacts.confirmDelete', { name: c.name }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!confirmed) return;
    deleteMutation.mutate(
      { customerId, id: c.id },
      {
        onSuccess: () => toast.success(t('customers.detail.contacts.toast.deleted')),
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
          {t('customers.detail.contacts.addNew')}
        </button>
      )}

      {editing !== null && (
        <ContactForm
          key={editing}
          initial={initialValues ? toFormValues(initialValues) : emptyCustomerContactForm}
          onSubmit={handleSubmit}
          onCancel={cancel}
          saving={createMutation.isPending || updateMutation.isPending}
        />
      )}

      {query.isPending && contacts.length === 0 ? (
        <div className="text-sm text-slate-500">{t('common.loading')}</div>
      ) : contacts.length === 0 && editing === null ? (
        <div className="rounded border border-slate-200 p-4 text-center text-sm text-slate-500 dark:border-slate-800">
          {t('customers.detail.contacts.empty')}
        </div>
      ) : (
        <ul className="space-y-2">
          {contacts.map((c) => (
            <li
              key={c.id}
              className="flex items-start justify-between gap-2 rounded-lg border border-slate-200 p-3 dark:border-slate-800"
            >
              <div className="min-w-0">
                <div className="flex items-center gap-1.5">
                  <User size={12} className="text-slate-500" />
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {c.name}
                  </span>
                  {c.role && (
                    <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-700/40 dark:text-slate-300">
                      {c.role}
                    </span>
                  )}
                  {c.isPrimary && (
                    <span className="inline-flex items-center gap-0.5 rounded-full bg-warning-100 px-1.5 py-0.5 text-[10px] font-medium text-warning-700 dark:bg-warning-500/20 dark:text-warning-300">
                      <Star size={9} fill="currentColor" />
                      {t('customers.detail.contacts.primary')}
                    </span>
                  )}
                </div>
                <div className="mt-0.5 space-y-0.5 text-xs text-slate-600 dark:text-slate-400">
                  {c.email && (
                    <div className="flex items-center gap-1">
                      <Mail size={10} />
                      <a href={`mailto:${c.email}`} className="hover:underline">
                        {c.email}
                      </a>
                    </div>
                  )}
                  {c.phone && (
                    <div className="flex items-center gap-1">
                      <Phone size={10} />
                      <a href={`tel:${c.phone}`} className="hover:underline">
                        {c.phone}
                      </a>
                    </div>
                  )}
                  {c.notes && <div className="text-[10px] italic text-slate-500">{c.notes}</div>}
                </div>
              </div>
              <div className="flex shrink-0 items-center gap-1">
                <button
                  type="button"
                  onClick={() => startEdit(c)}
                  className="rounded p-1 text-slate-500 hover:bg-slate-100 hover:text-primary-600 dark:hover:bg-slate-800 dark:hover:text-primary-400"
                  aria-label={t('common.edit')}
                >
                  <Pencil size={12} />
                </button>
                <button
                  type="button"
                  onClick={() => remove(c)}
                  className="rounded p-1 text-slate-500 hover:bg-danger-50 hover:text-danger-600 dark:hover:bg-danger-500/10 dark:hover:text-danger-400"
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

const ContactForm = ({
  initial,
  onSubmit,
  onCancel,
  saving,
}: {
  initial: CustomerContactFormValues;
  onSubmit: (values: CustomerContactFormValues) => void;
  onCancel: () => void;
  saving: boolean;
}) => {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CustomerContactFormValues>({
    resolver: zodResolver(customerContactSchema),
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
      className="space-y-2 rounded-lg border border-primary-200 bg-primary-50/30 p-3 dark:border-primary-500/30 dark:bg-primary-500/5"
    >
      <div className="grid grid-cols-2 gap-2">
        <Field
          label={t('customers.detail.contacts.fields.name')}
          error={fieldError(errors.name?.message)}
        >
          <input
            {...register('name')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
        <Field
          label={t('customers.detail.contacts.fields.role')}
          error={fieldError(errors.role?.message)}
        >
          <input
            {...register('role')}
            placeholder={t('customers.detail.contacts.fields.rolePlaceholder')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <Field
          label={t('customers.detail.contacts.fields.email')}
          error={fieldError(errors.email?.message)}
        >
          <input
            type="email"
            {...register('email')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
        <Field
          label={t('customers.detail.contacts.fields.phone')}
          error={fieldError(errors.phone?.message)}
        >
          <input
            {...register('phone')}
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </Field>
      </div>
      <Field
        label={t('customers.detail.contacts.fields.notes')}
        error={fieldError(errors.notes?.message)}
      >
        <textarea
          rows={2}
          {...register('notes')}
          className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </Field>
      <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200">
        <input type="checkbox" {...register('isPrimary')} />
        {t('customers.detail.contacts.fields.isPrimary')}
      </label>
      <div className="flex gap-2 pt-1">
        <button
          type="submit"
          disabled={saving}
          className="inline-flex flex-1 items-center justify-center gap-1.5 rounded bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700 disabled:opacity-50"
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
    {error && <span className="mt-0.5 block text-[10px] text-danger-500">{error}</span>}
  </label>
);
