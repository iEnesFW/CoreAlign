import { describe, expect, it } from 'vitest';
import { blockedByLock, blockedByLockOnDelete, dropLockedIds, lockedBodyIds } from './sceneGuards';

/**
 * `blockedByLock` inspects a PATCH. A delete carries none, so every remove* action wrote straight
 * through and a locked body could still be erased — the one thing the lock exists to prevent.
 */

describe('blockedByLockOnDelete', () => {
  it('refuses to delete a locked body', () => {
    expect(blockedByLockOnDelete({ locked: true })).toBe(true);
  });

  it('lets an unlocked body through', () => {
    expect(blockedByLockOnDelete({ locked: false })).toBe(false);
    expect(blockedByLockOnDelete({})).toBe(false);
    expect(blockedByLockOnDelete(undefined)).toBe(false);
  });

  it('the patch guard cannot stand in for it — an empty patch is never blocked', () => {
    // This is exactly why the delete path needed its own gate.
    expect(blockedByLock({ locked: true }, {})).toBe(false);
    expect(blockedByLockOnDelete({ locked: true })).toBe(true);
  });

  it('unlocking stays possible (the lock must not be a one-way door)', () => {
    expect(blockedByLock({ locked: true }, { locked: false })).toBe(false);
  });
});

/**
 * The single setters all gate on the two helpers above, but the BULK paths write through
 * `applyScenePatch`, which sees no guard: a group move, a wall move carrying its bonded glass, an
 * Alt-stack, a rotate, a multi-delete and the grouping button all moved a locked body anyway. Those
 * paths now filter their id sets through these two helpers.
 */
describe('bulk-path lock filtering', () => {
  const scene = {
    walls: [{ id: 'w-free' }, { id: 'w-locked', locked: true }],
    runs: [{ id: 'r-locked', locked: true }, { id: 'r-free' }],
    slabs: [{ id: 's-free', locked: false }],
    surfaces: [{ id: 'sf-locked', locked: true }],
  };

  it('collects every locked id across all four body kinds', () => {
    expect(lockedBodyIds(scene)).toEqual(new Set(['w-locked', 'r-locked', 'sf-locked']));
  });

  it('tolerates a scene with missing collections', () => {
    expect(lockedBodyIds({ runs: [] })).toEqual(new Set());
  });

  it('drops the locked members and reports that it did', () => {
    const locked = lockedBodyIds(scene);
    const result = dropLockedIds(['w-free', 'w-locked', 'r-locked'], locked);
    expect(result.ids).toEqual(new Set(['w-free']));
    expect(result.blocked).toBe(true);
  });

  it('reports nothing blocked when the set is clean — no spurious toast', () => {
    const result = dropLockedIds(['w-free', 'r-free'], lockedBodyIds(scene));
    expect(result.ids).toEqual(new Set(['w-free', 'r-free']));
    expect(result.blocked).toBe(false);
  });
});
