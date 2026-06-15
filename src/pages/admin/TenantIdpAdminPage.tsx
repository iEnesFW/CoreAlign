import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { KeyRound, Plus } from 'lucide-react';
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
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header className="flex items-center gap-3">
        <KeyRound className="text-indigo-600 dark:text-indigo-400" size={20} />
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('Sso.Admin.Title', { defaultValue: 'SSO Kimlik Sağlayıcıları' })}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Sso.Admin.Subtitle', {
              defaultValue: 'SAML 2.0 / OpenID Connect entegrasyonlarını yönetin.',
            })}
          </p>
        </div>
      </header>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <aside className="space-y-3 lg:col-span-4">
          <button
            type="button"
            onClick={() => {
              setSelected(undefined);
              setCreating(true);
            }}
            className="flex w-full items-center justify-center gap-2 rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700"
          >
            <Plus size={14} />
            {t('Sso.Admin.New', { defaultValue: 'Yeni Sağlayıcı' })}
          </button>

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
                    ? 'flex w-full flex-col items-start gap-1 border-b border-slate-200 bg-indigo-50 p-3 text-left last:border-b-0 dark:border-slate-700 dark:bg-indigo-900/20'
                    : 'flex w-full flex-col items-start gap-1 border-b border-slate-200 p-3 text-left last:border-b-0 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800'
                }
              >
                <div className="flex w-full items-center justify-between">
                  <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                    {idp.name}
                  </span>
                  <span
                    className={
                      idp.isActive
                        ? 'rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-medium text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300'
                        : 'rounded-full bg-slate-200 px-2 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                    }
                  >
                    {idp.isActive ? 'Active' : 'Inactive'}
                  </span>
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
    </div>
  );
};

export default TenantIdpAdminPage;
