export type QualityPreset = 'low' | 'medium' | 'high' | 'ultra';

export interface QualitySettings {
  shadows: boolean;
  shadowMapSize: number;
  glassTransmission: number;
  glassRoughness: number;
  envMapIntensity: number;
  antialias: boolean;
  pixelRatioMax: number;
  enableAo: boolean;
}

export const QUALITY_SETTINGS: Record<QualityPreset, QualitySettings> = {
  low: {
    shadows: false,
    shadowMapSize: 512,
    glassTransmission: 0,
    glassRoughness: 0.2,
    envMapIntensity: 0.5,
    antialias: false,
    pixelRatioMax: 1,
    enableAo: false,
  },
  medium: {
    shadows: true,
    shadowMapSize: 1024,
    glassTransmission: 0.6,
    glassRoughness: 0.16,
    envMapIntensity: 0.35,
    antialias: true,
    pixelRatioMax: 1.5,
    enableAo: false,
  },
  high: {
    shadows: true,
    shadowMapSize: 2048,
    glassTransmission: 0.72,
    glassRoughness: 0.14,
    envMapIntensity: 0.4,
    antialias: true,
    pixelRatioMax: 2,
    enableAo: true,
  },
  ultra: {
    shadows: true,
    shadowMapSize: 4096,
    glassTransmission: 0.8,
    glassRoughness: 0.1,
    envMapIntensity: 0.5,
    antialias: true,
    pixelRatioMax: 2,
    enableAo: true,
  },
};
