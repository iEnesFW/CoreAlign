import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { X } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateFeedback } from '../hooks/useFeedback';
import type { FeedbackPriority, FeedbackType } from '../model/feedback.types';

interface Props {
  onClose: () => void;
}

const TYPES: { value: FeedbackType; label: string }[] = [
  { value: 'Bug', label: 'Hata (Bug)' },
  { value: 'Feature', label: 'Yeni Özellik' },
  { value: 'Improvement', label: 'İyileştirme' },
  { value: 'Question', label: 'Soru' },
  { value: 'Other', label: 'Diğer' },
];

const PRIORITIES: { value: FeedbackPriority; label: string }[] = [
  { value: 'Low', label: 'Düşük' },
  { value: 'Medium', label: 'Orta' },
  { value: 'High', label: 'Yüksek' },
  { value: 'Critical', label: 'Kritik' },
];

const MODULES = [
  'Siparişler',
  'Stok',
  'Müşteriler',
  'Faturalar',
  'Ödemeler',
  'Tedarikçiler',
  'Muhasebe',
  'Raporlar',
  'Yönetim Paneli',
  'Diğer',
];

export const FeedbackFormModal = ({ onClose }: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateFeedback();

  const [type, setType] = useState<FeedbackType>('Bug');
  const [priority, setPriority] = useState<FeedbackPriority>('Medium');
  const [moduleName, setModuleName] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [steps, setSteps] = useState('');
  const [pageUrl, setPageUrl] = useState(
    typeof window !== 'undefined' ? window.location.pathname : '',
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) {
      toast.error(t('feedback.form.required', { defaultValue: 'Başlık ve açıklama zorunludur.' }));
      return;
    }
    try {
      await createMutation.mutateAsync({
        type,
        priority,
        title: title.trim(),
        description: description.trim(),
        module: moduleName || null,
        stepsToReproduce: type === 'Bug' && steps.trim() ? steps.trim() : null,
        pageUrl: pageUrl.trim() || null,
      });
      toast.success(
        t('feedback.form.sent', { defaultValue: 'Geri bildiriminiz alındı. Teşekkürler!' }),
      );
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  const inputClass =
    'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div className="flex max-h-[92vh] w-full max-w-lg flex-col rounded-lg bg-white shadow-xl dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
          <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('feedback.form.title', { defaultValue: 'Geri Bildirim / Hata Bildir' })}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-500 dark:hover:bg-slate-800 dark:hover:text-slate-200"
            aria-label={t('common.close', { defaultValue: 'Kapat' })}
          >
            <X size={16} />
          </button>
        </div>

        <form onSubmit={submit} className="flex min-h-0 flex-1 flex-col">
          <div className="space-y-3 overflow-y-auto p-4">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('feedback.form.type', { defaultValue: 'Tür' })}
                </label>
                <select
                  value={type}
                  onChange={(e) => setType(e.target.value as FeedbackType)}
                  className={inputClass}
                >
                  {TYPES.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('feedback.form.priority', { defaultValue: 'Öncelik' })}
                </label>
                <select
                  value={priority}
                  onChange={(e) => setPriority(e.target.value as FeedbackPriority)}
                  className={inputClass}
                >
                  {PRIORITIES.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('feedback.form.module', { defaultValue: 'İlgili Modül' })}
              </label>
              <select
                value={moduleName}
                onChange={(e) => setModuleName(e.target.value)}
                className={inputClass}
              >
                <option value="">
                  {t('feedback.form.modulePlaceholder', { defaultValue: 'Seçiniz (opsiyonel)' })}
                </option>
                {MODULES.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('feedback.form.titleField', { defaultValue: 'Başlık' })} *
              </label>
              <input
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                maxLength={200}
                className={inputClass}
                placeholder={t('feedback.form.titlePlaceholder', {
                  defaultValue: 'Kısa ve açıklayıcı bir başlık',
                })}
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('feedback.form.description', { defaultValue: 'Açıklama' })} *
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={4000}
                rows={4}
                className={inputClass}
                placeholder={t('feedback.form.descriptionPlaceholder', {
                  defaultValue: 'Ne olmasını bekliyordunuz, ne oldu?',
                })}
              />
            </div>

            {type === 'Bug' && (
              <div>
                <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                  {t('feedback.form.steps', { defaultValue: 'Tekrar Üretme Adımları' })}
                </label>
                <textarea
                  value={steps}
                  onChange={(e) => setSteps(e.target.value)}
                  maxLength={2000}
                  rows={3}
                  className={inputClass}
                  placeholder={t('feedback.form.stepsPlaceholder', {
                    defaultValue: '1) ... 2) ... 3) ...',
                  })}
                />
              </div>
            )}

            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('feedback.form.pageUrl', { defaultValue: 'Sayfa / Konum' })}
              </label>
              <input
                type="text"
                value={pageUrl}
                onChange={(e) => setPageUrl(e.target.value)}
                maxLength={500}
                className={`${inputClass} font-mono text-xs`}
              />
            </div>
          </div>

          <div className="flex justify-end gap-2 border-t border-slate-200 px-4 py-3 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              {t('common.cancel', { defaultValue: 'İptal' })}
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {createMutation.isPending
                ? t('common.saving', { defaultValue: 'Gönderiliyor…' })
                : t('feedback.form.submit', { defaultValue: 'Gönder' })}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
