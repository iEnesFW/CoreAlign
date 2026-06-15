const CARDINAL_TOLERANCE_DEG = 5;
const GRID_STEP_DEG = 15;
const GRID_TOLERANCE_DEG = 2.5;

export const snapAngleDeg = (deg: number): number => {
  const cardinal = Math.round(deg / 90) * 90;
  if (Math.abs(deg - cardinal) <= CARDINAL_TOLERANCE_DEG) return cardinal;
  const grid = Math.round(deg / GRID_STEP_DEG) * GRID_STEP_DEG;
  if (Math.abs(deg - grid) <= GRID_TOLERANCE_DEG) return grid;
  return Math.round(deg);
};
