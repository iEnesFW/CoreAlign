import { describe, expect, it } from 'vitest';
import { cn } from '@/shared/lib/cn';

describe('cn', () => {
  it('merges class strings', () => {
    expect(cn('a', 'b')).toBe('a b');
  });

  it('drops falsy values', () => {
    expect(cn('a', null, undefined, false, 'b')).toBe('a b');
  });

  it('uses tailwind merge to resolve conflicting padding classes', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4');
  });

  it('supports conditional objects', () => {
    expect(cn('base', { active: true, disabled: false })).toBe('base active');
  });

  it('flattens array inputs', () => {
    expect(cn(['flex', 'items-center'], 'gap-2')).toBe('flex items-center gap-2');
  });
});
