export type SsoProtocol = 'Saml' | 'Oidc';

export interface SsoIdentityProviderDto {
  id: string;
  tenantId: string;
  name: string;
  protocol: SsoProtocol;
  entityIdOrClientId: string;
  metadataUrl?: string | null;
  discoveryDocumentUrl?: string | null;
  attributeMappingsJson: string;
  isActive: boolean;
  lastUsedAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateSsoIdentityProviderRequest {
  name: string;
  protocol: SsoProtocol;
  entityIdOrClientId: string;
  metadataUrl?: string | null;
  discoveryDocumentUrl?: string | null;
  clientSecret?: string | null;
  attributeMappingsJson?: string | null;
}

export interface UpdateSsoIdentityProviderRequest {
  name: string;
  entityIdOrClientId: string;
  metadataUrl?: string | null;
  discoveryDocumentUrl?: string | null;
  clientSecret?: string | null;
  attributeMappingsJson?: string | null;
  isActive: boolean;
}

export interface SsoTestConnectionResult {
  success: boolean;
  message?: string | null;
}
