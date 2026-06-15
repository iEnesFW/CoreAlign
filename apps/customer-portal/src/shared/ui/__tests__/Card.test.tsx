import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';

describe('Card', () => {
  it('renders children', () => {
    render(
      <Card>
        <p>Hello</p>
      </Card>,
    );
    expect(screen.getByText('Hello')).toBeInTheDocument();
  });

  it('Header renders title and subtitle', () => {
    render(<CardHeader title="Account" subtitle="Profile details" />);
    expect(screen.getByText('Account')).toBeInTheDocument();
    expect(screen.getByText('Profile details')).toBeInTheDocument();
  });

  it('Header renders the action slot', () => {
    render(<CardHeader title="Account" action={<button type="button">Edit</button>} />);
    expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();
  });

  it('Body applies padding wrapper', () => {
    render(<CardBody>content</CardBody>);
    expect(screen.getByText('content')).toBeInTheDocument();
  });
});
