export const MIN_PANEL_MM = 100;

export const cascadePanelWidths = (
  widths: number[],
  i: number,
  j: number,
  deltaMm: number,
): number[] => {
  if (i < 0 || j < 0 || i === j || i >= widths.length || j >= widths.length) return widths;
  const delta = Math.round(deltaMm);
  if (delta === 0) return widths;
  const out = widths.slice();
  if (delta > 0) {
    const dir = j > i ? 1 : -1;
    const chain: number[] = [];
    for (let k = j; k >= 0 && k < out.length; k += dir) chain.push(k);
    const capacity = chain.reduce((sum, k) => sum + (out[k] - MIN_PANEL_MM), 0);
    const applied = Math.min(delta, Math.max(0, capacity));
    if (applied <= 0) return widths;
    out[i] += applied;
    let remaining = applied;
    for (const k of chain) {
      if (remaining <= 0) break;
      const take = Math.min(remaining, out[k] - MIN_PANEL_MM);
      out[k] -= take;
      remaining -= take;
    }
  } else {
    const give = Math.min(-delta, out[i] - MIN_PANEL_MM);
    if (give <= 0) return widths;
    out[i] -= give;
    out[j] += give;
  }
  return out;
};
