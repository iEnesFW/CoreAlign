import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  Building2,
  MapPin,
  Phone,
  CreditCard,
  BookOpen,
  Pencil,
  Plus,
  Star,
  Trash2,
} from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import {
  useDeleteVendorAddress,
  useDeleteVendorBankAccount,
  useDeleteVendorContact,
  useSetVendorRating,
  useVendorAddressesQuery,
  useVendorBankAccountsQuery,
  useVendorContactsQuery,
  useVendorLedgerQuery,
  useVendorQuery,
} from '@/features/vendors/hooks/useVendorQueries';
import { VendorFormModal } from '@/features/vendors/ui/VendorFormModal';
import {
  VendorAddressModal,
  VendorBankAccountModal,
  VendorContactModal,
} from '@/features/vendors/ui/VendorChildModals';
import type { VendorStatus } from '@/features/vendors/model/vendor.types';

type Tab = 'overview' | 'addresses' | 'contacts' | 'bank' | 'ledger';
type ChildModal = 'address' | 'contact' | 'bank' | null;

const STATUS_VARIANTS: Record<VendorStatus, BadgeVariant> = {
  Active: 'success',
  Blocked: 'danger',
  Archived: 'neutral',
  PendingApproval: 'warning',
};

export const VendorDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const confirm = useConfirm();

  const [tab, setTab] = useState<Tab>('overview');
  const [showEdit, setShowEdit] = useState(false);
  const [childModal, setChildModal] = useState<ChildModal>(null);
  const ledgerParams = useMemo(() => ({ page: 1, pageSize: 50 }), []);

  const vendor = useVendorQuery(id);
  const addresses = useVendorAddressesQuery(tab === 'addresses' ? id : undefined);
  const contacts = useVendorContactsQuery(tab === 'contacts' ? id : undefined);
  const banks = useVendorBankAccountsQuery(tab === 'bank' ? id : undefined);
  const ledger = useVendorLedgerQuery(tab === 'ledger' ? id : undefined, ledgerParams);

  const setRating = useSetVendorRating();
  const deleteAddress = useDeleteVendorAddress();
  const deleteContact = useDeleteVendorContact();
  const deleteBank = useDeleteVendorBankAccount();

  const v = vendor.data?.data;

  if (vendor.isPending) {
    return <div className="p-6 text-sm text-slate-500">{t('VendorDetail.loading')}</div>;
  }
  if (!v) {
    return <div className="p-6 text-sm text-slate-500">{t('VendorDetail.notFound')}</div>;
  }

  const tabs: { id: Tab; label: string; icon: typeof Building2 }[] = [
    { id: 'overview', label: t('VendorDetail.tabs.overview'), icon: Building2 },
    { id: 'addresses', label: t('VendorDetail.tabs.addresses'), icon: MapPin },
    { id: 'contacts', label: t('VendorDetail.tabs.contacts'), icon: Phone },
    { id: 'bank', label: t('VendorDetail.tabs.bank'), icon: CreditCard },
    { id: 'ledger', label: t('VendorDetail.tabs.ledger'), icon: BookOpen },
  ];

  const applyRating = async (rating: number) => {
    try {
      await setRating.mutateAsync({ id: v.id, rating });
      toast.success(t('VendorDetail.rating.success'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const removeChild = async (
    kind: 'address' | 'contact' | 'bank',
    childId: string,
    label: string,
  ) => {
    const ok = await confirm({
      title: t('VendorDetail.delete.title'),
      message: t('VendorDetail.delete.message', { label }),
      confirmLabel: t('VendorDetail.delete.confirm'),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      if (kind === 'address') await deleteAddress.mutateAsync(childId);
      else if (kind === 'contact') await deleteContact.mutateAsync(childId);
      else await deleteBank.mutateAsync(childId);
      toast.success(t('VendorDetail.delete.success'));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Building2 size={20} />}
          title={v.name}
          subtitle={v.legalName ?? undefined}
          crumbs={[
            { label: t('VendorDetail.backToList'), to: '/dashboard/vendors' },
            { label: v.name },
          ]}
          actions={
            <Button variant="secondary" size="sm" onClick={() => setShowEdit(true)}>
              <Pencil size={14} />
              {t('VendorDetail.edit')}
            </Button>
          }
          trailing={
            <div className="flex flex-col items-stretch gap-2 sm:items-end">
              <Badge variant={STATUS_VARIANTS[v.status]}>{t(`Vendors.status.${v.status}`)}</Badge>
              <div className="text-right">
                <div className="text-xs text-slate-500">{t('VendorDetail.currentBalance')}</div>
                <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
                  {formatCurrency(v.currentBalance, locale, v.defaultCurrency)}
                </div>
                {v.overdueAmount > 0 && (
                  <div className="text-xs text-danger-600 dark:text-danger-400">
                    {t('VendorDetail.overdue')}:{' '}
                    {formatCurrency(v.overdueAmount, locale, v.defaultCurrency)}
                  </div>
                )}
              </div>
            </div>
          }
        />
      }
    >
      <div className="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-4 text-xs text-slate-600 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300">
        {v.code && (
          <div>
            <span className="text-slate-500">{t('VendorDetail.fields.code')}:</span>{' '}
            <span className="font-mono">{v.code}</span>
          </div>
        )}
        {v.taxNumber && (
          <div>
            <span className="text-slate-500">{t('VendorDetail.fields.taxNumber')}:</span>{' '}
            <span className="font-mono">{v.taxNumber}</span>
          </div>
        )}
        {v.taxOffice && (
          <div>
            <span className="text-slate-500">{t('VendorDetail.fields.taxOffice')}:</span>{' '}
            {v.taxOffice}
          </div>
        )}
        {v.email && (
          <div>
            <span className="text-slate-500">{t('VendorDetail.fields.email')}:</span> {v.email}
          </div>
        )}
        {v.phone && (
          <div>
            <span className="text-slate-500">{t('VendorDetail.fields.phone')}:</span> {v.phone}
          </div>
        )}
      </div>

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        {tabs.map((t) => {
          const Icon = t.icon;
          const active = tab === t.id;
          return (
            <button
              key={t.id}
              type="button"
              onClick={() => setTab(t.id)}
              className={`inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition ${
                active
                  ? 'border-primary-600 text-primary-700 dark:border-primary-400 dark:text-primary-300'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              <Icon size={12} />
              {t.label}
            </button>
          );
        })}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        {tab === 'overview' && (
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
            <Field label={t('VendorDetail.fields.type')}>
              {v.type === 'Business' ? t('VendorDetail.business') : t('VendorDetail.individual')}
            </Field>
            <Field label={t('VendorDetail.fields.currency')}>{v.defaultCurrency}</Field>
            <Field label={t('VendorDetail.fields.paymentTerms')}>{v.paymentTermsName ?? '—'}</Field>
            <Field label={t('VendorDetail.fields.territory')}>{v.territory ?? '—'}</Field>
            <Field label={t('VendorDetail.fields.classification')}>{v.classification ?? '—'}</Field>
            <Field label={t('VendorDetail.fields.website')}>{v.website ?? '—'}</Field>
            <Field label={t('VendorDetail.fields.approval')}>
              {v.approvedAtUtc
                ? formatDate(v.approvedAtUtc, locale)
                : t('VendorDetail.notApproved')}
            </Field>
            <Field label={t('VendorDetail.fields.rating')}>
              <div className="flex items-center gap-0.5">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => applyRating(star)}
                    disabled={setRating.isPending}
                    aria-label={t('VendorDetail.star', { count: star })}
                    className="disabled:opacity-50"
                  >
                    <Star
                      size={16}
                      className={
                        (v.rating ?? 0) >= star
                          ? 'fill-warning-400 text-warning-400'
                          : 'text-slate-300 dark:text-slate-600'
                      }
                    />
                  </button>
                ))}
              </div>
            </Field>
            {v.blockReason && (
              <Field label={t('VendorDetail.fields.blockReason')} full>
                <span className="text-danger-600 dark:text-danger-400">{v.blockReason}</span>
              </Field>
            )}
            {v.notes && (
              <Field label={t('VendorDetail.fields.notes')} full>
                {v.notes}
              </Field>
            )}
          </dl>
        )}
        {tab === 'addresses' && (
          <SimpleList
            items={addresses.data?.data ?? []}
            isLoading={addresses.isPending}
            loadingLabel={t('VendorDetail.loading')}
            empty={t('VendorDetail.addresses.empty')}
            addLabel={t('VendorDetail.addresses.add')}
            deleteLabel={t('VendorDetail.delete.title')}
            onAdd={() => setChildModal('address')}
            onDelete={(a) => removeChild('address', a.id, a.label)}
            render={(a) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {a.label}{' '}
                  {a.isPrimary && (
                    <span className="text-[10px] text-primary-600">
                      [{t('VendorDetail.primary')}]
                    </span>
                  )}
                </div>
                <div className="text-slate-600 dark:text-slate-300">{a.line1}</div>
                {a.line2 && <div className="text-slate-500">{a.line2}</div>}
                <div className="text-slate-500">
                  {[a.city, a.state, a.postalCode, a.country].filter(Boolean).join(', ')}
                </div>
              </div>
            )}
          />
        )}
        {tab === 'contacts' && (
          <SimpleList
            items={contacts.data?.data ?? []}
            isLoading={contacts.isPending}
            loadingLabel={t('VendorDetail.loading')}
            empty={t('VendorDetail.contacts.empty')}
            addLabel={t('VendorDetail.contacts.add')}
            deleteLabel={t('VendorDetail.delete.title')}
            onAdd={() => setChildModal('contact')}
            onDelete={(c) => removeChild('contact', c.id, c.name)}
            render={(c) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {c.name}{' '}
                  {c.isPrimary && (
                    <span className="text-[10px] text-primary-600">
                      [{t('VendorDetail.primary')}]
                    </span>
                  )}
                </div>
                {c.role && <div className="text-slate-500">{c.role}</div>}
                {c.email && <div className="text-slate-600 dark:text-slate-300">{c.email}</div>}
                {c.phone && <div className="text-slate-600 dark:text-slate-300">{c.phone}</div>}
              </div>
            )}
          />
        )}
        {tab === 'bank' && (
          <SimpleList
            items={banks.data?.data ?? []}
            isLoading={banks.isPending}
            loadingLabel={t('VendorDetail.loading')}
            empty={t('VendorDetail.bank.empty')}
            addLabel={t('VendorDetail.bank.add')}
            deleteLabel={t('VendorDetail.delete.title')}
            onAdd={() => setChildModal('bank')}
            onDelete={(b) => removeChild('bank', b.id, b.bankName)}
            render={(b) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {b.bankName}{' '}
                  {b.isPrimary && (
                    <span className="text-[10px] text-primary-600">
                      [{t('VendorDetail.primary')}]
                    </span>
                  )}
                </div>
                {b.branchName && <div className="text-slate-500">{b.branchName}</div>}
                <div className="font-mono text-slate-600 dark:text-slate-300">{b.iban}</div>
                <div className="text-slate-500">
                  {b.accountHolder} — {b.currency}
                </div>
              </div>
            )}
          />
        )}
        {tab === 'ledger' && (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                <tr>
                  <th className="px-2 py-1.5 text-left">{t('VendorDetail.ledger.date')}</th>
                  <th className="px-2 py-1.5 text-left">{t('VendorDetail.ledger.document')}</th>
                  <th className="px-2 py-1.5 text-left">{t('VendorDetail.ledger.description')}</th>
                  <th className="px-2 py-1.5 text-right">{t('VendorDetail.ledger.debit')}</th>
                  <th className="px-2 py-1.5 text-right">{t('VendorDetail.ledger.credit')}</th>
                  <th className="px-2 py-1.5 text-right">{t('VendorDetail.ledger.balance')}</th>
                </tr>
              </thead>
              <tbody>
                {ledger.isPending ? (
                  <tr>
                    <td colSpan={6} className="px-2 py-6 text-center text-slate-500">
                      {t('VendorDetail.loading')}
                    </td>
                  </tr>
                ) : (ledger.data?.data?.items ?? []).length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-2 py-6 text-center text-slate-500">
                      {t('VendorDetail.ledger.empty')}
                    </td>
                  </tr>
                ) : (
                  (ledger.data?.data?.items ?? []).map((e) => (
                    <tr key={e.id} className="border-t border-slate-100 dark:border-slate-800">
                      <td className="px-2 py-1.5">{formatDate(e.postingDate, locale)}</td>
                      <td className="px-2 py-1.5 font-mono">{e.sourceDocumentNumber ?? '—'}</td>
                      <td className="px-2 py-1.5 text-slate-600 dark:text-slate-300">
                        {e.description ?? <span className="text-slate-400">—</span>}
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono">
                        {e.entryType === 'Debit' ? e.amount.toFixed(2) : '—'}
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono">
                        {e.entryType === 'Credit' ? e.amount.toFixed(2) : '—'}
                      </td>
                      <td className="px-2 py-1.5 text-right font-mono font-semibold">
                        {e.runningBalanceAfter.toFixed(2)}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showEdit && <VendorFormModal vendor={v} onClose={() => setShowEdit(false)} />}
      {childModal === 'address' && (
        <VendorAddressModal vendorId={v.id} onClose={() => setChildModal(null)} />
      )}
      {childModal === 'contact' && (
        <VendorContactModal vendorId={v.id} onClose={() => setChildModal(null)} />
      )}
      {childModal === 'bank' && (
        <VendorBankAccountModal vendorId={v.id} onClose={() => setChildModal(null)} />
      )}
    </DetailPageTemplate>
  );
};

const Field = ({
  label,
  children,
  full,
}: {
  label: string;
  children: React.ReactNode;
  full?: boolean;
}) => (
  <div className={full ? 'sm:col-span-2' : undefined}>
    <dt className="text-[10px] font-semibold uppercase text-slate-500">{label}</dt>
    <dd className="text-sm text-slate-900 dark:text-slate-100">{children}</dd>
  </div>
);

const SimpleList = <T extends { id: string }>({
  items,
  isLoading,
  loadingLabel,
  empty,
  addLabel,
  deleteLabel,
  onAdd,
  onDelete,
  render,
}: {
  items: T[];
  isLoading: boolean;
  loadingLabel: string;
  empty: string;
  addLabel: string;
  deleteLabel: string;
  onAdd?: () => void;
  onDelete?: (item: T) => void;
  render: (item: T) => React.ReactNode;
}) => {
  return (
    <div className="space-y-3">
      {onAdd && (
        <div className="flex justify-end">
          <Button size="sm" onClick={onAdd}>
            <Plus size={14} />
            {addLabel}
          </Button>
        </div>
      )}
      {isLoading ? (
        <div className="py-6 text-center text-sm text-slate-500">{loadingLabel}</div>
      ) : items.length === 0 ? (
        <div className="py-6 text-center text-sm text-slate-500">{empty}</div>
      ) : (
        <div className="grid grid-cols-1 gap-3 text-xs sm:grid-cols-2">
          {items.map((item) => (
            <div
              key={item.id}
              className="group relative rounded border border-slate-200 p-3 dark:border-slate-800"
            >
              {render(item)}
              {onDelete && (
                <button
                  type="button"
                  onClick={() => onDelete(item)}
                  className="absolute right-2 top-2 rounded p-1 text-slate-400 opacity-0 transition hover:bg-danger-50 hover:text-danger-700 group-hover:opacity-100 dark:hover:bg-danger-500/10"
                  aria-label={deleteLabel}
                >
                  <Trash2 size={12} />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default VendorDetailPage;
