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
