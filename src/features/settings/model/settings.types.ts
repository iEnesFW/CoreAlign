export interface CompanyProfile {
  id: string;
  name: string;
  slug: string;
  legalName?: string | null;
  tradeName?: string | null;
  taxNumber?: string | null;
  taxOffice?: string | null;
  nationalId?: string | null;
  mersisNumber?: string | null;
  tradeRegistryNumber?: string | null;
  sector?: string | null;
  foundedOn?: string | null;
  logoUrl?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  stateProvince?: string | null;
  postalCode?: string | null;
  country?: string | null;
  phone?: string | null;
  fax?: string | null;
  email?: string | null;
  website?: string | null;
  defaultCurrency: string;
  reportingCurrency?: string | null;
  localeCode: string;
  timeZoneId: string;
  fiscalYearStartMonth: number;
  primaryColor?: string | null;
  secondaryColor?: string | null;
}

export type UpdateCompanyProfileRequest = Omit<CompanyProfile, 'id' | 'slug'>;

export interface TenantSetting {
  id: string;
  category: string;
  key: string;
  value?: string | null;
  dataType: string;
  description?: string | null;
  isSensitive: boolean;
}

export interface SettingUpsertItem {
  category: string;
  key: string;
  value?: string | null;
  dataType?: string;
  description?: string | null;
  isSensitive?: boolean;
}

export interface EmailTemplate {
  id: string;
  code: string;
  name: string;
  subject: string;
  body: string;
  locale: string;
  isActive: boolean;
  description?: string | null;
  availableVariables?: string | null;
  updatedAtUtc: string;
}

export interface CreateEmailTemplateRequest {
  code: string;
  name: string;
  subject: string;
  body: string;
  locale?: string;
  description?: string | null;
  availableVariables?: string | null;
}

export interface UpdateEmailTemplateRequest {
  id: string;
  name: string;
  subject: string;
  body: string;
  locale: string;
  description?: string | null;
  availableVariables?: string | null;
  isActive: boolean;
}
