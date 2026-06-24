import { createContext, useContext } from 'react';

export interface TenantThemeContextValue {
  primaryColor: string;
  accentColor: string;
  brandName?: string | null;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  loginBackgroundUrl?: string | null;
  loginHeadingMd?: string | null;
}

export const DEFAULT_TENANT_THEME: TenantThemeContextValue = {
  primaryColor: '#6366f1',
  accentColor: '#22D3EE',
  brandName: null,
  logoUrl: null,
  faviconUrl: null,
  loginBackgroundUrl: null,
  loginHeadingMd: null,
};

export const TenantThemeContext = createContext<TenantThemeContextValue>(DEFAULT_TENANT_THEME);

export const useTenantTheme = (): TenantThemeContextValue => useContext(TenantThemeContext);
