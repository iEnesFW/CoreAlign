import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';

describe('b2b Card', () => {
  it('renders children', () => {
    render(
      <Card>
        <p>content</p>
      </Card>,
    );
    expect(screen.getByText('content')).toBeInTheDocument();
  });

  it('Header renders title', () => {
    render(<CardHeader title="Dealer" />);
    expect(screen.getByText('Dealer')).toBeInTheDocument();
  });

  it('CardBody wraps content', () => {
    render(<CardBody>body</CardBody>);
    expect(screen.getByText('body')).toBeInTheDocument();
  });
});
