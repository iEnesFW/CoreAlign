import { describe, expect, it, vi, afterEach } from 'vitest';
import { apiClient } from '@/shared/api/apiClient';
import { portalNotificationsApi } from '@/features/portal/notificationsApi';

describe('portalNotificationsApi', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('list builds the correct URL with default parameters', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] } as never);
    await portalNotificationsApi.list();
    expect(getSpy).toHaveBeenCalledWith(
      '/customer-portal/notifications',
      expect.objectContaining({ params: { unreadOnly: false, take: 30 } }),
    );
  });

  it('list forwards unreadOnly + take overrides', async () => {
    const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValue({ data: [] } as never);
    await portalNotificationsApi.list(true, 5);
    expect(getSpy).toHaveBeenCalledWith(
      '/customer-portal/notifications',
      expect.objectContaining({ params: { unreadOnly: true, take: 5 } }),
    );
  });

  it('unreadCount returns the numeric data envelope', async () => {
    vi.spyOn(apiClient, 'get').mockResolvedValue({ data: 4 } as never);
    const n = await portalNotificationsApi.unreadCount();
    expect(n).toBe(4);
  });

  it('markRead posts to the correct id-scoped endpoint', async () => {
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: true } as never);
    const ok = await portalNotificationsApi.markRead('abc');
    expect(postSpy).toHaveBeenCalledWith('/customer-portal/notifications/abc/read');
    expect(ok).toBe(true);
  });

  it('markAllRead posts and returns the count', async () => {
    const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValue({ data: 7 } as never);
    const count = await portalNotificationsApi.markAllRead();
    expect(postSpy).toHaveBeenCalledWith('/customer-portal/notifications/read-all');
    expect(count).toBe(7);
  });
});
