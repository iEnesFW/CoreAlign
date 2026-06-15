import type { Group } from 'three';
import type { RefObject } from 'react';

export const setBodyPreview = (
  ref: RefObject<Group | null>,
  scale: [number, number, number],
  positionM: [number, number, number],
) => {
  const body = ref.current;
  if (!body) return;
  body.scale.set(scale[0], scale[1], scale[2]);
  body.position.set(positionM[0], positionM[1], positionM[2]);
};
