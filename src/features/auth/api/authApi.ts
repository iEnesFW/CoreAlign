import { apiClient } from '@/shared/api/apiClient';
import type {
    ApiResponse,
    AuthResponse,
    ForgotPasswordRequest,
    LoginRequest,
    LogoutRequest,
    RefreshTokenRequest,
    RegisterRequest,
    ResetPasswordRequest,
    VerifyEmailRequest,
} from '../model/auth.types';

const AUTH_BASE = '/auth';

export const authApi = {
    login: (data: LoginRequest) =>
        apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/login`, data).then((r) => r.data),

    register: (data: RegisterRequest) =>
        apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/register`, data).then((r) => r.data),

    refreshToken: (data: RefreshTokenRequest) =>
        apiClient.post<ApiResponse<AuthResponse>>(`${AUTH_BASE}/refresh-token`, data).then((r) => r.data),

    forgotPassword: (data: ForgotPasswordRequest) =>
        apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/forgot-password`, data).then((r) => r.data),

    resetPassword: (data: ResetPasswordRequest) =>
        apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/reset-password`, data).then((r) => r.data),

    verifyEmail: (data: VerifyEmailRequest) =>
        apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/verify-email`, data).then((r) => r.data),

    logout: (data: LogoutRequest) =>
        apiClient.post<ApiResponse<boolean>>(`${AUTH_BASE}/logout`, data).then((r) => r.data),

    getCurrentUser: () =>
        apiClient.get<ApiResponse<AuthResponse['user']>>(`${AUTH_BASE}/me`).then((r) => r.data),
};
