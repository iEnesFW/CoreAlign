export type { ApiResponse } from '@/shared/types/api';

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface UserProfile {
  id: string;
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
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
  organizationName: string;
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

export interface UpdateProfileRequest {
  firstName?: string | null;
  lastName?: string | null;
  phoneNumber?: string | null;
  avatarUrl?: string | null;
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
