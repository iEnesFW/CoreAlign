import { useSurfaceMaterial, type QualityPreset } from '@/shared/three-engine';

interface AluminumMaterialOptions {
  quality: QualityPreset;
  hexColor: string;
  finish: 'Anodized' | 'PowderCoated' | 'WoodLook' | 'Raw';
  isSelected?: boolean;
}

const FINISH_PARAMS: Record<
  AluminumMaterialOptions['finish'],
  { metalness: number; roughness: number }
> = {
  Anodized: { metalness: 0.85, roughness: 0.28 },
  PowderCoated: { metalness: 0.4, roughness: 0.45 },
  WoodLook: { metalness: 0.15, roughness: 0.65 },
  Raw: { metalness: 0.9, roughness: 0.2 },
};

export function useAluminumMaterial({
  quality,
  hexColor,
  finish,
  isSelected,
}: AluminumMaterialOptions) {
  const params = FINISH_PARAMS[finish];
  return useSurfaceMaterial(quality, hexColor, {
    metalness: params.metalness,
    roughness: params.roughness,
    emissiveHex: isSelected ? '#f59e0b' : undefined,
    emissiveIntensity: 0.18,
  });
}
