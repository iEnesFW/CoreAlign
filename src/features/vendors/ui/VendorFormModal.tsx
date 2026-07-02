import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Truck } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { Label } from '@/shared/ui/Label/Label';
import { PhoneField } from '@/shared/ui/PhoneField/PhoneField';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { CurrencySelect } from '@/shared/ui/form/CurrencySelect';
import { useCreateVendor, useUpdateVendor } from '../hooks/useVendorQueries';
import type { CreateVendorRequest, Vendor, VendorType } from '../model/vendor.types';

interface VendorFormModalProps {
  vendor?: Vendor;
  onClose: () => void;
  onCreated?: (vendorId: string) => void;
}

export const VendorFormModal = ({ vendor, onClose, onCreated }: VendorFormModalProps) => {
  const { t } = useTranslation();
  const createMutation = useCreateVendor();
  const updateMutation = useUpdateVendor();
  const isEdit = !!vendor;

  const [form, setForm] = useState<CreateVendorRequest>({
    name: vendor?.name ?? '',
    type: vendor?.type ?? 'Business',
    code: vendor?.code ?? undefined,
    legalName: vendor?.legalName ?? undefined,
    tradeName: vendor?.tradeName ?? undefined,
    nationalId: vendor?.nationalId ?? undefined,
    taxNumber: vendor?.taxNumber ?? undefined,
    taxOffice: vendor?.taxOffice ?? undefined,
    email: vendor?.email ?? undefined,
    phone: vendor?.phone ?? undefined,
    website: vendor?.website ?? undefined,
    defaultCurrency: vendor?.defaultCurrency ?? 'TRY',
    paymentTermsId: vendor?.paymentTermsId ?? undefined,
    classification: vendor?.classification ?? undefined,
    territory: vendor?.territory ?? undefined,
    notes: vendor?.notes ?? undefined,
    defaultLeadTimeDays: vendor?.defaultLeadTimeDays ?? 0,
  });

  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose, false);

  const set = <K extends keyof CreateVendorRequest>(key: K, value: CreateVendorRequest[K]) => {
    setDirty(true);
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (isEdit && vendor) {
        await updateMutation.mutateAsync({
          id: vendor.id,
          body: {
            id: vendor.id,
            name: form.name.trim(),
            type: form.type ?? 'Business',
            code: form.code?.trim() || null,
            legalName: form.legalName?.trim() || null,
            tradeName: form.tradeName?.trim() || null,
            nationalId: form.nationalId?.trim() || null,
            taxNumber: form.taxNumber?.trim() || null,
            taxOffice: form.taxOffice?.trim() || null,
            email: form.email?.trim() || null,
            phone: form.phone?.trim() || null,
            website: form.website?.trim() || null,
            defaultCurrency: form.defaultCurrency ?? 'TRY',
            paymentTermsId: form.paymentTermsId ?? null,
            buyerUserId: vendor.buyerUserId ?? null,
            classification: form.classification?.trim() || null,
            territory: form.territory?.trim() || null,
            languageCode: vendor.languageCode ?? null,
            parentVendorId: vendor.parentVendorId ?? null,
            notes: form.notes?.trim() || null,
            defaultLeadTimeDays: form.defaultLeadTimeDays ?? 0,
          },
        });
        toast.success(t('Vendors.UpdateSuccess', { defaultValue: 'Tedarikçi güncellendi.' }));
      } else {
        const result = await createMutation.mutateAsync({
          ...form,
          name: form.name.trim(),
          code: form.code?.trim() || undefined,
          legalName: form.legalName?.trim() || undefined,
          taxNumber: form.taxNumber?.trim() || undefined,
          taxOffice: form.taxOffice?.trim() || undefined,
          email: form.email?.trim() || undefined,
          phone: form.phone?.trim() || undefined,
          website: form.website?.trim() || undefined,
          notes: form.notes?.trim() || undefined,
        });
        toast.success(
          t('Vendors.CreateSuccess', { defaultValue: 'Tedarikçi oluşturuldu (onay bekliyor).' }),
        );
        if (result.data?.id) onCreated?.(result.data.id);
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Modal
      open
      title={
        isEdit
          ? t('Vendors.EditTitle', { defaultValue: 'Tedarikçiyi Düzenle' })
          : t('Vendors.NewTitle', { defaultValue: 'Yeni Tedarikçi' })
      }
      icon={<Truck size={18} />}
      onClose={requestClose}
      size="xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={requestClose}>
            {t('Common.Cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="vendor-form" isLoading={isPending}>
            {isPending
              ? t('Common.Saving', { defaultValue: 'Kaydediliyor…' })
              : t('Common.Save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="vendor-form" onSubmit={submit} className="grid grid-cols-2 gap-3">
        <Input
          className="col-span-2"
          label={t('Vendors.NameLabel', { defaultValue: 'Tedarikçi Adı *' })}
          type="text"
          value={form.name}
          onChange={(e) => set('name', e.target.value)}
          required
          maxLength={200}
          placeholder={t('Vendors.NamePlaceholder', {
            defaultValue: 'Örn. Acme Tedarik Ltd. Şti.',
          })}
        />
        <Select
          label={t('Vendors.TypeLabel', { defaultValue: 'Tip' })}
          value={form.type}
          onChange={(e) => set('type', e.target.value as VendorType)}
        >
          <option value="Business">{t('Vendors.TypeBusiness', { defaultValue: 'Şirket' })}</option>
          <option value="Individual">
            {t('Vendors.TypeIndividual', { defaultValue: 'Şahıs' })}
          </option>
        </Select>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="vendor-default-currency">
            {t('Vendors.CurrencyLabel', { defaultValue: 'Para Birimi' })}
          </Label>
          <CurrencySelect
            id="vendor-default-currency"
            value={form.defaultCurrency ?? 'TRY'}
            onChange={(v) => set('defaultCurrency', v)}
          />
        </div>
        <Input
          label={t('Vendors.DefaultLeadTimeLabel', { defaultValue: 'Tedarik Süresi (gün)' })}
          type="number"
          min={0}
          value={String(form.defaultLeadTimeDays ?? 0)}
          onChange={(e) => set('defaultLeadTimeDays', Number(e.target.value) || 0)}
          placeholder={t('Vendors.DefaultLeadTimePlaceholder', {
            defaultValue: '0 = ürün varsayılanı (MRP)',
          })}
        />
        <Input
          label={t('Vendors.CodeLabel', { defaultValue: 'Kod' })}
          type="text"
          value={form.code ?? ''}
          onChange={(e) => set('code', e.target.value)}
          maxLength={32}
          placeholder={t('Vendors.CodePlaceholder', { defaultValue: 'Örn. TED-0001' })}
          className="font-mono"
        />
        <Input
          label={t('Vendors.LegalNameLabel', { defaultValue: 'Ticari Ünvan' })}
          type="text"
          value={form.legalName ?? ''}
          onChange={(e) => set('legalName', e.target.value)}
          maxLength={200}
          placeholder={t('Vendors.LegalNamePlaceholder', {
            defaultValue: 'Örn. Acme Tedarik Limited Şirketi',
          })}
        />
        <Input
          label={
            form.type === 'Individual'
              ? t('Vendors.NationalIdLabel', { defaultValue: 'TC Kimlik No' })
              : t('Vendors.TaxNumberLabel', { defaultValue: 'Vergi No (VKN)' })
          }
          type="text"
          value={form.type === 'Individual' ? (form.nationalId ?? '') : (form.taxNumber ?? '')}
          onChange={(e) =>
            form.type === 'Individual'
              ? set('nationalId', e.target.value)
              : set('taxNumber', e.target.value)
          }
          maxLength={50}
          className="font-mono"
        />
        <Input
          label={t('Vendors.TaxOfficeLabel', { defaultValue: 'Vergi Dairesi' })}
          type="text"
          value={form.taxOffice ?? ''}
          onChange={(e) => set('taxOffice', e.target.value)}
          maxLength={100}
        />
        <Input
          label={t('Vendors.EmailLabel', { defaultValue: 'E-posta' })}
          type="email"
          value={form.email ?? ''}
          onChange={(e) => set('email', e.target.value)}
          maxLength={256}
          placeholder={t('Vendors.EmailPlaceholder', {
            defaultValue: 'Örn. satinalma@acme.com',
          })}
        />
        <div>
          <PhoneField
            label={t('Vendors.PhoneLabel', { defaultValue: 'Telefon' })}
            value={form.phone ?? ''}
            onChange={(v) => set('phone', v)}
          />
        </div>
        <Textarea
          className="col-span-2"
          label={t('Vendors.NotesLabel', { defaultValue: 'Notlar' })}
          value={form.notes ?? ''}
          onChange={(e) => set('notes', e.target.value)}
          maxLength={2000}
          rows={2}
        />
      </form>
    </Modal>
  );
};
