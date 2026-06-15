import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { SurfaceFeatureEditor } from './SurfaceFeatureEditor';

export function SlabFeatureInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const updateSlabFeature = useDesignerStore((s) => s.updateSlabFeature);
  const removeSlabFeature = useDesignerStore((s) => s.removeSlabFeature);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const slab = useMemo(
    () => slabs.find((s) => s.id === selection.slabId),
    [slabs, selection.slabId],
  );
  const feature = useMemo(
    () => (slab?.features ?? []).find((f) => f.id === selection.featureId),
    [slab, selection.featureId],
  );

  if (!slab || !feature) return null;

  return (
    <SurfaceFeatureEditor
      feature={feature}
      hostThicknessMm={slab.thicknessMm}
      title={t('GlassEnclosure.Designer.WallFeature.SlabTitle', {
        defaultValue: 'Zemin/çatı katmanı',
      })}
      onUpdate={(patch) => updateSlabFeature(slab.id, feature.id, patch)}
      onRemove={() => {
        removeSlabFeature(slab.id, feature.id);
        setSelection({
          kind: 'slab',
          runId: null,
          panelId: null,
          connectionId: null,
          hardwareId: null,
          wallId: null,
          slabId: slab.id,
        });
      }}
    />
  );
}

export default SlabFeatureInspector;
