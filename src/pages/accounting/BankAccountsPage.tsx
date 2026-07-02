import { useTranslation } from 'react-i18next';
import { Landmark } from 'lucide-react';
import {
  useBankAccountsQuery,
  useCreateBankAccount,
  useDeleteBankAccount,
  useUpdateBankAccount,
} from '@/shared/master-data/hooks/useMasterData';
import type { BankAccount } from '@/shared/master-data/model/masterData.types';
import {
  MasterDataManager,
  type FieldValues,
  type MdColumn,
  type MdField,
} from '@/features/master-data/ui/MasterDataManager';

const BANK_ACCOUNTS_KEY = ['master-data', 'bank-accounts'] as const;

const toInput = (v: FieldValues) => ({
  accountName: String(v.accountName ?? '').trim(),
  bankName: String(v.bankName ?? '').trim(),
  iban: String(v.iban ?? '').trim(),
  currency: (String(v.currency ?? 'TRY').trim() || 'TRY').toUpperCase(),
  branchName: String(v.branchName ?? '').trim() || null,
  swift: String(v.swift ?? '').trim() || null,
  openingBalance: Number(v.openingBalance ?? 0) || 0,
  isPrimary: Boolean(v.isPrimary),
});

export const BankAccountsPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useBankAccountsQuery();
  const create = useCreateBankAccount();
  const update = useUpdateBankAccount();
  const remove = useDeleteBankAccount();

  const fields: MdField[] = [
    {
      name: 'accountName',
      label: t('masterData.bankAccounts.fields.accountName'),
      type: 'text',
      required: true,
    },
    {
      name: 'bankName',
      label: t('masterData.bankAccounts.fields.bankName'),
      type: 'text',
      required: true,
    },
    { name: 'branchName', label: t('masterData.bankAccounts.fields.branchName'), type: 'text' },
    {
      name: 'iban',
      label: t('masterData.bankAccounts.fields.iban'),
      type: 'text',
      required: true,
      placeholder: 'TR00 0000 0000 0000 0000 0000 00',
    },
    { name: 'swift', label: t('masterData.bankAccounts.fields.swift'), type: 'text' },
    {
      name: 'currency',
      label: t('masterData.bankAccounts.fields.currency'),
      type: 'text',
      required: true,
      placeholder: 'TRY',
    },
    {
      name: 'openingBalance',
      label: t('masterData.bankAccounts.fields.openingBalance'),
      type: 'number',
    },
    { name: 'isPrimary', label: t('masterData.bankAccounts.fields.isPrimary'), type: 'checkbox' },
    { name: 'isActive', label: t('masterData.bankAccounts.fields.isActive'), type: 'checkbox' },
  ];

  const columns: MdColumn<BankAccount>[] = [
    { key: 'bankName', label: t('masterData.bankAccounts.fields.bankName') },
    { key: 'accountName', label: t('masterData.bankAccounts.fields.accountName') },
    { key: 'iban', label: t('masterData.bankAccounts.fields.iban') },
    { key: 'currency', label: t('masterData.bankAccounts.fields.currency'), align: 'center' },
    {
      key: 'openingBalance',
      label: t('masterData.bankAccounts.fields.openingBalance'),
      align: 'right',
      render: (r) =>
        r.openingBalance.toLocaleString(undefined, {
          minimumFractionDigits: 2,
          maximumFractionDigits: 4,
        }),
    },
    {
      key: 'isPrimary',
      label: t('masterData.bankAccounts.fields.isPrimary'),
      align: 'center',
      render: (r) => (r.isPrimary ? '★' : '—'),
    },
  ];

  return (
    <div className="space-y-4 p-4">
      <header className="flex items-center gap-2">
        <Landmark size={18} className="text-primary-600 dark:text-primary-400" />
        <div>
          <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100">
            {t('masterData.bankAccounts.title')}
          </h2>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('masterData.bankAccounts.subtitle')}
          </p>
        </div>
      </header>

      <MasterDataManager<BankAccount>
        title={t('masterData.bankAccounts.title')}
        queryKey={BANK_ACCOUNTS_KEY}
        items={data?.data ?? []}
        isLoading={isLoading}
        fields={fields}
        columns={columns}
        toInitialValues={(row) => ({
          accountName: row.accountName,
          bankName: row.bankName,
          branchName: row.branchName ?? '',
          iban: row.iban,
          swift: row.swift ?? '',
          currency: row.currency,
          openingBalance: String(row.openingBalance),
          isPrimary: row.isPrimary,
          isActive: row.isActive,
        })}
        create={(v) => create.mutateAsync(toInput(v))}
        update={(id, v) => update.mutateAsync({ id, ...toInput(v), isActive: Boolean(v.isActive) })}
        remove={(id) => remove.mutateAsync(id)}
      />
    </div>
  );
};
