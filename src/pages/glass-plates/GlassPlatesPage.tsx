import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import {
  Boxes,
  LayoutGrid,
  PackageSearch,
  Plus,
  Scissors,
  ShieldCheck,
  SlidersHorizontal,
  Trash2,
  TriangleAlert,
} from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Card } from '@/shared/ui/Card/Card';
import { Input } from '@/shared/ui/Input/Input';
import { Badge } from '@/shared/ui/Badge/Badge';
import { Button } from '@/shared/ui/Button/Button';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import type { Product } from '@/shared/model/product.types';
import {
  useGlassPlatesQuery,
  useLowStockPlatesQuery,
  useStorageLocationsQuery,
  useUsablePlatesQuery,
} from '@/features/glass-plates/hooks/useGlassPlateQueries';
import { ReceiveGlassPlatesModal } from '@/features/glass-plates/ui/ReceiveGlassPlatesModal';
import { CreateStorageLocationModal } from '@/features/glass-plates/ui/CreateStorageLocationModal';
import { ConsumeGlassPlateModal } from '@/features/glass-plates/ui/ConsumeGlassPlateModal';
import { ScrapGlassPlateModal } from '@/features/glass-plates/ui/ScrapGlassPlateModal';
import { SetPlateTrackingModal } from '@/features/glass-plates/ui/SetPlateTrackingModal';
import { WarehouseAccessModal } from '@/features/glass-plates/ui/WarehouseAccessModal';
import type { GlassPlate, GlassPlateStatus } from '@/features/glass-plates/model/glassPlate.types';

type Tab = 'plates' | 'lowStock' | 'locations' | 'usable' | 'definitions';

const statusTone: Record<GlassPlateStatus, 'success' | 'warning' | 'info' | 'danger' | 'neutral'> =
  {
    Available: 'success',
    Reserved: 'warning',
    InUse: 'info',
    Consumed: 'neutral',
    Scrapped: 'danger',
  };

const m2 = (mm2: number) => (mm2 / 1_000_000).toFixed(3);

const TAB_IDS: Tab[] = ['plates', 'usable', 'locations', 'lowStock', 'definitions'];

export const GlassPlatesPage = () => {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const requestedTab = searchParams.get('tab');
  const initialTab = TAB_IDS.includes(requestedTab as Tab) ? (requestedTab as Tab) : 'plates';
  const [tab, setTab] = useState<Tab>(initialTab);
  const [modal, setModal] = useState<'receive' | 'location' | 'access' | null>(null);

  const tabs: { id: Tab; label: string; icon: typeof Boxes }[] = [
    { id: 'plates', label: t('GlassPlates.tabs.plates'), icon: Boxes },
    { id: 'usable', label: t('GlassPlates.tabs.usable'), icon: PackageSearch },
    { id: 'locations', label: t('GlassPlates.tabs.locations'), icon: LayoutGrid },
    { id: 'lowStock', label: t('GlassPlates.tabs.lowStock'), icon: TriangleAlert },
    { id: 'definitions', label: t('GlassPlates.tabs.definitions'), icon: SlidersHorizontal },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('GlassPlates.title')}
        subtitle={t('GlassPlates.subtitle')}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button variant="ghost" onClick={() => setModal('access')}>
              <ShieldCheck className="h-4 w-4" />
              {t('GlassPlates.actions.warehouseAccess')}
            </Button>
            <Button variant="outline" onClick={() => setModal('location')}>
              <LayoutGrid className="h-4 w-4" />
              {t('GlassPlates.actions.newLocation')}
            </Button>
            <Button onClick={() => setModal('receive')}>
              <Plus className="h-4 w-4" />
              {t('GlassPlates.actions.receive')}
            </Button>
          </div>
        }
      />

      <div className="flex flex-wrap gap-2">
        {tabs.map((item) => {
          const Icon = item.icon;
          const active = tab === item.id;
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              className={`inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition ${
                active
                  ? 'bg-primary-600 text-white'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
              }`}
            >
              <Icon className="h-4 w-4" />
              {item.label}
            </button>
          );
        })}
      </div>

      {tab === 'plates' && <PlatesTab />}
      {tab === 'usable' && <UsableTab />}
      {tab === 'locations' && <LocationsTab />}
      {tab === 'lowStock' && <LowStockTab />}
      {tab === 'definitions' && <DefinitionsTab />}

      {modal === 'receive' && <ReceiveGlassPlatesModal onClose={() => setModal(null)} />}
      {modal === 'location' && <CreateStorageLocationModal onClose={() => setModal(null)} />}
      {modal === 'access' && <WarehouseAccessModal onClose={() => setModal(null)} />}
    </div>
  );
};

const PlatesTab = () => {
  const { t } = useTranslation();
  const query = useGlassPlatesQuery({ take: 200 });
  const plates = query.data ?? [];
  const [consumePlate, setConsumePlate] = useState<GlassPlate | null>(null);
  const [scrapPlate, setScrapPlate] = useState<GlassPlate | null>(null);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  const isActionable = (p: GlassPlate) => p.status !== 'Consumed' && p.status !== 'Scrapped';

  return (
    <Card className="overflow-x-auto p-0">
      {plates.length === 0 ? (
        <EmptyState title={t('GlassPlates.plates.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('GlassPlates.plates.number')}</th>
              <th className="px-4 py-3">{t('GlassPlates.plates.kind')}</th>
              <th className="px-4 py-3">{t('GlassPlates.plates.status')}</th>
              <th className="px-4 py-3">{t('GlassPlates.plates.size')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.plates.remaining')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.plates.utilization')}</th>
              <th className="px-4 py-3">{t('GlassPlates.plates.location')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.plates.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {plates.map((p) => (
              <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="px-4 py-3 font-medium">{p.plateNumber}</td>
                <td className="px-4 py-3">
                  <Badge variant={p.kind === 'Remnant' ? 'warning' : 'info'}>
                    {t(`GlassPlates.kind.${p.kind}`)}
                  </Badge>
                </td>
                <td className="px-4 py-3">
                  <Badge variant={statusTone[p.status]}>
                    {t(`GlassPlates.status.${p.status}`)}
                  </Badge>
                </td>
                <td className="px-4 py-3 tabular-nums">
                  {p.widthMm}×{p.heightMm}×{p.thicknessMm}
                </td>
                <td className="px-4 py-3 text-right tabular-nums">{m2(p.remainingAreaMm2)} m²</td>
                <td className="px-4 py-3 text-right tabular-nums">%{p.utilizationPercent}</td>
                <td className="px-4 py-3 text-slate-500">
                  {p.warehouseName}
                  {p.storageLocationCode ? ` · ${p.storageLocationCode}` : ''}
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1">
                    <button
                      type="button"
                      onClick={() => setConsumePlate(p)}
                      disabled={!isActionable(p)}
                      title={t('GlassPlates.actions.consume')}
                      aria-label={t('GlassPlates.actions.consume')}
                      className="rounded p-1.5 text-slate-500 hover:bg-primary-50 hover:text-primary-700 disabled:opacity-30 dark:hover:bg-primary-500/10"
                    >
                      <Scissors size={15} />
                    </button>
                    <button
                      type="button"
                      onClick={() => setScrapPlate(p)}
                      disabled={!isActionable(p)}
                      title={t('GlassPlates.actions.scrap')}
                      aria-label={t('GlassPlates.actions.scrap')}
                      className="rounded p-1.5 text-slate-500 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {consumePlate && (
        <ConsumeGlassPlateModal plate={consumePlate} onClose={() => setConsumePlate(null)} />
      )}
      {scrapPlate && (
        <ScrapGlassPlateModal plate={scrapPlate} onClose={() => setScrapPlate(null)} />
      )}
    </Card>
  );
};

const UsableTab = () => {
  const { t } = useTranslation();
  const [productId, setProductId] = useState('');
  const [widthMm, setWidthMm] = useState('');
  const [heightMm, setHeightMm] = useState('');

  const params = useMemo(() => {
    const w = Number(widthMm);
    const h = Number(heightMm);
    if (!productId || !(w > 0) || !(h > 0)) return null;
    return { productId, widthMm: w, heightMm: h, take: 20 };
  }, [productId, widthMm, heightMm]);

  const query = useUsablePlatesQuery(params);
  const plates = query.data ?? [];

  return (
    <div className="space-y-4">
      <Card className="grid gap-4 sm:grid-cols-3">
        <Input
          label={t('GlassPlates.usable.productId')}
          value={productId}
          onChange={(e) => setProductId(e.target.value)}
          placeholder={t('GlassPlates.usable.productIdPlaceholder')}
        />
        <Input
          label={t('GlassPlates.usable.width')}
          type="number"
          value={widthMm}
          onChange={(e) => setWidthMm(e.target.value)}
        />
        <Input
          label={t('GlassPlates.usable.height')}
          type="number"
          value={heightMm}
          onChange={(e) => setHeightMm(e.target.value)}
        />
      </Card>

      <Card className="overflow-x-auto p-0">
        {!params ? (
          <EmptyState title={t('GlassPlates.usable.prompt')} />
        ) : plates.length === 0 ? (
          <EmptyState title={t('GlassPlates.usable.none')} />
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
              <tr>
                <th className="px-4 py-3">{t('GlassPlates.plates.number')}</th>
                <th className="px-4 py-3">{t('GlassPlates.plates.size')}</th>
                <th className="px-4 py-3 text-right">{t('GlassPlates.plates.remaining')}</th>
                <th className="px-4 py-3">{t('GlassPlates.plates.location')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {plates.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td className="px-4 py-3 font-medium">{p.plateNumber}</td>
                  <td className="px-4 py-3 tabular-nums">
                    {p.widthMm}×{p.heightMm}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums">{m2(p.remainingAreaMm2)} m²</td>
                  <td className="px-4 py-3 text-slate-500">
                    {p.warehouseName}
                    {p.storageLocationCode ? ` · ${p.storageLocationCode}` : ''}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
};

const LocationsTab = () => {
  const { t } = useTranslation();
  const query = useStorageLocationsQuery();
  const locations = query.data ?? [];

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  return (
    <Card className="overflow-x-auto p-0">
      {locations.length === 0 ? (
        <EmptyState title={t('GlassPlates.locations.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('GlassPlates.locations.code')}</th>
              <th className="px-4 py-3">{t('GlassPlates.locations.name')}</th>
              <th className="px-4 py-3">{t('GlassPlates.locations.kind')}</th>
              <th className="px-4 py-3">{t('GlassPlates.locations.active')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {locations.map((l) => (
              <tr key={l.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="px-4 py-3 font-medium">{l.code}</td>
                <td className="px-4 py-3">{l.name}</td>
                <td className="px-4 py-3">{t(`GlassPlates.locationKind.${l.kind}`)}</td>
                <td className="px-4 py-3">
                  <Badge variant={l.isActive ? 'success' : 'neutral'}>
                    {l.isActive ? t('Common.yes') : t('Common.no')}
                  </Badge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Card>
  );
};

const LowStockTab = () => {
  const { t } = useTranslation();
  const query = useLowStockPlatesQuery();
  const rows = query.data ?? [];
  const [replenish, setReplenish] = useState<{ productId: string; warehouseId: string } | null>(
    null,
  );

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  return (
    <Card className="overflow-x-auto p-0">
      {rows.length === 0 ? (
        <EmptyState title={t('GlassPlates.lowStock.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('GlassPlates.lowStock.sku')}</th>
              <th className="px-4 py-3">{t('GlassPlates.lowStock.product')}</th>
              <th className="px-4 py-3">{t('GlassPlates.plates.location')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.lowStock.available')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.lowStock.threshold')}</th>
              <th className="px-4 py-3 text-right">{t('GlassPlates.plates.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rows.map((r) => (
              <tr
                key={`${r.productId}-${r.warehouseId}`}
                className="hover:bg-slate-50 dark:hover:bg-slate-800/50"
              >
                <td className="px-4 py-3 font-medium">{r.sku}</td>
                <td className="px-4 py-3">{r.productName}</td>
                <td className="px-4 py-3 text-slate-500">{r.warehouseName}</td>
                <td className="px-4 py-3 text-right">
                  <Badge variant={r.availableCount === 0 ? 'danger' : 'warning'}>
                    {r.availableCount}
                  </Badge>
                </td>
                <td className="px-4 py-3 text-right tabular-nums">{r.minPlateCount}</td>
                <td className="px-4 py-3 text-right">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() =>
                      setReplenish({ productId: r.productId, warehouseId: r.warehouseId })
                    }
                  >
                    {t('GlassPlates.lowStock.replenish')}
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {replenish && (
        <ReceiveGlassPlatesModal
          initialProductId={replenish.productId}
          initialWarehouseId={replenish.warehouseId}
          onClose={() => setReplenish(null)}
        />
      )}
    </Card>
  );
};

const DefinitionsTab = () => {
  const { t } = useTranslation();
  const query = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const [editing, setEditing] = useState<Product | null>(null);
  const [creating, setCreating] = useState(false);

  const tracked = (query.data?.data?.items ?? []).filter((p) => p.isPlateTracked);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  return (
    <div className="space-y-3">
      <div className="flex justify-end">
        <Button onClick={() => setCreating(true)}>
          <Plus className="h-4 w-4" />
          {t('GlassPlates.trackingForm.newDefinition')}
        </Button>
      </div>

      <Card className="overflow-x-auto p-0">
        {tracked.length === 0 ? (
          <EmptyState title={t('GlassPlates.definitions.empty')} />
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
              <tr>
                <th className="px-4 py-3">{t('GlassPlates.lowStock.sku')}</th>
                <th className="px-4 py-3">{t('GlassPlates.lowStock.product')}</th>
                <th className="px-4 py-3">{t('GlassPlates.definitions.standardSize')}</th>
                <th className="px-4 py-3 text-right">
                  {t('GlassPlates.definitions.minPlateCount')}
                </th>
                <th className="px-4 py-3 text-right">
                  {t('GlassPlates.definitions.minRemnantArea')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {tracked.map((p) => (
                <tr
                  key={p.id}
                  onClick={() => setEditing(p)}
                  className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800/50"
                >
                  <td className="px-4 py-3 font-medium">{p.sku}</td>
                  <td className="px-4 py-3">{p.name}</td>
                  <td className="px-4 py-3 tabular-nums text-slate-500">
                    {p.standardWidthMm && p.standardHeightMm
                      ? `${p.standardWidthMm}×${p.standardHeightMm} mm`
                      : '—'}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums">{p.minPlateCount ?? '—'}</td>
                  <td className="px-4 py-3 text-right tabular-nums">
                    {p.minRemnantAreaMm2 ? `${m2(p.minRemnantAreaMm2)} m²` : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      {creating && <SetPlateTrackingModal onClose={() => setCreating(false)} />}
      {editing && <SetPlateTrackingModal product={editing} onClose={() => setEditing(null)} />}
    </div>
  );
};
