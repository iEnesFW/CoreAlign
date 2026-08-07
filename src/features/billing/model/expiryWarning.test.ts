import { describe, expect, it } from 'vitest';

import type { TenantModuleDto } from './billing.types';
import { daysUntil, dismissalKey, expiringSoon } from './expiryWarning';

const now = new Date('2026-08-07T12:00:00Z');

const grant = (over: Partial<TenantModuleDto> = {}): TenantModuleDto => ({
  id: 't1',
  moduleId: 'm1',
  code: 'Sales',
  name: 'Satış',
  startUtc: '2026-01-01T00:00:00Z',
  endUtc: '2026-08-09T12:00:00Z',
  isCurrentlyActive: true,
  source: 'Paid',
  notes: null,
  ...over,
});

describe('expiringSoon', () => {
  it('picks up a grant inside the three-day window', () => {
    expect(expiringSoon([grant()], now).map((m) => m.daysLeft)).toEqual([2]);
  });

  it('ignores a grant that is still comfortably ahead', () => {
    expect(expiringSoon([grant({ endUtc: '2026-08-20T12:00:00Z' })], now)).toHaveLength(0);
  });

  it('ignores an already-expired grant so the popup never claims days that are gone', () => {
    expect(expiringSoon([grant({ endUtc: '2026-08-01T12:00:00Z' })], now)).toHaveLength(0);
  });

  it('ignores a perpetual grant', () => {
    expect(expiringSoon([grant({ endUtc: null })], now)).toHaveLength(0);
  });

  it('ignores a grant whose term has not started yet', () => {
    expect(expiringSoon([grant({ isCurrentlyActive: false })], now)).toHaveLength(0);
  });

  it('honours a wider window for the reminder list', () => {
    expect(expiringSoon([grant({ endUtc: '2026-08-18T12:00:00Z' })], now, 15)).toHaveLength(1);
  });

  it('lists the most urgent module first', () => {
    const list = expiringSoon(
      [
        grant({ moduleId: 'a', endUtc: '2026-08-10T12:00:00Z' }),
        grant({ moduleId: 'b', endUtc: '2026-08-08T12:00:00Z' }),
      ],
      now,
    );
    expect(list.map((m) => m.moduleId)).toEqual(['b', 'a']);
  });
});

describe('daysUntil', () => {
  it('rounds a part-day up so the last day still reads as one day left', () => {
    expect(daysUntil('2026-08-08T01:00:00Z', now)).toBe(1);
  });
});

describe('dismissalKey', () => {
  it('changes when a module is extended, so the popup is not hidden by yesterday’s dismissal', () => {
    const before = expiringSoon([grant()], now);
    const after = expiringSoon([grant({ endUtc: '2026-08-09T18:00:00Z' })], now);
    expect(dismissalKey(before, now)).toBe(dismissalKey(after, now));

    const extended = expiringSoon([grant({ endUtc: '2026-08-10T12:00:00Z' })], now, 5);
    expect(dismissalKey(before, now)).not.toBe(dismissalKey(extended, now));
  });

  it('changes the next day so a still-unresolved expiry is raised again', () => {
    const list = expiringSoon([grant()], now);
    expect(dismissalKey(list, now)).not.toBe(dismissalKey(list, new Date('2026-08-08T09:00:00Z')));
  });

  it('does not depend on the order the modules arrive in', () => {
    const a = expiringSoon(
      [grant({ moduleId: 'a' }), grant({ moduleId: 'b', endUtc: '2026-08-08T12:00:00Z' })],
      now,
    );
    const b = expiringSoon(
      [grant({ moduleId: 'b', endUtc: '2026-08-08T12:00:00Z' }), grant({ moduleId: 'a' })],
      now,
    );
    expect(dismissalKey(a, now)).toBe(dismissalKey(b, now));
  });
});
