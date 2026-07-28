import type { ThreeEvent } from '@react-three/fiber';
import { PaneMount } from './PaneMount';
import type { PaneSurface } from '../../model/paneSurface';
import type { GlassOpeningType } from '../../model/glassEnclosure.types';

interface PanelFittingsProps {
  surface: PaneSurface;
  openingType: GlassOpeningType;
  hasHandle: boolean;
  hasLock: boolean;
  onSelect?: () => void;
}

export function PanelFittings({
  surface,
  openingType,
  hasHandle,
  hasLock,
  onSelect,
}: PanelFittingsProps) {
  if (!hasHandle && !hasLock) return null;
  const onLeftStile = openingType === 'SlidingLeft' || openingType === 'Hinged';
  // WHY the surface frame and not a flat step: the fittings used to be anchored ONCE at the pane
  // mid-angle and then stepped to the stile inside the FLAT chord frame. On a curved pane that
  // step leaves the cylinder — measured up to 353 mm off the glass on a single-pane 60° run, while
  // user-placed hardware on the SAME pane sat correctly. The stile is a DEVELOPED offset now.
  const stileUMm = (onLeftStile ? -1 : 1) * (surface.widthMm / 2 - 50);
  const handleClick = (event: ThreeEvent<MouseEvent>) => {
    event.stopPropagation();
    onSelect?.();
  };
  return (
    <PaneMount
      surface={surface}
      offset={{ uMm: stileUMm, vMm: 0, nMm: surface.thicknessMm / 2 + 20 }}
    >
      <group onClick={handleClick}>
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
    </PaneMount>
  );
}
