import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationBell } from '@/widgets/NotificationBell';
import { useAuthStore } from '@/features/auth/authStore';
import i18n from '@/app/i18n';

const listMock = vi.fn();
const unreadMock = vi.fn();
const markReadMock = vi.fn();
const markAllReadMock = vi.fn();

vi.mock('@/features/portal/notificationsApi', () => ({
  portalNotificationsApi: {
    list: (...args: unknown[]) => listMock(...args),
    unreadCount: () => unreadMock(),
    markRead: (id: string) => markReadMock(id),
    markAllRead: () => markAllReadMock(),
  },
}));

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

const renderBell = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

beforeEach(() => {
  listMock.mockReset();
  unreadMock.mockReset();
  markReadMock.mockReset();
  markAllReadMock.mockReset();
  useAuthStore.getState().setAuth('token', new Date(Date.now() + 3_600_000).toISOString(), {
    id: 'u-1',
    tenantId: 't-1',
    tenantName: 'Tenant 1',
    tenantSlug: 'tenant-1',
    username: 'user',
    email: 'user@example.com',
    firstName: null,
    lastName: null,
    avatarUrl: null,
    roles: ['User'],
    persona: 'customer',
  });
});

describe('NotificationBell', () => {
  it('shows the unread badge when count is positive', async () => {
    unreadMock.mockResolvedValue(3);
    listMock.mockResolvedValue([]);
    renderBell();
    await waitFor(() => expect(screen.getByText('3')).toBeInTheDocument());
  });

  it('does not show a badge when unread count is zero', async () => {
    unreadMock.mockResolvedValue(0);
    listMock.mockResolvedValue([]);
    renderBell();
    await waitFor(() => expect(unreadMock).toHaveBeenCalled());
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('caps the badge at 99+', async () => {
    unreadMock.mockResolvedValue(420);
    listMock.mockResolvedValue([]);
    renderBell();
    await waitFor(() => expect(screen.getByText('99+')).toBeInTheDocument());
  });

  it('mark-all-read button is disabled when unread is zero', async () => {
    unreadMock.mockResolvedValue(0);
    listMock.mockResolvedValue([]);
    renderBell();
    await userEvent.click(screen.getByRole('button'));
    const markAll = await screen.findByRole('button', { name: /mark all read|tümünü okundu/i });
    expect(markAll).toBeDisabled();
  });

  it('invokes markAllRead when clicked with unread > 0', async () => {
    unreadMock.mockResolvedValue(2);
    listMock.mockResolvedValue([]);
    markAllReadMock.mockResolvedValue(undefined);
    renderBell();
    await waitFor(() => expect(screen.getByText('2')).toBeInTheDocument());
    await userEvent.click(screen.getAllByRole('button')[0]);
    const markAll = await screen.findByRole('button', { name: /mark all read|tümünü okundu/i });
    await userEvent.click(markAll);
    await waitFor(() => expect(markAllReadMock).toHaveBeenCalled());
  });
});
