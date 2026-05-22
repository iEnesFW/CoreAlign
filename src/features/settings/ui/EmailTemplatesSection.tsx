import { useState } from 'react';
import { toast } from 'sonner';
import { Pencil, Plus, Trash2, X } from 'lucide-react';
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
  const templates = useEmailTemplatesQuery();
  const deleteMutation = useDeleteEmailTemplate();
  const confirm = useConfirm();
  const [editing, setEditing] = useState<EmailTemplate | 'new' | null>(null);

  const items = templates.data?.data ?? [];

  const remove = async (id: string) => {
    const ok = await confirm({
      title: 'Şablonu Sil',
      message: 'Şablon silinsin mi?',
      confirmLabel: 'Sil',
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success('Şablon silindi.');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-slate-500 dark:text-slate-400">
          Sipariş onayı, fatura bildirimi, gecikme hatırlatması gibi e-posta şablonları. {'{{'}
          değişken{'}}'} placeholder'ları gönderim anında doldurulur.
        </p>
        <button
          type="button"
          onClick={() => setEditing('new')}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={12} />
          Yeni Şablon
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">Kod</th>
              <th className="px-3 py-2 text-left">İsim</th>
              <th className="px-3 py-2 text-left">Konu</th>
              <th className="px-3 py-2 text-left">Dil</th>
              <th className="px-3 py-2 text-center">Aktif</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {templates.isPending ? (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  Yükleniyor…
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-3 py-6 text-center text-slate-500">
                  Henüz e-posta şablonu yok.
                </td>
              </tr>
            ) : (
              items.map((t) => (
                <tr
                  key={t.id}
                  className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs">{t.code}</td>
                  <td className="px-3 py-2 text-xs">{t.name}</td>
                  <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-300">
                    {t.subject}
                  </td>
                  <td className="px-3 py-2 text-xs">{t.locale}</td>
                  <td className="px-3 py-2 text-center">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
                        t.isActive
                          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
                          : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                      }`}
                    >
                      {t.isActive ? 'Aktif' : 'Pasif'}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-0.5">
                      <button
                        type="button"
                        onClick={() => setEditing(t)}
                        className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800"
                      >
                        <Pencil size={12} />
                      </button>
                      <button
                        type="button"
                        onClick={() => remove(t.id)}
                        className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
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
        toast.success('Şablon güncellendi.');
      } else {
        await createMutation.mutateAsync({
          code: code.trim(),
          name: name.trim(),
          subject: subject.trim(),
          body,
          locale,
        });
        toast.success('Şablon oluşturuldu.');
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="w-full max-w-2xl rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {template ? 'Şablonu Düzenle' : 'Yeni E-posta Şablonu'}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={16} />
          </button>
        </div>
        <form onSubmit={submit} className="space-y-3 p-4">
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Kod *
              </label>
              <input
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                required
                disabled={!!template}
                maxLength={64}
                placeholder="OrderConfirmation"
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm font-mono disabled:bg-slate-100 dark:border-slate-700 dark:bg-slate-800 dark:disabled:bg-slate-900"
              />
            </div>
            <div className="col-span-2">
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                İsim *
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                maxLength={200}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2">
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Konu *
              </label>
              <input
                type="text"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                required
                maxLength={500}
                placeholder="Siparişiniz alındı — {{orderNumber}}"
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                Dil
              </label>
              <select
                value={locale}
                onChange={(e) => setLocale(e.target.value)}
                className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
              >
                <option value="tr-TR">Türkçe</option>
                <option value="en-US">English</option>
              </select>
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
              Gövde (HTML) *
            </label>
            <textarea
              value={body}
              onChange={(e) => setBody(e.target.value)}
              required
              rows={8}
              placeholder="<p>Sayın {{customerName}}, …</p>"
              className="mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 font-mono text-xs dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          {template && (
            <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
              />
              Aktif
            </label>
          )}
          <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              İptal
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
