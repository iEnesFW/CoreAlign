import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Mail, Pencil, Plus, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  useCreateEmailTemplate,
  useDeleteEmailTemplate,
  useEmailTemplatesQuery,
  useUpdateEmailTemplate,
} from '../hooks/useSettingsQueries';
import type { EmailTemplate } from '../model/settings.types';

export const EmailTemplatesSection = () => {
  const { t } = useTranslation();
  const templates = useEmailTemplatesQuery();
  const deleteMutation = useDeleteEmailTemplate();
  const confirm = useConfirm();
  const [editing, setEditing] = useState<EmailTemplate | 'new' | null>(null);

  const items = templates.data?.data ?? [];

  const remove = async (id: string) => {
    const ok = await confirm({
      title: t('Settings.EmailTemplateDeleteTitle', { defaultValue: 'Şablonu Sil' }),
      message: t('Settings.EmailTemplateDeleteMessage', { defaultValue: 'Şablon silinsin mi?' }),
      confirmLabel: t('Settings.Delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success(t('Settings.EmailTemplateDeleted', { defaultValue: 'Şablon silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('Settings.EmailTemplatesIntro', {
            defaultValue:
              "Sipariş onayı, fatura bildirimi, gecikme hatırlatması gibi e-posta şablonları. {{değişken}} placeholder'ları gönderim anında doldurulur.",
          })}
        </p>
        <button
          type="button"
          onClick={() => setEditing('new')}
          className="inline-flex items-center gap-1.5 rounded bg-primary-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-primary-700"
        >
          <Plus size={12} />
          {t('Settings.NewEmailTemplate', { defaultValue: 'Yeni Şablon' })}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('Settings.EmailTemplateCode', { defaultValue: 'Kod' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('Settings.EmailTemplateName', { defaultValue: 'İsim' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('Settings.EmailTemplateSubject', { defaultValue: 'Konu' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('Settings.EmailTemplateLocale', { defaultValue: 'Dil' })}
              </th>
              <th className="px-3 py-2 text-center">
                {t('Settings.EmailTemplateActive', { defaultValue: 'Aktif' })}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {templates.isPending ? (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  {t('Settings.Loading', { defaultValue: 'Yükleniyor…' })}
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  {t('Settings.NoEmailTemplates', { defaultValue: 'Henüz e-posta şablonu yok.' })}
                </td>
              </tr>
            ) : (
              items.map((template) => (
                <tr
                  key={template.id}
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs">{template.code}</td>
                  <td className="px-3 py-2 text-xs">{template.name}</td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-300">
                    {template.subject}
                  </td>
                  <td className="px-3 py-2 text-xs">{template.locale}</td>
                  <td className="px-3 py-2 text-center">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
                        template.isActive
                          ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300'
                          : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {template.isActive
                        ? t('Settings.StatusActive', { defaultValue: 'Aktif' })
                        : t('Settings.StatusInactive', { defaultValue: 'Pasif' })}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-0.5">
                      <button
                        type="button"
                        onClick={() => setEditing(template)}
                        className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
                      >
                        <Pencil size={12} />
                      </button>
                      <button
                        type="button"
                        onClick={() => remove(template.id)}
                        className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                      >
                        <Trash2 size={12} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {editing && (
        <EmailTemplateModal
          template={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
        />
      )}
    </div>
  );
};

const EmailTemplateModal = ({
  template,
  onClose,
}: {
  template: EmailTemplate | null;
  onClose: () => void;
}) => {
  const { t } = useTranslation();
  const createMutation = useCreateEmailTemplate();
  const updateMutation = useUpdateEmailTemplate();

  const [code, setCode] = useState(template?.code ?? '');
  const [name, setName] = useState(template?.name ?? '');
  const [subject, setSubject] = useState(template?.subject ?? '');
  const [body, setBody] = useState(template?.body ?? '');
  const [locale, setLocale] = useState(template?.locale ?? 'tr-TR');
  const [isActive, setIsActive] = useState(template?.isActive ?? true);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (template) {
        await updateMutation.mutateAsync({
          id: template.id,
          name: name.trim(),
          subject: subject.trim(),
          body,
          locale,
          isActive,
        });
        toast.success(t('Settings.EmailTemplateUpdated', { defaultValue: 'Şablon güncellendi.' }));
      } else {
        await createMutation.mutateAsync({
          code: code.trim(),
          name: name.trim(),
          subject: subject.trim(),
          body,
          locale,
        });
        toast.success(t('Settings.EmailTemplateCreated', { defaultValue: 'Şablon oluşturuldu.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Modal
      open={true}
      title={
        template
          ? t('Settings.EditEmailTemplate', { defaultValue: 'Şablonu Düzenle' })
          : t('Settings.NewEmailTemplateTitle', { defaultValue: 'Yeni E-posta Şablonu' })
      }
      icon={<Mail size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose}>
            {t('Settings.Cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="email-template-form" isLoading={isPending}>
            {isPending
              ? t('Settings.Saving', { defaultValue: 'Kaydediliyor…' })
              : t('Settings.Save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="email-template-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-3 gap-3">
          <Input
            label={t('Settings.EmailTemplateCodeRequired', { defaultValue: 'Kod *' })}
            type="text"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            disabled={!!template}
            maxLength={64}
            placeholder="OrderConfirmation"
            className="font-mono"
          />
          <Input
            className="col-span-2"
            label={t('Settings.EmailTemplateNameRequired', { defaultValue: 'İsim *' })}
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={200}
          />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <Input
            className="col-span-2"
            label={t('Settings.EmailTemplateSubjectRequired', { defaultValue: 'Konu *' })}
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            required
            maxLength={500}
            placeholder={t('Settings.EmailTemplateSubjectPlaceholder', {
              defaultValue: 'Siparişiniz alındı — {{orderNumber}}',
            })}
          />
          <Select
            label={t('Settings.EmailTemplateLocaleLabel', { defaultValue: 'Dil' })}
            value={locale}
            onChange={(e) => setLocale(e.target.value)}
          >
            <option value="tr-TR">{t('Settings.LocaleTurkish', { defaultValue: 'Türkçe' })}</option>
            <option value="en-US">
              {t('Settings.LocaleEnglish', { defaultValue: 'English' })}
            </option>
          </Select>
        </div>
        <Textarea
          label={t('Settings.EmailTemplateBodyRequired', { defaultValue: 'Gövde (HTML) *' })}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          required
          rows={8}
          className="font-mono text-xs"
          placeholder={t('Settings.EmailTemplateBodyPlaceholder', {
            defaultValue: '<p>Sayın {{customerName}}, …</p>',
          })}
        />
        {template && (
          <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            {t('Settings.EmailTemplateActive', { defaultValue: 'Aktif' })}
          </label>
        )}
      </form>
    </Modal>
  );
};
