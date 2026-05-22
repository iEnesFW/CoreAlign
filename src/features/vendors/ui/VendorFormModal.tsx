import { useState } from 'react';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { PhoneField } from '@/shared/ui/PhoneField/PhoneField';
import { useModalClose } from '@/shared/hooks/useModalClose';
import { CurrencySelect } from '@/features/lookups/ui/CurrencySelect';
import { useCreateVendor, useUpdateVendor } from '../hooks/useVendorQueries';
import type { CreateVendorRequest, Vendor, VendorType } from '../model/vendor.types';

interface VendorFormModalProps {
  /** When provided, the modal edits an existing vendor instead of creating. */
  vendor?: Vendor;
  onClose: () => void;
  onCreated?: (vendorId: string) => void;
}

export const VendorFormModal = ({ vendor, onClose, onCreated }: VendorFormModalProps) => {
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
  });

  const [dirty, setDirty] = useState(false);
  const requestClose = useModalClose(dirty, onClose);

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
          },
        });
        toast.success('Tedarikçi güncellendi.');
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
        toast.success('Tedarikçi oluşturuldu (onay bekliyor).');
        if (result.data?.id) onCreated?.(result.data.id);
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onClick={requestClose}
      role="presentation"
    >
      <div
        className="w-full max-w-2xl rounded-lg bg-white shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {isEdit ? 'Tedarikçiyi Düzenle' : 'Yeni Tedarikçi'}
          </h2>
          <button
            type="button"
            onClick={requestClose}
            aria-label="Kapat"
            className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={16} />
          </button>
        </div>
        <form onSubmit={submit} className="grid grid-cols-2 gap-3 p-4">
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Tedarikçi Adı *
            </label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => set('name', e.target.value)}
              required
              maxLength={200}
              placeholder="Örn. Acme Tedarik Ltd. Şti."
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Tip
            </label>
            <select
              value={form.type}
              onChange={(e) => set('type', e.target.value as VendorType)}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="Business">Şirket</option>
              <option value="Individual">Şahıs</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Para Birimi
            </label>
            <CurrencySelect
              value={form.defaultCurrency ?? 'TRY'}
              onChange={(v) => set('defaultCurrency', v)}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Kod
            </label>
            <input
              type="text"
              value={form.code ?? ''}
              onChange={(e) => set('code', e.target.value)}
              maxLength={32}
              placeholder="Örn. TED-0001"
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Ticari Ünvan
            </label>
            <input
              type="text"
              value={form.legalName ?? ''}
              onChange={(e) => set('legalName', e.target.value)}
              maxLength={200}
              placeholder="Örn. Acme Tedarik Limited Şirketi"
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              {form.type === 'Individual' ? 'TC Kimlik No' : 'Vergi No (VKN)'}
            </label>
            <input
              type="text"
              value={form.type === 'Individual' ? (form.nationalId ?? '') : (form.taxNumber ?? '')}
              onChange={(e) =>
                form.type === 'Individual'
                  ? set('nationalId', e.target.value)
                  : set('taxNumber', e.target.value)
              }
              maxLength={50}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Vergi Dairesi
            </label>
            <input
              type="text"
              value={form.taxOffice ?? ''}
              onChange={(e) => set('taxOffice', e.target.value)}
              maxLength={100}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              E-posta
            </label>
            <input
              type="email"
              value={form.email ?? ''}
              onChange={(e) => set('email', e.target.value)}
              maxLength={256}
              placeholder="Örn. satinalma@acme.com"
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div>
            <PhoneField
              label="Telefon"
              value={form.phone ?? ''}
              onChange={(v) => set('phone', v)}
            />
          </div>
          <div className="col-span-2">
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Notlar
            </label>
            <textarea
              value={form.notes ?? ''}
              onChange={(e) => set('notes', e.target.value)}
              maxLength={2000}
              rows={2}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="col-span-2 flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
            <button
              type="button"
              onClick={requestClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              İptal
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
