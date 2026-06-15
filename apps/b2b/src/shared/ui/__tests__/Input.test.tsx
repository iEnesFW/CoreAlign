import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from '@/shared/ui/Input';

describe('Input', () => {
  it('renders the label', () => {
    render(<Input name="dealer" label="Dealer Code" />);
    expect(screen.getByLabelText('Dealer Code')).toBeInTheDocument();
  });

  it('captures typed value', async () => {
    render(<Input name="dealer" label="Dealer Code" />);
    const el = screen.getByLabelText('Dealer Code');
    await userEvent.type(el, 'BAYI-001');
    expect(el).toHaveValue('BAYI-001');
  });

  it('renders hint when no error', () => {
    render(<Input name="x" label="X" hint="info" />);
    expect(screen.getByText('info')).toBeInTheDocument();
  });

  it('prefers error over hint', () => {
    render(<Input name="x" label="X" hint="info" error="bad" />);
    expect(screen.getByText('bad')).toBeInTheDocument();
    expect(screen.queryByText('info')).not.toBeInTheDocument();
  });
});
