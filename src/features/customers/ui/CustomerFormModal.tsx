import { useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { PhoneField } from '@/shared/ui/PhoneField/PhoneField';
import { ModalTabs } from '@/shared/ui/ModalTabs/ModalTabs';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { getErroredTabs, firstErroredTab } from '@/shared/lib/formTabs';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { MasterDataQuickModal } from '@/shared/master-data/ui/MasterDataQuickModal';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCustomerGroupsQuery,
  usePaymentTermsQuery,
  usePriceListsQuery,
} from '@/shared/master-data/hooks/useMasterData';
import { customerSchema, type CustomerFormValues } from '../model/customerSchema';
import type { Customer } from '../model/customer.types';
import { useCreateCustomer, useUpdateCustomer } from '../hooks/useCustomerQueries';

type CustomerQuickAdd = 'paymentTerm' | 'priceList' | 'customerGroup';
type CustomerTab = 'general' | 'commercial' | 'notes';

const CUSTOMER_FIELD_TAB: Record<string, CustomerTab> = {
  name: 'general',
  type: 'general',
  code: 'general',
  legalName: 'general',
  tradeName: 'general',
  email: 'general',
  phone: 'general',
  nationalId: 'general',
  taxNumber: 'general',
  taxOffice: 'general',
  website: 'general',
  defaultCurrency: 'commercial',
  paymentTermsId: 'commercial',
  priceListId: 'commercial',
  customerGroupId: 'commercial',
  creditLimit: 'commercial',
  defaultDiscountPercent: 'commercial',
  classification: 'commercial',
  territory: 'commercial',
  notes: 'notes',
  isActive: 'notes',
};

interface Props {
  open: boolean;
  customer: Customer | null;
  onClose: () => void;
  onCreated?: (customer: Customer) => void;
}

const emptyValues: CustomerFormValues = {
  name: '',
  type: 'Business',
  code: '',
  legalName: '',
  tradeName: '',
  email: '',
  phone: '',
  nationalId: '',
  taxNumber: '',
  taxOffice: '',
  website: '',
  defaultCurrency: 'TRY',
  paymentTermsId: '',
  priceListId: '',
  customerGroupId: '',
  creditLimit: '',
  defaultDiscountPercent: '',
  classification: '',
  territory: '',
  notes: '',
  isActive: true,
};

const fieldCls =
  'w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';
const quickAddBtnCls =
  'inline-flex items-center gap-0.5 rounded px-1.5 py-0.5 text-[10px] font-medium text-primary-600 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-500/10';

const QUICK_ADD_FIELD: Record<
  CustomerQuickAdd,
  'paymentTermsId' | 'priceListId' | 'customerGroupId'
> = {
  paymentTerm: 'paymentTermsId',
  priceList: 'priceListId',
  customerGroup: 'customerGroupId',
};

export const CustomerFormModal = ({ open, customer, onClose, onCreated }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateCustomer();
  const updateMutation = useUpdateCustomer();
  const isEdit = customer !== null;

  const paymentTerms = usePaymentTermsQuery(true);
  const priceLists = usePriceListsQuery(true);
  const customerGroups = useCustomerGroupsQuery(true);

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: emptyValues,
    mode: 'onTouched',
  });

  const [quickAdd, setQuickAdd] = useState<CustomerQuickAdd | null>(null);
  const [tab, setTab] = useState<CustomerTab>('general');
  const requestClose = useModalClose(isDirty, onClose, open);
  const erroredTabs = getErroredTabs(errors, CUSTOMER_FIELD_TAB);

  useEffect(() => {
    if (!open) return;
    if (customer) {
      reset({
        name: customer.name,
        type: customer.type,
        code: customer.code ?? '',
        legalName: customer.legalName ?? '',
        tradeName: customer.tradeName ?? '',
        email: customer.email ?? '',
        phone: customer.phone ?? '',
        nationalId: customer.nationalId ?? '',
        taxNumber: customer.taxNumber ?? '',
        taxOffice: customer.taxOffice ?? '',
        website: customer.website ?? '',
        defaultCurrency: customer.defaultCurrency,
        paymentTermsId: customer.paymentTermsId ?? '',
        priceListId: customer.priceListId ?? '',
        customerGroupId: customer.customerGroupId ?? '',
        creditLimit: customer.creditLimit ? String(customer.creditLimit) : '',
        defaultDiscountPercent: customer.defaultDiscountPercent
          ? String(customer.defaultDiscountPercent)
          : '',
        classification: customer.classification ?? '',
        territory: customer.territory ?? '',
        notes: customer.notes ?? '',
        isActive: customer.isActive,
      });
    } else {
      reset(emptyValues);
    }
  }, [open, customer, reset]);

  const onSubmit = handleSubmit(
    (values) => {
      const base = {
        name: values.name,
        type: values.type,
        code: values.code || null,
        legalName: values.legalName || null,
        tradeName: values.tradeName || null,
        email: values.email || null,
        phone: values.phone || null,
        nationalId: values.nationalId || null,
        taxNumber: values.taxNumber || null,
        taxOffice: values.taxOffice || null,
        website: values.website || null,
        defaultCurrency: values.defaultCurrency.toUpperCase(),
        paymentTermsId: values.paymentTermsId || null,
        priceListId: values.priceListId || null,
        customerGroupId: values.customerGroupId || null,
        creditLimit: Number(values.creditLimit) || 0,
        defaultDiscountPercent: Number(values.defaultDiscountPercent) || 0,
        classification: values.classification || null,
        territory: values.territory || null,
        notes: values.notes || null,
      };

      if (isEdit && customer) {
        updateMutation.mutate(
          {
            ...base,
            id: customer.id,
            salesRepUserId: customer.salesRepUserId,
            channel: customer.channel,
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

      createMutation.mutate(base, {
        onSuccess: (response) => {
          if (response.isSuccess) {
            toast.success(t('customers.toast.created'));
            if (response.data) onCreated?.(response.data);
            onClose();
            return;
          }
          toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
        },
        onError: (error) => toastApiError(error, t('auth.common.unexpectedError')),
      });
    },
    (formErrors) => {
      const target = firstErroredTab(formErrors, CUSTOMER_FIELD_TAB, [
        'general',
        'commercial',
        'notes',
      ]);
      if (target) setTab(target as CustomerTab);
    },
  );

  const translateError = (key?: string): string | undefined =>
    key ? t(key, { defaultValue: key }) : undefined;

  if (!open) return null;

  const isBusy = isSubmitting || createMutation.isPending || updateMutation.isPending;

  const onFormKeyDown = (e: React.KeyboardEvent) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') onSubmit();
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-white px-5 py-3 dark:border-slate-800 dark:bg-slate-900">
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? t('customers.modal.editTitle') : t('customers.modal.createTitle')}
          </h2>
          <button
            type="button"
            onClick={requestClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label={t('common.cancel')}
          >
            <X size={18} />
          </button>
        </div>

        <ModalTabs
          tabs={[
            {
              id: 'general',
              label: t('customers.tabs.general', { defaultValue: 'Genel Bilgiler' }),
              hasError: erroredTabs.has('general'),
            },
            {
              id: 'commercial',
              label: t('customers.tabs.commercial', { defaultValue: 'Ticari Koşullar' }),
              hasError: erroredTabs.has('commercial'),
            },
            {
              id: 'notes',
              label: t('customers.tabs.notes', { defaultValue: 'Notlar' }),
              hasError: erroredTabs.has('notes'),
            },
          ]}
          active={tab}
          onChange={(id) => setTab(id as typeof tab)}
        />

        <form
          onSubmit={onSubmit}
          onKeyDown={onFormKeyDown}
          noValidate
          className="space-y-4 px-5 py-4"
        >
          <div className={tab === 'general' ? 'space-y-4' : 'hidden'}>
            <section className="space-y-3">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div className="sm:col-span-2">
                  <Input
                    label={`${t('customers.fields.name')} *`}
                    placeholder={t('CustomerForm.NamePlaceholder', {
                      defaultValue: 'Örn. Acme Teknoloji A.Ş.',
                    })}
                    autoFocus
                    error={translateError(errors.name?.message)}
                    {...register('name')}
                  />
                </div>
                <div>
                  <label className={labelCls}>
                    {t('CustomerForm.TypeLabel', { defaultValue: 'Tip' })}
                  </label>
                  <select className={fieldCls} {...register('type')}>
                    <option value="Business">
                      {t('CustomerForm.TypeBusiness', { defaultValue: 'Şirket' })}
                    </option>
                    <option value="Individual">
                      {t('CustomerForm.TypeIndividual', { defaultValue: 'Şahıs' })}
                    </option>
                    <option value="Government">
                      {t('CustomerForm.TypeGovernment', { defaultValue: 'Kamu' })}
                    </option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <Input
                  label={t('CustomerForm.CodeLabel', { defaultValue: 'Kod' })}
                  placeholder={t('CustomerForm.CodePlaceholder', { defaultValue: 'Örn. MUS-0001' })}
                  {...register('code')}
                />
                <Input
                  label={t('CustomerForm.LegalNameLabel', { defaultValue: 'Ticari Ünvan' })}
                  placeholder={t('CustomerForm.LegalNamePlaceholder', {
                    defaultValue: 'Örn. Acme Teknoloji Anonim Şirketi',
                  })}
                  {...register('legalName')}
                />
                <Input
                  label={t('CustomerForm.TradeNameLabel', { defaultValue: 'Marka Adı' })}
                  placeholder={t('CustomerForm.TradeNamePlaceholder', {
                    defaultValue: 'Örn. Acme',
                  })}
                  {...register('tradeName')}
                />
              </div>
            </section>

            <section className="space-y-3 border-t border-slate-200 pt-3 dark:border-slate-800">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Input
                  label={t('customers.fields.email')}
                  type="email"
                  placeholder={t('CustomerForm.EmailPlaceholder', {
                    defaultValue: 'Örn. info@acme.com',
                  })}
                  error={translateError(errors.email?.message)}
                  {...register('email')}
                />
                <Controller
                  name="phone"
                  control={control}
                  render={({ field }) => (
                    <PhoneField
                      label={t('customers.fields.phone')}
                      value={field.value ?? ''}
                      onChange={field.onChange}
                      error={translateError(errors.phone?.message)}
                    />
                  )}
                />
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <Input
                  label={t('CustomerForm.NationalIdLabel', { defaultValue: 'TC Kimlik No' })}
                  placeholder={t('CustomerForm.NationalIdPlaceholder', {
                    defaultValue: 'Örn. 12345678901',
                  })}
                  {...register('nationalId')}
                />
                <Input
                  label={t('customers.fields.taxNumber')}
                  placeholder={t('CustomerForm.TaxNumberPlaceholder', {
                    defaultValue: 'Örn. 1234567890',
                  })}
                  error={translateError(errors.taxNumber?.message)}
                  {...register('taxNumber')}
                />
                <Input
                  label={t('CustomerForm.TaxOfficeLabel', { defaultValue: 'Vergi Dairesi' })}
                  placeholder={t('CustomerForm.TaxOfficePlaceholder', {
                    defaultValue: 'Örn. Kadıköy',
                  })}
                  {...register('taxOffice')}
                />
              </div>
              <Input
                label={t('CustomerForm.WebsiteLabel', { defaultValue: 'Web Sitesi' })}
                placeholder={t('CustomerForm.WebsitePlaceholder', {
                  defaultValue: 'Örn. https://acme.com',
                })}
                {...register('website')}
              />
            </section>
          </div>

          <div className={tab === 'commercial' ? 'space-y-4' : 'hidden'}>
            <section className="space-y-3 border-t border-slate-200 pt-3 dark:border-slate-800">
              <h3 className="text-xs font-semibold uppercase text-slate-500">
                {t('CustomerForm.CommercialTermsHeading', { defaultValue: 'Ticari Koşullar' })}
              </h3>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <label className={labelCls}>
                    {t('CustomerForm.CurrencyLabel', { defaultValue: 'Para Birimi' })}
                  </label>
                  <Controller
                    name="defaultCurrency"
                    control={control}
                    render={({ field }) => (
                      <CurrencySelect value={field.value} onChange={field.onChange} />
                    )}
                  />
                </div>
                <div>
                  <label className={labelCls}>
                    {t('CustomerForm.CreditLimitLabel', { defaultValue: 'Kredi Limiti' })}
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    className={fieldCls}
                    {...register('creditLimit')}
                  />
                </div>
                <div>
                  <label className={labelCls}>
                    {t('CustomerForm.DefaultDiscountLabel', {
                      defaultValue: 'Varsayılan İskonto %',
                    })}
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    className={fieldCls}
                    {...register('defaultDiscountPercent')}
                  />
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div>
                  <div className="mb-1 flex items-center justify-between">
                    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      {t('CustomerForm.PaymentTermLabel', { defaultValue: 'Ödeme Vadesi' })}
                    </label>
                    <button
                      type="button"
                      onClick={() => setQuickAdd('paymentTerm')}
                      className={quickAddBtnCls}
                    >
                      <Plus size={11} /> {t('CustomerForm.QuickAddNew', { defaultValue: 'Yeni' })}
                    </button>
                  </div>
                  <select className={fieldCls} {...register('paymentTermsId')}>
                    <option value="">—</option>
                    {(paymentTerms.data?.data ?? []).map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <div className="mb-1 flex items-center justify-between">
                    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      {t('CustomerForm.PriceListLabel', { defaultValue: 'Fiyat Listesi' })}
                    </label>
                    <button
                      type="button"
                      onClick={() => setQuickAdd('priceList')}
                      className={quickAddBtnCls}
                    >
                      <Plus size={11} /> {t('CustomerForm.QuickAddNew', { defaultValue: 'Yeni' })}
                    </button>
                  </div>
                  <select className={fieldCls} {...register('priceListId')}>
                    <option value="">—</option>
                    {(priceLists.data?.data ?? []).map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <div className="mb-1 flex items-center justify-between">
                    <label className="text-xs font-medium text-slate-700 dark:text-slate-300">
                      {t('CustomerForm.CustomerGroupLabel', { defaultValue: 'Müşteri Grubu' })}
                    </label>
                    <button
                      type="button"
                      onClick={() => setQuickAdd('customerGroup')}
                      className={quickAddBtnCls}
                    >
                      <Plus size={11} /> {t('CustomerForm.QuickAddNew', { defaultValue: 'Yeni' })}
                    </button>
                  </div>
                  <select className={fieldCls} {...register('customerGroupId')}>
                    <option value="">—</option>
                    {(customerGroups.data?.data ?? []).map((g) => (
                      <option key={g.id} value={g.id}>
                        {g.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Input
                  label={t('CustomerForm.ClassificationLabel', { defaultValue: 'Sınıflandırma' })}
                  {...register('classification')}
                />
                <Input
                  label={t('CustomerForm.TerritoryLabel', { defaultValue: 'Bölge' })}
                  {...register('territory')}
                />
              </div>
            </section>
          </div>

          <div className={tab === 'notes' ? 'space-y-4' : 'hidden'}>
            <section className="border-t border-slate-200 pt-3 dark:border-slate-800">
              <label className={labelCls}>{t('customers.fields.notes')}</label>
              <textarea rows={2} className={fieldCls} {...register('notes')} />
              {errors.notes?.message && (
                <span className="mt-1 block text-xs text-danger-500">
                  {translateError(errors.notes.message)}
                </span>
              )}
              {isEdit && (
                <label className="mt-3 flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 text-primary-600"
                    {...register('isActive')}
                  />
                  {t('customers.fields.isActive')}
                </label>
              )}
            </section>
          </div>

          <div className="sticky bottom-0 flex justify-end gap-2 border-t border-slate-200 bg-white pt-3 dark:border-slate-800 dark:bg-slate-900">
            <button
              type="button"
              onClick={requestClose}
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

      {quickAdd && (
        <MasterDataQuickModal
          kind={quickAdd}
          onClose={() => setQuickAdd(null)}
          onCreated={(id) => {
            setValue(QUICK_ADD_FIELD[quickAdd], id, { shouldDirty: true });
            setQuickAdd(null);
          }}
        />
      )}
    </div>
  );
};
