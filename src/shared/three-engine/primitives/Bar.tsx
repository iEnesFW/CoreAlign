import type { Material } from 'three';

interface BarProps {
  lengthM: number;
  crossSectionMm: { width: number; height: number };
  material: Material;
  position: [number, number, number];
  rotation?: [number, number, number];
  castShadow?: boolean;
  receiveShadow?: boolean;
}

export function Bar({
  lengthM,
  crossSectionMm,
  material,
  position,
  rotation = [0, 0, 0],
  castShadow = true,
  receiveShadow = true,
}: BarProps) {
  return (
    <mesh
      position={position}
      rotation={rotation}
      castShadow={castShadow}
      receiveShadow={receiveShadow}
      material={material}
    >
      <boxGeometry args={[lengthM, crossSectionMm.height / 1000, crossSectionMm.width / 1000]} />
    </mesh>
  );
}
