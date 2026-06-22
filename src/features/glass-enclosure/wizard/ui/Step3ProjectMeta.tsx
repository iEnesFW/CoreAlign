import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, UserPlus } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useCustomersQuery } from '@/features/customers/hooks/useCustomerQueries';
import { CustomerFormModal } from '@/features/customers/ui/CustomerFormModal';
import { useWizardStore } from '../model/wizardStore';

const fieldCls =
  'w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';
const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';

const NAME_MIN = 3;
const NAME_MAX = 120;

export const Step3ProjectMeta = () => {
  const { t } = useTranslation();
  const meta = useWizardStore((s) => s.meta);
  const patchMeta = useWizardStore((s) => s.patchMeta);

  const [customerSearch, setCustomerSearch] = useState('');
  const [isCustomerModalOpen, setCustomerModalOpen] = useState(false);
  const debounced = useDebouncedValue(customerSearch, 250);

  const customersQuery = useCustomersQuery({
    page: 1,
    pageSize: 20,
    search: debounced || undefined,
    isActive: true,
  });

  const customers = useMemo(() => customersQuery.data?.data?.items ?? [], [customersQuery.data]);

  const selectedCustomer = useMemo(
    () => customers.find((c) => c.id === meta.customerId) ?? null,
    [customers, meta.customerId],
  );

  const nameError =
    meta.name.length > 0 && (meta.name.trim().length < NAME_MIN || meta.name.length > NAME_MAX)
      ? t('GlassEnclosure.NewProjectWizard.Step3.NameError', {
          min: NAME_MIN,
          max: NAME_MAX,
          defaultValue: 'Proje adı {{min}}-{{max}} karakter olmalı.',
        })
      : null;

  const showNewCustomerHint =
    debounced.trim().length > 0 && !customersQuery.isLoading && customers.length === 0;

  return (
    <>
      <section className="space-y-5">
        <header className="space-y-1">
          <h3 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.NewProjectWizard.Step3.Title', {
              defaultValue: 'Proje detayları',
            })}
          </h3>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.NewProjectWizard.Step3.Hint', {
              defaultValue: 'Proje adı ve müşteri zorunlu, diğer alanlar opsiyonel.',
            })}
          </p>
        </header>

        <div className="space-y-3">
          <div>
            <label className={labelCls} htmlFor="wizard-project-name">
              {t('GlassEnclosure.NewProjectWizard.Step3.ProjectName', {
                defaultValue: 'Proje adı *',
              })}
            </label>
            <input
              id="wizard-project-name"
              type="text"
              value={meta.name}
              maxLength={NAME_MAX}
              onChange={(e) => patchMeta({ name: e.target.value })}
              placeholder={t('GlassEnclosure.NewProjectWizard.Step3.ProjectNamePlaceholder', {
                defaultValue: 'Örn. Yıldız Apt. Balkon',
              })}
              className={cn(fieldCls, nameError && 'border-danger-500 focus:border-danger-500')}
            />
            {nameError && (
              <p className="mt-1 text-[11px] text-danger-600 dark:text-danger-400">{nameError}</p>
            )}
          </div>

          <div>
            <label className={labelCls} htmlFor="wizard-customer-search">
              {t('GlassEnclosure.NewProjectWizard.Step3.Customer', {
                defaultValue: 'Müşteri *',
              })}
            </label>
            <div className="relative">
              <Search
                size={14}
                className="pointer-events-none absolute left-2.5 top-2.5 text-slate-400"
                aria-hidden
              />
              <input
                id="wizard-customer-search"
                type="text"
                value={customerSearch}
                onChange={(e) => setCustomerSearch(e.target.value)}
                placeholder={t('GlassEnclosure.NewProjectWizard.Step3.CustomerSearchPlaceholder', {
                  defaultValue: 'Müşteri ara...',
                })}
                className={cn(fieldCls, 'pl-8')}
              />
            </div>

            {selectedCustomer && (
              <div className="mt-2 flex items-center justify-between rounded-md border border-primary-200 bg-primary-50 px-3 py-2 dark:border-primary-700 dark:bg-primary-500/10">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-primary-900 dark:text-primary-200">
                    {selectedCustomer.name}
                  </p>
                  {selectedCustomer.code && (
                    <p className="truncate text-[10px] text-primary-600 dark:text-primary-400">
                      {selectedCustomer.code}
                    </p>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => patchMeta({ customerId: null })}
                  className="text-[11px] font-medium text-primary-700 hover:underline dark:text-primary-300"
                >
                  {t('GlassEnclosure.NewProjectWizard.Step3.ChangeCustomer', {
                    defaultValue: 'Değiştir',
                  })}
                </button>
              </div>
            )}

            {!selectedCustomer && customers.length > 0 && (
              <ul className="mt-2 max-h-48 divide-y divide-slate-100 overflow-y-auto rounded-md border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-700 dark:bg-slate-900">
                {customers.map((c) => (
                  <li key={c.id}>
                    <button
                      type="button"
                      onClick={() => {
                        patchMeta({ customerId: c.id });
                        setCustomerSearch('');
                      }}
                      className="flex w-full flex-col items-start gap-0.5 px-3 py-2 text-left hover:bg-slate-50 dark:hover:bg-slate-800"
                    >
                      <span className="text-sm text-slate-900 dark:text-slate-100">{c.name}</span>
                      {c.code && <span className="text-[10px] text-slate-400">{c.code}</span>}
                    </button>
                  </li>
                ))}
              </ul>
            )}

            {showNewCustomerHint && (
              <button
                type="button"
                className="mt-2 inline-flex items-center gap-1.5 rounded-md border border-dashed border-primary-300 px-3 py-1.5 text-[11px] font-medium text-primary-600 hover:bg-primary-50 dark:border-primary-600 dark:text-primary-300 dark:hover:bg-primary-500/10"
                onClick={() => setCustomerModalOpen(true)}
              >
                <UserPlus size={12} />
                {t('GlassEnclosure.NewProjectWizard.Step3.CreateNewCustomer', {
                  defaultValue: 'Yeni müşteri oluştur',
                })}
              </button>
            )}
          </div>

          <div>
            <label className={labelCls} htmlFor="wizard-address">
              {t('GlassEnclosure.NewProjectWizard.Step3.Address', {
                defaultValue: 'Saha adresi',
              })}
            </label>
            <textarea
              id="wizard-address"
              value={meta.addressText}
              onChange={(e) => patchMeta({ addressText: e.target.value })}
              rows={2}
              placeholder={t('GlassEnclosure.NewProjectWizard.Step3.AddressPlaceholder', {
                defaultValue: 'Mahalle, sokak, no, bina, ilçe/şehir',
              })}
              className={fieldCls}
            />
          </div>

          <div>
            <label className={labelCls} htmlFor="wizard-notes">
              {t('GlassEnclosure.NewProjectWizard.Step3.Notes', {
                defaultValue: 'Notlar',
              })}
            </label>
            <textarea
              id="wizard-notes"
              value={meta.notes}
              onChange={(e) => patchMeta({ notes: e.target.value })}
              rows={2}
              placeholder={t('GlassEnclosure.NewProjectWizard.Step3.NotesPlaceholder', {
                defaultValue: 'İç notlar, montaj koşulu vb.',
              })}
              className={fieldCls}
            />
          </div>
        </div>
      </section>
      <CustomerFormModal
        open={isCustomerModalOpen}
        customer={null}
        onClose={() => setCustomerModalOpen(false)}
        onCreated={(created) => {
          patchMeta({ customerId: created.id });
          setCustomerSearch('');
          setCustomerModalOpen(false);
        }}
      />
    </>
  );
};

export default Step3ProjectMeta;
