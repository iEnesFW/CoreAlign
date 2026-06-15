import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Download, Play, Plus, Save, Trash2 } from 'lucide-react';
import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

type EntityType = 'Invoice' | 'Order' | 'Customer' | 'Product' | 'StockMovement';

interface CatalogField {
  key: string;
  labelEn: string;
  labelTr: string;
  dataType: string;
  isDimension: boolean;
  isMeasureEligible: boolean;
  allowedOperators: string[];
  allowedAggregations: string[] | null;
}

interface CatalogGroup {
  entityType: EntityType;
  fields: CatalogField[];
}

interface MeasureRow {
  field: string;
  function: string;
  alias?: string;
}

interface FilterRow {
  field: string;
  operator: string;
  value: string;
  value2?: string;
}

interface PreviewRow {
  cells: Record<string, unknown>;
}

interface PreviewResponse {
  columns: string[];
  rows: PreviewRow[];
  rowCount: number;
  truncated: boolean;
}

interface SavedReportSummary {
  id: string;
  name: string;
  description: string | null;
  entityType: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

interface ApiEnvelope<T> {
  data: T;
}

const ENTITY_OPTIONS: EntityType[] = ['Invoice', 'Order', 'Customer', 'Product', 'StockMovement'];

export const CustomReportBuilder = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isTurkish = i18n.language?.startsWith('tr') ?? false;

  const [catalog, setCatalog] = useState<CatalogGroup[]>([]);
  const [entityType, setEntityType] = useState<EntityType>('Invoice');
  const [dimensions, setDimensions] = useState<string[]>([]);
  const [measures, setMeasures] = useState<MeasureRow[]>([]);
  const [filters, setFilters] = useState<FilterRow[]>([]);
  const [limit, setLimit] = useState<number>(500);
  const [reportName, setReportName] = useState('');
  const [description] = useState('');
  const [preview, setPreview] = useState<PreviewResponse | null>(null);
  const [busy, setBusy] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [savedReports, setSavedReports] = useState<SavedReportSummary[]>([]);

  const activeGroup = useMemo(
    () => catalog.find((g) => g.entityType === entityType),
    [catalog, entityType],
  );

  const refreshSaved = async () => {
    const [response] = await safeRequest(
      apiClient.get<ApiEnvelope<SavedReportSummary[]>>('/reports/custom'),
    );
    if (response) setSavedReports(response.data.data);
  };

  useEffect(() => {
    let cancelled = false;
    const initial = async () => {
      const [catalogResponse, catalogErr] = await safeRequest(
        apiClient.get<ApiEnvelope<CatalogGroup[]>>('/reports/custom/catalog'),
      );
      if (cancelled) return;
      if (catalogErr) {
        logger.error('Failed to load catalog', catalogErr);
        setErrorMessage(t('reports.custom.error.catalog'));
      } else if (catalogResponse) {
        setCatalog(catalogResponse.data.data);
      }
      const [savedResponse] = await safeRequest(
        apiClient.get<ApiEnvelope<SavedReportSummary[]>>('/reports/custom'),
      );
      if (cancelled) return;
      if (savedResponse) setSavedReports(savedResponse.data.data);
    };
    void initial();
    return () => {
      cancelled = true;
    };
  }, [t]);

  const handleEntityChange = (next: EntityType) => {
    setEntityType(next);
    setDimensions([]);
    setMeasures([]);
    setFilters([]);
    setPreview(null);
  };

  const labelFor = (f: CatalogField) => (isTurkish ? f.labelTr : f.labelEn);

  const toggleDimension = (key: string) => {
    setDimensions((cur) => (cur.includes(key) ? cur.filter((c) => c !== key) : [...cur, key]));
  };

  const addMeasure = () => {
    const firstMeasure = activeGroup?.fields.find((f) => f.isMeasureEligible);
    if (!firstMeasure) return;
    const fn = firstMeasure.allowedAggregations?.[0] ?? 'Count';
    setMeasures((cur) => [...cur, { field: firstMeasure.key, function: fn }]);
  };

  const updateMeasure = (idx: number, patch: Partial<MeasureRow>) => {
    setMeasures((cur) => cur.map((m, i) => (i === idx ? { ...m, ...patch } : m)));
  };

  const removeMeasure = (idx: number) => setMeasures((cur) => cur.filter((_, i) => i !== idx));

  const addFilter = () => {
    const first = activeGroup?.fields[0];
    if (!first) return;
    setFilters((cur) => [
      ...cur,
      { field: first.key, operator: first.allowedOperators[0], value: '' },
    ]);
  };

  const updateFilter = (idx: number, patch: Partial<FilterRow>) => {
    setFilters((cur) => cur.map((f, i) => (i === idx ? { ...f, ...patch } : f)));
  };

  const removeFilter = (idx: number) => setFilters((cur) => cur.filter((_, i) => i !== idx));

  const buildPayload = () => ({
    entityType,
    dimensions,
    measures,
    filters,
    sortBy: null,
    limit,
  });

  const runPreview = async () => {
    setBusy(true);
    setErrorMessage(null);
    const [response, err] = await safeRequest(
      apiClient.post<ApiEnvelope<PreviewResponse>>('/reports/custom/preview', buildPayload()),
    );
    setBusy(false);
    if (err) {
      logger.error('Preview failed', err);
      setErrorMessage(t('reports.custom.error.preview'));
      return;
    }
    if (response) setPreview(response.data.data);
  };

  const saveDefinition = async () => {
    if (!reportName.trim()) {
      setErrorMessage(t('reports.custom.error.nameRequired'));
      return;
    }
    setBusy(true);
    setErrorMessage(null);
    const [, err] = await safeRequest(
      apiClient.post('/reports/custom', {
        name: reportName.trim(),
        description: description.trim() || null,
        definition: buildPayload(),
      }),
    );
    setBusy(false);
    if (err) {
      logger.error('Save failed', err);
      setErrorMessage(t('reports.custom.error.save'));
      return;
    }
    await refreshSaved();
  };

  const downloadSaved = async (id: string, fmt: 'pdf' | 'xlsx') => {
    const [response, err] = await safeRequest(
      apiClient.get<Blob>(`/reports/custom/${id}/run`, {
        params: { format: fmt },
        responseType: 'blob',
      }),
    );
    if (err || !response) {
      logger.error('Download failed', err);
      return;
    }
    const blob = response.data instanceof Blob ? response.data : new Blob([response.data]);
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `custom-${id}.${fmt}`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  };

  const deleteSaved = async (id: string) => {
    const [, err] = await safeRequest(apiClient.delete(`/reports/custom/${id}`));
    if (!err) await refreshSaved();
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <button
            type="button"
            onClick={() => navigate('/dashboard/reports')}
            className="mb-2 inline-flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
          >
            <ArrowLeft className="h-4 w-4" /> {t('common.back')}
          </button>
          <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-50">
            {t('reports.custom.title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('reports.custom.subtitle')}
          </p>
        </div>
      </div>

      {errorMessage && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/40 dark:text-red-300">
          {errorMessage}
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <section className="lg:col-span-4 space-y-4 rounded-md border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
          <h2 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('reports.custom.entityType')}
          </h2>
          <select
            value={entityType}
            onChange={(e) => handleEntityChange(e.target.value as EntityType)}
            className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            {ENTITY_OPTIONS.map((opt) => (
              <option key={opt} value={opt}>
                {t(`reports.custom.entity.${opt.toLowerCase()}`)}
              </option>
            ))}
          </select>

          <div>
            <h3 className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('reports.custom.dimensions')}
            </h3>
            <div className="space-y-1">
              {activeGroup?.fields
                .filter((f) => f.isDimension)
                .map((f) => (
                  <label
                    key={f.key}
                    className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200"
                  >
                    <input
                      type="checkbox"
                      checked={dimensions.includes(f.key)}
                      onChange={() => toggleDimension(f.key)}
                    />
                    {labelFor(f)}
                  </label>
                ))}
            </div>
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.custom.measures')}
              </h3>
              <button
                type="button"
                onClick={addMeasure}
                className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
              >
                <Plus className="h-3 w-3" /> {t('reports.custom.add')}
              </button>
            </div>
            <div className="space-y-2">
              {measures.map((m, idx) => {
                const fieldDesc = activeGroup?.fields.find((f) => f.key === m.field);
                const aggs = fieldDesc?.allowedAggregations ?? ['Count'];
                return (
                  <div key={idx} className="flex items-center gap-2 text-sm">
                    <select
                      value={m.field}
                      onChange={(e) => updateMeasure(idx, { field: e.target.value })}
                      className="flex-1 rounded-md border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    >
                      {activeGroup?.fields.map((f) => (
                        <option key={f.key} value={f.key}>
                          {labelFor(f)}
                        </option>
                      ))}
                    </select>
                    <select
                      value={m.function}
                      onChange={(e) => updateMeasure(idx, { function: e.target.value })}
                      className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    >
                      {aggs.map((a) => (
                        <option key={a} value={a}>
                          {a}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={() => removeMeasure(idx)}
                      className="text-slate-400 hover:text-red-500"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </div>
                );
              })}
            </div>
          </div>

          <div>
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.custom.filters')}
              </h3>
              <button
                type="button"
                onClick={addFilter}
                className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
              >
                <Plus className="h-3 w-3" /> {t('reports.custom.add')}
              </button>
            </div>
            <div className="space-y-2">
              {filters.map((f, idx) => {
                const fieldDesc = activeGroup?.fields.find((c) => c.key === f.field);
                return (
                  <div key={idx} className="flex items-center gap-2 text-xs">
                    <select
                      value={f.field}
                      onChange={(e) => updateFilter(idx, { field: e.target.value })}
                      className="flex-1 rounded-md border border-slate-300 bg-white px-2 py-1 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    >
                      {activeGroup?.fields.map((c) => (
                        <option key={c.key} value={c.key}>
                          {labelFor(c)}
                        </option>
                      ))}
                    </select>
                    <select
                      value={f.operator}
                      onChange={(e) => updateFilter(idx, { operator: e.target.value })}
                      className="rounded-md border border-slate-300 bg-white px-2 py-1 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    >
                      {(fieldDesc?.allowedOperators ?? []).map((op) => (
                        <option key={op} value={op}>
                          {op}
                        </option>
                      ))}
                    </select>
                    <input
                      value={f.value}
                      onChange={(e) => updateFilter(idx, { value: e.target.value })}
                      placeholder={t('reports.custom.value')}
                      className="w-24 rounded-md border border-slate-300 bg-white px-2 py-1 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                    />
                    <button
                      type="button"
                      onClick={() => removeFilter(idx)}
                      className="text-slate-400 hover:text-red-500"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
            <label className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('reports.custom.rowLimit')}
            </label>
            <input
              type="number"
              value={limit}
              min={1}
              max={5000}
              onChange={(e) => setLimit(Number.parseInt(e.target.value, 10) || 100)}
              className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </div>
        </section>

        <section className="lg:col-span-8 space-y-4">
          <div className="flex flex-wrap items-end gap-2">
            <div className="flex-1 min-w-[200px]">
              <label className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.custom.name')}
              </label>
              <input
                value={reportName}
                onChange={(e) => setReportName(e.target.value)}
                placeholder={t('reports.custom.namePlaceholder')}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </div>
            <button
              type="button"
              onClick={runPreview}
              disabled={busy}
              className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-500 disabled:opacity-50"
            >
              <Play className="h-4 w-4" /> {t('reports.custom.preview')}
            </button>
            <button
              type="button"
              onClick={saveDefinition}
              disabled={busy}
              className="inline-flex items-center gap-2 rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-800 disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200"
            >
              <Save className="h-4 w-4" /> {t('reports.custom.save')}
            </button>
          </div>

          <div className="rounded-md border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
            <div className="border-b border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:text-slate-200">
              {t('reports.custom.previewTitle')}
            </div>
            <div className="overflow-x-auto">
              {!preview ? (
                <p className="px-4 py-6 text-sm text-slate-500 dark:text-slate-400">
                  {t('reports.custom.previewEmpty')}
                </p>
              ) : (
                <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-700">
                  <thead className="bg-slate-50 dark:bg-slate-800/40">
                    <tr>
                      {preview.columns.map((col) => (
                        <th
                          key={col}
                          className="px-3 py-2 text-left font-medium text-slate-600 dark:text-slate-300"
                        >
                          {col}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                    {preview.rows.map((row, idx) => (
                      <tr key={idx}>
                        {preview.columns.map((col) => (
                          <td key={col} className="px-3 py-2 text-slate-700 dark:text-slate-300">
                            {row.cells[col]?.toString() ?? '—'}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>

          <div className="rounded-md border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
            <div className="border-b border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:text-slate-200">
              {t('reports.custom.savedTitle')}
            </div>
            <ul className="divide-y divide-slate-200 dark:divide-slate-700">
              {savedReports.length === 0 && (
                <li className="px-4 py-3 text-sm text-slate-500 dark:text-slate-400">
                  {t('reports.custom.savedEmpty')}
                </li>
              )}
              {savedReports.map((s) => (
                <li key={s.id} className="flex items-center justify-between px-4 py-2 text-sm">
                  <div>
                    <p className="font-medium text-slate-800 dark:text-slate-100">{s.name}</p>
                    <p className="text-xs text-slate-500 dark:text-slate-400">{s.entityType}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => void downloadSaved(s.id, 'pdf')}
                      className="inline-flex items-center gap-1 rounded-md border border-slate-300 px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"
                    >
                      <Download className="h-3 w-3" /> PDF
                    </button>
                    <button
                      type="button"
                      onClick={() => void downloadSaved(s.id, 'xlsx')}
                      className="inline-flex items-center gap-1 rounded-md border border-slate-300 px-2 py-1 text-xs hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800"
                    >
                      <Download className="h-3 w-3" /> XLSX
                    </button>
                    <button
                      type="button"
                      onClick={() => void deleteSaved(s.id)}
                      className="text-slate-400 hover:text-red-500"
                    >
                      <Trash2 className="h-3 w-3" />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        </section>
      </div>
    </div>
  );
};

export default CustomReportBuilder;
