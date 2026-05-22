import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
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

  // Initial state derived once from props — the parent passes a fresh component
  // instance per open via key prop on the page, so we never need to "sync"
  // state to changing props (avoids the set-state-in-effect anti-pattern).
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
          // Suggest a child-code stub when a parent is given (e.g. parent "120" → "120.01").
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-lg rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {mode === 'create'
              ? parent
                ? `${parent.code} altına yeni hesap`
                : t('accounting.coa.newAccount', { defaultValue: 'Yeni Hesap' })
              : t('accounting.coa.editAccount', { defaultValue: 'Hesabı Düzenle' })}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={16} />
          </button>
        </div>
        <form onSubmit={submit} className="space-y-3 p-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Kod
            </label>
            <input
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              required
              maxLength={32}
              disabled={mode === 'edit'}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
              placeholder="120.01"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              İsim
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={200}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Tip
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as AccountType)}
                disabled={mode === 'edit'}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
              >
                {ACCOUNT_TYPES.map((tp) => (
                  <option key={tp} value={tp}>
                    {tp}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Para Birimi
              </label>
              <input
                type="text"
                value={currency}
                onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                required
                maxLength={3}
                minLength={3}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm uppercase dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Açıklama
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={1000}
              rows={2}
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isPostable}
              onChange={(e) => setIsPostable(e.target.checked)}
            />
            Post edilebilir (leaf hesap — yevmiye fişi bu hesaba yazılabilir)
          </label>

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
