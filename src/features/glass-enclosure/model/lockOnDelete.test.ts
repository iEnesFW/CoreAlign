import { describe, expect, it } from 'vitest';
import { blockedByLock, blockedByLockOnDelete } from './sceneGuards';

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
