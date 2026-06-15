import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PageHeader } from '@/shared/ui/PageHeader';

describe('PageHeader', () => {
  it('renders title as h1', () => {
    render(<PageHeader title="Dashboard" />);
    const h1 = screen.getByRole('heading', { level: 1 });
    expect(h1).toHaveTextContent('Dashboard');
  });

  it('renders subtitle when provided', () => {
    render(<PageHeader title="t" subtitle="Sub line" />);
    expect(screen.getByText('Sub line')).toBeInTheDocument();
  });

  it('omits subtitle node when not provided', () => {
    render(<PageHeader title="t" />);
    expect(screen.queryByText('Sub line')).not.toBeInTheDocument();
  });

  it('renders action slot when supplied', () => {
    render(<PageHeader title="t" action={<button type="button">Action</button>} />);
    expect(screen.getByRole('button', { name: 'Action' })).toBeInTheDocument();
  });
});
