import { usePersistedState } from '@/shared/hooks/usePersistedState';
import type { ViewportAppearance } from '@/shared/three-engine';

export type ViewerAppearancePreset =
  | 'studio'
  | 'daylight'
  | 'street'
  | 'sunset'
  | 'city'
  | 'dawn'
  | 'plain';

export interface ViewerAppearanceDefinition extends ViewportAppearance {
  labelKey: string;
  defaultLabel: string;
}

export const VIEWER_APPEARANCE_PRESETS: Record<ViewerAppearancePreset, ViewerAppearanceDefinition> =
  {
    studio: {
      environment: 'apartment',
      background: '#cfe6fb',
      ground: '#dde4ea',
      sky: true,
      sunPosition: [10, 16, 8],
      labelKey: 'GlassEnclosure.Designer.Appearance.Studio',
      defaultLabel: 'Stüdyo',
    },
    daylight: {
      environment: 'apartment',
      background: '#bcd6f0',
      ground: '#4b7a36',
      groundTexture: 'grass',
      sky: true,
      sunPosition: [9, 12, 7],
      labelKey: 'GlassEnclosure.Designer.Appearance.Daylight',
      defaultLabel: 'Gün Işığı',
    },
    street: {
      environment: 'city',
      background: '#b4c4d6',
      ground: '#3a3d42',
      groundTexture: 'asphalt',
      sky: true,
      sunPosition: [11, 9, 5],
      labelKey: 'GlassEnclosure.Designer.Appearance.Street',
      defaultLabel: 'Sokak',
    },
    sunset: {
      environment: 'sunset',
      background: '#0f172a',
      ground: '#1f2937',
      labelKey: 'GlassEnclosure.Designer.Appearance.Sunset',
      defaultLabel: 'Gün Batımı',
    },
    city: {
      environment: 'city',
      background: '#1e293b',
      ground: '#334155',
      labelKey: 'GlassEnclosure.Designer.Appearance.City',
      defaultLabel: 'Şehir',
    },
    dawn: {
      environment: 'dawn',
      background: '#fde7d4',
      ground: '#f1d8c4',
      labelKey: 'GlassEnclosure.Designer.Appearance.Dawn',
      defaultLabel: 'Şafak',
    },
    plain: {
      environment: 'none',
      background: '#e2e8f0',
      ground: '#dde3e9',
      labelKey: 'GlassEnclosure.Designer.Appearance.Plain',
      defaultLabel: 'Düz',
    },
  };

export const VIEWER_APPEARANCE_ORDER: ViewerAppearancePreset[] = [
  'studio',
  'daylight',
  'street',
  'sunset',
  'city',
  'dawn',
  'plain',
];

const STORAGE_KEY = 'glassDesigner.viewerAppearance';
const DEFAULT_PRESET: ViewerAppearancePreset = 'studio';

export function useViewerAppearance() {
  const [stored, setPreset] = usePersistedState<ViewerAppearancePreset>(
    STORAGE_KEY,
    DEFAULT_PRESET,
  );
  const preset = VIEWER_APPEARANCE_PRESETS[stored] ? stored : DEFAULT_PRESET;
  const { environment, background, ground, groundTexture, sky, sunPosition } =
    VIEWER_APPEARANCE_PRESETS[preset];
  const appearance: ViewportAppearance = {
    environment,
    background,
    ground,
    groundTexture,
    sky,
    sunPosition,
  };
  return { preset, setPreset, appearance };
}
