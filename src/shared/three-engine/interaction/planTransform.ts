export interface PlanPosition {
  x: number;
  y: number;
}

const DEG2RAD = Math.PI / 180;

export const rotatePlanPointDeg = (
  xMm: number,
  yMm: number,
  pivotXMm: number,
  pivotYMm: number,
  sweepDeg: number,
): PlanPosition => {
  const rad = sweepDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  const dx = xMm - pivotXMm;
  const dy = yMm - pivotYMm;
  return { x: pivotXMm + dx * cos - dy * sin, y: pivotYMm + dx * sin + dy * cos };
};
