const DEG2RAD = Math.PI / 180;

export interface BoxFootprint {
  originX: number;
  originY: number;
  lengthMm: number;
  crossMm: number;
  rotationDeg: number;
}

export interface CornerPoint {
  x: number;
  y: number;
}

const axes = (rotationDeg: number) => {
  const rad = rotationDeg * DEG2RAD;
  return {
    alongX: Math.cos(rad),
    alongY: Math.sin(rad),
    acrossX: -Math.sin(rad),
    acrossY: Math.cos(rad),
  };
};

// The four plan corners of a box whose origin is the centreline start (centred across the
// thickness/depth). Order: 0 start-/, 1 end-/, 2 end-+, 3 start-+ (across sign), CCW-ish.
export const boxCornersMm = (box: BoxFootprint): CornerPoint[] => {
  const { alongX, alongY, acrossX, acrossY } = axes(box.rotationDeg);
  const h = box.crossMm / 2;
  const local: [number, number][] = [
    [0, -h],
    [box.lengthMm, -h],
    [box.lengthMm, h],
    [0, h],
  ];
  return local.map(([u, v]) => ({
    x: box.originX + u * alongX + v * acrossX,
    y: box.originY + u * alongY + v * acrossY,
  }));
};

// Soft-stick a value to the nearest multiple of `stepMm` when it lands within `tolMm` of it, so a
// resize gently catches round dimensions (100, 200, 300…); otherwise it stays free.
const stickRound = (valueMm: number, stepMm: number, tolMm: number): number => {
  if (stepMm <= 0) return valueMm;
  const nearest = Math.round(valueMm / stepMm) * stepMm;
  return Math.abs(valueMm - nearest) <= tolMm ? nearest : valueMm;
};

// Resize a box by dragging one corner to a new world position while the diagonally-opposite
// corner stays fixed. Returns the new origin (centreline start) + length + cross, rotation
// unchanged. Length/cross are clamped to minMm so the box can't collapse or invert. When
// stickStepMm > 0 the length/cross softly stick to round multiples within stickTolMm.
export const resizeBoxFromCorner = (
  box: BoxFootprint,
  cornerIndex: number,
  newX: number,
  newY: number,
  minMm = 50,
  stickStepMm = 0,
  stickTolMm = 0,
): BoxFootprint => {
  const { alongX, alongY, acrossX, acrossY } = axes(box.rotationDeg);
  const h = box.crossMm / 2;
  const localCorners: [number, number][] = [
    [0, -h],
    [box.lengthMm, -h],
    [box.lengthMm, h],
    [0, h],
  ];
  const opp = localCorners[(cornerIndex + 2) % 4];
  let uP = (newX - box.originX) * alongX + (newY - box.originY) * alongY;
  let vP = (newX - box.originX) * acrossX + (newY - box.originY) * acrossY;
  // Stick the DIMENSION (distance from the pinned opposite corner) to round numbers, then put the
  // dragged corner back at that distance so its direction from the anchor is preserved.
  if (stickStepMm > 0) {
    const stuckU = stickRound(Math.abs(uP - opp[0]), stickStepMm, stickTolMm);
    const stuckV = stickRound(Math.abs(vP - opp[1]), stickStepMm, stickTolMm);
    uP = opp[0] + Math.sign(uP - opp[0] || 1) * stuckU;
    vP = opp[1] + Math.sign(vP - opp[1] || 1) * stuckV;
  }
  const uMin = Math.min(opp[0], uP);
  const uMax = Math.max(opp[0], uP);
  const vMin = Math.min(opp[1], vP);
  const vMax = Math.max(opp[1], vP);
  const lengthMm = Math.max(minMm, Math.round(uMax - uMin));
  const crossMm = Math.max(minMm, Math.round(vMax - vMin));
  const vCenter = (vMin + vMax) / 2;
  return {
    originX: Math.round(box.originX + uMin * alongX + vCenter * acrossX),
    originY: Math.round(box.originY + uMin * alongY + vCenter * acrossY),
    lengthMm,
    crossMm,
    rotationDeg: box.rotationDeg,
  };
};
