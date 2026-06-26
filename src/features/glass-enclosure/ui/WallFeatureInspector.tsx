import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { SurfaceFeatureEditor } from './SurfaceFeatureEditor';
import {
  normalizeWallSide,
  wallFaceFrame,
  type WallFeatureSide,
} from '../scene/builders/wallFaces';
import type { WallFeatureSideValue } from '../model/project.types';

const FACE_ORDER: WallFeatureSide[] = ['front', 'back', 'left', 'right', 'top', 'bottom'];

const toSideValue = (side: WallFeatureSide): WallFeatureSideValue =>
  side === 'front' ? 1 : side === 'back' ? -1 : side;

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

  const currentSide = normalizeWallSide(feature.side);

  // Switching to another face re-centres the feature on it (its width/height keep meaning, but
  // its offset/centre would be out of range on a different face), so it lands valid and visible.
  const changeFace = (side: WallFeatureSide) => {
    if (side === currentSide) return;
    const frame = wallFaceFrame(side, {
      lengthM: wall.lengthMm / 1000,
      heightM: wall.heightMm / 1000,
      thicknessM: wall.thicknessMm / 1000,
    });
    const uMaxMm = frame.uMaxM * 1000;
    const vMaxMm = frame.vMaxM * 1000;
    updateWallFeature(wall.id, feature.id, {
      side: toSideValue(side),
      offsetMm: Math.round(uMaxMm / 2),
      centerZMm: Math.round(vMaxMm / 2),
      widthMm: Math.round(Math.min(feature.widthMm, uMaxMm * 0.8)),
      heightMm: Math.round(Math.min(feature.heightMm, vMaxMm * 0.8)),
    });
  };

  return (
    <div className="flex flex-col gap-3">
      <label className="flex flex-col gap-1 px-4 pt-4 text-sm text-slate-600 dark:text-slate-400">
        <span className="text-xs uppercase tracking-wide">
          {t('GlassEnclosure.Designer.WallFeature.Face', { defaultValue: 'Yüz' })}
        </span>
        <select
          value={currentSide}
          onChange={(e) => changeFace(e.target.value as WallFeatureSide)}
          className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100"
        >
          {FACE_ORDER.map((side) => (
            <option key={side} value={side}>
              {t(`GlassEnclosure.Designer.WallFeature.FaceName.${side}` as never, {
                defaultValue: {
                  front: 'Ön',
                  back: 'Arka',
                  left: 'Sol',
                  right: 'Sağ',
                  top: 'Üst',
                  bottom: 'Alt',
                }[side],
              })}
            </option>
          ))}
        </select>
      </label>
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
    </div>
  );
}

export default WallFeatureInspector;
