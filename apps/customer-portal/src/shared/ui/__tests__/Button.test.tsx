import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '@/shared/ui/Button';

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Click me</Button>);
    expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument();
  });

  it('defaults to type=button', () => {
    render(<Button>Action</Button>);
    expect(screen.getByRole('button')).toHaveAttribute('type', 'button');
  });

  it('invokes onClick when clicked', async () => {
    const handler = vi.fn();
    render(<Button onClick={handler}>Press</Button>);
    await userEvent.click(screen.getByRole('button'));
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('respects disabled state', async () => {
    const handler = vi.fn();
    render(
      <Button onClick={handler} disabled>
        Disabled
      </Button>,
    );
    const btn = screen.getByRole('button');
    expect(btn).toBeDisabled();
    await userEvent.click(btn);
    expect(handler).not.toHaveBeenCalled();
  });

  it('applies the secondary variant class', () => {
    render(<Button variant="secondary">Sec</Button>);
    const btn = screen.getByRole('button');
    expect(btn.className).toContain('bg-slate-100');
  });

  it('applies the danger variant class', () => {
    render(<Button variant="danger">Del</Button>);
    expect(screen.getByRole('button').className).toContain('bg-rose-600');
  });
});
