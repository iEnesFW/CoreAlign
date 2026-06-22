const DEG2RAD = Math.PI / 180;
const LATERAL_TOL_MM = 80;
const PARALLEL_TOL = 0.05;

export interface StretchBody {
  id: string;
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  minLengthMm: number;
}

export interface PushResolution {
  selfGrowMm: number;
  neighbour?: {
    id: string;
    newLengthMm: number;
    newOriginX: number;
    newOriginY: number;
  };
}

export const computeNeighbourShrink = (
  a: StretchBody,
  face: 'start' | 'end',
  b: StretchBody,
  desiredGrowMm: number,
  clampedGrowMm: number,
): PushResolution => {
  const safe = Math.max(0, Math.min(desiredGrowMm, clampedGrowMm));
  if (desiredGrowMm <= 0 || clampedGrowMm >= desiredGrowMm || a.id === b.id) {
    return { selfGrowMm: safe };
  }

  const aRad = a.rotationDeg * DEG2RAD;
  const ax = Math.cos(aRad);
  const ay = Math.sin(aRad);
  const gx = face === 'end' ? ax : -ax;
  const gy = face === 'end' ? ay : -ay;

  const bRad = b.rotationDeg * DEG2RAD;
  const bx = Math.cos(bRad);
  const by = Math.sin(bRad);
  if (Math.abs(gx * by - gy * bx) > PARALLEL_TOL) return { selfGrowMm: safe };

  const edgeX = face === 'end' ? a.originX + a.lengthMm * ax : a.originX;
  const edgeY = face === 'end' ? a.originY + a.lengthMm * ay : a.originY;
  const projectAlong = (px: number, py: number) => (px - edgeX) * gx + (py - edgeY) * gy;
  const projectLateral = (px: number, py: number) => {
    const rx = px - edgeX;
    const ry = py - edgeY;
    const along = rx * gx + ry * gy;
    return Math.hypot(rx - along * gx, ry - along * gy);
  };

  const b1x = b.originX + b.lengthMm * bx;
  const b1y = b.originY + b.lengthMm * by;
  if (
    projectLateral(b.originX, b.originY) > LATERAL_TOL_MM ||
    projectLateral(b1x, b1y) > LATERAL_TOL_MM
  ) {
    return { selfGrowMm: safe };
  }

  const originAlong = projectAlong(b.originX, b.originY);
  const endAlong = projectAlong(b1x, b1y);
  const nearIsOrigin = originAlong <= endAlong;
  const gap = Math.max(0, Math.min(originAlong, endAlong));
  const capacity = Math.max(0, b.lengthMm - b.minLengthMm);
  const give = Math.max(0, Math.min(desiredGrowMm - gap, capacity));
  if (give <= 0) return { selfGrowMm: safe };

  const newLengthMm = Math.round(b.lengthMm - give);
  const newOriginX = nearIsOrigin ? Math.round(b.originX + bx * give) : Math.round(b.originX);
  const newOriginY = nearIsOrigin ? Math.round(b.originY + by * give) : Math.round(b.originY);
  return {
    selfGrowMm: Math.round(Math.max(clampedGrowMm, gap + give)),
    neighbour: { id: b.id, newLengthMm, newOriginX, newOriginY },
  };
};
