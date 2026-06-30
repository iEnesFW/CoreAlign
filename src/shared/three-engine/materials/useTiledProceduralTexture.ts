import { useEffect, useMemo } from 'react';
import { RepeatWrapping, type Texture } from 'three';
import { getProceduralTexture, isProceduralMaterialKey } from './proceduralTextures';

export function useTiledProceduralTexture(
  key: string | null | undefined,
  repeatX: number,
  repeatY: number,
): Texture | null {
  const rx = Math.min(64, Math.max(1, Math.round(repeatX)));
  const ry = Math.min(64, Math.max(1, Math.round(repeatY)));
  const texture = useMemo(() => {
    if (!key || !isProceduralMaterialKey(key)) return null;
    const clone = getProceduralTexture(key).clone();
    clone.wrapS = RepeatWrapping;
    clone.wrapT = RepeatWrapping;
    clone.repeat.set(rx, ry);
    clone.needsUpdate = true;
    return clone;
  }, [key, rx, ry]);
  useEffect(() => () => texture?.dispose(), [texture]);
  return texture;
}
