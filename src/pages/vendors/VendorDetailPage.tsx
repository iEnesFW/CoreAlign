import { useMemo, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  ArrowLeft,
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

const STATUS_STYLES: Record<VendorStatus, string> = {
  Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Blocked: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Archived: 'bg-slate-200 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  PendingApproval: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
};

export const VendorDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { i18n } = useTranslation();
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
    return <div className="p-6 text-sm text-slate-500">Yükleniyor…</div>;
  }
  if (!v) {
    return <div className="p-6 text-sm text-slate-500">Tedarikçi bulunamadı.</div>;
  }

  const tabs: { id: Tab; label: string; icon: typeof Building2 }[] = [
    { id: 'overview', label: 'Genel', icon: Building2 },
    { id: 'addresses', label: 'Adresler', icon: MapPin },
    { id: 'contacts', label: 'Kontaklar', icon: Phone },
    { id: 'bank', label: 'Banka Bilgileri', icon: CreditCard },
    { id: 'ledger', label: 'Cari Hesap', icon: BookOpen },
  ];

  const applyRating = async (rating: number) => {
    try {
      await setRating.mutateAsync({ id: v.id, rating });
      toast.success('Performans puanı güncellendi.');
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
      title: 'Sil',
      message: `${label} silinsin mi?`,
      confirmLabel: 'Sil',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      if (kind === 'address') await deleteAddress.mutateAsync(childId);
      else if (kind === 'contact') await deleteContact.mutateAsync(childId);
      else await deleteBank.mutateAsync(childId);
      toast.success('Silindi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div>
        <Link
          to="/dashboard/vendors"
          className="inline-flex items-center gap-1 text-xs text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
        >
          <ArrowLeft size={11} />
          Tedarikçilere dön
        </Link>
      </div>

      <div className="flex items-start justify-between rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">{v.name}</h1>
            <span
              className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[v.status]}`}
            >
              {v.status}
            </span>
          </div>
          {v.legalName && (
            <div className="text-sm text-slate-500 dark:text-slate-400">{v.legalName}</div>
          )}
          <div className="mt-2 flex flex-wrap gap-3 text-xs text-slate-600 dark:text-slate-300">
            {v.code && (
              <div>
                <span className="text-slate-500">Kod:</span>{' '}
                <span className="font-mono">{v.code}</span>
              </div>
            )}
            {v.taxNumber && (
              <div>
                <span className="text-slate-500">VKN:</span>{' '}
                <span className="font-mono">{v.taxNumber}</span>
              </div>
            )}
            {v.taxOffice && (
              <div>
                <span className="text-slate-500">VD:</span> {v.taxOffice}
              </div>
            )}
            {v.email && (
              <div>
                <span className="text-slate-500">E-posta:</span> {v.email}
              </div>
            )}
            {v.phone && (
              <div>
                <span className="text-slate-500">Tel:</span> {v.phone}
              </div>
            )}
          </div>
        </div>
        <div className="flex flex-col items-end gap-2">
          <button
            type="button"
            onClick={() => setShowEdit(true)}
            className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
          >
            <Pencil size={12} />
            Düzenle
          </button>
          <div className="text-right">
            <div className="text-xs text-slate-500">Cari Bakiye</div>
            <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
              {formatCurrency(v.currentBalance, locale, v.defaultCurrency)}
            </div>
            {v.overdueAmount > 0 && (
              <div className="text-xs text-rose-600 dark:text-rose-400">
                Vadesi geçen: {formatCurrency(v.overdueAmount, locale, v.defaultCurrency)}
              </div>
            )}
          </div>
        </div>
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
                  ? 'border-indigo-600 text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
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
            <Field label="Tip">{v.type === 'Business' ? 'Şirket' : 'Şahıs'}</Field>
            <Field label="Para Birimi">{v.defaultCurrency}</Field>
            <Field label="Ödeme Vadesi">{v.paymentTermsName ?? '—'}</Field>
            <Field label="Bölge">{v.territory ?? '—'}</Field>
            <Field label="Sınıflandırma">{v.classification ?? '—'}</Field>
            <Field label="Web">{v.website ?? '—'}</Field>
            <Field label="Onay">
              {v.approvedAtUtc ? formatDate(v.approvedAtUtc, locale) : 'Henüz onaylanmadı'}
            </Field>
            <Field label="Performans">
              <div className="flex items-center gap-0.5">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => applyRating(star)}
                    disabled={setRating.isPending}
                    aria-label={`${star} yıldız`}
                    className="disabled:opacity-50"
                  >
                    <Star
                      size={16}
                      className={
                        (v.rating ?? 0) >= star
                          ? 'fill-amber-400 text-amber-400'
                          : 'text-slate-300 dark:text-slate-600'
                      }
                    />
                  </button>
                ))}
              </div>
            </Field>
            {v.blockReason && (
              <Field label="Bloke Nedeni" full>
                <span className="text-rose-600 dark:text-rose-400">{v.blockReason}</span>
              </Field>
            )}
            {v.notes && (
              <Field label="Notlar" full>
                {v.notes}
              </Field>
            )}
          </dl>
        )}
        {tab === 'addresses' && (
          <SimpleList
            items={addresses.data?.data ?? []}
            isLoading={addresses.isPending}
            empty="Henüz adres tanımlanmadı."
            addLabel="Adres ekle"
            onAdd={() => setChildModal('address')}
            onDelete={(a) => removeChild('address', a.id, a.label)}
            render={(a) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {a.label}{' '}
                  {a.isPrimary && <span className="text-[10px] text-indigo-600">[Birincil]</span>}
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
            empty="Henüz kontak tanımlanmadı."
            addLabel="Kontak ekle"
            onAdd={() => setChildModal('contact')}
            onDelete={(c) => removeChild('contact', c.id, c.name)}
            render={(c) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {c.name}{' '}
                  {c.isPrimary && <span className="text-[10px] text-indigo-600">[Birincil]</span>}
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
            empty="Henüz banka hesabı tanımlanmadı."
            addLabel="Banka hesabı ekle"
            onAdd={() => setChildModal('bank')}
            onDelete={(b) => removeChild('bank', b.id, b.bankName)}
            render={(b) => (
              <div className="space-y-0.5">
                <div className="font-semibold text-slate-900 dark:text-slate-100">
                  {b.bankName}{' '}
                  {b.isPrimary && <span className="text-[10px] text-indigo-600">[Birincil]</span>}
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
                  <th className="px-2 py-1.5 text-left">Tarih</th>
                  <th className="px-2 py-1.5 text-left">Belge</th>
                  <th className="px-2 py-1.5 text-left">Açıklama</th>
                  <th className="px-2 py-1.5 text-right">Borç</th>
                  <th className="px-2 py-1.5 text-right">Alacak</th>
                  <th className="px-2 py-1.5 text-right">Bakiye</th>
                </tr>
              </thead>
              <tbody>
                {ledger.isPending ? (
                  <tr>
                    <td colSpan={6} className="px-2 py-6 text-center text-slate-500">
                      Yükleniyor…
                    </td>
                  </tr>
                ) : (ledger.data?.data?.items ?? []).length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-2 py-6 text-center text-slate-500">
                      Henüz cari hareket yok.
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
    </div>
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
  empty,
  addLabel,
  onAdd,
  onDelete,
  render,
}: {
  items: T[];
  isLoading: boolean;
  empty: string;
  addLabel?: string;
  onAdd?: () => void;
  onDelete?: (item: T) => void;
  render: (item: T) => React.ReactNode;
}) => {
  return (
    <div className="space-y-3">
      {onAdd && (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={onAdd}
            className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
          >
            <Plus size={12} />
            {addLabel ?? 'Ekle'}
          </button>
        </div>
      )}
      {isLoading ? (
        <div className="py-6 text-center text-sm text-slate-500">Yükleniyor…</div>
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
                  className="absolute right-2 top-2 rounded p-1 text-slate-400 opacity-0 transition hover:bg-rose-50 hover:text-rose-700 group-hover:opacity-100 dark:hover:bg-rose-500/10"
                  aria-label="Sil"
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
