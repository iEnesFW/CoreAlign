import { useState } from 'react';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { toastApiError } from '@/shared/lib/mutationToast';
import { CurrencySelect } from '@/features/lookups/ui/CurrencySelect';
import {
  useCreateBrand,
  useCreateCustomerGroup,
  useCreatePaymentTerm,
  useCreatePriceList,
  useCreateProductCategory,
  useCreateTaxRate,
  useCreateUnitOfMeasure,
} from '../hooks/useMasterData';

export type QuickAddKind =
  | 'paymentTerm'
  | 'priceList'
  | 'customerGroup'
  | 'brand'
  | 'category'
  | 'uom'
  | 'taxRate';

interface Props {
  kind: QuickAddKind;
  onClose: () => void;
  onCreated: (id: string) => void;
}

const TITLES: Record<QuickAddKind, string> = {
  paymentTerm: 'Yeni Ödeme Vadesi',
  priceList: 'Yeni Fiyat Listesi',
  customerGroup: 'Yeni Müşteri Grubu',
  brand: 'Yeni Marka',
  category: 'Yeni Kategori',
  uom: 'Yeni Birim',
  taxRate: 'Yeni Vergi Oranı',
};

const CODE_PLACEHOLDER: Record<QuickAddKind, string> = {
  paymentTerm: 'Örn. NET30',
  priceList: 'Örn. PL-TRY',
  customerGroup: 'Örn. BAYI',
  brand: 'Örn. ACME',
  category: 'Örn. ELEKTRONIK',
  uom: 'Örn. ADET',
  taxRate: 'Örn. KDV20',
};

const NAME_PLACEHOLDER: Record<QuickAddKind, string> = {
  paymentTerm: 'Örn. 30 Gün Vade',
  priceList: 'Örn. Perakende TRY',
  customerGroup: 'Örn. Bayiler',
  brand: 'Örn. Acme',
  category: 'Örn. Elektronik',
  uom: 'Örn. Adet',
  taxRate: 'Örn. KDV %20',
};

const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';

export const MasterDataQuickModal = ({ kind, onClose, onCreated }: Props) => {
  const createPaymentTerm = useCreatePaymentTerm();
  const createPriceList = useCreatePriceList();
  const createCustomerGroup = useCreateCustomerGroup();
  const createBrand = useCreateBrand();
  const createCategory = useCreateProductCategory();
  const createUom = useCreateUnitOfMeasure();
  const createTaxRate = useCreateTaxRate();

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [netDays, setNetDays] = useState('30');
  const [currency, setCurrency] = useState('TRY');
  const [ratePercent, setRatePercent] = useState('20');
  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose);

  const isPending =
    createPaymentTerm.isPending ||
    createPriceList.isPending ||
    createCustomerGroup.isPending ||
    createBrand.isPending ||
    createCategory.isPending ||
    createUom.isPending ||
    createTaxRate.isPending;

  const handleCreated = (response: {
    isSuccess: boolean;
    data?: { id: string } | null;
    errors: string[];
  }) => {
    if (response.isSuccess && response.data) {
      toast.success('Eklendi ve seçildi.');
      onCreated(response.data.id);
      return;
    }
    toast.error(response.errors[0] ?? 'Kaydedilemedi.');
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      toast.error('Kod ve ad zorunludur.');
      return;
    }
    const c = code.trim();
    const n = name.trim();
    try {
      switch (kind) {
        case 'paymentTerm':
          handleCreated(
            await createPaymentTerm.mutateAsync({
              code: c,
              name: n,
              netDays: Number(netDays) || 0,
            }),
          );
          break;
        case 'priceList':
          handleCreated(
            await createPriceList.mutateAsync({
              code: c,
              name: n,
              currency: currency.toUpperCase(),
            }),
          );
          break;
        case 'customerGroup':
          handleCreated(await createCustomerGroup.mutateAsync({ code: c, name: n }));
          break;
        case 'brand':
          handleCreated(await createBrand.mutateAsync({ code: c, name: n }));
          break;
        case 'category':
          handleCreated(await createCategory.mutateAsync({ code: c, name: n }));
          break;
        case 'uom':
          handleCreated(await createUom.mutateAsync({ code: c, name: n }));
          break;
        case 'taxRate':
          handleCreated(
            await createTaxRate.mutateAsync({
              code: c,
              name: n,
              ratePercent: Number(ratePercent) || 0,
            }),
          );
          break;
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div
      className="fixed inset-0 z-[55] flex items-center justify-center bg-black/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="w-full max-w-sm rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {TITLES[kind]}
          </h3>
          <button
            type="button"
            onClick={requestClose}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            aria-label="Kapat"
          >
            <X size={16} />
          </button>
        </div>

        <form onSubmit={submit} onChange={() => setDirty(true)} className="space-y-3 px-4 py-4">
          <div className="grid grid-cols-3 gap-3">
            <Input
              label="Kod"
              placeholder={CODE_PLACEHOLDER[kind]}
              value={code}
              onChange={(e) => setCode(e.target.value)}
            />
            <div className="col-span-2">
              <Input
                label="Ad"
                placeholder={NAME_PLACEHOLDER[kind]}
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
          </div>

          {kind === 'paymentTerm' && (
            <Input
              label="Net Gün"
              type="number"
              min="0"
              value={netDays}
              onChange={(e) => setNetDays(e.target.value)}
            />
          )}

          {kind === 'priceList' && (
            <div>
              <label className={labelCls}>Para Birimi</label>
              <CurrencySelect value={currency} onChange={setCurrency} />
            </div>
          )}

          {kind === 'taxRate' && (
            <Input
              label="Oran (%)"
              type="number"
              min="0"
              max="100"
              step="0.01"
              value={ratePercent}
              onChange={(e) => setRatePercent(e.target.value)}
            />
          )}

          <div className="flex justify-end gap-2 pt-1">
            <button
              type="button"
              onClick={requestClose}
              className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              İptal
            </button>
            <Button type="submit" isLoading={isPending}>
              Ekle ve Seç
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
