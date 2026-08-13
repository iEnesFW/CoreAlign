export interface DocumentTotalsLine {
  productId?: string;
  quantity?: number | string;
  unitPrice?: number | string;
  lineDiscountPercent?: number | string;
  taxRatePercent?: number | string;
  withholdingRatePercent?: number | string;
  withholdingTaxCodeId?: string;
}

export interface WithholdingFraction {
  numerator: number;
  denominator: number;
}

export interface DocumentTotalsInput {
  lines: readonly DocumentTotalsLine[] | undefined;
  headerDiscountPercent?: number | string;
  shippingCost?: number | string;
  withholdingCodeById?: ReadonlyMap<string, WithholdingFraction>;
}

export interface DocumentTotals {
  subtotal: number;
  lineDiscount: number;
  headerDiscount: number;
  taxableTotal: number;
  tax: number;
  withholding: number;
  shipping: number;
  grandTotal: number;
  taxPct: number | null;
  withholdingPct: number | null;
  lineDiscountPct: number | null;
  headerDiscountPct: number;
}

const num = (value: unknown): number => Number(value) || 0;

export const computeDocumentTotals = ({
  lines,
  headerDiscountPercent,
  shippingCost,
  withholdingCodeById,
}: DocumentTotalsInput): DocumentTotals => {
  const rows = lines ?? [];
  let subtotal = 0;
  let lineDiscount = 0;
  let tax = 0;
  let withholding = 0;

  for (const l of rows) {
    const gross = num(l.quantity) * num(l.unitPrice);
    const disc = gross * (num(l.lineDiscountPercent) / 100);
    const net = gross - disc;
    subtotal += gross;
    lineDiscount += disc;
    const lineTax = net * (num(l.taxRatePercent) / 100);
    tax += lineTax;
    const code = l.withholdingTaxCodeId
      ? withholdingCodeById?.get(l.withholdingTaxCodeId)
      : undefined;
    withholding +=
      code && code.denominator > 0
        ? lineTax * (code.numerator / code.denominator)
        : net * (num(l.withholdingRatePercent) / 100);
  }

  const afterLineDiscount = subtotal - lineDiscount;
  const headerDiscount = afterLineDiscount * (num(headerDiscountPercent) / 100);
  const taxableTotal = afterLineDiscount - headerDiscount;
  const shipping = num(shippingCost);
  const grandTotal = taxableTotal + tax - withholding + shipping;

  const activeLines = rows.filter((l) => l.productId);
  const uniformPct = (pick: (line: DocumentTotalsLine) => unknown): number | null => {
    if (activeLines.length === 0) return null;
    const rates = activeLines.map((l) => num(pick(l)));
    const first = rates[0];
    if (!rates.every((r) => r === first)) return null;
    return first > 0 ? first : null;
  };

  return {
    subtotal,
    lineDiscount,
    headerDiscount,
    taxableTotal,
    tax,
    withholding,
    shipping,
    grandTotal,
    taxPct: uniformPct((l) => l.taxRatePercent),
    withholdingPct: uniformPct((l) => l.withholdingRatePercent),
    lineDiscountPct: uniformPct((l) => l.lineDiscountPercent),
    headerDiscountPct: num(headerDiscountPercent),
  };
};
