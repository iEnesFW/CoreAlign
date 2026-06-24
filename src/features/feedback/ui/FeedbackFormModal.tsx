import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Paperclip, X } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateFeedback, useUploadFeedbackAttachment } from '../hooks/useFeedback';
import type { FeedbackPriority, FeedbackType } from '../model/feedback.types';

const ATTACHMENT_ACCEPT = 'image/jpeg,image/png,image/webp,application/pdf';
const ATTACHMENT_MAX_BYTES = 5 * 1024 * 1024;

interface Props {
  onClose: () => void;
  initialType?: FeedbackType;
  initialTitle?: string;
  initialDescription?: string;
  initialModule?: string;
  initialPageUrl?: string;
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

export const FeedbackFormModal = ({
  onClose,
  initialType,
  initialTitle,
  initialDescription,
  initialModule,
  initialPageUrl,
}: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateFeedback();
  const uploadMutation = useUploadFeedbackAttachment();

  const [type, setType] = useState<FeedbackType>(initialType ?? 'Bug');
  const [priority, setPriority] = useState<FeedbackPriority>('Medium');
  const [moduleName, setModuleName] = useState(initialModule ?? '');
  const [title, setTitle] = useState(initialTitle ?? '');
  const [description, setDescription] = useState(initialDescription ?? '');
  const [steps, setSteps] = useState('');
  const [pageUrl, setPageUrl] = useState(
    initialPageUrl ?? (typeof window !== 'undefined' ? window.location.pathname : ''),
  );
  const [file, setFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  useEffect(
    () => () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    },
    [previewUrl],
  );

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0] ?? null;
    if (selected && selected.size > ATTACHMENT_MAX_BYTES) {
      toast.error(
        t('feedback.form.attachmentTooLarge', { defaultValue: 'Dosya en fazla 5 MB olabilir.' }),
      );
      e.target.value = '';
      return;
    }
    setFile(selected);
    setPreviewUrl(
      selected && selected.type.startsWith('image/') ? URL.createObjectURL(selected) : null,
    );
  };

  const clearFile = () => {
    setFile(null);
    setPreviewUrl(null);
  };

  const busy = createMutation.isPending || uploadMutation.isPending;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) {
      toast.error(t('feedback.form.required', { defaultValue: 'Başlık ve açıklama zorunludur.' }));
      return;
    }
    try {
      const created = await createMutation.mutateAsync({
        type,
        priority,
        title: title.trim(),
        description: description.trim(),
        module: moduleName || null,
        stepsToReproduce: type === 'Bug' && steps.trim() ? steps.trim() : null,
        pageUrl: pageUrl.trim() || null,
      });
      if (file && created.data?.id) {
        try {
          await uploadMutation.mutateAsync({ id: created.data.id, file });
        } catch (uploadErr) {
          toastApiError(uploadErr);
          toast.warning(
            t('feedback.form.attachmentFailed', {
              defaultValue: 'Talep oluşturuldu ancak dosya yüklenemedi.',
            }),
          );
          onClose();
          return;
        }
      }
      toast.success(
        t('feedback.form.sent', { defaultValue: 'Geri bildiriminiz alındı. Teşekkürler!' }),
      );
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open={true}
      title={t('feedback.form.title', { defaultValue: 'Geri Bildirim / Hata Bildir' })}
      icon={<Paperclip size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="feedback-form" isLoading={busy}>
            {busy
              ? t('common.saving', { defaultValue: 'Gönderiliyor…' })
              : t('feedback.form.submit', { defaultValue: 'Gönder' })}
          </Button>
        </>
      }
    >
      <form id="feedback-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <Select
            label={t('feedback.form.type', { defaultValue: 'Tür' })}
            value={type}
            onChange={(e) => setType(e.target.value as FeedbackType)}
          >
            {TYPES.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </Select>
          <Select
            label={t('feedback.form.priority', { defaultValue: 'Öncelik' })}
            value={priority}
            onChange={(e) => setPriority(e.target.value as FeedbackPriority)}
          >
            {PRIORITIES.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </Select>
        </div>

        <Select
          label={t('feedback.form.module', { defaultValue: 'İlgili Modül' })}
          value={moduleName}
          onChange={(e) => setModuleName(e.target.value)}
        >
          <option value="">
            {t('feedback.form.modulePlaceholder', { defaultValue: 'Seçiniz (opsiyonel)' })}
          </option>
          {MODULES.map((m) => (
            <option key={m} value={m}>
              {m}
            </option>
          ))}
        </Select>

        <Input
          label={`${t('feedback.form.titleField', { defaultValue: 'Başlık' })} *`}
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          maxLength={200}
          placeholder={t('feedback.form.titlePlaceholder', {
            defaultValue: 'Kısa ve açıklayıcı bir başlık',
          })}
        />

        <Textarea
          label={`${t('feedback.form.description', { defaultValue: 'Açıklama' })} *`}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={4000}
          rows={4}
          placeholder={t('feedback.form.descriptionPlaceholder', {
            defaultValue: 'Ne olmasını bekliyordunuz, ne oldu?',
          })}
        />

        {type === 'Bug' && (
          <Textarea
            label={t('feedback.form.steps', { defaultValue: 'Tekrar Üretme Adımları' })}
            value={steps}
            onChange={(e) => setSteps(e.target.value)}
            maxLength={2000}
            rows={3}
            placeholder={t('feedback.form.stepsPlaceholder', {
              defaultValue: '1) ... 2) ... 3) ...',
            })}
          />
        )}

        <Input
          label={t('feedback.form.pageUrl', { defaultValue: 'Sayfa / Konum' })}
          type="text"
          value={pageUrl}
          onChange={(e) => setPageUrl(e.target.value)}
          maxLength={500}
          className="font-mono text-xs"
        />

        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('feedback.form.attachment', { defaultValue: 'Ek (Fotoğraf / PDF)' })}
          </label>
          {file ? (
            <div className="mt-1 flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-2 dark:border-white/10 dark:bg-slate-800">
              {previewUrl ? (
                <img
                  src={previewUrl}
                  alt={file.name}
                  className="h-12 w-12 shrink-0 rounded object-cover"
                />
              ) : (
                <Paperclip size={18} className="shrink-0 text-slate-500 dark:text-slate-400" />
              )}
              <span className="min-w-0 flex-1 truncate text-xs text-slate-700 dark:text-slate-200">
                {file.name}
              </span>
              <button
                type="button"
                onClick={clearFile}
                className="shrink-0 rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                aria-label={t('feedback.form.attachmentRemove', { defaultValue: 'Eki kaldır' })}
              >
                <X size={14} />
              </button>
            </div>
          ) : (
            <input
              type="file"
              accept={ATTACHMENT_ACCEPT}
              onChange={onFileChange}
              className="mt-1 block w-full text-xs text-slate-600 file:mr-3 file:rounded file:border-0 file:bg-primary-50 file:px-3 file:py-1.5 file:text-xs file:font-medium file:text-primary-700 hover:file:bg-primary-100 dark:text-slate-400 dark:file:bg-slate-800 dark:file:text-slate-200"
            />
          )}
          <p className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
            {t('feedback.form.attachmentHint', {
              defaultValue: 'JPG, PNG, WEBP veya PDF · en fazla 5 MB',
            })}
          </p>
        </div>
      </form>
    </Modal>
  );
};
