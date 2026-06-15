import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ReportBuilder } from '@/features/bi/ui/ReportBuilder';
import { TableWidget } from '@/features/bi/ui/TableWidget';
import { biApi } from '@/features/bi/api/biApi';
import { useCreateReport } from '@/features/bi/hooks/useReports';
import type { BIDataSource, BIQueryConfig, BIResult } from '@/features/bi/model/bi.types';

export const ReportBuilderPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [dataSource, setDataSource] = useState<BIDataSource>('Sales');
  const [config, setConfig] = useState<BIQueryConfig>({ filters: [] });
  const [preview, setPreview] = useState<BIResult | null>(null);
  const createMutation = useCreateReport();

  const handlePreview = async () => {
    const [data, error] = await biApi.executeAdHoc(dataSource, config);
    if (error) {
      toast.error(t('BI.Builder.PreviewFailed', { defaultValue: 'Preview failed' }));
      return;
    }
    setPreview(data ?? null);
  };

  const handleSave = async () => {
    if (!name.trim()) {
      toast.error(t('BI.Builder.NameRequired', { defaultValue: 'Name is required' }));
      return;
    }
    try {
      const created = await createMutation.mutateAsync({
        name: name.trim(),
        dataSource,
        queryConfigJson: JSON.stringify(config),
        isPublic,
      });
      toast.success(t('BI.Builder.Saved', { defaultValue: 'Report saved' }));
      navigate(`/bi/reports/${created.id}`);
    } catch {
      toast.error(t('BI.Builder.SaveFailed', { defaultValue: 'Failed to save report' }));
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-50">
          {t('BI.Builder.Title', { defaultValue: 'Report builder' })}
        </h1>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handlePreview}
            className="rounded bg-slate-700 px-3 py-1.5 text-sm text-white hover:bg-slate-800"
          >
            {t('BI.Builder.Preview', { defaultValue: 'Preview' })}
          </button>
          <button
            type="button"
            onClick={handleSave}
            className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700"
          >
            {t('BI.Builder.Save', { defaultValue: 'Save' })}
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <label className="block text-sm sm:col-span-2">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.ReportName', { defaultValue: 'Report name' })}
          </span>
          <input
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </label>
        <label className="flex items-end gap-2 text-sm">
          <input
            type="checkbox"
            checked={isPublic}
            onChange={(e) => setIsPublic(e.target.checked)}
          />
          <span>{t('BI.Builder.Public', { defaultValue: 'Visible to all tenant users' })}</span>
        </label>
      </div>

      <ReportBuilder
        initialDataSource={dataSource}
        initialConfig={config}
        onChange={(ds, cfg) => {
          setDataSource(ds);
          setConfig(cfg);
        }}
      />

      {preview ? (
        <div className="h-96">
          <TableWidget
            title={t('BI.Builder.PreviewResult', { defaultValue: 'Preview' })}
            result={preview}
          />
        </div>
      ) : null}
    </div>
  );
};

export default ReportBuilderPage;
