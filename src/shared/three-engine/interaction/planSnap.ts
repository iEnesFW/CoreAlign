export interface PlanPoint {
  x: number;
  y: number;
}

export interface PlanSnapPoint extends PlanPoint {
  ownerId?: string;
}

export interface PlanSnapSegment {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  ownerId?: string;
}

export interface PlanSnapTargets {
  points: PlanSnapPoint[];
  segments: PlanSnapSegment[];
}

export interface PlanMoveDelta {
  dxMm: number;
  dyMm: number;
}

export type PlanSnapGuideKind = 'corner' | 'edge' | 'axis';

export interface PlanSnapGuide {
  kind: PlanSnapGuideKind;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

export interface PlanSnapResult extends PlanMoveDelta {
  guides: PlanSnapGuide[];
}

const PLAN_GRID_MM = 5;
const FACE_SNAP_MM = 110;
// A tighter corner radius so a body only snaps when its corner is genuinely close —
// the old 150mm grabbed from far away and felt like it deflected to the wrong spot.
const CORNER_SNAP_MM = 100;
const AXIS_SNAP_MM = 50;
const DIMENSION_STEP_MM = 10;

export const EMPTY_SNAP_TARGETS: PlanSnapTargets = { points: [], segments: [] };

const DEG_TO_RAD = Math.PI / 180;

// Salient snap probes for a straight, rectangular body (wall / run): the two
// centerline ends, the four face corners, and the two side-edge midpoints. Snapping
// by the face corners — not just the centerline — is what lets a body butt FLUSH
// against a neighbour's face instead of overlapping it by half its thickness; the
// side-edge midpoints add the "middle of the edge" tick so an edge can line up to a
// neighbour's edge midpoint. Mirrors the target points buildPlanSnapTargets emits.
export const lineProbePoints = (
  originXMm: number,
  originYMm: number,
  lengthMm: number,
  rotationDeg: number,
  halfWidthMm: number,
): PlanPoint[] => {
  const rad = rotationDeg * DEG_TO_RAD;
  const dirX = Math.cos(rad);
  const dirY = Math.sin(rad);
  const endX = originXMm + lengthMm * dirX;
  const endY = originYMm + lengthMm * dirY;
  const midX = originXMm + (lengthMm / 2) * dirX;
  const midY = originYMm + (lengthMm / 2) * dirY;
  const nx = -dirY * halfWidthMm;
  const ny = dirX * halfWidthMm;
  return [
    { x: originXMm, y: originYMm },
    { x: endX, y: endY },
    { x: originXMm + nx, y: originYMm + ny },
    { x: originXMm - nx, y: originYMm - ny },
    { x: endX + nx, y: endY + ny },
    { x: endX - nx, y: endY - ny },
    { x: midX + nx, y: midY + ny },
    { x: midX - nx, y: midY - ny },
  ];
};

export const snapDimensionMm = (value: number) =>
  Math.round(value / DIMENSION_STEP_MM) * DIMENSION_STEP_MM;

const STICKY_DIMENSION_STEP_MM = 100;
const STICKY_DIMENSION_TOLERANCE_MM = 35;

export const stickyDimensionMm = (value: number) => {
  const nearest = Math.round(value / STICKY_DIMENSION_STEP_MM) * STICKY_DIMENSION_STEP_MM;
  if (Math.abs(value - nearest) <= STICKY_DIMENSION_TOLERANCE_MM) return nearest;
  return snapDimensionMm(value);
};

const snapToPlanGrid = (value: number) => Math.round(value / PLAN_GRID_MM) * PLAN_GRID_MM;

export const filterSnapTargets = (targets: PlanSnapTargets, ownerId: string): PlanSnapTargets => ({
  points: targets.points.filter((p) => p.ownerId !== ownerId),
  segments: targets.segments.filter((s) => s.ownerId !== ownerId),
});

interface CornerCorrection {
  x: number;
  y: number;
  point: PlanSnapPoint;
}

interface FaceCorrection {
  x: number;
  y: number;
  segment: PlanSnapSegment;
}

interface AxisCorrection {
  delta: number;
  point: PlanSnapPoint;
  probeX: number;
  probeY: number;
}

const findCornerCorrection = (
  probes: PlanPoint[],
  dx: number,
  dy: number,
  points: PlanSnapPoint[],
): CornerCorrection | null => {
  let best: CornerCorrection | null = null;
  let bestDist = CORNER_SNAP_MM;
  for (const probe of probes) {
    const px = probe.x + dx;
    const py = probe.y + dy;
    for (const point of points) {
      const cx = point.x - px;
      const cy = point.y - py;
      const dist = Math.hypot(cx, cy);
      if (dist <= bestDist) {
        bestDist = dist;
        best = { x: cx, y: cy, point };
      }
    }
  }
  return best;
};

const findFaceCorrection = (
  probes: PlanPoint[],
  dx: number,
  dy: number,
  segments: PlanSnapSegment[],
): FaceCorrection | null => {
  let best: FaceCorrection | null = null;
  let bestDist = FACE_SNAP_MM;
  for (const probe of probes) {
    const px = probe.x + dx;
    const py = probe.y + dy;
    for (const segment of segments) {
      const vx = segment.x2 - segment.x1;
      const vy = segment.y2 - segment.y1;
      const lenSq = vx * vx + vy * vy;
      if (lenSq === 0) continue;
      const t = Math.min(1, Math.max(0, ((px - segment.x1) * vx + (py - segment.y1) * vy) / lenSq));
      const cx = segment.x1 + t * vx - px;
      const cy = segment.y1 + t * vy - py;
      const dist = Math.hypot(cx, cy);
      if (dist <= bestDist) {
        bestDist = dist;
        best = { x: cx, y: cy, segment };
      }
    }
  }
  return best;
};

const findAxisCorrection = (
  probes: PlanPoint[],
  dx: number,
  dy: number,
  points: PlanSnapPoint[],
  axis: 'x' | 'y',
): AxisCorrection | null => {
  let best: AxisCorrection | null = null;
  let bestAbs = AXIS_SNAP_MM;
  for (const probe of probes) {
    const px = probe.x + dx;
    const py = probe.y + dy;
    for (const point of points) {
      const diff = axis === 'x' ? point.x - px : point.y - py;
      const abs = Math.abs(diff);
      if (abs <= bestAbs) {
        bestAbs = abs;
        best = { delta: diff, point, probeX: px, probeY: py };
      }
    }
  }
  return best;
};

const cornerGuide = (point: PlanSnapPoint): PlanSnapGuide => ({
  kind: 'corner',
  x1: point.x,
  y1: point.y,
  x2: point.x,
  y2: point.y,
});

export const applyPlanMoveSnap = (
  probes: PlanPoint[],
  rawDxMm: number,
  rawDyMm: number,
  targets: PlanSnapTargets,
): PlanSnapResult => {
  const dx = snapToPlanGrid(rawDxMm);
  const dy = snapToPlanGrid(rawDyMm);
  const corner = findCornerCorrection(probes, dx, dy, targets.points);
  if (corner) {
    return {
      dxMm: Math.round(dx + corner.x),
      dyMm: Math.round(dy + corner.y),
      guides: [cornerGuide(corner.point)],
    };
  }
  const guides: PlanSnapGuide[] = [];
  let cx = 0;
  let cy = 0;
  const face = findFaceCorrection(probes, dx, dy, targets.segments);
  if (face) {
    cx = face.x;
    cy = face.y;
    const slid = findCornerCorrection(probes, dx + cx, dy + cy, targets.points);
    if (slid) {
      return {
        dxMm: Math.round(dx + cx + slid.x),
        dyMm: Math.round(dy + cy + slid.y),
        guides: [cornerGuide(slid.point)],
      };
    }
    guides.push({
      kind: 'edge',
      x1: face.segment.x1,
      y1: face.segment.y1,
      x2: face.segment.x2,
      y2: face.segment.y2,
    });
  }
  const faceAlongX = face ? Math.abs(face.x) >= Math.abs(face.y) : false;
  if (!face || !faceAlongX) {
    const alignX = findAxisCorrection(probes, dx + cx, dy + cy, targets.points, 'x');
    if (alignX) {
      cx += alignX.delta;
      guides.push({
        kind: 'axis',
        x1: alignX.point.x,
        y1: alignX.point.y,
        x2: alignX.point.x,
        y2: alignX.probeY,
      });
    }
  }
  if (!face || faceAlongX) {
    const alignY = findAxisCorrection(probes, dx + cx, dy + cy, targets.points, 'y');
    if (alignY) {
      cy += alignY.delta;
      guides.push({
        kind: 'axis',
        x1: alignY.point.x,
        y1: alignY.point.y,
        x2: alignY.probeX,
        y2: alignY.point.y,
      });
    }
  }
  return { dxMm: Math.round(dx + cx), dyMm: Math.round(dy + cy), guides };
};
