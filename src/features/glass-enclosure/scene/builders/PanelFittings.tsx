import type { ThreeEvent } from '@react-three/fiber';
import type { GlassOpeningType } from '../../model/glassEnclosure.types';

interface PanelFittingsProps {
  widthM: number;
  thicknessM: number;
  openingType: GlassOpeningType;
  hasHandle: boolean;
  hasLock: boolean;
  onSelect?: () => void;
}

export function PanelFittings({
  widthM,
  thicknessM,
  openingType,
  hasHandle,
  hasLock,
  onSelect,
}: PanelFittingsProps) {
  if (!hasHandle && !hasLock) return null;
  const onLeftStile = openingType === 'SlidingLeft' || openingType === 'Hinged';
  const stileX = onLeftStile ? -widthM / 2 + 0.05 : widthM / 2 - 0.05;
  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    onSelect?.();
  };
  return (
    <group position={[stileX, 0, thicknessM / 2 + 0.02]} onClick={handleClick}>
      {hasHandle && (
        <>
          <mesh castShadow>
            <boxGeometry args={[0.028, 0.36, 0.028]} />
            <meshStandardMaterial
              color="#c4ccd2"
              metalness={0.95}
              roughness={0.25}
              envMapIntensity={0.6}
            />
          </mesh>
          <mesh position={[0, 0.16, -0.025]} rotation={[Math.PI / 2, 0, 0]} castShadow>
            <cylinderGeometry args={[0.014, 0.014, 0.05, 16]} />
            <meshStandardMaterial color="#aab4bb" metalness={0.95} roughness={0.25} />
          </mesh>
          <mesh position={[0, -0.16, -0.025]} rotation={[Math.PI / 2, 0, 0]} castShadow>
            <cylinderGeometry args={[0.014, 0.014, 0.05, 16]} />
            <meshStandardMaterial color="#aab4bb" metalness={0.95} roughness={0.25} />
          </mesh>
        </>
      )}
      {hasLock && (
        <mesh position={[0, hasHandle ? -0.24 : 0, 0]} castShadow>
          <cylinderGeometry args={[0.022, 0.022, 0.03, 20]} />
          <meshStandardMaterial
            color="#d4af37"
            metalness={0.9}
            roughness={0.28}
            envMapIntensity={0.7}
          />
        </mesh>
      )}
    </group>
  );
}
