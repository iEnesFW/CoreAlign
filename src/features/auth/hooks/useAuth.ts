import { useMutation, useQuery } from '@tanstack/react-query';
import { authApi } from '../api/authApi';
import { useAuthStore } from '../model/authStore';
import type {
    ChangePasswordRequest,
    ForgotPasswordRequest,
    LoginRequest,
    RegisterRequest,
    ResetPasswordRequest,
    VerifyEmailRequest,
} from '../model/auth.types';

export const useLogin = () => {
    const setAuth = useAuthStore((s) => s.setAuth);

    return useMutation({
        mutationFn: (data: LoginRequest) => authApi.login(data),
        onSuccess: (response) => {
            if (response.isSuccess && response.data) {
                setAuth(response.data.accessToken, response.data.user);
            }
        },
    });
};

export const useRegister = () =>
    useMutation({
        mutationFn: (data: RegisterRequest) => authApi.register(data),
    });

export const useForgotPassword = () =>
    useMutation({
        mutationFn: (data: ForgotPasswordRequest) => authApi.forgotPassword(data),
    });

export const useResetPassword = () =>
    useMutation({
        mutationFn: (data: ResetPasswordRequest) => authApi.resetPassword(data),
    });

export const useVerifyEmail = () =>
    useMutation({
        mutationFn: (data: VerifyEmailRequest) => authApi.verifyEmail(data),
    });

export const useLogout = () => {
    const clearAuth = useAuthStore((s) => s.clearAuth);

    return useMutation({
        mutationFn: () => authApi.logout(),
        onSettled: () => {
            clearAuth();
        },
    });
};

export const useChangePassword = () =>
    useMutation({
        mutationFn: (data: ChangePasswordRequest) => authApi.changePassword(data),
    });

export const useSessions = () =>
    useQuery({
        queryKey: ['auth', 'sessions'],
        queryFn: () => authApi.getSessions(),
    });

export const useRevokeSession = () =>
    useMutation({
        mutationFn: (sessionId: string) => authApi.revokeSession(sessionId),
    });

export const useRevokeAllSessions = () => {
    const clearAuth = useAuthStore((s) => s.clearAuth);

    return useMutation({
        mutationFn: () => authApi.revokeAllSessions(),
        onSuccess: () => {
            clearAuth();
        },
    });
};

export const useLoginHistory = (count = 20) =>
    useQuery({
        queryKey: ['auth', 'loginHistory', count],
        queryFn: () => authApi.getLoginHistory(count),
    });
