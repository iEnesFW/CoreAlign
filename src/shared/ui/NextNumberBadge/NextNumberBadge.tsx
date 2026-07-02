import { useTranslation } from 'react-i18next';
import { Sparkles } from 'lucide-react';
import { useDocumentSequencesQuery } from '@/shared/document-sequences';
import type { DocumentSequenceType } from '@/shared/document-sequences';

interface Props {
  type: DocumentSequenceType;
  className?: string;
}

export const NextNumberBadge = ({ type, className }: Props) => {
  const { t } = useTranslation();
  const { data, isPending, isError } = useDocumentSequencesQuery();
  const preview = data?.data?.find((s) => s.type === type)?.preview;

  const label = t('numbering.autoLabel', { defaultValue: 'Otomatik' });
  const hint = t('numbering.autoHint', {
    defaultValue: 'Sıradaki numara kaydederken otomatik atanır.',
  });

  return (
    <span
      title={hint}
      aria-label={preview ? `${label} · ${preview}` : label}
      className={`inline-flex max-w-full items-center gap-1.5 rounded-md border border-primary-200 bg-primary-50 px-2.5 py-1.5 text-xs font-medium text-primary-700 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 ${className ?? ''}`}
    >
      <Sparkles size={13} className="shrink-0" aria-hidden="true" />
      <span>{label}</span>
      {preview ? (
        <span className="truncate font-mono text-primary-900 dark:text-primary-200">
          · {preview}
        </span>
      ) : isPending ? (
        <span className="text-primary-400">…</span>
      ) : isError ? null : null}
    </span>
  );
};
