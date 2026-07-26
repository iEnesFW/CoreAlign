import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ImagePlus, Paperclip, X } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateFeedback, useUploadFeedbackAttachments } from '../hooks/useFeedback';
import { newOperationId } from '@/shared/lib/operationId';
import { FEEDBACK_ATTACHMENT_MAX } from '../model/feedback.types';
import type { FeedbackPriority, FeedbackType } from '../model/feedback.types';

const ATTACHMENT_ACCEPT = 'image/jpeg,image/png,image/webp,application/pdf';
const ATTACHMENT_MAX_BYTES = 5 * 1024 * 1024;

interface PickedFile {
  id: string;
  file: File;
  previewUrl: string | null;
}

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
  const uploadMutation = useUploadFeedbackAttachments();

  const [type, setType] = useState<FeedbackType>(initialType ?? 'Bug');
  const [priority, setPriority] = useState<FeedbackPriority>('Medium');
  const [moduleName, setModuleName] = useState(initialModule ?? '');
  const [title, setTitle] = useState(initialTitle ?? '');
  const [description, setDescription] = useState(initialDescription ?? '');
  const [steps, setSteps] = useState('');
  const [pageUrl, setPageUrl] = useState(
    initialPageUrl ?? (typeof window !== 'undefined' ? window.location.pathname : ''),
  );
  const [picked, setPicked] = useState<PickedFile[]>([]);
  const [dragging, setDragging] = useState(false);

  // Cleanup-only effect: object URLs are created in the event handler (setState inside an effect
  // is a lint error here), so this just releases whatever is currently held on unmount.
  const pickedRef = useRef<PickedFile[]>([]);
  useEffect(() => {
    pickedRef.current = picked;
  }, [picked]);
  useEffect(
    () => () => {
      for (const item of pickedRef.current) {
        if (item.previewUrl) URL.revokeObjectURL(item.previewUrl);
      }
    },
    [],
  );

  const addFiles = (incoming: File[]) => {
    if (incoming.length === 0) return;
    const accepted: PickedFile[] = [];
    let rejectedSize = false;
    let rejectedCount = false;
    for (const candidate of incoming) {
      if (candidate.size > ATTACHMENT_MAX_BYTES) {
        rejectedSize = true;
        continue;
      }
      if (picked.length + accepted.length >= FEEDBACK_ATTACHMENT_MAX) {
        rejectedCount = true;
        break;
      }
      accepted.push({
        id: newOperationId(),
        file: candidate,
        previewUrl: candidate.type.startsWith('image/') ? URL.createObjectURL(candidate) : null,
      });
    }
    if (rejectedSize) {
      toast.error(
        t('feedback.form.attachmentTooLarge', { defaultValue: 'Dosya en fazla 5 MB olabilir.' }),
      );
    }
    if (rejectedCount) {
      toast.error(
        t('feedback.form.attachmentTooMany', {
          defaultValue: 'En fazla {{n}} dosya ekleyebilirsiniz.',
          n: FEEDBACK_ATTACHMENT_MAX,
        }),
      );
    }
    if (accepted.length > 0) setPicked((prev) => [...prev, ...accepted]);
  };

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    addFiles(Array.from(e.target.files ?? []));
    e.target.value = '';
  };

  const onDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setDragging(false);
    addFiles(Array.from(e.dataTransfer.files ?? []));
  };

  const onPaste = (e: React.ClipboardEvent<HTMLDivElement>) => {
    const files = Array.from(e.clipboardData?.files ?? []);
    if (files.length > 0) addFiles(files);
  };

  const removePicked = (id: string) => {
    setPicked((prev) => {
      const target = prev.find((item) => item.id === id);
      if (target?.previewUrl) URL.revokeObjectURL(target.previewUrl);
      return prev.filter((item) => item.id !== id);
    });
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
      if (picked.length > 0 && created.data?.id) {
        try {
          await uploadMutation.mutateAsync({
            ticketId: created.data.id,
            files: picked.map((item) => item.file),
          });
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
          <div
            onDragOver={(e) => {
              e.preventDefault();
              setDragging(true);
            }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            onPaste={onPaste}
            className={`mt-1 rounded-lg border border-dashed p-3 transition-colors ${
              dragging
                ? 'border-primary-400 bg-primary-50/60 dark:bg-primary-500/10'
                : 'border-slate-300 bg-slate-50/60 dark:border-white/10 dark:bg-slate-800/50'
            }`}
          >
            {picked.length > 0 && (
              <ul className="mb-2 grid grid-cols-2 gap-2 sm:grid-cols-3">
                {picked.map((item) => (
                  <li
                    key={item.id}
                    className="relative flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-1.5 dark:border-white/10 dark:bg-slate-900"
                  >
                    {item.previewUrl ? (
                      <img
                        src={item.previewUrl}
                        alt={item.file.name}
                        className="h-10 w-10 shrink-0 rounded object-cover"
                      />
                    ) : (
                      <Paperclip
                        size={16}
                        className="h-10 w-10 shrink-0 p-3 text-slate-500 dark:text-slate-400"
                      />
                    )}
                    <span className="min-w-0 flex-1 truncate text-[11px] text-slate-700 dark:text-slate-200">
                      {item.file.name}
                    </span>
                    <button
                      type="button"
                      onClick={() => removePicked(item.id)}
                      className="shrink-0 rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
                      aria-label={t('feedback.form.attachmentRemove', {
                        defaultValue: 'Eki kaldır',
                      })}
                    >
                      <X size={12} />
                    </button>
                  </li>
                ))}
              </ul>
            )}
            {picked.length < FEEDBACK_ATTACHMENT_MAX && (
              <label className="flex cursor-pointer items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
                <ImagePlus size={16} className="shrink-0 text-primary-500" />
                <span>
                  {t('feedback.form.attachmentDrop', {
                    defaultValue: 'Sürükleyip bırakın, panodan yapıştırın veya seçin',
                  })}
                </span>
                <input
                  type="file"
                  multiple
                  accept={ATTACHMENT_ACCEPT}
                  onChange={onFileChange}
                  className="sr-only"
                />
              </label>
            )}
          </div>
          <p className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
            {t('feedback.form.attachmentHint', {
              defaultValue: 'JPG, PNG, WEBP veya PDF · dosya başına en fazla 5 MB',
            })}
          </p>
        </div>
      </form>
    </Modal>
  );
};
