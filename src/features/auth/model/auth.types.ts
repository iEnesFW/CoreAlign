export interface ApiResponse<T> {
    isSuccess: boolean;
    data: T | null;
    errors: string[];
    statusCode: number;
}

export interface AuthResponse {
    accessToken: string;
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
    rememberMe?: boolean;
    captchaToken?: string;
    deviceFingerprint?: string;
}

export interface RegisterRequest {
    username: string;
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
    captchaToken?: string;
}

export interface ForgotPasswordRequest {
    email: string;
    captchaToken?: string;
}

export interface ResetPasswordRequest {
    token: string;
    newPassword: string;
}

export interface VerifyEmailRequest {
    token: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface SessionInfo {
    id: string;
    deviceInfo: string | null;
    ipAddress: string | null;
    createdAtUtc: string;
    lastActivityAtUtc: string;
    isCurrent: boolean;
}

export interface LoginHistoryEntry {
    ipAddress: string | null;
    userAgent: string | null;
    deviceFingerprint: string | null;
    loginResult: string;
    failureReason: string | null;
    attemptedAtUtc: string;
}
