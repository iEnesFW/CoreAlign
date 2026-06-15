import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { UploadCloud, Check, X } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { importsApi } from '@/features/imports/api/importsApi';
import type {
  ImportEntityKind,
  ImportPreviewResult,
  ImportRowPreview,
} from '@/features/imports/model/import.types';

const KIND_KEYS: { value: ImportEntityKind; labelKey: string }[] = [
  { value: 'customers', labelKey: 'Settings.Imports.Tabs.Customers' },
  { value: 'products', labelKey: 'Settings.Imports.Tabs.Products' },
  { value: 'gl-accounts', labelKey: 'Settings.Imports.Tabs.GLAccounts' },
];

export const ImportsPage = () => {
  const { t } = useTranslation();
  const [activeKind, setActiveKind] = useState<ImportEntityKind>('customers');
  const [preview, setPreview] = useState<ImportPreviewResult<Record<string, unknown>> | null>(null);
  const [skipInvalid, setSkipInvalid] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isCommitting, setIsCommitting] = useState(false);

  const handleFile = async (file: File | null) => {
    if (!file) return;
    setIsUploading(true);
    const [data] = await safeRequestWithNotify(
      importsApi.preview<Record<string, unknown>>(activeKind, file),
    );
    setIsUploading(false);
    if (data?.data) {
      setPreview(data.data);
    }
  };

  const handleDrop = (e: React.DragEvent<HTMLLabelElement>) => {
    e.preventDefault();
    void handleFile(e.dataTransfer.files?.[0] ?? null);
  };

  const handleCommit = async () => {
    if (!preview) return;
    if (preview.invalidRowCount > 0 && !skipInvalid) {
      toast.error(t('Settings.Imports.InvalidBlockToast', { count: preview.invalidRowCount }));
      return;
    }
    setIsCommitting(true);
    const [data] = await safeRequestWithNotify(
      importsApi.commit(activeKind, preview.sessionId, skipInvalid),
    );
    setIsCommitting(false);
    if (data?.data) {
      toast.success(t('Settings.Imports.CommittedToast', { count: data.data.committedCount }));
      setPreview(null);
    }
  };

  return (
    <div className="space-y-4 p-4 sm:p-6">
      <div>
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('Settings.Imports.Title')}
        </h1>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('Settings.Imports.Subtitle')}
        </p>
      </div>

      <div className="flex flex-wrap gap-1 border-b border-slate-200 dark:border-slate-800">
        {KIND_KEYS.map((tab) => (
          <button
            key={tab.value}
            type="button"
            onClick={() => {
              setActiveKind(tab.value);
              setPreview(null);
            }}
            className={`-mb-px border-b-2 px-3 py-1.5 text-sm ${
              activeKind === tab.value
                ? 'border-indigo-600 font-medium text-indigo-600 dark:text-indigo-400'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
            }`}
          >
            {t(tab.labelKey as 'Settings.Imports.Tabs.Customers')}
          </button>
        ))}
      </div>

      <label
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleDrop}
        className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-600 hover:border-indigo-400 hover:bg-indigo-50/40 dark:border-slate-700 dark:bg-slate-900/30 dark:text-slate-300 dark:hover:border-indigo-500"
      >
        <UploadCloud size={32} className="text-slate-400 dark:text-slate-500" />
        <span>{t('Settings.Imports.DropFile')}</span>
        <input
          type="file"
          accept=".csv,.xlsx"
          className="hidden"
          onChange={(e) => void handleFile(e.target.files?.[0] ?? null)}
        />
        {isUploading && <span className="text-xs text-slate-500">...</span>}
      </label>

      {preview && (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-3 text-xs text-slate-600 dark:text-slate-400">
            <span>
              {t('Settings.Imports.TotalRows')}: <strong>{preview.totalRowCount}</strong>
            </span>
            <span className="text-emerald-700 dark:text-emerald-400">
              {t('Settings.Imports.ValidRows')}: <strong>{preview.validRowCount}</strong>
            </span>
            <span className="text-rose-700 dark:text-rose-400">
              {t('Settings.Imports.InvalidRows')}: <strong>{preview.invalidRowCount}</strong>
            </span>
            <label className="ml-auto flex items-center gap-2">
              <input
                type="checkbox"
                checked={skipInvalid}
                onChange={(e) => setSkipInvalid(e.target.checked)}
              />
              {t('Settings.Imports.SkipInvalid')}
            </label>
            <button
              type="button"
              onClick={handleCommit}
              disabled={
                isCommitting ||
                (preview.invalidRowCount > 0 && !skipInvalid && preview.validRowCount === 0)
              }
              className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {skipInvalid ? t('Settings.Imports.Commit') : t('Settings.Imports.CommitAll')}
            </button>
          </div>

          <div className="overflow-auto rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-800/50 dark:text-slate-400">
                <tr>
                  <th className="px-2 py-2">#</th>
                  <th className="px-2 py-2">{t('Settings.Imports.Row')}</th>
                  {preview.headers.map((h) => (
                    <th key={h} className="px-2 py-2">
                      {h}
                    </th>
                  ))}
                  <th className="px-2 py-2">{t('Settings.Imports.Errors')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                {preview.rows.map((row) => (
                  <PreviewRow key={row.rowNumber} row={row} headers={preview.headers} />
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

const PreviewRow = ({
  row,
  headers,
}: {
  row: ImportRowPreview<Record<string, unknown>>;
  headers: string[];
}) => (
  <tr className={row.isValid ? '' : 'bg-rose-50 dark:bg-rose-900/20'}>
    <td className="px-2 py-1.5">
      {row.isValid ? (
        <Check size={12} className="text-emerald-600" />
      ) : (
        <X size={12} className="text-rose-600" />
      )}
    </td>
    <td className="px-2 py-1.5 font-mono">{row.rowNumber}</td>
    {headers.map((h) => (
      <td key={h} className="px-2 py-1.5">
        {String(
          (row.row as Record<string, unknown>)?.[h.charAt(0).toLowerCase() + h.slice(1)] ?? '',
        )}
      </td>
    ))}
    <td className="px-2 py-1.5 text-rose-700 dark:text-rose-400">
      {row.errors.map((e) => `${e.field}: ${e.message}`).join('; ')}
    </td>
  </tr>
);

export default ImportsPage;
