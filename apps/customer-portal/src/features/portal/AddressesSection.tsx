import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Plus, Star, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { Input } from '@/shared/ui/Input';
import { Spinner } from '@/shared/ui/Spinner';
import type { PortalAddress, PortalAddressInput } from './types';
import {
  useCreatePortalAddress,
  useDeletePortalAddress,
  usePortalAddresses,
  useUpdatePortalAddress,
} from './hooks';

interface DraftAddress extends PortalAddressInput {
  id?: string;
}

const blankAddress = (): DraftAddress => ({
  label: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
  isPrimary: false,
});

export const AddressesSection = () => {
  const { t } = useTranslation();
  const addresses = usePortalAddresses();
  const createMutation = useCreatePortalAddress();
  const updateMutation = useUpdatePortalAddress();
  const deleteMutation = useDeletePortalAddress();

  const [draft, setDraft] = useState<DraftAddress | null>(null);

  const onSave = async () => {
    if (!draft) return;
    if (!draft.label.trim() || !draft.line1.trim()) {
      toast.error(t('common.errorGeneric'));
      return;
    }
    const payload: PortalAddressInput = {
      label: draft.label.trim(),
      line1: draft.line1.trim(),
      line2: draft.line2?.trim() || null,
      city: draft.city?.trim() || null,
      state: draft.state?.trim() || null,
      postalCode: draft.postalCode?.trim() || null,
      country: draft.country?.trim() || null,
      isPrimary: draft.isPrimary,
    };
    try {
      if (draft.id) {
        await updateMutation.mutateAsync({ id: draft.id, input: payload });
      } else {
        await createMutation.mutateAsync(payload);
      }
      toast.success(t('addresses.savedToast'));
      setDraft(null);
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  const onDelete = async (id: string) => {
    if (!confirm(t('addresses.confirmDelete'))) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success(t('addresses.deletedToast'));
    } catch {
      toast.error(t('common.errorGeneric'));
    }
  };

  return (
    <Card>
      <CardHeader title={t('addresses.title')} subtitle={t('addresses.subtitle')} />
      <CardBody>
        <div className="mb-4 flex justify-end">
          <Button type="button" variant="primary" onClick={() => setDraft(blankAddress())}>
            <Plus size={14} /> {t('addresses.addAddress')}
          </Button>
        </div>

        {addresses.isLoading ? (
          <Spinner />
        ) : (addresses.data?.length ?? 0) === 0 ? (
          <p className="text-sm text-slate-500">{t('addresses.empty')}</p>
        ) : (
          <div className="space-y-2">
            {addresses.data?.map((a) => (
              <AddressRow
                key={a.id}
                address={a}
                onEdit={() =>
                  setDraft({
                    id: a.id,
                    label: a.label,
                    line1: a.line1,
                    line2: a.line2 ?? '',
                    city: a.city ?? '',
                    state: a.state ?? '',
                    postalCode: a.postalCode ?? '',
                    country: a.country ?? '',
                    isPrimary: a.isPrimary,
                  })
                }
                onDelete={() => onDelete(a.id)}
              />
            ))}
          </div>
        )}

        {draft && (
          <AddressForm
            draft={draft}
            onChange={setDraft}
            onCancel={() => setDraft(null)}
            onSave={onSave}
            saving={createMutation.isPending || updateMutation.isPending}
          />
        )}
      </CardBody>
    </Card>
  );
};

interface AddressRowProps {
  address: PortalAddress;
  onEdit: () => void;
  onDelete: () => void;
}

const AddressRow = ({ address, onEdit, onDelete }: AddressRowProps) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col gap-2 rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <p className="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
          {address.label}
          {address.isPrimary ? <Star size={14} className="text-amber-500" /> : null}
        </p>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {[
            address.line1,
            address.line2,
            address.city,
            address.state,
            address.postalCode,
            address.country,
          ]
            .filter((s): s is string => !!s && s.length > 0)
            .join(', ')}
        </p>
      </div>
      <div className="flex shrink-0 gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onEdit}>
          <Pencil size={14} /> {t('addresses.editAddress')}
        </Button>
        <Button type="button" variant="ghost" size="sm" onClick={onDelete}>
          <Trash2 size={14} className="text-rose-500" /> {t('addresses.remove')}
        </Button>
      </div>
    </div>
  );
};

interface AddressFormProps {
  draft: DraftAddress;
  onChange: (next: DraftAddress) => void;
  onCancel: () => void;
  onSave: () => void;
  saving: boolean;
}

const AddressForm = ({ draft, onChange, onCancel, onSave, saving }: AddressFormProps) => {
  const { t } = useTranslation();
  return (
    <div className="mt-4 space-y-3 rounded-xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-900/50">
      <p className="text-sm font-semibold text-slate-800 dark:text-slate-100">
        {draft.id ? t('addresses.editAddress') : t('addresses.newAddress')}
      </p>
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        <Input
          label={t('addresses.label')}
          value={draft.label}
          onChange={(e) => onChange({ ...draft, label: e.target.value })}
        />
        <Input
          label={t('addresses.line1')}
          value={draft.line1}
          onChange={(e) => onChange({ ...draft, line1: e.target.value })}
        />
        <Input
          label={t('addresses.line2')}
          value={draft.line2 ?? ''}
          onChange={(e) => onChange({ ...draft, line2: e.target.value })}
        />
        <Input
          label={t('addresses.city')}
          value={draft.city ?? ''}
          onChange={(e) => onChange({ ...draft, city: e.target.value })}
        />
        <Input
          label={t('addresses.state')}
          value={draft.state ?? ''}
          onChange={(e) => onChange({ ...draft, state: e.target.value })}
        />
        <Input
          label={t('addresses.postalCode')}
          value={draft.postalCode ?? ''}
          onChange={(e) => onChange({ ...draft, postalCode: e.target.value })}
        />
        <Input
          label={t('addresses.country')}
          value={draft.country ?? ''}
          onChange={(e) => onChange({ ...draft, country: e.target.value })}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
          <input
            type="checkbox"
            checked={draft.isPrimary}
            onChange={(e) => onChange({ ...draft, isPrimary: e.target.checked })}
            className="h-4 w-4 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
          />
          {t('addresses.isPrimary')}
        </label>
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancel} disabled={saving}>
          {t('common.cancel')}
        </Button>
        <Button type="button" variant="primary" onClick={onSave} disabled={saving}>
          {t('addresses.save')}
        </Button>
      </div>
    </div>
  );
};
