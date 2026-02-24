export interface ApiResponse<T> {
    isSuccess: boolean;
    data: T | null;
    errors: string[];
    statusCode: number;
}

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: UserProfile;
}

export interface UserProfile {
    id: string;
    username: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    avatarUrl: string | null;
    roles: string[];
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    username: string;
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
}

export interface ForgotPasswordRequest {
    email: string;
}

export interface ResetPasswordRequest {
    token: string;
    newPassword: string;
}

export interface VerifyEmailRequest {
    token: string;
}

export interface RefreshTokenRequest {
    refreshToken: string;
}

export interface LogoutRequest {
    refreshToken: string;
}
