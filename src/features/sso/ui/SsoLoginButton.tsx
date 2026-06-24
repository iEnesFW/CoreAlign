import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { KeyRound } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { env } from '@/shared/lib/env';
import { ssoApi } from '../api/ssoApi';

interface Props {
  defaultIdpName?: string;
  className?: string;
}

export const SsoLoginButton = ({ defaultIdpName = 'default', className }: Props) => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [tenantSlug, setTenantSlug] = useState('');
  const [idpName, setIdpName] = useState(defaultIdpName);
  const [protocol, setProtocol] = useState<'saml' | 'oidc'>('oidc');

  const startSso = () => {
    if (!tenantSlug.trim() || !idpName.trim()) return;
    const apiBaseUrl = env.VITE_API_URL ?? '';
    const returnUrl = `${window.location.origin}/auth/sso/callback`;
    const url = `${apiBaseUrl}${ssoApi.buildLoginUrl(tenantSlug.trim(), idpName.trim(), protocol, returnUrl)}`;
    window.location.href = url;
  };

  return (
    <div className={className}>
      {!open ? (
        <Button type="button" variant="secondary" className="w-full" onClick={() => setOpen(true)}>
          <KeyRound size={16} className="mr-2" />
          {t('Sso.Login.OpenButton', { defaultValue: 'SSO ile Giriş' })}
        </Button>
      ) : (
        <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
          <Input
            label={t('Sso.Login.TenantSlug', { defaultValue: 'Tenant kodu' })}
            value={tenantSlug}
            onChange={(e) => setTenantSlug(e.target.value)}
            placeholder="acme"
          />
          <Input
            label={t('Sso.Login.IdpName', { defaultValue: 'Sağlayıcı adı' })}
            value={idpName}
            onChange={(e) => setIdpName(e.target.value)}
            placeholder="azure-ad"
          />
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setProtocol('oidc')}
              className={
                protocol === 'oidc'
                  ? 'flex-1 rounded-md bg-primary-600 px-3 py-2 text-xs font-semibold text-white'
                  : 'flex-1 rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 dark:border-slate-600 dark:text-slate-300'
              }
            >
              OpenID Connect
            </button>
            <button
              type="button"
              onClick={() => setProtocol('saml')}
              className={
                protocol === 'saml'
                  ? 'flex-1 rounded-md bg-primary-600 px-3 py-2 text-xs font-semibold text-white'
                  : 'flex-1 rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 dark:border-slate-600 dark:text-slate-300'
              }
            >
              SAML 2.0
            </button>
          </div>
          <div className="flex gap-2">
            <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
              {t('Common.Cancel', { defaultValue: 'İptal' })}
            </Button>
            <Button type="button" className="flex-1" onClick={startSso}>
              {t('Sso.Login.Continue', { defaultValue: 'Devam Et' })}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
