import { describe, it, expect, beforeAll } from 'vitest';
import { render } from '@testing-library/react';
import { DealerStatusBadge, InvoiceStatusBadge, OrderStatusBadge } from '@/shared/ui/StatusBadge';
import i18n from '@/app/i18n';

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

describe('OrderStatusBadge', () => {
  it('renders a badge with the appropriate tone class', () => {
    const { container } = render(<OrderStatusBadge status="Cancelled" />);
    const badge = container.querySelector('span');
    expect(badge).not.toBeNull();
    expect(badge!.className).toContain('bg-rose-100');
  });
});

describe('InvoiceStatusBadge', () => {
  it('upgrades to Overdue tone when isOverdue is true and status is not Paid', () => {
    const { container } = render(<InvoiceStatusBadge status="Issued" isOverdue />);
    const badge = container.querySelector('span');
    expect(badge!.className).toContain('bg-rose-100');
  });

  it('keeps the success tone when status is Paid even if isOverdue is true', () => {
    const { container } = render(<InvoiceStatusBadge status="Paid" isOverdue />);
    const badge = container.querySelector('span');
    expect(badge!.className).toContain('bg-emerald-100');
  });
});

describe('DealerStatusBadge', () => {
  it('renders Suspended with warning tone', () => {
    const { container } = render(<DealerStatusBadge status="Suspended" />);
    const badge = container.querySelector('span');
    expect(badge!.className).toContain('bg-amber-100');
  });
});
