import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from '@/shared/ui/Input';

describe('Input', () => {
  it('renders a label when provided', () => {
    render(<Input name="email" label="Email" />);
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
  });

  it('accepts user input', async () => {
    render(<Input name="email" label="Email" />);
    const input = screen.getByLabelText('Email');
    await userEvent.type(input, 'hello@example.com');
    expect(input).toHaveValue('hello@example.com');
  });

  it('shows hint text when there is no error', () => {
    render(<Input name="user" label="Username" hint="Letters only" />);
    expect(screen.getByText('Letters only')).toBeInTheDocument();
  });

  it('shows error text and sets aria-invalid', () => {
    render(<Input name="user" label="Username" hint="Letters only" error="Required" />);
    expect(screen.getByText('Required')).toBeInTheDocument();
    expect(screen.queryByText('Letters only')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Username')).toHaveAttribute('aria-invalid', 'true');
  });
});
