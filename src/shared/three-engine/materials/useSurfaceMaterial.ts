import { useEffect, useMemo } from 'react';
import { Color, MeshStandardMaterial } from 'three';
import { QUALITY_SETTINGS, type QualityPreset } from '../quality/qualityPreset';

export interface SurfaceMaterialParams {
  metalness: number;
  roughness: number;
  emissiveHex?: string;
  emissiveIntensity?: number;
}

export function useSurfaceMaterial(
  quality: QualityPreset,
  hexColor: string,
  params: SurfaceMaterialParams,
) {
  const material = useMemo(() => {
    const settings = QUALITY_SETTINGS[quality];
    const next = new MeshStandardMaterial({
      color: new Color(hexColor || '#cccccc'),
      metalness: params.metalness,
      roughness: params.roughness,
      envMapIntensity: settings.envMapIntensity,
    });
    if (params.emissiveHex) {
      next.emissive = new Color(params.emissiveHex);
      next.emissiveIntensity = params.emissiveIntensity ?? 0.18;
    }
    return next;
  }, [
    quality,
    hexColor,
    params.metalness,
    params.roughness,
    params.emissiveHex,
    params.emissiveIntensity,
  ]);
  // Free the previous GPU material when params change or the mesh unmounts.
  useEffect(() => () => material.dispose(), [material]);
  return material;
}
