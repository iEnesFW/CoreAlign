import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import type { BIExportFormat } from '../model/bi.types';
import { useExportReport } from '../hooks/useReports';

interface Props {
  reportId: string;
  fileName: string;
}

const EXTENSIONS: Record<BIExportFormat, string> = {
  Pdf: 'pdf',
  Xlsx: 'xlsx',
  Csv: 'csv',
};

export const ExportButton = ({ reportId, fileName }: Props) => {
  const { t } = useTranslation();
  const exportMutation = useExportReport();
  const [open, setOpen] = useState(false);

  const handle = async (format: BIExportFormat) => {
    setOpen(false);
    try {
      const blob = await exportMutation.mutateAsync({ id: reportId, format });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${fileName}.${EXTENSIONS[format]}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      toast.success(t('BI.Export.Success', { defaultValue: 'Export downloaded' }));
    } catch {
      toast.error(t('BI.Export.Failed', { defaultValue: 'Export failed' }));
    }
  };

  return (
    <div className="relative inline-block">
      <button
        type="button"
        onClick={() => setOpen((p) => !p)}
        className="rounded bg-slate-700 px-3 py-1.5 text-sm text-white hover:bg-slate-800"
      >
        {t('BI.Export.Button', { defaultValue: 'Export' })}
      </button>
      {open ? (
        <div className="absolute right-0 z-10 mt-1 w-32 rounded-md border border-slate-200 bg-white shadow-md dark:border-slate-700 dark:bg-slate-900">
          {(['Pdf', 'Xlsx', 'Csv'] as BIExportFormat[]).map((f) => (
            <button
              key={f}
              type="button"
              onClick={() => handle(f)}
              className="block w-full px-3 py-1.5 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-800"
            >
              {f.toUpperCase()}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
};
