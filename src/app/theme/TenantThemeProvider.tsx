import { createContext, useContext, useEffect, useMemo } from 'react';
import { useAuthStore } from '@/features/auth/model/authStore';
import {
  usePublicThemeQuery,
  useTenantThemeQuery,
} from '@/features/whitelabel/hooks/useTenantTheme';
import type {
  PublicTenantThemeDto,
  TenantThemeDto,
} from '@/features/whitelabel/model/whitelabel.types';

interface TenantThemeContextValue {
  primaryColor: string;
  accentColor: string;
  brandName?: string | null;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  loginBackgroundUrl?: string | null;
  loginHeadingMd?: string | null;
}

const DEFAULT_VALUE: TenantThemeContextValue = {
  primaryColor: '#0EA5E9',
  accentColor: '#22D3EE',
  brandName: null,
  logoUrl: null,
  faviconUrl: null,
  loginBackgroundUrl: null,
  loginHeadingMd: null,
};

const TenantThemeContext = createContext<TenantThemeContextValue>(DEFAULT_VALUE);

const extractSubdomain = (): string | undefined => {
  if (typeof window === 'undefined') return undefined;
  const host = window.location.hostname;
  if (!host || host === 'localhost') return undefined;
  if (/^\d+\.\d+\.\d+\.\d+$/.test(host)) return undefined;
  const parts = host.split('.');
  if (parts.length < 3) return undefined;
  const sub = parts[0];
  if (sub === 'www' || sub === 'api') return undefined;
  return sub.toLowerCase();
};

const applyThemeVariables = (value: TenantThemeContextValue) => {
  if (typeof document === 'undefined') return;
  const root = document.documentElement;
  root.style.setProperty('--color-primary', value.primaryColor);
  root.style.setProperty('--color-accent', value.accentColor);
  if (value.logoUrl) {
    root.style.setProperty('--logo-url', `url("${value.logoUrl}")`);
  } else {
    root.style.removeProperty('--logo-url');
  }
  root.setAttribute('data-tenant-theme', value.brandName ? 'custom' : 'default');
  if (value.faviconUrl) {
    let link = document.querySelector("link[rel='icon']") as HTMLLinkElement | null;
    if (!link) {
      link = document.createElement('link');
      link.rel = 'icon';
      document.head.appendChild(link);
    }
    link.href = value.faviconUrl;
  }
};

const fromAuthenticated = (data: TenantThemeDto | undefined): TenantThemeContextValue | null => {
  if (!data) return null;
  return {
    primaryColor: data.primaryColor,
    accentColor: data.accentColor,
    brandName: data.brandName,
    logoUrl: data.logoUrl,
    faviconUrl: data.faviconUrl,
    loginBackgroundUrl: data.loginBackgroundUrl,
    loginHeadingMd: data.loginHeadingMd,
  };
};

const fromPublic = (data: PublicTenantThemeDto | undefined): TenantThemeContextValue | null => {
  if (!data) return null;
  return {
    primaryColor: data.primaryColor,
    accentColor: data.accentColor,
    brandName: data.brandName,
    logoUrl: data.logoUrl,
    faviconUrl: data.faviconUrl,
    loginBackgroundUrl: data.loginBackgroundUrl,
    loginHeadingMd: data.loginHeadingMd,
  };
};

export const TenantThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const accessToken = useAuthStore((state) => state.accessToken);
  const isAuthenticated = Boolean(accessToken);
  const subdomain = useMemo(() => extractSubdomain(), []);

  const authedQuery = useTenantThemeQuery(isAuthenticated);
  const publicQuery = usePublicThemeQuery(!isAuthenticated ? subdomain : undefined);

  const value = useMemo<TenantThemeContextValue>(() => {
    const resolved = isAuthenticated
      ? fromAuthenticated(authedQuery.data)
      : fromPublic(publicQuery.data);
    return resolved ?? DEFAULT_VALUE;
  }, [isAuthenticated, authedQuery.data, publicQuery.data]);

  useEffect(() => {
    applyThemeVariables(value);
  }, [value]);

  return <TenantThemeContext.Provider value={value}>{children}</TenantThemeContext.Provider>;
};

// eslint-disable-next-line react-refresh/only-export-components
export const useTenantTheme = (): TenantThemeContextValue => useContext(TenantThemeContext);
