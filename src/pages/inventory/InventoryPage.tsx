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
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
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
    tone: 'border-success-200 bg-success-50 text-success-700 hover:bg-success-100 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300 dark:hover:bg-success-500/20',
  },
  {
    type: 'issue',
    label: 'Çıkış Fişi',
    icon: ArrowUpFromLine,
    tone: 'border-warning-200 bg-warning-50 text-warning-700 hover:bg-warning-100 dark:border-warning-500/30 dark:bg-warning-500/10 dark:text-warning-300 dark:hover:bg-warning-500/20',
  },
  {
    type: 'count',
    label: 'Sayım Fişi',
    icon: ClipboardList,
    tone: 'border-primary-200 bg-primary-50 text-primary-700 hover:bg-primary-100 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20',
  },
  {
    type: 'transfer',
    label: 'Transfer Fişi',
    icon: ArrowRightLeft,
    tone: 'border-info-200 bg-info-50 text-info-700 hover:bg-info-100 dark:border-info-500/30 dark:bg-info-500/10 dark:text-info-300 dark:hover:bg-info-500/20',
  },
];

export const InventoryPage = () => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('status');
  const [voucher, setVoucher] = useState<StockVoucherType | null>(null);

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Boxes size={20} />}
          title={t('inventory.page.title', { defaultValue: 'Stok Yönetimi' })}
          subtitle={t('inventory.page.subtitle', {
            defaultValue:
              'Depo bazında stok durumu, miktarları ve tüm stok/depo hareketleri tek ekranda.',
          })}
          actions={VOUCHER_ACTIONS.map((action) => {
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
        />
      }
      toolbar={
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
                    ? 'border-primary-600 text-primary-700 dark:border-primary-400 dark:text-primary-300'
                    : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
                }`}
              >
                <Icon size={13} />
                {t(`inventory.page.tab.${tItem.id}`, { defaultValue: tItem.label })}
              </button>
            );
          })}
        </div>
      }
    >
      {tab === 'status' && <StockStatusLedger />}
      {tab === 'movements' && <StockMovementsLedger />}
      {tab === 'warehouses' && <WarehousesTab />}

      {voucher && <StockVoucherModal type={voucher} onClose={() => setVoucher(null)} />}
    </ListPageTemplate>
  );
};

export default InventoryPage;
