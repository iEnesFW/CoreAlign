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

// Resize a box by dragging one corner to a new world position while the diagonally-opposite
// corner stays fixed. Returns the new origin (centreline start) + length + cross, rotation
// unchanged. Length/cross are clamped to minMm so the box can't collapse or invert.
export const resizeBoxFromCorner = (
  box: BoxFootprint,
  cornerIndex: number,
  newX: number,
  newY: number,
  minMm = 50,
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
  const uP = (newX - box.originX) * alongX + (newY - box.originY) * alongY;
  const vP = (newX - box.originX) * acrossX + (newY - box.originY) * acrossY;
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
