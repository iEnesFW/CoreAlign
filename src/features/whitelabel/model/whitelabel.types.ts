export type TenantThemeAssetKind = 'Logo' | 'Favicon' | 'LoginBackground' | 'EmailHeader';

export interface TenantThemeDto {
  tenantId: string;
  primaryColor: string;
  accentColor: string;
  brandName?: string | null;
  customSubdomain?: string | null;
  customDomain?: string | null;
  emailFromName: string;
  emailFromAddress?: string | null;
  loginHeadingMd?: string | null;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  loginBackgroundUrl?: string | null;
  concurrencyToken: number;
}

export interface PublicTenantThemeDto {
  tenantId: string;
  primaryColor: string;
  accentColor: string;
  brandName?: string | null;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  loginBackgroundUrl?: string | null;
  loginHeadingMd?: string | null;
}

export interface UpdateTenantThemeInput {
  primaryColor: string;
  accentColor: string;
  brandName?: string | null;
  customSubdomain?: string | null;
  customDomain?: string | null;
  emailFromName: string;
  emailFromAddress?: string | null;
  loginHeadingMd?: string | null;
}

export interface TenantThemeAssetDto {
  id: string;
  kind: TenantThemeAssetKind;
  contentType: string;
  sizeBytes: number;
  publicUrl?: string | null;
  createdAtUtc: string;
}
