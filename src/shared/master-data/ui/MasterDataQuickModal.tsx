import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Label } from '@/shared/ui/Label/Label';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { toastApiError } from '@/shared/lib/mutationToast';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import {
  useCreateBrand,
  useCreateCustomerGroup,
  useCreatePaymentTerm,
  useCreatePriceList,
  useCreateProductCategory,
  useCreateTaxRate,
  useCreateUnitOfMeasure,
} from '@/shared/master-data/hooks/useMasterData';

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

type TFunction = ReturnType<typeof useTranslation>['t'];

const buildTitles = (t: TFunction): Record<QuickAddKind, string> => ({
  paymentTerm: t('MasterDataQuickAdd.TitlePaymentTerm', { defaultValue: 'Yeni Ödeme Vadesi' }),
  priceList: t('MasterDataQuickAdd.TitlePriceList', { defaultValue: 'Yeni Fiyat Listesi' }),
  customerGroup: t('MasterDataQuickAdd.TitleCustomerGroup', { defaultValue: 'Yeni Müşteri Grubu' }),
  brand: t('MasterDataQuickAdd.TitleBrand', { defaultValue: 'Yeni Marka' }),
  category: t('MasterDataQuickAdd.TitleCategory', { defaultValue: 'Yeni Kategori' }),
  uom: t('MasterDataQuickAdd.TitleUom', { defaultValue: 'Yeni Birim' }),
  taxRate: t('MasterDataQuickAdd.TitleTaxRate', { defaultValue: 'Yeni Vergi Oranı' }),
});

const buildCodePlaceholders = (t: TFunction): Record<QuickAddKind, string> => ({
  paymentTerm: t('MasterDataQuickAdd.CodePlaceholderPaymentTerm', { defaultValue: 'Örn. NET30' }),
  priceList: t('MasterDataQuickAdd.CodePlaceholderPriceList', { defaultValue: 'Örn. PL-TRY' }),
  customerGroup: t('MasterDataQuickAdd.CodePlaceholderCustomerGroup', {
    defaultValue: 'Örn. BAYI',
  }),
  brand: t('MasterDataQuickAdd.CodePlaceholderBrand', { defaultValue: 'Örn. ACME' }),
  category: t('MasterDataQuickAdd.CodePlaceholderCategory', { defaultValue: 'Örn. ELEKTRONIK' }),
  uom: t('MasterDataQuickAdd.CodePlaceholderUom', { defaultValue: 'Örn. ADET' }),
  taxRate: t('MasterDataQuickAdd.CodePlaceholderTaxRate', { defaultValue: 'Örn. KDV20' }),
});

const buildNamePlaceholders = (t: TFunction): Record<QuickAddKind, string> => ({
  paymentTerm: t('MasterDataQuickAdd.NamePlaceholderPaymentTerm', {
    defaultValue: 'Örn. 30 Gün Vade',
  }),
  priceList: t('MasterDataQuickAdd.NamePlaceholderPriceList', {
    defaultValue: 'Örn. Perakende TRY',
  }),
  customerGroup: t('MasterDataQuickAdd.NamePlaceholderCustomerGroup', {
    defaultValue: 'Örn. Bayiler',
  }),
  brand: t('MasterDataQuickAdd.NamePlaceholderBrand', { defaultValue: 'Örn. Acme' }),
  category: t('MasterDataQuickAdd.NamePlaceholderCategory', { defaultValue: 'Örn. Elektronik' }),
  uom: t('MasterDataQuickAdd.NamePlaceholderUom', { defaultValue: 'Örn. Adet' }),
  taxRate: t('MasterDataQuickAdd.NamePlaceholderTaxRate', { defaultValue: 'Örn. KDV %20' }),
});

export const MasterDataQuickModal = ({ kind, onClose, onCreated }: Props) => {
  const { t } = useTranslation();
  const titles = buildTitles(t);
  const codePlaceholders = buildCodePlaceholders(t);
  const namePlaceholders = buildNamePlaceholders(t);
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
  const requestClose = useModalClose(dirty, onClose, false);

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
      toast.success(
        t('MasterDataQuickAdd.AddedAndSelected', { defaultValue: 'Eklendi ve seçildi.' }),
      );
      onCreated(response.data.id);
      return;
    }
    toast.error(
      response.errors[0] ?? t('MasterDataQuickAdd.SaveFailed', { defaultValue: 'Kaydedilemedi.' }),
    );
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      toast.error(
        t('MasterDataQuickAdd.CodeAndNameRequired', { defaultValue: 'Kod ve ad zorunludur.' }),
      );
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
    <Modal
      open={true}
      title={titles[kind]}
      icon={<Plus size={18} />}
      onClose={requestClose}
      size="sm"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={requestClose}>
            {t('Common.Cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="master-data-quick-form" isLoading={isPending}>
            {t('MasterDataQuickAdd.AddAndSelect', { defaultValue: 'Ekle ve Seç' })}
          </Button>
        </>
      }
    >
      <form
        id="master-data-quick-form"
        onSubmit={submit}
        onChange={() => setDirty(true)}
        className="space-y-3"
      >
        <div className="grid grid-cols-3 gap-3">
          <Input
            label={t('MasterDataQuickAdd.CodeLabel', { defaultValue: 'Kod' })}
            placeholder={codePlaceholders[kind]}
            value={code}
            onChange={(e) => setCode(e.target.value)}
          />
          <Input
            className="col-span-2"
            label={t('MasterDataQuickAdd.NameLabel', { defaultValue: 'Ad' })}
            placeholder={namePlaceholders[kind]}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>

        {kind === 'paymentTerm' && (
          <Input
            label={t('MasterDataQuickAdd.NetDaysLabel', { defaultValue: 'Net Gün' })}
            type="number"
            min="0"
            value={netDays}
            onChange={(e) => setNetDays(e.target.value)}
          />
        )}

        {kind === 'priceList' && (
          <div className="flex w-full flex-col gap-1.5">
            <Label htmlFor="master-data-quick-currency">
              {t('MasterDataQuickAdd.CurrencyLabel', { defaultValue: 'Para Birimi' })}
            </Label>
            <CurrencySelect
              id="master-data-quick-currency"
              value={currency}
              onChange={setCurrency}
            />
          </div>
        )}

        {kind === 'taxRate' && (
          <Input
            label={t('MasterDataQuickAdd.RatePercentLabel', { defaultValue: 'Oran (%)' })}
            type="number"
            min="0"
            max="100"
            step="0.01"
            value={ratePercent}
            onChange={(e) => setRatePercent(e.target.value)}
          />
        )}
      </form>
    </Modal>
  );
};
