import { useMemo } from 'react';
import { useCountriesQuery, useDistrictsQuery, useProvincesQuery } from '../hooks/useLookups';

interface Props {
  country: string;
  state: string;
  city: string;
  onCountryChange: (value: string) => void;
  onStateChange: (value: string) => void;
  onCityChange: (value: string) => void;
  labels: { country: string; province: string; district: string };
  /** Country code used to scope provinces (TR by default). */
  provinceCountryCode?: string;
  selectClassName?: string;
}

const defaultSelectCls =
  'w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

/**
 * Country / province (il) / district (ilçe) selects sourced from the global
 * lookup tables. Values are stored as human-readable names (matching the address
 * snapshot fields). District options are scoped to the selected province.
 */
export const AddressRegionFields = ({
  country,
  state,
  city,
  onCountryChange,
  onStateChange,
  onCityChange,
  labels,
  provinceCountryCode = 'TR',
  selectClassName,
}: Props) => {
  const cls = selectClassName ?? defaultSelectCls;
  const countriesData = useCountriesQuery(true).data?.data;
  const provincesData = useProvincesQuery(provinceCountryCode).data?.data;
  const countries = useMemo(() => countriesData ?? [], [countriesData]);
  const provinces = useMemo(() => provincesData ?? [], [provincesData]);

  const selectedProvinceId = useMemo(
    () => provinces.find((p) => p.name === state)?.id ?? null,
    [provinces, state],
  );
  const districts = useDistrictsQuery(selectedProvinceId).data?.data ?? [];

  const countryHasValue = country && countries.some((c) => c.name === country);
  const stateHasValue = state && provinces.some((p) => p.name === state);
  const cityHasValue = city && districts.some((d) => d.name === city);

  const labelCls =
    'mb-0.5 block text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400';

  return (
    <>
      <label className="block">
        <span className={labelCls}>{labels.province}</span>
        <select
          className={cls}
          value={state}
          onChange={(e) => {
            onStateChange(e.target.value);
            onCityChange(''); // district depends on province
          }}
        >
          <option value="">—</option>
          {!stateHasValue && state && <option value={state}>{state}</option>}
          {provinces.map((p) => (
            <option key={p.id} value={p.name}>
              {p.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className={labelCls}>{labels.district}</span>
        <select
          className={cls}
          value={city}
          disabled={!selectedProvinceId}
          onChange={(e) => onCityChange(e.target.value)}
        >
          <option value="">—</option>
          {!cityHasValue && city && <option value={city}>{city}</option>}
          {districts.map((d) => (
            <option key={d.id} value={d.name}>
              {d.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className={labelCls}>{labels.country}</span>
        <select className={cls} value={country} onChange={(e) => onCountryChange(e.target.value)}>
          <option value="">—</option>
          {!countryHasValue && country && <option value={country}>{country}</option>}
          {countries.map((c) => (
            <option key={c.code} value={c.name}>
              {c.name}
            </option>
          ))}
        </select>
      </label>
    </>
  );
};
