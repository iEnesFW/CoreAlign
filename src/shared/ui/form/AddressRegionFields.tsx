import { useEffect, useMemo, useRef, useState } from 'react';
import type { ICity, ICountry, IState } from 'country-state-city';
import { detectRegionCode } from '@/shared/lib/locale';

interface CscModule {
  Country: { getAllCountries(): ICountry[] };
  State: { getStatesOfCountry(countryCode: string): IState[] };
  City: { getCitiesOfState(countryCode: string, stateCode: string): ICity[] };
}

interface Props {
  country: string;
  state: string;
  city: string;
  onCountryChange: (value: string) => void;
  onStateChange: (value: string) => void;
  onCityChange: (value: string) => void;
  labels: { country: string; province: string; district: string };
  provinceCountryCode?: string;
  selectClassName?: string;
}

const defaultSelectCls =
  'w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

export const AddressRegionFields = ({
  country,
  state,
  city,
  onCountryChange,
  onStateChange,
  onCityChange,
  labels,
  selectClassName,
}: Props) => {
  const cls = selectClassName ?? defaultSelectCls;
  const [csc, setCsc] = useState<CscModule | null>(null);

  useEffect(() => {
    let active = true;
    void import('country-state-city').then((mod) => {
      if (active) setCsc(mod);
    });
    return () => {
      active = false;
    };
  }, []);

  const countries = useMemo(() => {
    if (!csc) return [];
    return csc.Country.getAllCountries().map((c) =>
      c.isoCode === 'TR' ? { ...c, name: 'Türkiye' } : c,
    );
  }, [csc]);

  const selectedCountryCode = useMemo(
    () => countries.find((c) => c.name === country)?.isoCode ?? null,
    [countries, country],
  );

  const states = useMemo(
    () => (csc && selectedCountryCode ? csc.State.getStatesOfCountry(selectedCountryCode) : []),
    [csc, selectedCountryCode],
  );

  const selectedStateCode = useMemo(
    () => states.find((s) => s.name === state)?.isoCode ?? null,
    [states, state],
  );

  const cities = useMemo(
    () =>
      csc && selectedCountryCode && selectedStateCode
        ? csc.City.getCitiesOfState(selectedCountryCode, selectedStateCode)
        : [],
    [csc, selectedCountryCode, selectedStateCode],
  );

  const detectedCountryName = useMemo(() => {
    const code = detectRegionCode()?.toUpperCase();
    if (!code) return '';
    return countries.find((c) => c.isoCode === code)?.name ?? '';
  }, [countries]);

  const hasAutoFilled = useRef(false);

  useEffect(() => {
    if (!country && detectedCountryName && !hasAutoFilled.current) {
      hasAutoFilled.current = true;
      const id = setTimeout(() => onCountryChange(detectedCountryName), 10);
      return () => clearTimeout(id);
    }
  }, [country, detectedCountryName, onCountryChange]);

  const countryHasValue = country && countries.some((c) => c.name === country);
  const stateHasValue = state && states.some((s) => s.name === state);
  const cityHasValue = city && cities.some((c) => c.name === city);

  const labelCls =
    'mb-0.5 block text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400';

  return (
    <>
      <label className="block">
        <span className={labelCls}>{labels.country}</span>
        <select
          className={cls}
          value={country}
          onChange={(e) => {
            onCountryChange(e.target.value);
            onStateChange('');
            onCityChange('');
          }}
        >
          <option value="">—</option>
          {!countryHasValue && country && <option value={country}>{country}</option>}
          {countries.map((c) => (
            <option key={c.isoCode} value={c.name}>
              {c.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className={labelCls}>{labels.province}</span>
        <select
          className={cls}
          value={state}
          disabled={!selectedCountryCode}
          onChange={(e) => {
            onStateChange(e.target.value);
            onCityChange('');
          }}
        >
          <option value="">—</option>
          {!stateHasValue && state && <option value={state}>{state}</option>}
          {states.map((s) => (
            <option key={s.isoCode} value={s.name}>
              {s.name}
            </option>
          ))}
        </select>
      </label>

      <label className="block">
        <span className={labelCls}>{labels.district}</span>
        <select
          className={cls}
          value={city}
          disabled={!selectedStateCode}
          onChange={(e) => onCityChange(e.target.value)}
        >
          <option value="">—</option>
          {!cityHasValue && city && <option value={city}>{city}</option>}
          {cities.map((c) => (
            <option key={c.name} value={c.name}>
              {c.name}
            </option>
          ))}
        </select>
      </label>
    </>
  );
};
