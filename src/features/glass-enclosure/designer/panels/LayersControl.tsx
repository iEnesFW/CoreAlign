import { useTranslation } from 'react-i18next';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';

const LAYERS: { key: 'runs' | 'walls' | 'slabs' | 'surfaces'; labelKey: string; def: string }[] = [
  { key: 'runs', labelKey: 'Runs', def: 'Hatlar' },
  { key: 'walls', labelKey: 'Walls', def: 'Duvarlar' },
  { key: 'slabs', labelKey: 'Slabs', def: 'Döşemeler' },
  { key: 'surfaces', labelKey: 'Surfaces', def: 'Yüzeyler' },
];

export function LayersControl() {
  const { t } = useTranslation();
  const layerVisibility = useDesignerStore((s) => s.layerVisibility);
  const toggleLayer = useDesignerStore((s) => s.toggleLayer);

  return (
    <div className="flex items-center gap-0.5">
      {LAYERS.map(({ key, labelKey, def }) => {
        const visible = layerVisibility[key];
        const label = t(`GlassEnclosure.Designer.Layer.${labelKey}`, { defaultValue: def });
        return (
          <button
            key={key}
            type="button"
            title={label}
            aria-pressed={visible}
            onClick={() => toggleLayer(key)}
            className={cn(
              'inline-flex items-center gap-1 rounded-md border px-1.5 py-1 text-[10px] font-medium transition',
              visible
                ? 'border-slate-300 text-slate-700 dark:border-slate-600 dark:text-slate-200'
                : 'border-slate-200 text-slate-400 dark:border-slate-700 dark:text-slate-500',
            )}
          >
            {visible ? <Eye size={12} /> : <EyeOff size={12} />}
            {label}
          </button>
        );
      })}
    </div>
  );
}
