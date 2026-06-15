import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '@/shared/ui/Button';

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Save</Button>);
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });

  it('defaults to type=button', () => {
    render(<Button>Save</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');
  });

  it('invokes onClick', async () => {
    const handler = vi.fn();
    render(<Button onClick={handler}>Save</Button>);
    await userEvent.click(screen.getByRole('button'));
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('respects disabled', async () => {
    const handler = vi.fn();
    render(
      <Button onClick={handler} disabled>
        Save
      </Button>,
    );
    await userEvent.click(screen.getByRole('button'));
    expect(handler).not.toHaveBeenCalled();
  });

  it('applies the primary amber tint', () => {
    render(<Button variant="primary">Primary</Button>);
    expect(screen.getByRole('button').className).toContain('bg-amber-600');
  });

  it('applies the danger variant', () => {
    render(<Button variant="danger">Danger</Button>);
    expect(screen.getByRole('button').className).toContain('bg-rose-600');
  });
});
