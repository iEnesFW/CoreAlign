import type { SceneHardwareItem, SceneHardwareKind } from './project.types';

export const HARDWARE_KINDS: SceneHardwareKind[] = [
  'Handle',
  'PullHandle',
  'Lock',
  'Hinge',
  'Roller',
  'Stopper',
  'CornerJoint',
  'GasketStrip',
  'DripProfile',
  'Vent',
  'Louver',
  'Bracket',
  'Accessory',
];

interface HardwareDefault {
  colorHex: string;
  widthMm: number;
  heightMm: number;
  depthMm: number;
}

const DEFAULTS: Record<SceneHardwareKind, HardwareDefault> = {
  Handle: { colorHex: '#b8c0c6', widthMm: 28, heightMm: 320, depthMm: 28 },
  PullHandle: { colorHex: '#b8c0c6', widthMm: 25, heightMm: 450, depthMm: 25 },
  Lock: { colorHex: '#d4af37', widthMm: 44, heightMm: 44, depthMm: 30 },
  Hinge: { colorHex: '#9aa4ab', widthMm: 30, heightMm: 90, depthMm: 24 },
  Roller: { colorHex: '#7c878e', widthMm: 60, heightMm: 30, depthMm: 30 },
  Stopper: { colorHex: '#3f3f46', widthMm: 40, heightMm: 30, depthMm: 40 },
  CornerJoint: { colorHex: '#8a949b', widthMm: 60, heightMm: 60, depthMm: 25 },
  GasketStrip: { colorHex: '#1f2937', widthMm: 12, heightMm: 2200, depthMm: 10 },
  DripProfile: { colorHex: '#aeb6bc', widthMm: 600, heightMm: 25, depthMm: 40 },
  Vent: { colorHex: '#c9d1d6', widthMm: 300, heightMm: 120, depthMm: 24 },
  Louver: { colorHex: '#c9d1d6', widthMm: 300, heightMm: 160, depthMm: 30 },
  Bracket: { colorHex: '#8a949b', widthMm: 80, heightMm: 80, depthMm: 40 },
  Accessory: { colorHex: '#94a3b8', widthMm: 60, heightMm: 60, depthMm: 40 },
};

// Size/colour defaults for a hardware kind — used to seed a new item AND to reset dimensions when
// the user changes an existing item's kind (otherwise it kept the old kind's size).
export const hardwareKindDefault = (kind: SceneHardwareKind): HardwareDefault => DEFAULTS[kind];

export const createHardwareItem = (
  kind: SceneHardwareKind,
  glassThicknessMm = 8,
): SceneHardwareItem => {
  const def = DEFAULTS[kind];
  return {
    id: crypto.randomUUID(),
    kind,
    colorHex: def.colorHex,
    offsetXmm: 0,
    offsetYmm: 0,
    // WHY: sit the piece ON the glass face (centre-plane + half glass), not half-embedded at z=0.
    offsetZmm: def.depthMm / 2 + glassThicknessMm / 2,
    widthMm: def.widthMm,
    heightMm: def.heightMm,
    depthMm: def.depthMm,
  };
};
