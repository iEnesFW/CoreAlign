import { useEffect } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useSmtpSettingsQuery, useUpsertSmtpMutation } from '../hooks/useSmtpAdmin';

const schema = z
  .object({
    host: z.string().trim().min(1).max(255),
    port: z.number().int().min(1).max(65535),
    useSsl: z.boolean(),
    username: z.string().trim().max(320).optional().or(z.literal('')),
    password: z.string().max(500).optional().or(z.literal('')),
    fromAddress: z.string().trim().email().max(254).optional().or(z.literal('')),
    fromName: z.string().trim().max(128).optional().or(z.literal('')),
    isEnabled: z.boolean(),
    authMode: z.enum(['Password', 'OAuth2']),
    oAuthProvider: z.enum(['Google', 'Microsoft', 'Custom']),
    oAuthTenantId: z.string().trim().max(128).optional().or(z.literal('')),
    oAuthClientId: z.string().trim().max(255).optional().or(z.literal('')),
    oAuthClientSecret: z.string().max(500).optional().or(z.literal('')),
    oAuthRefreshToken: z.string().max(2000).optional().or(z.literal('')),
    oAuthTokenEndpoint: z.string().trim().max(500).optional().or(z.literal('')),
    oAuthScope: z.string().trim().max(500).optional().or(z.literal('')),
  })
  .superRefine((values, ctx) => {
    if (values.authMode !== 'OAuth2') return;
    if (!values.oAuthClientId?.trim()) {
      ctx.addIssue({ code: 'custom', path: ['oAuthClientId'], message: 'required' });
    }
    if (!values.username?.trim() && !values.fromAddress?.trim()) {
      ctx.addIssue({ code: 'custom', path: ['username'], message: 'required' });
    }
    if (values.oAuthProvider === 'Custom' && !values.oAuthTokenEndpoint?.trim()) {
      ctx.addIssue({ code: 'custom', path: ['oAuthTokenEndpoint'], message: 'required' });
    }
  });

type SmtpFormValues = z.infer<typeof schema>;

const emptyValues: SmtpFormValues = {
  host: '',
  port: 587,
  useSsl: true,
  username: '',
  password: '',
  fromAddress: '',
  fromName: '',
  isEnabled: true,
  authMode: 'Password',
  oAuthProvider: 'Google',
  oAuthTenantId: '',
  oAuthClientId: '',
  oAuthClientSecret: '',
  oAuthRefreshToken: '',
  oAuthTokenEndpoint: '',
  oAuthScope: '',
};

const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';
const selectCls =
  'w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';
const toggleRowCls =
  'flex items-center justify-between gap-3 rounded-md border border-slate-200 px-3 py-2 dark:border-slate-700';

interface ToggleProps {
  checked: boolean;
  onChange: (next: boolean) => void;
}

const Toggle = ({ checked, onChange }: ToggleProps) => (
  <label className="relative inline-flex cursor-pointer items-center">
    <input
      type="checkbox"
      checked={checked}
      onChange={(e) => onChange(e.target.checked)}
      className="peer sr-only"
    />
    <div className="h-5 w-9 rounded-full bg-slate-200 transition-colors peer-checked:bg-primary-500 dark:bg-slate-700" />
    <div className="pointer-events-none absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-white transition-transform peer-checked:translate-x-4" />
  </label>
);

const stripEmpty = (value: string | undefined): string | null => {
  if (!value) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
};

export const SmtpSettingsForm = () => {
  const { t } = useTranslation();
  const settingsQuery = useSmtpSettingsQuery();
  const upsertMutation = useUpsertSmtpMutation();

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isSubmitting },
  } = useForm<SmtpFormValues>({
    resolver: zodResolver(schema),
    defaultValues: emptyValues,
  });

  const settings = settingsQuery.data;
  const authMode = useWatch({ control, name: 'authMode' });
  const oAuthProvider = useWatch({ control, name: 'oAuthProvider' });
  const isOAuth = authMode === 'OAuth2';

  useEffect(() => {
    if (!settings) return;
    reset({
      host: settings.host ?? '',
      port: settings.port || 587,
      useSsl: settings.useSsl,
      username: settings.username ?? '',
      password: '',
      fromAddress: settings.fromAddress ?? '',
      fromName: settings.fromName ?? '',
      isEnabled: settings.isEnabled,
      authMode: settings.authMode === 'OAuth2' ? 'OAuth2' : 'Password',
      oAuthProvider: settings.oAuthProvider ?? 'Google',
      oAuthTenantId: settings.oAuthTenantId ?? '',
      oAuthClientId: settings.oAuthClientId ?? '',
      oAuthClientSecret: '',
      oAuthRefreshToken: '',
      oAuthTokenEndpoint: settings.oAuthTokenEndpoint ?? '',
      oAuthScope: settings.oAuthScope ?? '',
    });
  }, [settings, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const oauth = values.authMode === 'OAuth2';
    try {
      await upsertMutation.mutateAsync({
        host: values.host.trim(),
        port: values.port,
        useSsl: values.useSsl,
        username: stripEmpty(values.username),
        password: oauth ? null : stripEmpty(values.password),
        fromAddress: stripEmpty(values.fromAddress),
        fromName: stripEmpty(values.fromName),
        isEnabled: values.isEnabled,
        authMode: values.authMode,
        oAuthProvider: oauth ? values.oAuthProvider : null,
        oAuthTenantId: oauth ? stripEmpty(values.oAuthTenantId) : null,
        oAuthClientId: oauth ? stripEmpty(values.oAuthClientId) : null,
        oAuthClientSecret: oauth ? stripEmpty(values.oAuthClientSecret) : null,
        oAuthRefreshToken: oauth ? stripEmpty(values.oAuthRefreshToken) : null,
        oAuthTokenEndpoint: oauth ? stripEmpty(values.oAuthTokenEndpoint) : null,
        oAuthScope: oauth ? stripEmpty(values.oAuthScope) : null,
      });
      toast.success(t('Admin.Smtp.Toast.Saved'));
    } catch (err) {
      toastApiError(err, t('Admin.Smtp.Toast.SaveFailed'));
    }
  });

  return (
    <form
      onSubmit={onSubmit}
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900"
    >
      <Controller
        control={control}
        name="isEnabled"
        render={({ field }) => (
          <div className={toggleRowCls}>
            <div>
              <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                {t('Admin.Smtp.Form.IsEnabled')}
              </p>
              <p className="text-[11px] text-slate-500 dark:text-slate-400">
                {t('Admin.Smtp.Form.IsEnabledHint')}
              </p>
            </div>
            <Toggle checked={field.value} onChange={field.onChange} />
          </div>
        )}
      />

      <div className="grid gap-3 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <label className={labelCls}>{t('Admin.Smtp.Form.Host')}</label>
          <Input
            {...register('host')}
            placeholder="smtp.example.com"
            error={errors.host?.message}
          />
        </div>
        <div>
          <label className={labelCls}>{t('Admin.Smtp.Form.Port')}</label>
          <Input
            type="number"
            {...register('port', { valueAsNumber: true })}
            error={errors.port?.message}
          />
        </div>
        <Controller
          control={control}
          name="useSsl"
          render={({ field }) => (
            <div className={toggleRowCls}>
              <div>
                <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                  {t('Admin.Smtp.Form.UseSsl')}
                </p>
                <p className="text-[11px] text-slate-500 dark:text-slate-400">
                  {t('Admin.Smtp.Form.UseSslHint')}
                </p>
              </div>
              <Toggle checked={field.value} onChange={field.onChange} />
            </div>
          )}
        />
      </div>

      <div>
        <label className={labelCls}>{t('Admin.Smtp.Form.AuthMode')}</label>
        <Controller
          control={control}
          name="authMode"
          render={({ field }) => (
            <select
              className={selectCls}
              value={field.value}
              onChange={(e) => field.onChange(e.target.value)}
            >
              <option value="Password">{t('Admin.Smtp.Form.AuthModePassword')}</option>
              <option value="OAuth2">{t('Admin.Smtp.Form.AuthModeOAuth')}</option>
            </select>
          )}
        />
        <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
          {t('Admin.Smtp.Form.AuthModeHint')}
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className={labelCls}>{t('Admin.Smtp.Form.Username')}</label>
          <Input {...register('username')} autoComplete="off" error={errors.username?.message} />
          {isOAuth && (
            <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {t('Admin.Smtp.Form.OAuthMailboxHint')}
            </p>
          )}
        </div>
        {!isOAuth && (
          <div>
            <label className={labelCls}>{t('Admin.Smtp.Form.Password')}</label>
            <Input
              type="password"
              autoComplete="new-password"
              placeholder={settings?.hasPassword ? '••••••••' : ''}
              {...register('password')}
              error={errors.password?.message}
            />
            <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {t('Admin.Smtp.Form.PasswordHint')}
            </p>
          </div>
        )}
      </div>

      {isOAuth && (
        <div className="grid gap-3 rounded-md border border-slate-200 p-3 sm:grid-cols-2 dark:border-slate-700">
          <div className="sm:col-span-2">
            <label className={labelCls}>{t('Admin.Smtp.Form.OAuthProvider')}</label>
            <Controller
              control={control}
              name="oAuthProvider"
              render={({ field }) => (
                <select
                  className={selectCls}
                  value={field.value}
                  onChange={(e) => field.onChange(e.target.value)}
                >
                  <option value="Google">{t('Admin.Smtp.Form.OAuthProviderGoogle')}</option>
                  <option value="Microsoft">{t('Admin.Smtp.Form.OAuthProviderMicrosoft')}</option>
                  <option value="Custom">{t('Admin.Smtp.Form.OAuthProviderCustom')}</option>
                </select>
              )}
            />
          </div>

          <div>
            <label className={labelCls}>{t('Admin.Smtp.Form.OAuthClientId')}</label>
            <Input
              {...register('oAuthClientId')}
              autoComplete="off"
              error={errors.oAuthClientId?.message}
            />
          </div>

          {oAuthProvider === 'Microsoft' && (
            <div>
              <label className={labelCls}>{t('Admin.Smtp.Form.OAuthTenantId')}</label>
              <Input
                {...register('oAuthTenantId')}
                autoComplete="off"
                placeholder="common"
                error={errors.oAuthTenantId?.message}
              />
              <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
                {t('Admin.Smtp.Form.OAuthTenantIdHint')}
              </p>
            </div>
          )}

          <div>
            <label className={labelCls}>{t('Admin.Smtp.Form.OAuthClientSecret')}</label>
            <Input
              type="password"
              autoComplete="new-password"
              placeholder={settings?.hasOAuthClientSecret ? '••••••••' : ''}
              {...register('oAuthClientSecret')}
              error={errors.oAuthClientSecret?.message}
            />
            <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {t('Admin.Smtp.Form.OAuthClientSecretHint')}
            </p>
          </div>

          <div>
            <label className={labelCls}>{t('Admin.Smtp.Form.OAuthRefreshToken')}</label>
            <Input
              type="password"
              autoComplete="new-password"
              placeholder={settings?.hasOAuthRefreshToken ? '••••••••' : ''}
              {...register('oAuthRefreshToken')}
              error={errors.oAuthRefreshToken?.message}
            />
            <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {t('Admin.Smtp.Form.OAuthRefreshTokenHint')}
            </p>
          </div>

          {oAuthProvider === 'Custom' && (
            <div className="sm:col-span-2">
              <label className={labelCls}>{t('Admin.Smtp.Form.OAuthTokenEndpoint')}</label>
              <Input
                {...register('oAuthTokenEndpoint')}
                placeholder="https://idp.example.com/oauth/token"
                error={errors.oAuthTokenEndpoint?.message}
              />
              <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
                {t('Admin.Smtp.Form.OAuthTokenEndpointHint')}
              </p>
            </div>
          )}

          <div className="sm:col-span-2">
            <label className={labelCls}>{t('Admin.Smtp.Form.OAuthScope')}</label>
            <Input {...register('oAuthScope')} error={errors.oAuthScope?.message} />
            <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {t('Admin.Smtp.Form.OAuthScopeHint')}
            </p>
          </div>
        </div>
      )}

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label className={labelCls}>{t('Admin.Smtp.Form.FromAddress')}</label>
          <Input
            {...register('fromAddress')}
            placeholder="noreply@example.com"
            error={errors.fromAddress?.message}
          />
        </div>
        <div>
          <label className={labelCls}>{t('Admin.Smtp.Form.FromName')}</label>
          <Input {...register('fromName')} error={errors.fromName?.message} />
        </div>
      </div>

      <div className="flex justify-end">
        <Button type="submit" isLoading={isSubmitting || upsertMutation.isPending}>
          {t('common.save')}
        </Button>
      </div>
    </form>
  );
};
