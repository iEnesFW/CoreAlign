import { useTranslation } from 'react-i18next';
import { Building2, Mail, MapPin, Phone, Star } from 'lucide-react';
import type { CustomerOverview } from '@/features/customers/model/customer.types';

export const PrimaryAddressPreview = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const address = overview?.primaryShippingAddress ?? overview?.primaryBillingAddress ?? null;
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <MapPin size={12} />
          {t('customers.detail.primaryAddress')}
        </span>
        {address?.isPrimary && (
          <span className="inline-flex items-center gap-0.5 text-warning-500">
            <Star size={10} fill="currentColor" />
          </span>
        )}
      </header>
      <div className="mt-1.5 min-h-[42px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
        {address ? (
          <>
            <div className="font-semibold">{address.label}</div>
            <div className="mt-0.5 text-slate-600 dark:text-slate-300">{address.line1}</div>
            {address.line2 && (
              <div className="text-slate-600 dark:text-slate-300">{address.line2}</div>
            )}
            <div className="mt-0.5 text-slate-500 dark:text-slate-400">
              {[address.postalCode, address.city, address.state, address.country]
                .filter(Boolean)
                .join(', ')}
            </div>
          </>
        ) : (
          <span className="italic text-slate-400 dark:text-slate-500">
            {loading ? t('common.loading') : t('customers.detail.noPrimaryAddress')}
          </span>
        )}
      </div>
    </article>
  );
};

export const PrimaryContactPreview = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const contact = overview?.primaryContact ?? null;
  return (
    <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <span className="inline-flex items-center gap-1">
          <Building2 size={12} />
          {t('customers.detail.primaryContact')}
        </span>
        {contact?.isPrimary && (
          <span className="inline-flex items-center gap-0.5 text-warning-500">
            <Star size={10} fill="currentColor" />
          </span>
        )}
      </header>
      <div className="mt-1.5 min-h-[42px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
        {contact ? (
          <>
            <div className="font-semibold">{contact.name}</div>
            {contact.role && (
              <div className="mt-0.5 text-slate-500 dark:text-slate-400">{contact.role}</div>
            )}
            <div className="mt-0.5 flex flex-wrap gap-x-2 gap-y-0.5 text-slate-600 dark:text-slate-300">
              {contact.email && (
                <span className="inline-flex items-center gap-1">
                  <Mail size={10} /> {contact.email}
                </span>
              )}
              {contact.phone && (
                <span className="inline-flex items-center gap-1">
                  <Phone size={10} /> {contact.phone}
                </span>
              )}
            </div>
          </>
        ) : (
          <span className="italic text-slate-400 dark:text-slate-500">
            {loading ? t('common.loading') : t('customers.detail.noPrimaryContact')}
          </span>
        )}
      </div>
    </article>
  );
};
