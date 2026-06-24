import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Boxes, Edit3 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { useStockMovementsQuery, useLotsByProductQuery } from '../hooks/useInventoryQueries';
import type { StockItem } from '../model/inventory.types';
import { HeaderChips, KpiRow } from './stockItemDetail/StockKpis';
import { VelocityCard } from './stockItemDetail/VelocityCard';
import { LotInfoCard, ReorderCard } from './stockItemDetail/ReorderLotCards';
import { MovementChart, MovementsList } from './stockItemDetail/MovementViews';

interface Props {
  open: boolean;
  stockItem: StockItem;
  currency: string;
  onClose: () => void;
  onAdjust?: (stockItem: StockItem) => void;
}

export const StockItemDetailModal = ({ open, stockItem, currency, onClose, onAdjust }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;

  const movementsQuery = useStockMovementsQuery({
    productId: stockItem.productId,
    warehouseId: stockItem.warehouseId,
    page: 1,
    pageSize: 50,
  });
  const lotsQuery = useLotsByProductQuery(stockItem.lotId ? stockItem.productId : null);

  const movements = useMemo(() => {
    const all = movementsQuery.data?.data?.items ?? [];
    return stockItem.lotId ? all.filter((m) => m.lotId === stockItem.lotId) : all;
  }, [movementsQuery.data, stockItem.lotId]);

  const lot = useMemo(() => {
    if (!stockItem.lotId) return null;
    return (lotsQuery.data?.data ?? []).find((l) => l.id === stockItem.lotId) ?? null;
  }, [lotsQuery.data, stockItem.lotId]);

  const belowReorder =
    stockItem.reorderPoint !== null && stockItem.availableToPromise <= stockItem.reorderPoint;
  const reorderQty =
    stockItem.reorderPoint !== null
      ? Math.max(
          0,
          stockItem.reorderPoint - stockItem.availableToPromise + (stockItem.minStock ?? 0),
        )
      : 0;

  const value = stockItem.onHand * stockItem.avgCost;

  return (
    <Modal
      open={open}
      title={t('inventory.stockItem.title')}
      subtitle={`${stockItem.productName} · ${stockItem.productSku}`}
      icon={<Boxes size={18} />}
      onClose={onClose}
      size="xl"
      bodyClassName="space-y-3 p-3"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          {onAdjust && (
            <Button variant="outline" type="button" onClick={() => onAdjust(stockItem)}>
              <Edit3 size={14} />
              {t('inventory.byWarehouse.adjust')}
            </Button>
          )}
        </>
      }
    >
      <HeaderChips stockItem={stockItem} />

      <KpiRow stockItem={stockItem} value={value} currency={currency} locale={locale} />

      <ReorderCard
        stockItem={stockItem}
        belowReorder={belowReorder}
        reorderQty={reorderQty}
        locale={locale}
      />

      {lot && <LotInfoCard lot={lot} locale={locale} />}

      <VelocityCard stockItem={stockItem} movements={movements} locale={locale} />

      <MovementChart movements={movements} locale={locale} />

      <MovementsList
        movements={movements}
        loading={movementsQuery.isPending}
        locale={locale}
        currency={currency}
      />
    </Modal>
  );
};
