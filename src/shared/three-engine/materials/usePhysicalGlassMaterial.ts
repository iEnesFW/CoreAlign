import { useEffect, useMemo } from 'react';
import { Color, DoubleSide, MeshPhysicalMaterial } from 'three';
import { QUALITY_SETTINGS, type QualityPreset } from '../quality/qualityPreset';

export interface PhysicalGlassParams {
  tintHex: string;
  thicknessMm: number;
  opacity: number;
  emissiveHex?: string;
  emissiveIntensity?: number;
}

export function usePhysicalGlassMaterial(quality: QualityPreset, params: PhysicalGlassParams) {
  const material = useMemo(() => {
    const settings = QUALITY_SETTINGS[quality];
    const next = new MeshPhysicalMaterial({
      color: new Color(params.tintHex),
      transmission: 0,
      roughness: settings.glassRoughness,
      metalness: 0,
      ior: 1.5,
      reflectivity: 0.06,
      thickness: Math.max(0.1, params.thicknessMm / 10),
      transparent: true,
      opacity: params.opacity,
      depthWrite: false,
      envMapIntensity: settings.envMapIntensity * 0.2,
      side: DoubleSide,
    });
    if (params.emissiveHex) {
      next.emissive = new Color(params.emissiveHex);
      next.emissiveIntensity = params.emissiveIntensity ?? 0.15;
    }
    return next;
  }, [
    quality,
    params.tintHex,
    params.thicknessMm,
    params.opacity,
    params.emissiveHex,
    params.emissiveIntensity,
  ]);
  useEffect(() => () => material.dispose(), [material]);
  return material;
}
