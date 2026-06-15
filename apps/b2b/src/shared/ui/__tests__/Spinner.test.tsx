import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { Spinner } from '@/shared/ui/Spinner';

describe('Spinner', () => {
  it('renders an svg element', () => {
    const { container } = render(<Spinner />);
    expect(container.querySelector('svg')).not.toBeNull();
  });

  it('applies animate-spin class', () => {
    const { container } = render(<Spinner />);
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('class')).toContain('animate-spin');
  });

  it('merges custom className', () => {
    const { container } = render(<Spinner className="text-rose-500" />);
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('class')).toContain('text-rose-500');
  });

  it('honors size prop', () => {
    const { container } = render(<Spinner size={32} />);
    const svg = container.querySelector('svg');
    expect(svg?.getAttribute('width')).toBe('32');
  });
});
