import { Bar, type QualityPreset } from '@/shared/three-engine';
import { useAluminumMaterial } from '../materials/aluminumMaterial';

interface ProfileBarProps {
  lengthM: number;
  crossSectionMm: { width: number; height: number };
  hexColor: string;
  finish: 'Anodized' | 'PowderCoated' | 'WoodLook' | 'Raw';
  quality: QualityPreset;
  position: [number, number, number];
  rotation?: [number, number, number];
  receiveShadow?: boolean;
}

export function ProfileBar({
  lengthM,
  crossSectionMm,
  hexColor,
  finish,
  quality,
  position,
  rotation = [0, 0, 0],
  receiveShadow = true,
}: ProfileBarProps) {
  const material = useAluminumMaterial({ quality, hexColor, finish });
  return (
    <Bar
      lengthM={lengthM}
      crossSectionMm={crossSectionMm}
      material={material}
      position={position}
      rotation={rotation}
      receiveShadow={receiveShadow}
    />
  );
}
