import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { customerSchema, type CustomerFormValues } from '../model/customerSchema';
import type { Customer } from '../model/customer.types';
import { useCreateCustomer, useUpdateCustomer } from '../hooks/useCustomerQueries';

interface Props {
  open: boolean;
  customer: Customer | null;
  onClose: () => void;
}

const emptyValues: CustomerFormValues = {
  name: '',
  email: '',
  phone: '',
  taxNumber: '',
  notes: '',
  isActive: true,
};

export const CustomerFormModal = ({ open, customer, onClose }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateCustomer();
  const updateMutation = useUpdateCustomer();
  const isEdit = customer !== null;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (!open) {
      return;
    }
    if (customer) {
      reset({
        name: customer.name,
        email: customer.email ?? '',
        phone: customer.phone ?? '',
        taxNumber: customer.taxNumber ?? '',
        notes: customer.notes ?? '',
        isActive: customer.isActive,
      });
    } else {
      reset(emptyValues);
    }
  }, [open, customer, reset]);

  const onSubmit = handleSubmit((values) => {
    const payload = {
      name: values.name,
      email: values.email || null,
      phone: values.phone || null,
      taxNumber: values.taxNumber || null,
      notes: values.notes || null,
    };

    if (isEdit && customer) {
      updateMutation.mutate(
        {
          ...payload,
          id: customer.id,
          type: customer.type,
          legalName: customer.legalName,
          tradeName: customer.tradeName,
          nationalId: customer.nationalId,
          taxOffice: customer.taxOffice,
          website: customer.website,
          defaultCurrency: customer.defaultCurrency,
          paymentTermsId: customer.paymentTermsId,
          priceListId: customer.priceListId,
          customerGroupId: customer.customerGroupId,
          salesRepUserId: customer.salesRepUserId,
          creditLimit: customer.creditLimit,
          defaultDiscountPercent: customer.defaultDiscountPercent,
          classification: customer.classification,
          channel: customer.channel,
          territory: customer.territory,
          languageCode: customer.languageCode,
          parentCustomerId: customer.parentCustomerId,
          status: values.isActive ? 'Active' : 'Blocked',
        },
        {
          onSuccess: (response) => {
            if (response.isSuccess) {
              toast.success(t('customers.toast.updated'));
              onClose();
              return;
            }
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
          },
          onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
        },
      );
      return;
    }

    createMutation.mutate(payload, {
      onSuccess: (response) => {
        if (response.isSuccess) {
          toast.success(t('customers.toast.created'));
          onClose();
          return;
        }
        toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
      },
      onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
    });
  });

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) {
    return null;
  }

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="w-full max-w-lg overflow-hidden rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-3 dark:border-slate-800">
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? t('customers.modal.editTitle') : t('customers.modal.createTitle')}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <form onSubmit={onSubmit} noValidate className="space-y-3 px-5 py-4">
          <Input
            label={t('customers.fields.name')}
            placeholder={t('customers.fields.namePlaceholder')}
            error={translateError(errors.name?.message)}
            {...register('name')}
          />

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Input
              label={t('customers.fields.email')}
              placeholder={t('customers.fields.emailPlaceholder')}
              type="email"
              error={translateError(errors.email?.message)}
              {...register('email')}
            />
            <Input
              label={t('customers.fields.phone')}
              placeholder={t('customers.fields.phonePlaceholder')}
              error={translateError(errors.phone?.message)}
              {...register('phone')}
            />
          </div>

          <Input
            label={t('customers.fields.taxNumber')}
            placeholder={t('customers.fields.taxNumberPlaceholder')}
            error={translateError(errors.taxNumber?.message)}
            {...register('taxNumber')}
          />

          <div>
            <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
              {t('customers.fields.notes')}
            </label>
            <textarea
              rows={3}
              placeholder={t('customers.fields.notesPlaceholder')}
              className="w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:placeholder-slate-500"
              {...register('notes')}
            />
            {errors.notes?.message && (
              <span className="mt-1 block text-xs text-red-500">
                {translateError(errors.notes.message)}
              </span>
            )}
          </div>

          {isEdit && (
            <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 text-indigo-600"
                {...register('isActive')}
              />
              {t('customers.fields.isActive')}
            </label>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel')}
            </button>
            <Button type="submit" isLoading={isBusy}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
