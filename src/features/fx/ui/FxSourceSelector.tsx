import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import type { FxSourceCode } from '../api/fxRatesApi';
import { useFxPreferencesQuery, useUpdateFxPreferencesMutation } from '../hooks/useFxRates';

const FX_SOURCES: FxSourceCode[] = ['TCMB', 'ECB', 'MANUAL'];
const DEFAULT_CURRENCIES = ['USD', 'EUR', 'GBP', 'JPY', 'CHF'];

interface FxSourceSelectorProps {
  managedCurrencies?: readonly string[];
}

export const FxSourceSelector = ({
  managedCurrencies = DEFAULT_CURRENCIES,
}: FxSourceSelectorProps) => {
  const { t } = useTranslation();
  const { data, isLoading } = useFxPreferencesQuery();
  const mutation = useUpdateFxPreferencesMutation();

  const [defaultSource, setDefaultSource] = useState<FxSourceCode>('TCMB');
  const [overrides, setOverrides] = useState<Record<string, FxSourceCode>>({});

  const [syncedData, setSyncedData] = useState(data);
  if (data && data !== syncedData) {
    setSyncedData(data);
    setDefaultSource((data.defaultSource as FxSourceCode) ?? 'TCMB');
    const normalized: Record<string, FxSourceCode> = {};
    for (const [code, source] of Object.entries(data.perCurrencyOverrides ?? {})) {
      normalized[code.toUpperCase()] = source as FxSourceCode;
    }
    setOverrides(normalized);
  }

  const rows = useMemo(
    () => managedCurrencies.map((code) => code.toUpperCase()),
    [managedCurrencies],
  );

  const handleSave = async () => {
    const payload = {
      defaultSource,
      perCurrencyOverrides: overrides,
    };
    try {
      await mutation.mutateAsync(payload);
      toast.success(t('Fx.Preferences.Saved', 'FX preferences saved'));
    } catch {
      toast.error(t('Fx.Preferences.SaveFailed', 'Failed to save FX preferences'));
    }
  };

  if (isLoading) {
    return (
      <div className="rounded-md bg-slate-100 px-4 py-3 text-sm text-slate-500 dark:bg-slate-800 dark:text-slate-400">
        {t('Fx.Preferences.Loading', 'Loading FX preferences...')}
      </div>
    );
  }

  return (
    <section
      className="space-y-4 rounded-md border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"
      data-testid="fx-source-selector"
    >
      <header>
        <h3 className="text-base font-semibold text-slate-800 dark:text-slate-200">
          {t('Fx.Preferences.Title', 'FX Source Preferences')}
        </h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t(
            'Fx.Preferences.Description',
            'Choose which FX source the tenant uses by default and override per currency.',
          )}
        </p>
      </header>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
        <label
          htmlFor="fx-default-source"
          className="text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Fx.Preferences.DefaultSource', 'Default Source')}
        </label>
        <select
          id="fx-default-source"
          value={defaultSource}
          onChange={(event) => setDefaultSource(event.target.value as FxSourceCode)}
          className="w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm focus:border-success-500 focus:outline-none dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 sm:w-48"
        >
          {FX_SOURCES.map((source) => (
            <option key={source} value={source}>
              {source}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:border-slate-700 dark:text-slate-400">
              <th className="px-3 py-2">{t('Fx.Preferences.Currency', 'Currency')}</th>
              <th className="px-3 py-2">{t('Fx.Preferences.OverrideSource', 'Override Source')}</th>
              <th className="px-3 py-2 text-right">{t('Fx.Preferences.Reset', 'Reset')}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((code) => {
              const current = overrides[code];
              return (
                <tr key={code} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-200">
                    {code}
                  </td>
                  <td className="px-3 py-2">
                    <select
                      value={current ?? 'DEFAULT'}
                      onChange={(event) => {
                        const value = event.target.value;
                        setOverrides((prev) => {
                          const next = { ...prev };
                          if (value === 'DEFAULT') {
                            delete next[code];
                          } else {
                            next[code] = value as FxSourceCode;
                          }
                          return next;
                        });
                      }}
                      className="rounded-md border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
                    >
                      <option value="DEFAULT">
                        {t('Fx.Preferences.UseDefault', 'Use default')}
                      </option>
                      {FX_SOURCES.map((source) => (
                        <option key={source} value={source}>
                          {source}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-3 py-2 text-right">
                    {current && (
                      <button
                        type="button"
                        className="text-xs font-medium text-success-700 hover:text-success-900 dark:text-success-400"
                        onClick={() =>
                          setOverrides((prev) => {
                            const next = { ...prev };
                            delete next[code];
                            return next;
                          })
                        }
                      >
                        {t('Fx.Preferences.Clear', 'Clear')}
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="flex justify-end">
        <button
          type="button"
          className="rounded-md bg-success-600 px-4 py-2 text-sm font-semibold text-white hover:bg-success-700 disabled:opacity-50"
          onClick={handleSave}
          disabled={mutation.isPending}
        >
          {mutation.isPending
            ? t('Fx.Preferences.Saving', 'Saving...')
            : t('Fx.Preferences.Save', 'Save preferences')}
        </button>
      </div>
    </section>
  );
};
