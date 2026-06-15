import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { SurfaceFeatureEditor } from './SurfaceFeatureEditor';

export function WallFeatureInspector() {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const updateWallFeature = useDesignerStore((s) => s.updateWallFeature);
  const removeWallFeature = useDesignerStore((s) => s.removeWallFeature);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const wall = useMemo(
    () => walls.find((w) => w.id === selection.wallId),
    [walls, selection.wallId],
  );
  const feature = useMemo(
    () => (wall?.features ?? []).find((f) => f.id === selection.featureId),
    [wall, selection.featureId],
  );

  if (!wall || !feature) return null;

  return (
    <SurfaceFeatureEditor
      feature={feature}
      hostThicknessMm={wall.thicknessMm}
      title={t('GlassEnclosure.Designer.WallFeature.Title', { defaultValue: 'Duvar katmanı' })}
      onUpdate={(patch) => updateWallFeature(wall.id, feature.id, patch)}
      onRemove={() => {
        removeWallFeature(wall.id, feature.id);
        setSelection({
          kind: 'wall',
          runId: null,
          panelId: null,
          connectionId: null,
          hardwareId: null,
          wallId: wall.id,
        });
      }}
    />
  );
}

export default WallFeatureInspector;
