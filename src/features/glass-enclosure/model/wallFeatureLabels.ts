import type { SceneWallFeature, WallFeatureMode } from './project.types';

export interface FeatureLabelRef {
  key: string;
  fallback: string;
}

export const wallFeatureShapeLabelKey = (shape: SceneWallFeature['shape']): FeatureLabelRef => {
  switch (shape) {
    case 'rect':
      return { key: 'GlassEnclosure.Designer.Tool.ShapeRect', fallback: 'Dikdörtgen' };
    case 'circle':
      return { key: 'GlassEnclosure.Designer.Tool.ShapeCircle', fallback: 'Daire' };
    case 'ellipse':
      return { key: 'GlassEnclosure.Designer.Tool.ShapeEllipse', fallback: 'Oval' };
    case 'triangle':
      return { key: 'GlassEnclosure.Designer.Tool.ShapeTriangle', fallback: 'Üçgen' };
    case 'polygon':
      return { key: 'GlassEnclosure.Designer.Tool.ShapePolygon', fallback: 'Çokgen' };
    case 'free':
      return { key: 'GlassEnclosure.Designer.Tool.ShapeFree', fallback: 'Serbest çizim' };
  }
};

export const wallFeatureModeLabelKey = (mode: WallFeatureMode): FeatureLabelRef => {
  switch (mode) {
    case 'recess':
      return { key: 'GlassEnclosure.Designer.Tool.ModeRecess', fallback: 'Girinti (oyma)' };
    case 'protrude':
      return { key: 'GlassEnclosure.Designer.Tool.ModeProtrude', fallback: 'Çıkıntı (katman)' };
    case 'hole':
      return { key: 'GlassEnclosure.Designer.Tool.ModeHole', fallback: 'Boşluk (delik)' };
  }
};
