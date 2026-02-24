import { useMutation } from '@tanstack/react-query';
import { useCallback } from 'react';
import { authApi } from '../api/authApi';
import { useAuthStore } from '../model/authStore';
import type {
    ForgotPasswordRequest,
    LoginRequest,
    RegisterRequest,
    ResetPasswordRequest,
    VerifyEmailRequest,
} from '../model/auth.types';

export const useLogin = () => {
    const setAuth = useAuthStore((state) => state.setAuth);

    return useMutation({
        mutationFn: (data: LoginRequest) => authApi.login(data),
        onSuccess: (response) => {
            if (response.isSuccess && response.data) {
                setAuth(response.data.accessToken, response.data.refreshToken, response.data.user);
            }
        },
    });
};

export const useRegister = () => {
    return useMutation({
        mutationFn: (data: RegisterRequest) => authApi.register(data),
    });
};

export const useForgotPassword = () => {
    return useMutation({
        mutationFn: (data: ForgotPasswordRequest) => authApi.forgotPassword(data),
    });
};

export const useResetPassword = () => {
    return useMutation({
        mutationFn: (data: ResetPasswordRequest) => authApi.resetPassword(data),
    });
};

export const useVerifyEmail = () => {
    return useMutation({
        mutationFn: (data: VerifyEmailRequest) => authApi.verifyEmail(data),
    });
};

export const useLogout = () => {
    const clearAuth = useAuthStore((state) => state.clearAuth);
    const refreshToken = useAuthStore((state) => state.refreshToken);

    return useMutation({
        mutationFn: useCallback(() => {
            if (!refreshToken) return Promise.resolve(null);
            return authApi.logout({ refreshToken });
        }, [refreshToken]),
        onSettled: () => {
            clearAuth();
        },
    });
};
