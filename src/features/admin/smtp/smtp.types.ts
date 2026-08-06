export type SmtpAuthMode = 'Password' | 'OAuth2';

export type SmtpOAuthProvider = 'Google' | 'Microsoft' | 'Custom';

export interface SmtpSettings {
  isConfigured: boolean;
  isEnabled: boolean;
  host: string;
  port: number;
  useSsl: boolean;
  username: string | null;
  fromAddress: string | null;
  fromName: string | null;
  hasPassword: boolean;
  lastHealthStatus: string | null;
  lastHealthCheckUtc: string | null;
  authMode: SmtpAuthMode;
  oAuthProvider: SmtpOAuthProvider | null;
  oAuthTenantId: string | null;
  oAuthClientId: string | null;
  oAuthTokenEndpoint: string | null;
  oAuthScope: string | null;
  hasOAuthClientSecret: boolean;
  hasOAuthRefreshToken: boolean;
}

export interface UpsertSmtpInput {
  host: string;
  port: number;
  useSsl: boolean;
  username?: string | null;
  password?: string | null;
  fromAddress?: string | null;
  fromName?: string | null;
  isEnabled: boolean;
  authMode?: SmtpAuthMode | null;
  oAuthProvider?: SmtpOAuthProvider | null;
  oAuthTenantId?: string | null;
  oAuthClientId?: string | null;
  oAuthClientSecret?: string | null;
  oAuthRefreshToken?: string | null;
  oAuthTokenEndpoint?: string | null;
  oAuthScope?: string | null;
}

export interface SmtpTestResult {
  success: boolean;
  message: string | null;
}

export interface SmtpHealthResult {
  isHealthy: boolean;
  message: string | null;
  checkedAtUtc: string;
}
