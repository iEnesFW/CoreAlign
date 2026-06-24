import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save, FlaskConical, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import type { SsoIdentityProviderDto, SsoProtocol } from '../model/sso.types';
import {
  useCreateSsoIdentityProvider,
  useDeleteSsoIdentityProvider,
  useTestSsoConnection,
  useUpdateSsoIdentityProvider,
} from '../hooks/useSsoQueries';

interface Props {
  initial?: SsoIdentityProviderDto;
  onSaved?: () => void;
}

interface FormState {
  name: string;
  protocol: SsoProtocol;
  entityIdOrClientId: string;
  metadataUrl: string;
  discoveryDocumentUrl: string;
  clientSecret: string;
  attributeMappingsJson: string;
  isActive: boolean;
}

const emptyForm: FormState = {
  name: '',
  protocol: 'Oidc',
  entityIdOrClientId: '',
  metadataUrl: '',
  discoveryDocumentUrl: '',
  clientSecret: '',
  attributeMappingsJson: '{"email":"email","firstName":"given_name","lastName":"family_name"}',
  isActive: true,
};

export const TenantIdpEditor = ({ initial, onSaved }: Props) => {
  const { t } = useTranslation();
  const [form, setForm] = useState<FormState>(() => fromInitial(initial));
  const createMutation = useCreateSsoIdentityProvider();
  const updateMutation = useUpdateSsoIdentityProvider();
  const deleteMutation = useDeleteSsoIdentityProvider();
  const testMutation = useTestSsoConnection();

  useEffect(() => {
    setForm(fromInitial(initial));
  }, [initial]);

  const isEdit = Boolean(initial?.id);

  const handleSave = () => {
    if (!form.name.trim() || !form.entityIdOrClientId.trim()) {
      toast.error(
        t('Sso.Admin.Validation.Required', { defaultValue: 'Zorunlu alanları doldurun.' }),
      );
      return;
    }

    if (form.protocol === 'Oidc' && !form.discoveryDocumentUrl.trim()) {
      toast.error(
        t('Sso.Admin.DiscoveryUrlRequired', {
          defaultValue: 'Discovery URL alanı OpenID Connect için zorunludur.',
        }),
      );
      return;
    }

    if (form.protocol === 'Saml' && !form.metadataUrl.trim()) {
      toast.error(
        t('Sso.Admin.MetadataUrlRequired', {
          defaultValue: 'Metadata URL alanı SAML 2.0 için zorunludur.',
        }),
      );
      return;
    }
    const payload = {
      name: form.name.trim(),
      protocol: form.protocol,
      entityIdOrClientId: form.entityIdOrClientId.trim(),
      metadataUrl: form.metadataUrl.trim() || null,
      discoveryDocumentUrl: form.discoveryDocumentUrl.trim() || null,
      clientSecret: form.clientSecret.trim() || null,
      attributeMappingsJson: form.attributeMappingsJson.trim() || null,
    };

    if (isEdit && initial) {
      updateMutation.mutate(
        { id: initial.id, body: { ...payload, isActive: form.isActive } },
        {
          onSuccess: () => {
            toast.success(t('Sso.Admin.Saved', { defaultValue: 'Sağlayıcı güncellendi.' }));
            onSaved?.();
          },
          onError: (e) =>
            toastApiError(e, t('Sso.Admin.SaveFailed', { defaultValue: 'Kayıt başarısız.' })),
        },
      );
    } else {
      createMutation.mutate(payload, {
        onSuccess: () => {
          toast.success(t('Sso.Admin.Created', { defaultValue: 'Sağlayıcı oluşturuldu.' }));
          setForm(emptyForm);
          onSaved?.();
        },
        onError: (e) =>
          toastApiError(e, t('Sso.Admin.SaveFailed', { defaultValue: 'Kayıt başarısız.' })),
      });
    }
  };

  const handleTest = () => {
    if (!initial?.id) {
      toast.error(t('Sso.Admin.TestRequiresSave', { defaultValue: 'Önce kayıt edin.' }));
      return;
    }
    testMutation.mutate(initial.id, {
      onSuccess: (response) => {
        const testFailed = t('Sso.Admin.TestConnectionFailed', {
          defaultValue: 'Bağlantı testi başarısız.',
        });
        if (!response.isSuccess) {
          toast.error(response.errors?.[0] ?? testFailed);
          return;
        }
        if (response.data?.success) {
          toast.success(
            response.data.message ??
              t('Sso.Admin.TestConnectionSuccess', { defaultValue: 'Bağlantı başarılı.' }),
          );
        } else {
          toast.error(response.data?.message ?? testFailed);
        }
      },
      onError: (e) =>
        toastApiError(e, t('Sso.Admin.TestFailed', { defaultValue: 'Test başarısız.' })),
    });
  };

  const handleDelete = () => {
    if (!initial?.id) return;
    deleteMutation.mutate(initial.id, {
      onSuccess: () => {
        toast.success(t('Sso.Admin.Deleted', { defaultValue: 'Sağlayıcı silindi.' }));
        onSaved?.();
      },
      onError: (e) =>
        toastApiError(e, t('Sso.Admin.DeleteFailed', { defaultValue: 'Silme başarısız.' })),
    });
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-4 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Input
          label={t('Sso.Admin.Name', { defaultValue: 'İsim' })}
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">
            {t('Sso.Admin.Protocol', { defaultValue: 'Protokol' })}
          </label>
          <select
            className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-600 dark:bg-slate-800"
            value={form.protocol}
            onChange={(e) => setForm({ ...form, protocol: e.target.value as SsoProtocol })}
          >
            <option value="Oidc">OpenID Connect</option>
            <option value="Saml">SAML 2.0</option>
          </select>
        </div>
        <Input
          label={t('Sso.Admin.EntityIdOrClientId', { defaultValue: 'EntityId / ClientId' })}
          value={form.entityIdOrClientId}
          onChange={(e) => setForm({ ...form, entityIdOrClientId: e.target.value })}
        />
        <Input
          label={
            form.protocol === 'Saml'
              ? t('Sso.Admin.MetadataUrlLabel', { defaultValue: 'Metadata URL' })
              : t('Sso.Admin.DiscoveryUrlLabel', { defaultValue: 'Discovery URL' })
          }
          value={form.protocol === 'Saml' ? form.metadataUrl : form.discoveryDocumentUrl}
          onChange={(e) =>
            setForm(
              form.protocol === 'Saml'
                ? { ...form, metadataUrl: e.target.value }
                : { ...form, discoveryDocumentUrl: e.target.value },
            )
          }
        />
        {form.protocol === 'Oidc' && (
          <Input
            label={t('Sso.Admin.ClientSecret', { defaultValue: 'Client Secret' })}
            type="password"
            value={form.clientSecret}
            onChange={(e) => setForm({ ...form, clientSecret: e.target.value })}
          />
        )}
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">
          {t('Sso.Admin.AttributeMappings', { defaultValue: 'Attribute Mappings (JSON)' })}
        </label>
        <textarea
          className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 font-mono text-xs dark:border-slate-600 dark:bg-slate-800"
          rows={4}
          value={form.attributeMappingsJson}
          onChange={(e) => setForm({ ...form, attributeMappingsJson: e.target.value })}
        />
      </div>

      <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
        <input
          type="checkbox"
          checked={form.isActive}
          onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
        />
        {t('Sso.Admin.IsActive', { defaultValue: 'Aktif' })}
      </label>

      <div className="flex flex-wrap gap-2">
        <Button type="button" onClick={handleSave} isLoading={isSaving}>
          <Save size={14} className="mr-1" />
          {isEdit
            ? t('Sso.Admin.UpdateButton', { defaultValue: 'Güncelle' })
            : t('Sso.Admin.CreateButton', { defaultValue: 'Oluştur' })}
        </Button>
        {isEdit && (
          <>
            <Button
              type="button"
              variant="secondary"
              onClick={handleTest}
              isLoading={testMutation.isPending}
            >
              <FlaskConical size={14} className="mr-1" />
              {t('Sso.Admin.TestButton', { defaultValue: 'Bağlantı Testi' })}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={handleDelete}
              isLoading={deleteMutation.isPending}
            >
              <Trash2 size={14} className="mr-1" />
              {t('Sso.Admin.DeleteButton', { defaultValue: 'Sil' })}
            </Button>
          </>
        )}
      </div>
    </div>
  );
};

const fromInitial = (initial?: SsoIdentityProviderDto): FormState => ({
  name: initial?.name ?? '',
  protocol: (initial?.protocol as SsoProtocol) ?? 'Oidc',
  entityIdOrClientId: initial?.entityIdOrClientId ?? '',
  metadataUrl: initial?.metadataUrl ?? '',
  discoveryDocumentUrl: initial?.discoveryDocumentUrl ?? '',
  clientSecret: '',
  attributeMappingsJson:
    initial?.attributeMappingsJson ??
    '{"email":"email","firstName":"given_name","lastName":"family_name"}',
  isActive: initial?.isActive ?? true,
});
