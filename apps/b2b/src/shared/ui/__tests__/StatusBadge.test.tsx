import { describe, it, expect, beforeAll } from 'vitest';
import { render } from '@testing-library/react';
import {
  ApprovalStatusBadge,
  CommissionStatusBadge,
  InvoiceStatusBadge,
  OrderStatusBadge,
} from '@/shared/ui/StatusBadge';
import i18n from '@/app/i18n';

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

describe('b2b OrderStatusBadge', () => {
  it('renders success tone for Shipped', () => {
    const { container } = render(<OrderStatusBadge status="Shipped" />);
    expect(container.querySelector('span')!.className).toContain('bg-emerald-100');
  });

  it('renders danger tone for Cancelled', () => {
    const { container } = render(<OrderStatusBadge status="Cancelled" />);
    expect(container.querySelector('span')!.className).toContain('bg-rose-100');
  });
});

describe('b2b InvoiceStatusBadge', () => {
  it('shows overdue tone when applicable', () => {
    const { container } = render(<InvoiceStatusBadge status="Sent" isOverdue />);
    expect(container.querySelector('span')!.className).toContain('bg-rose-100');
  });
});

describe('b2b ApprovalStatusBadge', () => {
  it('renders warning tone for PendingCustomerApproval', () => {
    const { container } = render(<ApprovalStatusBadge status="PendingCustomerApproval" />);
    expect(container.querySelector('span')!.className).toContain('bg-amber-100');
  });
});

describe('b2b CommissionStatusBadge', () => {
  it('renders success tone for Paid', () => {
    const { container } = render(<CommissionStatusBadge status="Paid" />);
    expect(container.querySelector('span')!.className).toContain('bg-emerald-100');
  });
});
