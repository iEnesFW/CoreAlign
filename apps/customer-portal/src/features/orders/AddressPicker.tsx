import { useTranslation } from 'react-i18next';
import { Home, MapPin } from 'lucide-react';
import type { PortalAddress } from '@/features/portal/types';
import { usePortalAddresses } from '@/features/portal/hooks';
import { Spinner } from '@/shared/ui/Spinner';

interface AddressPickerProps {
  selectedShippingAddressId: string | null;
  selectedBillingAddressId: string | null;
  onChangeShipping: (id: string | null) => void;
  onChangeBilling: (id: string | null) => void;
}

const formatAddress = (a: PortalAddress) => {
  const parts = [a.line1, a.line2, a.city, a.state, a.postalCode, a.country].filter(
    (s): s is string => !!s && s.length > 0,
  );
  return parts.join(', ');
};

export const AddressPicker = ({
  selectedShippingAddressId,
  selectedBillingAddressId,
  onChangeShipping,
  onChangeBilling,
}: AddressPickerProps) => {
  const { t } = useTranslation();
  const addresses = usePortalAddresses();

  if (addresses.isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Spinner size={14} /> {t('common.loading')}
      </div>
    );
  }

  const items = addresses.data ?? [];
  const defaultAddress = items.find((a) => a.isPrimary) ?? items[0];

  if (items.length === 0) {
    return (
      <p className="rounded-xl border border-dashed border-slate-200 px-4 py-4 text-xs text-slate-500 dark:border-slate-700">
        {t('orders.create.noAddresses')}
      </p>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
      <AddressSection
        title={t('orders.create.shippingAddress')}
        addresses={items}
        defaultAddress={defaultAddress}
        selectedId={selectedShippingAddressId ?? defaultAddress?.id ?? null}
        onChange={onChangeShipping}
        icon={<Home size={14} />}
      />
      <AddressSection
        title={t('orders.create.billingAddress')}
        addresses={items}
        defaultAddress={defaultAddress}
        selectedId={selectedBillingAddressId ?? defaultAddress?.id ?? null}
        onChange={onChangeBilling}
        icon={<MapPin size={14} />}
      />
    </div>
  );
};

interface AddressSectionProps {
  title: string;
  addresses: PortalAddress[];
  defaultAddress: PortalAddress | undefined;
  selectedId: string | null;
  onChange: (id: string | null) => void;
  icon: React.ReactNode;
}

const AddressSection = ({ title, addresses, selectedId, onChange, icon }: AddressSectionProps) => (
  <div className="rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
    <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-slate-700 dark:text-slate-200">
      {icon}
      <span>{title}</span>
    </div>
    <div className="space-y-2">
      {addresses.map((a) => (
        <label
          key={a.id}
          className={`flex cursor-pointer items-start gap-2 rounded-lg border px-3 py-2 text-xs ${
            selectedId === a.id
              ? 'border-sky-500 bg-sky-50 dark:border-sky-400 dark:bg-sky-500/10'
              : 'border-slate-200 dark:border-slate-700'
          }`}
        >
          <input
            type="radio"
            className="mt-0.5"
            checked={selectedId === a.id}
            onChange={() => onChange(a.id)}
          />
          <span>
            <span className="block font-semibold text-slate-800 dark:text-slate-100">
              {a.label}
              {a.isPrimary ? ' ★' : ''}
            </span>
            <span className="text-slate-500 dark:text-slate-400">{formatAddress(a)}</span>
          </span>
        </label>
      ))}
    </div>
  </div>
);
