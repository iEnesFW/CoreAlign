import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { KeyRound, Plus } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useSsoIdentityProviders } from '@/features/sso/hooks/useSsoQueries';
import { TenantIdpEditor } from '@/features/sso/ui/TenantIdpEditor';
import type { SsoIdentityProviderDto } from '@/features/sso/model/sso.types';

export const TenantIdpAdminPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useSsoIdentityProviders();
  const [selected, setSelected] = useState<SsoIdentityProviderDto | undefined>();
  const [creating, setCreating] = useState(false);

  const providers = data?.isSuccess ? (data.data ?? []) : [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<KeyRound size={20} />}
          title={t('Sso.Admin.Title', { defaultValue: 'SSO Kimlik Sağlayıcıları' })}
          subtitle={t('Sso.Admin.Subtitle', {
            defaultValue: 'SAML 2.0 / OpenID Connect entegrasyonlarını yönetin.',
          })}
          actions={
            <Button
              size="sm"
              onClick={() => {
                setSelected(undefined);
                setCreating(true);
              }}
            >
              <Plus size={14} />
              {t('Sso.Admin.New', { defaultValue: 'Yeni Sağlayıcı' })}
            </Button>
          }
        />
      }
    >
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <aside className="space-y-3 lg:col-span-4">
          <div className="rounded-lg border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
            {isLoading && (
              <div className="p-4 text-sm text-slate-500 dark:text-slate-400">
                {t('Common.Loading', { defaultValue: 'Yükleniyor...' })}
              </div>
            )}
            {!isLoading && providers.length === 0 && (
              <div className="p-4 text-sm text-slate-500 dark:text-slate-400">
                {t('Sso.Admin.Empty', { defaultValue: 'Henüz tanımlı sağlayıcı yok.' })}
              </div>
            )}
            {providers.map((idp) => (
              <button
                key={idp.id}
                type="button"
                onClick={() => {
                  setSelected(idp);
                  setCreating(false);
                }}
                className={
                  selected?.id === idp.id
                    ? 'flex w-full flex-col items-start gap-1 border-b border-slate-200 bg-primary-50 p-3 text-left last:border-b-0 dark:border-slate-700 dark:bg-primary-900/20'
                    : 'flex w-full flex-col items-start gap-1 border-b border-slate-200 p-3 text-left last:border-b-0 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800'
                }
              >
                <div className="flex w-full items-center justify-between">
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {idp.name}
                  </span>
                  <Badge variant={idp.isActive ? 'success' : 'neutral'} pill>
                    {idp.isActive ? 'Active' : 'Inactive'}
                  </Badge>
                </div>
                <span className="text-xs text-slate-500 dark:text-slate-400">{idp.protocol}</span>
              </button>
            ))}
          </div>
        </aside>

        <section className="lg:col-span-8">
          {(selected || creating) && (
            <TenantIdpEditor
              initial={selected}
              onSaved={() => {
                setCreating(false);
              }}
            />
          )}
          {!selected && !creating && (
            <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-800/40 dark:text-slate-400">
              {t('Sso.Admin.SelectHint', {
                defaultValue: 'Bir sağlayıcı seçin veya yeni oluşturun.',
              })}
            </div>
          )}
        </section>
      </div>
    </ListPageTemplate>
  );
};

export default TenantIdpAdminPage;
