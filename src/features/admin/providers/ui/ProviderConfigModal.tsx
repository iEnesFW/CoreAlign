import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useUpsertProviderConfigMutation } from '../hooks/useProvidersAdmin';
import type { ProviderInfo } from '../api/providersAdminApi';

interface Props {
  open: boolean;
  provider: ProviderInfo | null;
  onClose: () => void;
}

const configSchema = z.object({
  isEnabled: z.boolean(),
  isSandbox: z.boolean(),
  endpoint: z.string().trim().max(500).optional().or(z.literal('')),
  apiKey: z.string().trim().max(500).optional().or(z.literal('')),
  secretKey: z.string().trim().max(500).optional().or(z.literal('')),
  webhookSecret: z.string().trim().max(500).optional().or(z.literal('')),
  displayName: z.string().trim().max(200).optional().or(z.literal('')),
});

type ConfigFormValues = z.infer<typeof configSchema>;

const emptyValues: ConfigFormValues = {
  isEnabled: true,
  isSandbox: true,
  endpoint: '',
  apiKey: '',
  secretKey: '',
  webhookSecret: '',
  displayName: '',
};

const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';
const toggleRowCls =
  'flex items-center justify-between gap-3 rounded-md border border-slate-200 px-3 py-2 dark:border-slate-700';

interface ToggleProps {
  checked: boolean;
  onChange: (next: boolean) => void;
  activeColor?: string;
}

const Toggle = ({ checked, onChange, activeColor = 'peer-checked:bg-indigo-500' }: ToggleProps) => (
  <label className="relative inline-flex cursor-pointer items-center">
    <input
      type="checkbox"
      checked={checked}
      onChange={(e) => onChange(e.target.checked)}
      className="peer sr-only"
    />
    <div
      className={`h-5 w-9 rounded-full bg-slate-200 transition-colors ${activeColor} dark:bg-slate-700`}
    />
    <div className="pointer-events-none absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-white transition-transform peer-checked:translate-x-4" />
  </label>
);

const stripEmpty = (value: string | undefined): string | null => {
  if (!value) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
};

const buildCredentialsPayload = (values: ConfigFormValues): string | null => {
  const payload: Record<string, string | boolean> = { isSandbox: values.isSandbox };
  const endpoint = stripEmpty(values.endpoint);
  const apiKey = stripEmpty(values.apiKey);
  const secretKey = stripEmpty(values.secretKey);
  const webhookSecret = stripEmpty(values.webhookSecret);
  if (endpoint) payload.endpoint = endpoint;
  if (apiKey) payload.apiKey = apiKey;
  if (secretKey) payload.secretKey = secretKey;
  if (webhookSecret) payload.webhookSecret = webhookSecret;
  return JSON.stringify(payload);
};

export const ProviderConfigModal = ({ open, provider, onClose }: Props) => {
  const { t } = useTranslation();
  const upsertMutation = useUpsertProviderConfigMutation();

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<ConfigFormValues>({
    resolver: zodResolver(configSchema),
    defaultValues: emptyValues,
  });

  useEffect(() => {
    if (!open) {
      reset(emptyValues);
      return;
    }
    reset({
      isEnabled: provider?.isEnabled ?? true,
      isSandbox: provider?.isSandbox ?? true,
      endpoint: '',
      apiKey: '',
      secretKey: '',
      webhookSecret: '',
      displayName: provider?.displayName ?? '',
    });
  }, [open, provider, reset]);

  if (!provider) return null;

  const onSubmit = handleSubmit(async (values) => {
    try {
      await upsertMutation.mutateAsync({
        category: provider.category,
        providerName: provider.name,
        displayName: stripEmpty(values.displayName),
        isDefault: provider.isDefault,
        isEnabled: values.isEnabled,
        plaintextCredentialsJson: buildCredentialsPayload(values),
        enabledCapabilities: 0,
      });
      toast.success(t('Admin.Providers.Toast.Saved'));
      onClose();
    } catch (err) {
      toastApiError(err, t('Admin.Providers.Toast.SaveFailed'));
    }
  });

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('Admin.Providers.Form.Title', { name: provider.displayName })}
      subtitle={provider.category}
      size="lg"
      footer={
        <>
          <Button variant="ghost" onClick={onClose} type="button">
            {t('common.cancel')}
          </Button>
          <Button onClick={onSubmit} isLoading={isSubmitting || upsertMutation.isPending}>
            {t('common.save')}
          </Button>
        </>
      }
    >
      <form onSubmit={onSubmit} className="space-y-3">
        <Controller
          control={control}
          name="isEnabled"
          render={({ field }) => (
            <div className={toggleRowCls}>
              <div>
                <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                  {t('Admin.Providers.Form.IsEnabled')}
                </p>
                <p className="text-[11px] text-slate-500 dark:text-slate-400">
                  {t('Admin.Providers.Form.IsEnabledHint')}
                </p>
              </div>
              <Toggle checked={field.value} onChange={field.onChange} />
            </div>
          )}
        />

        <Controller
          control={control}
          name="isSandbox"
          render={({ field }) => (
            <div className={toggleRowCls}>
              <div>
                <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                  {t('Admin.Providers.Form.IsSandbox')}
                </p>
                <p className="text-[11px] text-slate-500 dark:text-slate-400">
                  {field.value
                    ? t('Admin.Providers.Form.SandboxHint')
                    : t('Admin.Providers.Form.ProdHint')}
                </p>
              </div>
              <Toggle
                checked={field.value}
                onChange={field.onChange}
                activeColor="peer-checked:bg-amber-500"
              />
            </div>
          )}
        />

        <div>
          <label className={labelCls}>{t('Admin.Providers.Form.DisplayName')}</label>
          <Input {...register('displayName')} error={errors.displayName?.message} />
        </div>

        <div>
          <label className={labelCls}>{t('Admin.Providers.Form.Endpoint')}</label>
          <Input
            {...register('endpoint')}
            placeholder="https://api.example.com/v1"
            error={errors.endpoint?.message}
          />
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <div>
            <label className={labelCls}>{t('Admin.Providers.Form.ApiKey')}</label>
            <Input
              type="password"
              autoComplete="new-password"
              placeholder="••••••••"
              {...register('apiKey')}
              error={errors.apiKey?.message}
            />
          </div>
          <div>
            <label className={labelCls}>{t('Admin.Providers.Form.SecretKey')}</label>
            <Input
              type="password"
              autoComplete="new-password"
              placeholder="••••••••"
              {...register('secretKey')}
              error={errors.secretKey?.message}
            />
          </div>
        </div>

        <div>
          <label className={labelCls}>{t('Admin.Providers.Form.WebhookSecret')}</label>
          <Input
            type="password"
            autoComplete="new-password"
            placeholder="••••••••"
            {...register('webhookSecret')}
            error={errors.webhookSecret?.message}
          />
          <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
            {t('Admin.Providers.Form.SecretsHint')}
          </p>
        </div>
      </form>
    </Modal>
  );
};
