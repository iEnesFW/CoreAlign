import {
  usePhysicalGlassMaterial,
  QUALITY_SETTINGS,
  type QualityPreset,
} from '@/shared/three-engine';
import type { GlassStructure } from '../../model/glassEnclosure.types';

const STRUCTURE_PROFILE: Record<GlassStructure, { tint: string; transmissionScale: number }> = {
  Tempered: { tint: '#d7ecf2', transmissionScale: 1.0 },
  Laminated: { tint: '#cfe6ee', transmissionScale: 0.92 },
  DoubleGlazed: { tint: '#c2dfe8', transmissionScale: 0.85 },
  TripleGlazed: { tint: '#b6d8e3', transmissionScale: 0.78 },
  LowE: { tint: '#bfe3d8', transmissionScale: 0.88 },
};

interface GlassMaterialFactoryOptions {
  quality: QualityPreset;
  thicknessMm: number;
  structure?: GlassStructure;
  tintHex?: string;
  isSelected?: boolean;
}

export function useGlassMaterial({
  quality,
  thicknessMm,
  structure,
  tintHex,
  isSelected,
}: GlassMaterialFactoryOptions) {
  const settings = QUALITY_SETTINGS[quality];
  const profile = structure ? STRUCTURE_PROFILE[structure] : undefined;
  const baseTint = tintHex ?? profile?.tint ?? '#cbe3ec';
  const opacity =
    settings.glassTransmission > 0 ? 0.5 - 0.22 * (profile?.transmissionScale ?? 1) : 0.55;

  return usePhysicalGlassMaterial(quality, {
    tintHex: baseTint,
    thicknessMm,
    opacity,
    emissiveHex: isSelected ? '#2563eb' : undefined,
    emissiveIntensity: 0.3,
  });
}
