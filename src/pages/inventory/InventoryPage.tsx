import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ArrowDownToLine,
  ArrowLeftRight,
  ArrowRightLeft,
  ArrowUpFromLine,
  Boxes,
  ClipboardList,
  Warehouse,
} from 'lucide-react';
import { StockStatusLedger } from '@/features/inventory/ui/StockStatusLedger';
import { StockMovementsLedger } from '@/features/inventory/ui/StockMovementsLedger';
import { WarehousesTab } from '@/features/inventory/ui/WarehousesTab';
import {
  StockVoucherModal,
  type StockVoucherType,
} from '@/features/inventory/ui/StockVoucherModal';

type Tab = 'status' | 'movements' | 'warehouses';

const TABS: { id: Tab; label: string; icon: typeof Boxes }[] = [
  { id: 'status', label: 'Stok Durumu', icon: Boxes },
  { id: 'movements', label: 'Stok Hareketleri', icon: ArrowLeftRight },
  { id: 'warehouses', label: 'Depolar', icon: Warehouse },
];

const VOUCHER_ACTIONS: {
  type: StockVoucherType;
  label: string;
  icon: typeof Boxes;
  tone: string;
}[] = [
  {
    type: 'receive',
    label: 'Giriş Fişi',
    icon: ArrowDownToLine,
    tone: 'border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300 dark:hover:bg-emerald-500/20',
  },
  {
    type: 'issue',
    label: 'Çıkış Fişi',
    icon: ArrowUpFromLine,
    tone: 'border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300 dark:hover:bg-amber-500/20',
  },
  {
    type: 'count',
    label: 'Sayım Fişi',
    icon: ClipboardList,
    tone: 'border-indigo-200 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-300 dark:hover:bg-indigo-500/20',
  },
  {
    type: 'transfer',
    label: 'Transfer Fişi',
    icon: ArrowRightLeft,
    tone: 'border-sky-200 bg-sky-50 text-sky-700 hover:bg-sky-100 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-300 dark:hover:bg-sky-500/20',
  },
];

export const InventoryPage = () => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('status');
  const [voucher, setVoucher] = useState<StockVoucherType | null>(null);

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('inventory.page.title', { defaultValue: 'Stok Yönetimi' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('inventory.page.subtitle', {
              defaultValue:
                'Depo bazında stok durumu, miktarları ve tüm stok/depo hareketleri tek ekranda.',
            })}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {VOUCHER_ACTIONS.map((action) => {
            const Icon = action.icon;
            return (
              <button
                key={action.type}
                type="button"
                onClick={() => setVoucher(action.type)}
                className={`inline-flex items-center gap-1.5 rounded border px-2.5 py-1.5 text-xs font-semibold ${action.tone}`}
              >
                <Icon size={13} />
                {t(`inventory.voucher.action.${action.type}`, { defaultValue: action.label })}
              </button>
            );
          })}
        </div>
      </div>

      <div className="flex flex-wrap gap-1 border-b border-slate-200 dark:border-slate-800">
        {TABS.map((tItem) => {
          const Icon = tItem.icon;
          const active = tab === tItem.id;
          return (
            <button
              key={tItem.id}
              type="button"
              onClick={() => setTab(tItem.id)}
              className={`inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition ${
                active
                  ? 'border-indigo-600 text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              <Icon size={13} />
              {t(`inventory.page.tab.${tItem.id}`, { defaultValue: tItem.label })}
            </button>
          );
        })}
      </div>

      {tab === 'status' && <StockStatusLedger />}
      {tab === 'movements' && <StockMovementsLedger />}
      {tab === 'warehouses' && <WarehousesTab />}

      {voucher && <StockVoucherModal type={voucher} onClose={() => setVoucher(null)} />}
    </div>
  );
};

export default InventoryPage;
