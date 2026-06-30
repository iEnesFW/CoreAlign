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
    const transmission = settings.glassTransmission;
    const usesTransmission = transmission > 0;
    const tint = new Color(params.tintHex);
    const next = new MeshPhysicalMaterial({
      color: tint,
      transmission,
      roughness: settings.glassRoughness,
      metalness: 0,
      ior: 1.52,
      reflectivity: 0.5,
      thickness: Math.max(0.002, params.thicknessMm / 1000),
      attenuationColor: tint,
      attenuationDistance: usesTransmission ? 0.55 : Number.POSITIVE_INFINITY,
      clearcoat: 0.12,
      clearcoatRoughness: 0.1,
      specularIntensity: 1,
      transparent: true,
      opacity: usesTransmission ? 1 : params.opacity,
      depthWrite: false,
      envMapIntensity: settings.envMapIntensity * (usesTransmission ? 1.4 : 0.6),
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
