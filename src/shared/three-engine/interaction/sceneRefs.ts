import type { Group } from 'three';

const refs = new Map<string, Group>();

export const registerSceneRef = (id: string, group: Group | null) => {
  if (group) refs.set(id, group);
  else refs.delete(id);
};

export const getSceneRef = (id: string): Group | undefined => refs.get(id);
