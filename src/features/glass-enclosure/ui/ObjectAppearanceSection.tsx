import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { PROCEDURAL_MATERIAL_KEYS } from '@/shared/three-engine';
import { useColorOptionsQuery } from '../hooks/useGlassEnclosureQueries';

const MATERIAL_LABELS: Record<string, string> = {
  wood: 'Ahşap',
  marble: 'Mermer',
  concrete: 'Beton',
  panel: 'Panel',
  grass: 'Çim',
  asphalt: 'Asfalt',
  brick: 'Tuğla',
  plaster: 'Sıva',
};

export interface AppearancePatch {
  colorHex?: string | null;
  materialKey?: string | null;
}

interface Props {
  colorHex?: string | null;
  materialKey?: string | null;
  onChange: (patch: AppearancePatch) => void;
}

export function ObjectAppearanceSection({ colorHex, materialKey, onChange }: Props) {
  const { t } = useTranslation();
  const colorsQuery = useColorOptionsQuery();
  const colors = colorsQuery.data?.data ?? [];
  const hasTexture = Boolean(materialKey);

  const swatchClass = (active: boolean) =>
    cn(
      'h-6 w-6 rounded border transition',
      active
        ? 'border-primary-500 ring-2 ring-primary-400/60'
        : 'border-slate-300 dark:border-slate-600',
    );
  const chipClass = (active: boolean) =>
    cn(
      'rounded border px-2 py-1 text-[11px] font-medium transition',
      active
        ? 'border-primary-600 bg-primary-600 text-white'
        : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800',
    );

  return (
    <div className="space-y-2">
      <p className="text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.Appearance.Section', { defaultValue: 'Görünüm' })}
      </p>

      <div className="flex flex-wrap items-center gap-1">
        {colors.map((color) => (
          <button
            key={color.id}
            type="button"
            title={color.name}
            aria-label={color.name}
            onClick={() => onChange({ colorHex: color.hexColor, materialKey: null })}
            className={swatchClass(!hasTexture && colorHex === color.hexColor)}
            style={{ backgroundColor: color.hexColor }}
          />
        ))}
        <label
          title={t('GlassEnclosure.Designer.WallFeature.ColorCustom', {
            defaultValue: 'Özel renk',
          })}
          className="inline-flex h-6 w-6 cursor-pointer items-center justify-center overflow-hidden rounded border border-slate-300 dark:border-slate-600"
        >
          <span className="sr-only">
            {t('GlassEnclosure.Designer.WallFeature.ColorCustom', { defaultValue: 'Özel renk' })}
          </span>
          <input
            type="color"
            value={colorHex ?? '#94a3b8'}
            onChange={(e) => onChange({ colorHex: e.target.value, materialKey: null })}
            className="h-8 w-8 cursor-pointer border-0 bg-transparent p-0"
          />
        </label>
      </div>

      <div className="flex flex-wrap items-center gap-1">
        <button
          type="button"
          onClick={() => onChange({ materialKey: null })}
          className={chipClass(!hasTexture)}
        >
          {t('GlassEnclosure.Designer.Appearance.NoTexture', { defaultValue: 'Dokusuz' })}
        </button>
        {PROCEDURAL_MATERIAL_KEYS.map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => onChange({ materialKey: key, colorHex: null })}
            className={chipClass(materialKey === key)}
          >
            {t(`GlassEnclosure.Designer.Material.${key}`, {
              defaultValue: MATERIAL_LABELS[key] ?? key,
            })}
          </button>
        ))}
      </div>
    </div>
  );
}

export default ObjectAppearanceSection;
