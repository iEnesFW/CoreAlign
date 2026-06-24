import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, KeyRound } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { env } from '@/shared/lib/env';
import { ssoApi } from '@/features/sso/api/ssoApi';
import styles from './LoginForm.module.css';

interface Props {
  onBack: () => void;
  defaultIdpName?: string;
}

export const SsoLoginFormView = ({ onBack, defaultIdpName = 'default' }: Props) => {
  const { t } = useTranslation();
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
    <div className={`${styles.ssoView} animate-in fade-in slide-in-from-right-4 duration-300`}>
      <div className={styles.ssoHeader}>
        <button
          type="button"
          onClick={onBack}
          className={styles.backButton}
          aria-label={t('Sso.Login.Back', { defaultValue: 'Back' })}
        >
          <ArrowLeft size={18} />
        </button>
        <div className="flex items-center gap-2">
          <KeyRound size={18} className="text-primary-500" />
          <h3 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t('Sso.Login.OpenButton', { defaultValue: 'SSO ile Giriş' })}
          </h3>
        </div>
      </div>

      <div className={styles.fields}>
        <Input
          label={t('Sso.Login.TenantSlug', { defaultValue: 'Tenant Kodu' })}
          value={tenantSlug}
          onChange={(e) => setTenantSlug(e.target.value)}
          placeholder={t('Sso.Login.TenantSlugPlaceholder', { defaultValue: 'Örn: acme' })}
          autoFocus
        />
        <Input
          label={t('Sso.Login.IdpName', { defaultValue: 'Sağlayıcı Adı' })}
          value={idpName}
          onChange={(e) => setIdpName(e.target.value)}
          placeholder={t('Sso.Login.IdpNamePlaceholder', { defaultValue: 'Örn: azure-ad, okta' })}
        />

        <div className="flex gap-2 mt-1">
          <button
            type="button"
            onClick={() => setProtocol('oidc')}
            className={`flex-1 rounded-md px-3 py-2 text-xs font-semibold transition-all ${
              protocol === 'oidc'
                ? 'bg-primary-600 text-white shadow-md shadow-primary-500/20'
                : 'border border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
            }`}
          >
            OpenID Connect
          </button>
          <button
            type="button"
            onClick={() => setProtocol('saml')}
            className={`flex-1 rounded-md px-3 py-2 text-xs font-semibold transition-all ${
              protocol === 'saml'
                ? 'bg-primary-600 text-white shadow-md shadow-primary-500/20'
                : 'border border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
            }`}
          >
            SAML 2.0
          </button>
        </div>
      </div>

      <div className="mt-6">
        <Button
          type="button"
          className="w-full"
          onClick={startSso}
          disabled={!tenantSlug.trim() || !idpName.trim()}
        >
          {t('Sso.Login.Continue', { defaultValue: 'Devam Et' })}
        </Button>
      </div>
    </div>
  );
};
