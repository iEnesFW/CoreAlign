import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Landmark } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { useCreateGLAccount, useUpdateGLAccount } from '../hooks/useGLAccountQueries';
import type { AccountType, GLAccount } from '../model/glAccount.types';

interface GLAccountFormModalProps {
  mode: 'create' | 'edit';
  account?: GLAccount;
  parent?: GLAccount;
  onClose: () => void;
}

const ACCOUNT_TYPES: AccountType[] = [
  'Asset',
  'Liability',
  'Equity',
  'Revenue',
  'Expense',
  'CostOfGoodsSold',
  'Memorandum',
];

export const GLAccountFormModal = ({ mode, account, parent, onClose }: GLAccountFormModalProps) => {
  const { t } = useTranslation();
  const createMutation = useCreateGLAccount();
  const updateMutation = useUpdateGLAccount();

  const initial =
    mode === 'edit' && account
      ? {
          code: account.code,
          name: account.name,
          description: account.description ?? '',
          type: account.type,
          currency: account.currency,
          isPostable: account.isPostable,
        }
      : {
          code: parent ? `${parent.code}.01` : '',
          name: '',
          description: '',
          type: parent?.type ?? ('Asset' as AccountType),
          currency: parent?.currency ?? 'TRY',
          isPostable: true,
        };

  const [code, setCode] = useState(initial.code);
  const [name, setName] = useState(initial.name);
  const [description, setDescription] = useState(initial.description);
  const [type, setType] = useState<AccountType>(initial.type);
  const [currency, setCurrency] = useState(initial.currency);
  const [isPostable, setIsPostable] = useState(initial.isPostable);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (mode === 'create') {
        await createMutation.mutateAsync({
          code: code.trim(),
          name: name.trim(),
          description: description.trim() || undefined,
          type,
          currency: currency.trim().toUpperCase(),
          isPostable,
          parentId: parent?.id ?? null,
        });
        toast.success(t('accounting.coa.created', { defaultValue: 'Hesap oluşturuldu.' }));
      } else if (account) {
        await updateMutation.mutateAsync({
          id: account.id,
          name: name.trim(),
          description: description.trim() || undefined,
          isPostable,
          currency: currency.trim().toUpperCase(),
        });
        toast.success(t('accounting.coa.updated', { defaultValue: 'Hesap güncellendi.' }));
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
        mode === 'create'
          ? parent
            ? `${parent.code} altına yeni hesap`
            : t('accounting.coa.newAccount', { defaultValue: 'Yeni Hesap' })
          : t('accounting.coa.editAccount', { defaultValue: 'Hesabı Düzenle' })
      }
      icon={<Landmark size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose}>
            İptal
          </Button>
          <Button type="submit" form="gl-account-form" isLoading={isPending}>
            {isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <form id="gl-account-form" onSubmit={submit} className="space-y-3">
        <Input
          label="Kod"
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          required
          maxLength={32}
          disabled={mode === 'edit'}
          placeholder="120.01"
          className="font-mono"
        />
        <Input
          label="İsim"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={200}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Tip"
            value={type}
            onChange={(e) => setType(e.target.value as AccountType)}
            disabled={mode === 'edit'}
          >
            {ACCOUNT_TYPES.map((tp) => (
              <option key={tp} value={tp}>
                {tp}
              </option>
            ))}
          </Select>
          <Input
            label="Para Birimi"
            type="text"
            value={currency}
            onChange={(e) => setCurrency(e.target.value.toUpperCase())}
            required
            maxLength={3}
            minLength={3}
            className="uppercase"
          />
        </div>
        <Textarea
          label="Açıklama"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={1000}
          rows={2}
        />
        <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPostable}
            onChange={(e) => setIsPostable(e.target.checked)}
          />
          Post edilebilir (leaf hesap — yevmiye fişi bu hesaba yazılabilir)
        </label>
      </form>
    </Modal>
  );
};
