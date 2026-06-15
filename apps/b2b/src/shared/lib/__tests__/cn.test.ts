import { describe, expect, it } from 'vitest';
import { cn } from '@/shared/lib/cn';

describe('cn', () => {
  it('merges simple class strings', () => {
    expect(cn('a', 'b')).toBe('a b');
  });

  it('honors tailwind precedence (later wins)', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4');
  });

  it('drops falsy values', () => {
    const skip = false as boolean;
    expect(cn('a', skip && 'b', null, undefined, 'c')).toBe('a c');
  });

  it('supports conditional objects', () => {
    expect(cn('base', { on: true, off: false })).toBe('base on');
  });

  it('preserves multi-class strings', () => {
    expect(cn('flex items-center', 'gap-2')).toBe('flex items-center gap-2');
  });
});
