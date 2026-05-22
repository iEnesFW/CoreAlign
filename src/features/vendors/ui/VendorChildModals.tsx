import { useState } from 'react';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { PhoneField } from '@/shared/ui/PhoneField/PhoneField';
import { CurrencySelect } from '@/features/lookups/ui/CurrencySelect';
import { AddressRegionFields } from '@/features/lookups/ui/AddressRegionFields';
import {
  useCreateVendorAddress,
  useCreateVendorBankAccount,
  useCreateVendorContact,
} from '../hooks/useVendorQueries';

const overlay = 'fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4';
const panel = 'w-full max-w-lg rounded-lg bg-white shadow-xl dark:bg-slate-900';
const inputCls =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800';
const labelCls = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

const ModalShell = ({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) => (
  <div className={overlay}>
    <div className={panel}>
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
        <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{title}</h2>
        <button
          type="button"
          onClick={onClose}
          className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          <X size={16} />
        </button>
      </div>
      {children}
    </div>
  </div>
);

const Actions = ({ onClose, pending }: { onClose: () => void; pending: boolean }) => (
  <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
    <button
      type="button"
      onClick={onClose}
      className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
    >
      İptal
    </button>
    <button
      type="submit"
      disabled={pending}
      className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
    >
      {pending ? 'Kaydediliyor…' : 'Kaydet'}
    </button>
  </div>
);

export const VendorAddressModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
  const create = useCreateVendorAddress();
  const [label, setLabel] = useState('');
  const [line1, setLine1] = useState('');
  const [line2, setLine2] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [country, setCountry] = useState('Türkiye');
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
      toast.success('Adres eklendi.');
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ModalShell title="Yeni Adres" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3 p-4">
        <div>
          <label className={labelCls}>Etiket *</label>
          <input
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            required
            maxLength={64}
            className={inputCls}
            placeholder="Merkez / Depo / Fatura"
          />
        </div>
        <div>
          <label className={labelCls}>Adres Satırı 1 *</label>
          <input
            value={line1}
            onChange={(e) => setLine1(e.target.value)}
            required
            maxLength={200}
            className={inputCls}
          />
        </div>
        <div>
          <label className={labelCls}>Adres Satırı 2</label>
          <input
            value={line2}
            onChange={(e) => setLine2(e.target.value)}
            maxLength={200}
            className={inputCls}
          />
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <AddressRegionFields
            country={country}
            state={state}
            city={city}
            onCountryChange={setCountry}
            onStateChange={setState}
            onCityChange={setCity}
            labels={{ country: 'Ülke', province: 'İl', district: 'İlçe' }}
            selectClassName={inputCls}
          />
          <div>
            <label className={labelCls}>Posta Kodu</label>
            <input
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
              maxLength={20}
              className={inputCls}
            />
          </div>
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          Birincil adres
        </label>
        <Actions onClose={onClose} pending={create.isPending} />
      </form>
    </ModalShell>
  );
};

export const VendorContactModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
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
      toast.success('Kontak eklendi.');
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ModalShell title="Yeni Kontak" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3 p-4">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className={labelCls}>Ad Soyad *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={150}
              className={inputCls}
            />
          </div>
          <div>
            <label className={labelCls}>Görev</label>
            <input
              value={role}
              onChange={(e) => setRole(e.target.value)}
              maxLength={100}
              className={inputCls}
            />
          </div>
          <div>
            <label className={labelCls}>E-posta</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              maxLength={256}
              className={inputCls}
            />
          </div>
          <PhoneField label="Telefon" value={phone} onChange={setPhone} />
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          Birincil kontak
        </label>
        <Actions onClose={onClose} pending={create.isPending} />
      </form>
    </ModalShell>
  );
};

export const VendorBankAccountModal = ({
  vendorId,
  onClose,
}: {
  vendorId: string;
  onClose: () => void;
}) => {
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
      toast.success('Banka hesabı eklendi.');
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ModalShell title="Yeni Banka Hesabı" onClose={onClose}>
      <form onSubmit={submit} className="space-y-3 p-4">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className={labelCls}>Banka *</label>
            <input
              value={bankName}
              onChange={(e) => setBankName(e.target.value)}
              required
              maxLength={150}
              className={inputCls}
            />
          </div>
          <div>
            <label className={labelCls}>Şube</label>
            <input
              value={branchName}
              onChange={(e) => setBranchName(e.target.value)}
              maxLength={150}
              className={inputCls}
            />
          </div>
        </div>
        <div>
          <label className={labelCls}>Hesap Sahibi *</label>
          <input
            value={accountHolder}
            onChange={(e) => setAccountHolder(e.target.value)}
            required
            maxLength={200}
            className={inputCls}
          />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <div className="col-span-2">
            <label className={labelCls}>IBAN *</label>
            <input
              value={iban}
              onChange={(e) => setIban(e.target.value.toUpperCase())}
              required
              maxLength={34}
              className={`${inputCls} font-mono`}
              placeholder="TR00 0000 0000 …"
            />
          </div>
          <div>
            <label className={labelCls}>Para Birimi</label>
            <CurrencySelect value={currency} onChange={setCurrency} className={inputCls} />
          </div>
        </div>
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
          />
          Birincil hesap
        </label>
        <Actions onClose={onClose} pending={create.isPending} />
      </form>
    </ModalShell>
  );
};
