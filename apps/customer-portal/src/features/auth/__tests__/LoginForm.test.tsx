import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import * as ReactRouterDom from 'react-router-dom';
import { LoginForm } from '@/features/auth/LoginForm';
import i18n from '@/app/i18n';

const loginMock = vi.fn();
const challengeMock = vi.fn();
const setAuthMock = vi.fn();
const clearAuthMock = vi.fn();
const navigateMock = vi.fn();

vi.mock('@/features/auth/loginApi', () => ({
  login: (...args: unknown[]) => loginMock(...args),
  completeTwoFactorChallenge: (...args: unknown[]) => challengeMock(...args),
}));

vi.mock('@/features/auth/authStore', () => ({
  useAuthStore: (selector: (s: unknown) => unknown) =>
    selector({ setAuth: setAuthMock, clearAuth: clearAuthMock }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof ReactRouterDom>('react-router-dom');
  return { ...actual, useNavigate: () => navigateMock };
});

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

beforeEach(() => {
  loginMock.mockReset();
  challengeMock.mockReset();
  setAuthMock.mockReset();
  clearAuthMock.mockReset();
  navigateMock.mockReset();
});

const renderForm = () => {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <ReactRouterDom.MemoryRouter>
        <LoginForm />
      </ReactRouterDom.MemoryRouter>
    </QueryClientProvider>,
  );
};

describe('LoginForm', () => {
  it('logs in directly when 2FA is not required', async () => {
    loginMock.mockResolvedValue({
      accessToken: 'tok',
      expiresAt: '2099-01-01T00:00:00Z',
      user: { persona: 'customer' },
    });

    const { container } = renderForm();
    fireEvent.submit(container.querySelector('form')!);

    await waitFor(() =>
      expect(setAuthMock).toHaveBeenCalledWith(
        'tok',
        expect.any(String),
        expect.objectContaining({ persona: 'customer' }),
      ),
    );
    expect(navigateMock).toHaveBeenCalledWith('/', { replace: true });
    expect(challengeMock).not.toHaveBeenCalled();
  });

  it('shows the 2FA challenge and completes login on a valid code', async () => {
    loginMock.mockResolvedValue({
      accessToken: '',
      expiresAt: '2099-01-01T00:00:00Z',
      user: null,
      requiresTwoFactor: true,
      twoFactorChallengeToken: 'chal-1',
    });
    challengeMock.mockResolvedValue({
      accessToken: 'tok',
      expiresAt: '2099-01-01T00:00:00Z',
      user: { persona: 'customer' },
    });

    const { container } = renderForm();
    fireEvent.submit(container.querySelector('form')!);

    // Challenge screen appears and we are NOT yet authenticated.
    await screen.findByText('Enter the 6-digit code from your authenticator app.');
    expect(setAuthMock).not.toHaveBeenCalled();

    fireEvent.submit(container.querySelector('form')!);

    await waitFor(() =>
      expect(challengeMock).toHaveBeenCalledWith(
        'chal-1',
        expect.objectContaining({ code: expect.any(String) }),
      ),
    );
    await waitFor(() =>
      expect(setAuthMock).toHaveBeenCalledWith(
        'tok',
        expect.any(String),
        expect.objectContaining({ persona: 'customer' }),
      ),
    );
  });
});
