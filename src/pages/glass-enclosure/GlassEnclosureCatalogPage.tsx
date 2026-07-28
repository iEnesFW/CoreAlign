import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import type {
  GlassEnclosureSettingsDto,
  UpdateSettingsCoreInput,
} from '@/features/glass-enclosure/model/glassEnclosure.types';
import {
  useUpdateSettingsCoreMutation,
  useColorOptionsQuery,
  useGlassTypesQuery,
  useProfileSystemsQuery,
  useHardwareItemsQuery,
  useHardwareKitsQuery,
  useDiscountRulesQuery,
  useNotificationTemplatesQuery,
  useWindZonesQuery,
  useClimateZonesQuery,
  useSettingsQuery,
} from '@/features/glass-enclosure/hooks/useGlassEnclosureQueries';

type Tab =
  | 'profile-systems'
  | 'glass-types'
  | 'colors'
  | 'hardware-items'
  | 'hardware-kits'
  | 'discount-rules'
  | 'notification-templates'
  | 'wind-zones'
  | 'climate-zones'
  | 'settings';

const TABS: { id: Tab; key: string }[] = [
  { id: 'profile-systems', key: 'GlassEnclosure.Tab.ProfileSystems' },
  { id: 'glass-types', key: 'GlassEnclosure.Tab.GlassTypes' },
  { id: 'colors', key: 'GlassEnclosure.Tab.Colors' },
  { id: 'hardware-items', key: 'GlassEnclosure.Tab.HardwareItems' },
  { id: 'hardware-kits', key: 'GlassEnclosure.Tab.HardwareKits' },
  { id: 'discount-rules', key: 'GlassEnclosure.Tab.DiscountRules' },
  { id: 'notification-templates', key: 'GlassEnclosure.Tab.NotificationTemplates' },
  { id: 'wind-zones', key: 'GlassEnclosure.Tab.WindZones' },
  { id: 'climate-zones', key: 'GlassEnclosure.Tab.ClimateZones' },
  { id: 'settings', key: 'GlassEnclosure.Tab.Settings' },
];

export function GlassEnclosureCatalogPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<Tab>('profile-systems');

  return (
    <div className="flex h-full flex-col gap-4 p-4 sm:p-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
          {t('GlassEnclosure.Title')}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('GlassEnclosure.Subtitle')}</p>
      </header>

      <nav className="flex flex-wrap gap-2 border-b border-slate-200 dark:border-slate-700">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveTab(tab.id)}
            className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium transition ${
              activeTab === tab.id
                ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
            }`}
          >
            {t(tab.key as never)}
          </button>
        ))}
      </nav>

      <main className="flex-1 overflow-auto">
        {activeTab === 'profile-systems' && <ProfileSystemsPanel />}
        {activeTab === 'glass-types' && <GlassTypesPanel />}
        {activeTab === 'colors' && <ColorsPanel />}
        {activeTab === 'hardware-items' && <HardwareItemsPanel />}
        {activeTab === 'hardware-kits' && <HardwareKitsPanel />}
        {activeTab === 'discount-rules' && <DiscountRulesPanel />}
        {activeTab === 'notification-templates' && <NotificationTemplatesPanel />}
        {activeTab === 'wind-zones' && <WindZonesPanel />}
        {activeTab === 'climate-zones' && <ClimateZonesPanel />}
        {activeTab === 'settings' && <SettingsPanel />}
      </main>
    </div>
  );
}

export default GlassEnclosureCatalogPage;

const ProfileSystemsPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useProfileSystemsQuery();
  if (isLoading) return <Skeleton />;
  const systems = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Name'),
        t('GlassEnclosure.Field.Brand'),
        t('GlassEnclosure.Field.SystemType'),
        t('GlassEnclosure.Field.MaxPanel'),
        t('GlassEnclosure.Field.ItemCount'),
      ]}
      rows={systems.map((s) => [
        s.code,
        s.name,
        s.brandName ?? '—',
        t(`GlassEnclosure.System.${s.systemType}` as never),
        `${s.maxPanelWidthMm} × ${s.maxPanelHeightMm} mm`,
        s.items.length.toString(),
      ])}
    />
  );
};

const GlassTypesPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useGlassTypesQuery();
  if (isLoading) return <Skeleton />;
  const types = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Name'),
        t('GlassEnclosure.Field.Thickness'),
        t('GlassEnclosure.Field.Structure'),
        t('GlassEnclosure.Field.UValue'),
        t('GlassEnclosure.Field.SoundDb'),
        t('GlassEnclosure.Field.PricePerM2'),
      ]}
      rows={types.map((g) => [
        g.code,
        g.name,
        `${g.thicknessMm} mm`,
        t(`GlassEnclosure.GlassStructure.${g.structure}` as never),
        `${g.uValue.toFixed(2)} W/m²K`,
        `${g.soundDb.toFixed(0)} dB`,
        `${g.pricePerM2.toFixed(2)} ${g.currency}`,
      ])}
    />
  );
};

const ColorsPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useColorOptionsQuery();
  if (isLoading) return <Skeleton />;
  const colors = data?.data ?? [];
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
      {colors.map((c) => (
        <div
          key={c.id}
          className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800"
        >
          <div
            className="h-10 w-10 rounded border border-slate-200 dark:border-slate-600"
            style={{ backgroundColor: c.hexColor }}
          />
          <div className="flex-1 min-w-0">
            <div className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">
              {c.name}
            </div>
            <div className="truncate text-xs text-slate-500 dark:text-slate-400">
              {c.ralCode ?? c.code} · {t(`GlassEnclosure.Finish.${c.finishType}` as never)}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};

const HardwareItemsPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useHardwareItemsQuery();
  if (isLoading) return <Skeleton />;
  const items = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Name'),
        t('GlassEnclosure.Field.Category'),
        t('GlassEnclosure.Field.Brand'),
        t('GlassEnclosure.Field.Unit'),
        t('GlassEnclosure.Field.UnitPrice'),
        t('GlassEnclosure.Field.MaxLoad'),
      ]}
      rows={items.map((h) => [
        h.code,
        h.name,
        t(`GlassEnclosure.HardwareCategory.${h.category}` as never),
        h.brandName ?? '—',
        h.unit,
        `${h.unitPrice.toFixed(2)} ${h.currency}`,
        h.maxLoadKg ? `${h.maxLoadKg} kg` : '—',
      ])}
    />
  );
};

const HardwareKitsPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useHardwareKitsQuery();
  if (isLoading) return <Skeleton />;
  const kits = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Name'),
        t('GlassEnclosure.Field.System'),
        t('GlassEnclosure.Field.ItemCount'),
        t('GlassEnclosure.Field.Active'),
      ]}
      rows={kits.map((k) => [
        k.code,
        k.name,
        k.systemName ?? '—',
        k.items.length.toString(),
        k.isActive ? t('Common.Yes') : t('Common.No'),
      ])}
    />
  );
};

const DiscountRulesPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useDiscountRulesQuery();
  if (isLoading) return <Skeleton />;
  const rules = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Name'),
        t('GlassEnclosure.Field.Scope'),
        t('GlassEnclosure.Field.DiscountKind'),
        t('GlassEnclosure.Field.DiscountValue'),
        t('GlassEnclosure.Field.Coupon'),
      ]}
      rows={rules.map((r) => [
        r.code,
        r.name,
        t(`GlassEnclosure.DiscountScope.${r.scope}` as never),
        t(`GlassEnclosure.DiscountKind.${r.discountKind}` as never),
        r.discountKind === 'Percent' ? `${r.discountValue}%` : r.discountValue.toFixed(2),
        r.couponCode ?? '—',
      ])}
    />
  );
};

const NotificationTemplatesPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useNotificationTemplatesQuery();
  if (isLoading) return <Skeleton />;
  const templates = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Event'),
        t('GlassEnclosure.Field.Channel'),
        t('GlassEnclosure.Field.Locale'),
        t('GlassEnclosure.Field.Subject'),
        t('GlassEnclosure.Field.Active'),
      ]}
      rows={templates.map((t2) => [
        t2.eventCode,
        t2.channel,
        t2.locale,
        t2.subjectTemplate ?? '—',
        t2.isActive ? t('Common.Yes') : t('Common.No'),
      ])}
    />
  );
};

const WindZonesPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useWindZonesQuery();
  if (isLoading) return <Skeleton />;
  const zones = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Region'),
        t('GlassEnclosure.Field.BasePressure'),
        t('GlassEnclosure.Field.HeightFactor'),
        t('GlassEnclosure.Field.Coastal'),
      ]}
      rows={zones.map((z) => [
        z.code,
        z.regionLabelTr,
        `${z.baseWindPressurePa} Pa`,
        z.heightFactorMultiplier.toFixed(2),
        z.isCoastal ? t('Common.Yes') : t('Common.No'),
      ])}
    />
  );
};

const ClimateZonesPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useClimateZonesQuery();
  if (isLoading) return <Skeleton />;
  const zones = data?.data ?? [];
  return (
    <Table
      columns={[
        t('GlassEnclosure.Field.Code'),
        t('GlassEnclosure.Field.Region'),
        t('GlassEnclosure.Field.WinterTemp'),
        t('GlassEnclosure.Field.Humidity'),
        t('GlassEnclosure.Field.Corrosion'),
        t('GlassEnclosure.Field.Recommendations'),
      ]}
      rows={zones.map((z) => [
        z.code,
        z.nameTr,
        `${z.avgWinterTemperatureC}°C`,
        `${z.avgHumidityPercent}%`,
        z.corrosionClass,
        [
          z.recommendsDoubleGlazing && t('GlassEnclosure.Recommend.DoubleGlazing'),
          z.recommendsCorrosionResistantCoating && t('GlassEnclosure.Recommend.CorrosionCoating'),
          z.recommendsSeismicSmallerPanel && t('GlassEnclosure.Recommend.SeismicSmaller'),
        ]
          .filter(Boolean)
          .join(', ') || '—',
      ])}
    />
  );
};

const SettingsPanel = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useSettingsQuery();
  if (isLoading) return <Skeleton />;
  const s = data?.data;
  if (!s) return null;
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
      <SettingsCard title={t('GlassEnclosure.Settings.Core')}>
        <KV
          label={t('GlassEnclosure.Field.StockBarLength')}
          value={`${s.defaultStockBarLengthMm} mm`}
        />
        <KV
          label={t('GlassEnclosure.Field.JumboGlass')}
          value={`${s.defaultJumboGlassWidthMm} × ${s.defaultJumboGlassHeightMm} mm`}
        />
        <KV label={t('GlassEnclosure.Field.SawKerf')} value={`${s.sawKerfMm} mm`} />
        <KV label={t('GlassEnclosure.Field.GlassKerf')} value={`${s.glassKerfMm} mm`} />
        <KV
          label={t('GlassEnclosure.Field.GuillotineRequired')}
          value={s.guillotineRequired ? t('Common.Yes') : t('Common.No')}
        />
        <KV label={t('GlassEnclosure.Field.WastePercent')} value={`${s.defaultWastePercent}%`} />
        <KV
          label={t('GlassEnclosure.Field.LaborCost')}
          value={`${s.laborCostPerM2.toFixed(2)} ${s.defaultCurrency}/m²`}
        />
        <KV label={t('GlassEnclosure.Field.MarginPercent')} value={`${s.defaultMarginPercent}%`} />
        <KV
          label={t('GlassEnclosure.Field.TaxRatePercent')}
          value={`${s.defaultTaxRatePercent}%`}
        />
      </SettingsCard>
      <ArcPricingCard settings={s} />
      <SettingsCard title={t('GlassEnclosure.Settings.Field')}>
        <KV label={t('GlassEnclosure.Field.ToleranceTop')} value={`${s.fieldToleranceTopMm} mm`} />
        <KV
          label={t('GlassEnclosure.Field.ToleranceSide')}
          value={`${s.fieldToleranceSideMm} mm`}
        />
      </SettingsCard>
      <SettingsCard title={t('GlassEnclosure.Settings.Installation')}>
        <KV
          label={t('GlassEnclosure.Field.TransportPerKm')}
          value={`${s.transportRatePerKm.toFixed(2)} ${s.defaultCurrency}`}
        />
        <KV
          label={t('GlassEnclosure.Field.TransportPerKg')}
          value={`${s.transportRatePerKg.toFixed(2)} ${s.defaultCurrency}`}
        />
        <KV
          label={t('GlassEnclosure.Field.ScaffoldingFromFloor')}
          value={s.scaffoldingRequiredFromFloor.toString()}
        />
        <KV
          label={t('GlassEnclosure.Field.ScaffoldingRate')}
          value={`${s.scaffoldingRatePerM2.toFixed(2)} ${s.defaultCurrency}/m²`}
        />
        <KV
          label={t('GlassEnclosure.Field.CraneFromFloor')}
          value={s.craneRequiredFromFloor.toString()}
        />
        <KV
          label={t('GlassEnclosure.Field.CraneRate')}
          value={`${s.craneRatePerMeter.toFixed(2)} ${s.defaultCurrency}/m`}
        />
        <KV
          label={t('GlassEnclosure.Field.WorkshopCapacity')}
          value={`${s.workshopDailyCapacityM2} m²/${t('Common.Day')}`}
        />
      </SettingsCard>
      <SettingsCard title={t('GlassEnclosure.Settings.Locale')}>
        <KV label={t('GlassEnclosure.Field.DefaultLocale')} value={s.defaultLocale} />
        <KV label={t('GlassEnclosure.Field.DefaultCurrency')} value={s.defaultCurrency} />
        <KV
          label={t('GlassEnclosure.Field.DataRetention')}
          value={`${s.dataRetentionDays} ${t('Common.Days')}`}
        />
        <KV
          label={t('GlassEnclosure.Field.QuoteTokenTtl')}
          value={`${s.quoteShareTokenTtlDays} ${t('Common.Days')}`}
        />
      </SettingsCard>
      <SettingsCard title={t('GlassEnclosure.Settings.Onboarding')}>
        <KV
          label={t('GlassEnclosure.Field.OnboardingComplete')}
          value={s.onboardingComplete ? t('Common.Yes') : t('Common.No')}
        />
      </SettingsCard>
    </div>
  );
};

const ArcPricingCard = ({ settings }: { settings: GlassEnclosureSettingsDto }) => {
  const { t } = useTranslation();
  const isAdmin = useIsTenantAdmin();
  const updateCore = useUpdateSettingsCoreMutation();
  const [factor, setFactor] = useState(String(settings.bentGlassCostFactor));
  const [railFee, setRailFee] = useState(String(settings.bendRailFeePerM));
  const [tracked, setTracked] = useState(settings);
  if (tracked !== settings) {
    setTracked(settings);
    setFactor(String(settings.bentGlassCostFactor));
    setRailFee(String(settings.bendRailFeePerM));
  }

  const parsedFactor = Number(factor);
  const parsedRailFee = Number(railFee);
  const factorValid = Number.isFinite(parsedFactor) && parsedFactor >= 1 && parsedFactor <= 10;
  const railFeeValid = Number.isFinite(parsedRailFee) && parsedRailFee >= 0;
  const dirty =
    parsedFactor !== settings.bentGlassCostFactor || parsedRailFee !== settings.bendRailFeePerM;

  const save = () => {
    if (!factorValid || !railFeeValid) return;
    // WHY every core field: the server DTO is a positional record with C# defaults, so any field the
    // client omits silently resets to that default — sending the current values keeps them intact.
    const input: UpdateSettingsCoreInput = {
      defaultStockBarLengthMm: settings.defaultStockBarLengthMm,
      defaultJumboGlassWidthMm: settings.defaultJumboGlassWidthMm,
      defaultJumboGlassHeightMm: settings.defaultJumboGlassHeightMm,
      sawKerfMm: settings.sawKerfMm,
      glassKerfMm: settings.glassKerfMm,
      guillotineRequired: settings.guillotineRequired,
      defaultWastePercent: settings.defaultWastePercent,
      laborCostPerM2: settings.laborCostPerM2,
      defaultMarginPercent: settings.defaultMarginPercent,
      defaultTaxRatePercent: settings.defaultTaxRatePercent,
      bendRailFeePerM: parsedRailFee,
      bentGlassCostFactor: parsedFactor,
    };
    updateCore.mutate(input, {
      onSuccess: () => toast.success(t('GlassEnclosure.Settings.ArcPricingSaved')),
      onError: (error) => toastApiError(error),
    });
  };

  return (
    <SettingsCard title={t('GlassEnclosure.Settings.ArcPricing')}>
      <p className="mb-3 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Settings.ArcPricingHint')}
      </p>
      <SettingsNumberField
        label={t('GlassEnclosure.Field.BentGlassCostFactor')}
        value={factor}
        onChange={setFactor}
        onBlurCommit={() =>
          setFactor(String(factorValid ? parsedFactor : settings.bentGlassCostFactor))
        }
        min={1}
        max={10}
        step={0.05}
        suffix="×"
        invalid={!factorValid}
        disabled={!isAdmin}
      />
      <SettingsNumberField
        label={t('GlassEnclosure.Field.BendRailFeePerM')}
        value={railFee}
        onChange={setRailFee}
        onBlurCommit={() =>
          setRailFee(String(railFeeValid ? parsedRailFee : settings.bendRailFeePerM))
        }
        min={0}
        step={1}
        suffix={`${settings.defaultCurrency}/m`}
        invalid={!railFeeValid}
        disabled={!isAdmin}
      />
      <p className="pt-1 text-xs text-slate-500 dark:text-slate-400">
        {parsedFactor === 1
          ? t('GlassEnclosure.Settings.ArcPricingNoPremium')
          : t('GlassEnclosure.Settings.ArcPricingSeparateLine')}
      </p>
      {isAdmin ? (
        <div className="flex justify-end pt-2">
          <button
            type="button"
            onClick={save}
            disabled={!dirty || !factorValid || !railFeeValid || updateCore.isPending}
            className="rounded bg-primary-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {t('Common.Save')}
          </button>
        </div>
      ) : (
        <p className="pt-2 text-xs italic text-slate-400 dark:text-slate-500">
          {t('GlassEnclosure.Settings.ArcPricingAdminOnly')}
        </p>
      )}
    </SettingsCard>
  );
};

const SettingsNumberField = ({
  label,
  value,
  onChange,
  onBlurCommit,
  min,
  max,
  step,
  suffix,
  invalid,
  disabled,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  onBlurCommit: () => void;
  min?: number;
  max?: number;
  step?: number;
  suffix: string;
  invalid: boolean;
  disabled: boolean;
}) => (
  <label className="flex items-center justify-between gap-3 text-sm">
    <span className="text-slate-500 dark:text-slate-400">{label}</span>
    <span className="flex items-center gap-1">
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        step={step}
        disabled={disabled}
        onChange={(e) => onChange(e.target.value)}
        onBlur={onBlurCommit}
        className={`w-24 rounded border bg-white px-2 py-1 text-right font-mono text-sm text-slate-900 focus:outline-none disabled:cursor-not-allowed disabled:opacity-60 dark:bg-slate-900 dark:text-slate-100 ${
          invalid
            ? 'border-danger-500 focus:border-danger-500'
            : 'border-slate-300 focus:border-primary-500 dark:border-slate-600'
        }`}
      />
      <span className="w-14 text-xs text-slate-400 dark:text-slate-500">{suffix}</span>
    </span>
  </label>
);

const Table = ({ columns, rows }: { columns: string[]; rows: (string | number)[][] }) => (
  <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
    <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
      <thead className="bg-slate-50 dark:bg-slate-900/50">
        <tr>
          {columns.map((col) => (
            <th
              key={col}
              className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500 dark:text-slate-400"
            >
              {col}
            </th>
          ))}
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
        {rows.length === 0 ? (
          <tr>
            <td
              colSpan={columns.length}
              className="px-4 py-8 text-center text-sm text-slate-500 dark:text-slate-400"
            >
              —
            </td>
          </tr>
        ) : (
          rows.map((row, i) => (
            <tr key={i} className="hover:bg-slate-50 dark:hover:bg-slate-900/30">
              {row.map((cell, j) => (
                <td key={j} className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">
                  {cell}
                </td>
              ))}
            </tr>
          ))
        )}
      </tbody>
    </table>
  </div>
);

const Skeleton = () => (
  <div className="space-y-2 p-4">
    {Array.from({ length: 6 }).map((_, i) => (
      <div key={i} className="h-10 animate-pulse rounded bg-slate-100 dark:bg-slate-800" />
    ))}
  </div>
);

const SettingsCard = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
    <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
      {title}
    </h3>
    <dl className="space-y-2">{children}</dl>
  </div>
);

const KV = ({ label, value }: { label: string; value: string }) => (
  <div className="flex items-center justify-between text-sm">
    <dt className="text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="font-mono text-slate-700 dark:text-slate-300">{value}</dd>
  </div>
);
