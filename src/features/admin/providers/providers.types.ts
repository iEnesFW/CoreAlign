export type ProviderCategory =
  | 'EFatura'
  | 'Payment'
  | 'LaserMeter'
  | 'LabelPrinter'
  | 'CncExport'
  | 'CadImport'
  | 'Freight'
  | 'BankReconciliation'
  | 'Calendar'
  | 'Export'
  | 'Sms'
  | 'WhatsApp';

export type ProviderHealthStatus =
  | 'Unknown'
  | 'Healthy'
  | 'Degraded'
  | 'Unhealthy'
  | 'NotConfigured';

export interface TenantProviderConfigDto {
  id: string;
  category: ProviderCategory;
  providerName: string;
  displayName: string | null;
  isDefault: boolean;
  isEnabled: boolean;
  enabledCapabilities: number;
  lastHealthCheckUtc: string | null;
  lastHealthStatus: ProviderHealthStatus;
  lastHealthMessage: string | null;
}

export interface UpsertTenantProviderConfigInput {
  category: ProviderCategory;
  providerName: string;
  displayName?: string | null;
  isDefault: boolean;
  isEnabled: boolean;
  plaintextCredentialsJson?: string | null;
  enabledCapabilities: number;
}
