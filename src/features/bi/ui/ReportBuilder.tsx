import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { BIDataSource, BIQueryConfig, BIQueryFilter } from '../model/bi.types';

interface Props {
  initialDataSource?: BIDataSource;
  initialConfig?: BIQueryConfig;
  onChange: (dataSource: BIDataSource, config: BIQueryConfig) => void;
}

const DATA_SOURCES: BIDataSource[] = [
  'Sales',
  'Inventory',
  'Warranty',
  'Service',
  'Cash',
  'AR',
  'AP',
];
const AGGREGATIONS = ['Sum', 'Count', 'Avg', 'Min', 'Max'] as const;
const OPERATORS = ['Equals', 'NotEquals', 'GreaterThan', 'LessThan', 'Contains'] as const;

export const ReportBuilder = ({ initialDataSource, initialConfig, onChange }: Props) => {
  const { t } = useTranslation();
  const [dataSource, setDataSource] = useState<BIDataSource>(initialDataSource ?? 'Sales');
  const [groupBy, setGroupBy] = useState<string>(initialConfig?.groupBy ?? '');
  const [aggregation, setAggregation] = useState<string>(initialConfig?.aggregation ?? 'Sum');
  const [measureField, setMeasureField] = useState<string>(initialConfig?.measureField ?? '');
  const [fromUtc, setFromUtc] = useState<string>(initialConfig?.fromUtc ?? '');
  const [toUtc, setToUtc] = useState<string>(initialConfig?.toUtc ?? '');
  const [filters, setFilters] = useState<BIQueryFilter[]>(initialConfig?.filters ?? []);

  const emit = (next: Partial<BIQueryConfig> & { dataSource?: BIDataSource } = {}) => {
    const newConfig: BIQueryConfig = {
      groupBy: groupBy || null,
      aggregation: aggregation || null,
      measureField: measureField || null,
      fromUtc: fromUtc || null,
      toUtc: toUtc || null,
      filters,
      limit: null,
      ...next,
    };
    onChange(next.dataSource ?? dataSource, newConfig);
  };

  const addFilter = () => {
    const next = [...filters, { field: '', operator: 'Equals', value: '' } as BIQueryFilter];
    setFilters(next);
    emit({ filters: next });
  };

  const updateFilter = (idx: number, patch: Partial<BIQueryFilter>) => {
    const next = filters.map((f, i) => (i === idx ? { ...f, ...patch } : f));
    setFilters(next);
    emit({ filters: next });
  };

  const removeFilter = (idx: number) => {
    const next = filters.filter((_, i) => i !== idx);
    setFilters(next);
    emit({ filters: next });
  };

  return (
    <div className="space-y-4 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.DataSource', { defaultValue: 'Data Source' })}
          </span>
          <select
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={dataSource}
            onChange={(e) => {
              const next = e.target.value as BIDataSource;
              setDataSource(next);
              emit({ dataSource: next });
            }}
          >
            {DATA_SOURCES.map((ds) => (
              <option key={ds} value={ds}>
                {ds}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.GroupBy', { defaultValue: 'Group By' })}
          </span>
          <input
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={groupBy}
            onChange={(e) => {
              setGroupBy(e.target.value);
              emit({ groupBy: e.target.value });
            }}
          />
        </label>
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.Aggregation', { defaultValue: 'Aggregation' })}
          </span>
          <select
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={aggregation}
            onChange={(e) => {
              setAggregation(e.target.value);
              emit({ aggregation: e.target.value });
            }}
          >
            {AGGREGATIONS.map((a) => (
              <option key={a} value={a}>
                {a}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.MeasureField', { defaultValue: 'Measure Field' })}
          </span>
          <input
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={measureField}
            onChange={(e) => {
              setMeasureField(e.target.value);
              emit({ measureField: e.target.value });
            }}
          />
        </label>
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.FromDate', { defaultValue: 'From' })}
          </span>
          <input
            type="date"
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={fromUtc.substring(0, 10)}
            onChange={(e) => {
              const v = e.target.value ? `${e.target.value}T00:00:00Z` : '';
              setFromUtc(v);
              emit({ fromUtc: v });
            }}
          />
        </label>
        <label className="block text-sm">
          <span className="text-slate-600 dark:text-slate-300">
            {t('BI.Builder.ToDate', { defaultValue: 'To' })}
          </span>
          <input
            type="date"
            className="mt-1 block w-full rounded border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800"
            value={toUtc.substring(0, 10)}
            onChange={(e) => {
              const v = e.target.value ? `${e.target.value}T23:59:59Z` : '';
              setToUtc(v);
              emit({ toUtc: v });
            }}
          />
        </label>
      </div>

      <div>
        <div className="mb-2 flex items-center justify-between">
          <h4 className="text-sm font-medium text-slate-700 dark:text-slate-200">
            {t('BI.Builder.Filters', { defaultValue: 'Filters' })}
          </h4>
          <button
            type="button"
            onClick={addFilter}
            className="rounded bg-primary-600 px-3 py-1 text-xs text-white hover:bg-primary-700"
          >
            {t('BI.Builder.AddFilter', { defaultValue: 'Add filter' })}
          </button>
        </div>
        <div className="space-y-2">
          {filters.map((f, idx) => (
            <div key={idx} className="grid grid-cols-12 gap-2">
              <input
                className="col-span-4 rounded border-slate-300 bg-white p-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                placeholder={t('BI.Builder.Field', { defaultValue: 'Field' })}
                value={f.field}
                onChange={(e) => updateFilter(idx, { field: e.target.value })}
              />
              <select
                className="col-span-3 rounded border-slate-300 bg-white p-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                value={f.operator}
                onChange={(e) => updateFilter(idx, { operator: e.target.value })}
              >
                {OPERATORS.map((op) => (
                  <option key={op} value={op}>
                    {op}
                  </option>
                ))}
              </select>
              <input
                className="col-span-4 rounded border-slate-300 bg-white p-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
                placeholder={t('BI.Builder.Value', { defaultValue: 'Value' })}
                value={f.value ?? ''}
                onChange={(e) => updateFilter(idx, { value: e.target.value })}
              />
              <button
                type="button"
                onClick={() => removeFilter(idx)}
                className="col-span-1 rounded text-sm text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-900/20"
              >
                {'×'}
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
