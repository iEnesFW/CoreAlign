import { describe, expect, it } from 'vitest';
import { runViolatesCatalog } from './catalogValidation';
import type { GlassTypeDto, ProfileSystemDto } from './glassEnclosure.types';
import type { SceneRunState } from './project.types';

const sys = (over: Record<string, unknown>): ProfileSystemDto =>
  ({
    id: 'sys-1',
    maxPanelWidthMm: 0,
    maxPanelHeightMm: 0,
    maxPanelWeightKg: 0,
    supportedGlassThicknesses: [],
    supportedOpenings: [],
    ...over,
  }) as unknown as ProfileSystemDto;

const glass = (id: string, thicknessMm: number): GlassTypeDto =>
  ({ id, thicknessMm, weightKgPerM2: 0 }) as unknown as GlassTypeDto;

const run = (panels: Array<{ glassTypeId: string; openingType: string }>): SceneRunState =>
  ({
    profileSystemId: 'sys-1',
    heightMm: 2000,
    panels: panels.map((p) => ({
      widthMm: 1000,
      glassTypeId: p.glassTypeId,
      openingType: p.openingType,
    })),
  }) as unknown as SceneRunState;

const glassMap = new Map<string, GlassTypeDto>([
  ['g8', glass('g8', 8)],
  ['g12', glass('g12', 12)],
]);

describe('runViolatesCatalog — catalog compatibility', () => {
  it('flags a glass thickness the profile does not carry', () => {
    const systemMap = new Map([['sys-1', sys({ supportedGlassThicknesses: [6, 8, 10] })]]);
    expect(
      runViolatesCatalog(run([{ glassTypeId: 'g12', openingType: 'Fixed' }]), systemMap, glassMap),
    ).toBe(true);
    expect(
      runViolatesCatalog(run([{ glassTypeId: 'g8', openingType: 'Fixed' }]), systemMap, glassMap),
    ).toBe(false);
  });

  it('flags an opening the profile does not support', () => {
    const systemMap = new Map([['sys-1', sys({ supportedOpenings: ['Fixed', 'SlidingLeft'] })]]);
    expect(
      runViolatesCatalog(run([{ glassTypeId: 'g8', openingType: 'Folding' }]), systemMap, glassMap),
    ).toBe(true);
    expect(
      runViolatesCatalog(run([{ glassTypeId: 'g8', openingType: 'Fixed' }]), systemMap, glassMap),
    ).toBe(false);
  });

  it('treats empty support lists as "no constraint declared" (does not flag)', () => {
    const systemMap = new Map([['sys-1', sys({})]]);
    expect(
      runViolatesCatalog(
        run([{ glassTypeId: 'g12', openingType: 'Folding' }]),
        systemMap,
        glassMap,
      ),
    ).toBe(false);
  });

  it('does not flag when both thickness and opening are supported', () => {
    const systemMap = new Map([
      [
        'sys-1',
        sys({ supportedGlassThicknesses: [8, 10], supportedOpenings: ['Fixed', 'Folding'] }),
      ],
    ]);
    expect(
      runViolatesCatalog(
        run([
          { glassTypeId: 'g8', openingType: 'Fixed' },
          { glassTypeId: 'g8', openingType: 'Folding' },
        ]),
        systemMap,
        glassMap,
      ),
    ).toBe(false);
  });
});
