export const fmtCurrency = (value: number, currency: string, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value);
  } catch {
    return `${value.toFixed(2)} ${currency}`;
  }
};

export const fmtNumber = (value: number, locale: string, decimals = 0) =>
  new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);

export const fmtPercent = (value: number, locale: string) => {
  try {
    return new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(value) + '%';
  } catch {
    return `${value.toFixed(1)}%`;
  }
};
