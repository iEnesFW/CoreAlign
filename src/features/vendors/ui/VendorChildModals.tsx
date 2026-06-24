import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Landmark, MapPin, User } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Label } from '@/shared/ui/Label/Label';
import { PhoneField } from '@/shared/ui/PhoneField/PhoneField';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { AddressRegionFields } from '@/shared/ui/form/AddressRegionFields';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import {
  useCreateVendorAddress,
  useCreateVendorBankAccount,
  useCreateVendorContact,
} from '../hooks/useVendorQueries';

const ModalFooter = ({
  onClose,
  pending,
  formId,
}: {
  onClose: () => void;
  pending: boolean;
  formId: string;
}) => {
  const { t } = useTranslation();
  return (
    <>
      <Button variant="ghost" type="button" onClick={onClose}>
        {t('Vendors.cancel')}
      </Button>
      <Button type="submit" form={formId} isLoading={pending}>
        {pending ? t('Vendors.saving') : t('Vendors.save')}
      </Button>
    </>
  );
};

export const VendorAddressModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const create = useCreateVendorAddress();
  const [label, setLabel] = useState('');
  const [line1, setLine1] = useState('');
  const [line2, setLine2] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [country, setCountry] = useState(t('Vendors.defaultCountry'));
  const [isPrimary, setIsPrimary] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({
        vendorId,
        body: {
          label,
          line1,
          line2: line2 || null,
          city: city || null,
          state: state || null,
          postalCode: postalCode || null,
          country: country || null,
          isPrimary,
        },
      });
      toast.success(t('Vendors.address.success'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      title={t('Vendors.modal.newAddress')}
      icon={<MapPin size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <ModalFooter onClose={onClose} pending={create.isPending} formId="vendor-address-form" />
      }
    >
      <form id="vendor-address-form" onSubmit={submit} className="space-y-3">
        <Input
          label={t('Vendors.address.label')}
          required
          value={label}
          onChange={(e) => setLabel(e.target.value)}
          maxLength={64}
          placeholder={t('Vendors.address.labelPlaceholder')}
        />
        <Input
          label={t('Vendors.address.line1')}
          required
          value={line1}
          onChange={(e) => setLine1(e.target.value)}
          maxLength={200}
        />
        <Input
          label={t('Vendors.address.line2')}
          value={line2}
          onChange={(e) => setLine2(e.target.value)}
          maxLength={200}
        />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <AddressRegionFields
            country={country}
            state={state}
            city={city}
            onCountryChange={setCountry}
            onStateChange={setState}
            onCityChange={setCity}
            labels={{
              country: t('Vendors.address.country'),
              province: t('Vendors.address.province'),
              district: t('Vendors.address.district'),
            }}
            selectClassName={fieldBaseClasses()}
          />
          <label className="block">
            <span className="mb-0.5 block text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('Vendors.address.postalCode')}
            </span>
            <input
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
              maxLength={20}
              className={fieldBaseClasses()}
            />
          </label>
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          {t('Vendors.address.isPrimary')}
        </label>
      </form>
    </Modal>
  );
};

export const VendorContactModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const create = useCreateVendorContact();
  const [name, setName] = useState('');
  const [role, setRole] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [isPrimary, setIsPrimary] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({
        vendorId,
        body: {
          name,
          role: role || null,
          email: email || null,
          phone: phone || null,
          notes: null,
          isPrimary,
        },
      });
      toast.success(t('Vendors.contact.success'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      title={t('Vendors.modal.newContact')}
      icon={<User size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <ModalFooter onClose={onClose} pending={create.isPending} formId="vendor-contact-form" />
      }
    >
      <form id="vendor-contact-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <Input
            label={t('Vendors.contact.name')}
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={150}
          />
          <Input
            label={t('Vendors.contact.role')}
            value={role}
            onChange={(e) => setRole(e.target.value)}
            maxLength={100}
          />
          <Input
            label={t('Vendors.contact.email')}
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            maxLength={256}
          />
          <PhoneField label={t('Vendors.contact.phone')} value={phone} onChange={setPhone} />
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          {t('Vendors.contact.isPrimary')}
        </label>
      </form>
    </Modal>
  );
};

export const VendorBankAccountModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const create = useCreateVendorBankAccount();
  const [bankName, setBankName] = useState('');
  const [branchName, setBranchName] = useState('');
  const [accountHolder, setAccountHolder] = useState('');
  const [iban, setIban] = useState('');
  const [currency, setCurrency] = useState('TRY');
  const [isPrimary, setIsPrimary] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({
        vendorId,
        body: {
          bankName,
          branchName: branchName || null,
          accountHolder,
          iban,
          swift: null,
          currency,
          accountNumber: null,
          isPrimary,
          notes: null,
        },
      });
      toast.success(t('Vendors.bank.success'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      title={t('Vendors.modal.newBankAccount')}
      icon={<Landmark size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <ModalFooter onClose={onClose} pending={create.isPending} formId="vendor-bank-form" />
      }
    >
      <form id="vendor-bank-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <Input
            label={t('Vendors.bank.bankName')}
            required
            value={bankName}
            onChange={(e) => setBankName(e.target.value)}
            maxLength={150}
          />
          <Input
            label={t('Vendors.bank.branchName')}
            value={branchName}
            onChange={(e) => setBranchName(e.target.value)}
            maxLength={150}
          />
        </div>
        <Input
          label={t('Vendors.bank.accountHolder')}
          required
          value={accountHolder}
          onChange={(e) => setAccountHolder(e.target.value)}
          maxLength={200}
        />
        <div className="grid grid-cols-3 gap-3">
          <label className="col-span-2 flex w-full flex-col gap-1.5">
            <Label required>{t('Vendors.bank.iban')}</Label>
            <input
              value={iban}
              onChange={(e) => setIban(e.target.value.toUpperCase())}
              required
              maxLength={34}
              className={`${fieldBaseClasses()} font-mono`}
              placeholder="TR00 0000 0000 …"
            />
          </label>
          <label className="flex w-full flex-col gap-1.5">
            <Label>{t('Vendors.bank.currency')}</Label>
            <CurrencySelect
              value={currency}
              onChange={setCurrency}
              className={fieldBaseClasses()}
            />
          </label>
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          {t('Vendors.bank.isPrimary')}
        </label>
      </form>
    </Modal>
  );
};
