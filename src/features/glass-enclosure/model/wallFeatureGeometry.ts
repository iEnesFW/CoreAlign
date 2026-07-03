import { isRealArc, resolveArc } from './arcGeometry';
import type {
  SceneWallFeature,
  SceneWallFeaturePoint,
  SceneWallState,
  WallFeatureShape,
} from './project.types';

export interface FeatureOutlinePoint {
  x: number;
  z: number;
}

export interface FeatureBounds {
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
}

export interface FeatureOutlineSpec {
  shape: WallFeatureShape;
  offsetMm: number;
  centerZMm: number;
  widthMm: number;
  heightMm: number;
  sides?: number;
  points?: SceneWallFeaturePoint[];
}

const ELLIPSE_SEGMENTS = 40;
const DEFAULT_POLYGON_SIDES = 6;
export const FEATURE_EDGE_MARGIN_MM = 20;
export const MIN_FEATURE_SIZE_MM = 60;
export const FREE_SAMPLE_STEP_MM = 25;
// 4mm keeps freehand DRAW strokes faithful (12mm visibly rounded corners); point counts stay small
// (the stream is already sampled at FREE_SAMPLE_STEP_MM).
export const FREE_SIMPLIFY_TOLERANCE_MM = 4;

const ellipseOutline = (
  cx: number,
  cz: number,
  rx: number,
  rz: number,
  segments: number,
): FeatureOutlinePoint[] => {
  const points: FeatureOutlinePoint[] = [];
  for (let i = 0; i < segments; i += 1) {
    const angle = (i / segments) * Math.PI * 2;
    points.push({ x: cx + rx * Math.cos(angle), z: cz + rz * Math.sin(angle) });
  }
  return points;
};

const signedAreaMm = (outline: FeatureOutlinePoint[]): number => {
  let area = 0;
  for (let i = 0; i < outline.length; i += 1) {
    const a = outline[i];
    const b = outline[(i + 1) % outline.length];
    area += a.x * b.z - b.x * a.z;
  }
  return area / 2;
};

const ensureCcw = (outline: FeatureOutlinePoint[]): FeatureOutlinePoint[] =>
  signedAreaMm(outline) < 0 ? [...outline].reverse() : outline;

export const featureOutlineMm = (spec: FeatureOutlineSpec): FeatureOutlinePoint[] => {
  const cx = spec.offsetMm;
  const cz = spec.centerZMm;
  const hw = spec.widthMm / 2;
  const hh = spec.heightMm / 2;
  switch (spec.shape) {
    case 'rect':
      return [
        { x: cx - hw, z: cz - hh },
        { x: cx + hw, z: cz - hh },
        { x: cx + hw, z: cz + hh },
        { x: cx - hw, z: cz + hh },
      ];
    case 'triangle':
      return [
        { x: cx - hw, z: cz - hh },
        { x: cx + hw, z: cz - hh },
        { x: cx, z: cz + hh },
      ];
    case 'circle': {
      const r = Math.min(hw, hh);
      return ellipseOutline(cx, cz, r, r, ELLIPSE_SEGMENTS);
    }
    case 'ellipse':
      return ellipseOutline(cx, cz, hw, hh, ELLIPSE_SEGMENTS);
    case 'polygon':
      return ellipseOutline(cx, cz, hw, hh, Math.max(3, spec.sides ?? DEFAULT_POLYGON_SIDES));
    case 'free':
      return ensureCcw((spec.points ?? []).map((p) => ({ x: cx + p.x, z: cz + p.z })));
  }
};

export const outlineBoundsMm = (outline: FeatureOutlinePoint[]): FeatureBounds => {
  let minX = Number.POSITIVE_INFINITY;
  let maxX = Number.NEGATIVE_INFINITY;
  let minZ = Number.POSITIVE_INFINITY;
  let maxZ = Number.NEGATIVE_INFINITY;
  for (const p of outline) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
    if (p.z < minZ) minZ = p.z;
    if (p.z > maxZ) maxZ = p.z;
  }
  return { minX, maxX, minZ, maxZ };
};

// Live size label for a shape being drawn: diameter for a circle, W × H for everything else.
export const formatDraftDimensionMm = (draft: {
  shape: string;
  widthMm: number;
  heightMm: number;
}): string => {
  const w = Math.round(draft.widthMm);
  const h = Math.round(draft.heightMm);
  if (draft.shape === 'circle') return `⌀ ${Math.max(w, h)} mm`;
  return `${w} × ${h} mm`;
};

export const shrinkOutlineMm = (
  outline: FeatureOutlinePoint[],
  insetMm: number,
): FeatureOutlinePoint[] => {
  const bounds = outlineBoundsMm(outline);
  const width = bounds.maxX - bounds.minX;
  const height = bounds.maxZ - bounds.minZ;
  if (width <= insetMm * 4 || height <= insetMm * 4) return outline;
  const cx = (bounds.minX + bounds.maxX) / 2;
  const cz = (bounds.minZ + bounds.maxZ) / 2;
  const sx = (width - 2 * insetMm) / width;
  const sz = (height - 2 * insetMm) / height;
  return outline.map((p) => ({ x: cx + (p.x - cx) * sx, z: cz + (p.z - cz) * sz }));
};

const pointToLineDistance = (
  p: SceneWallFeaturePoint,
  a: SceneWallFeaturePoint,
  b: SceneWallFeaturePoint,
) => {
  const vx = b.x - a.x;
  const vz = b.z - a.z;
  const lenSq = vx * vx + vz * vz;
  if (lenSq === 0) return Math.hypot(p.x - a.x, p.z - a.z);
  const t = ((p.x - a.x) * vx + (p.z - a.z) * vz) / lenSq;
  return Math.hypot(a.x + t * vx - p.x, a.z + t * vz - p.z);
};

const simplifySegment = (
  points: SceneWallFeaturePoint[],
  first: number,
  last: number,
  toleranceMm: number,
  keep: boolean[],
) => {
  let maxDist = 0;
  let index = -1;
  for (let i = first + 1; i < last; i += 1) {
    const dist = pointToLineDistance(points[i], points[first], points[last]);
    if (dist > maxDist) {
      maxDist = dist;
      index = i;
    }
  }
  if (maxDist > toleranceMm && index > 0) {
    keep[index] = true;
    simplifySegment(points, first, index, toleranceMm, keep);
    simplifySegment(points, index, last, toleranceMm, keep);
  }
};

export const simplifyFreePoints = (
  points: SceneWallFeaturePoint[],
  toleranceMm: number,
): SceneWallFeaturePoint[] => {
  if (points.length <= 3) return points;
  const keep = points.map(() => false);
  keep[0] = true;
  keep[points.length - 1] = true;
  simplifySegment(points, 0, points.length - 1, toleranceMm, keep);
  return points.filter((_, i) => keep[i]);
};

const orient = (o: SceneWallFeaturePoint, a: SceneWallFeaturePoint, b: SceneWallFeaturePoint) =>
  (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);

const segmentsCross = (
  p1: SceneWallFeaturePoint,
  p2: SceneWallFeaturePoint,
  p3: SceneWallFeaturePoint,
  p4: SceneWallFeaturePoint,
) => {
  const d1 = orient(p3, p4, p1);
  const d2 = orient(p3, p4, p2);
  const d3 = orient(p1, p2, p3);
  const d4 = orient(p1, p2, p4);
  return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
};

export const outlineSelfIntersects = (points: SceneWallFeaturePoint[]): boolean => {
  const n = points.length;
  if (n < 4) return false;
  for (let i = 0; i < n; i += 1) {
    const a1 = points[i];
    const a2 = points[(i + 1) % n];
    for (let j = i + 2; j < n; j += 1) {
      if (i === 0 && j === n - 1) continue;
      if (segmentsCross(a1, a2, points[j], points[(j + 1) % n])) return true;
    }
  }
  return false;
};

// A freehand stroke often hooks past its own start when closing, and the implicit closing edge
// then CROSSES the loop — earcut mis-triangulates a self-intersecting contour into caps that no
// longer match the side walls, so the CSG carve goes partial/unpredictable. Trim the tail (then
// the head) until the closed loop is simple; a stroke that stays self-crossing is rejected (null).
export const sanitizeFreeOutline = (
  points: SceneWallFeaturePoint[],
): SceneWallFeaturePoint[] | null => {
  let pts = points;
  for (let i = 0; i < 12 && pts.length > 3; i += 1) {
    if (!outlineSelfIntersects(pts)) return pts;
    pts = pts.slice(0, -1);
  }
  for (let i = 0; i < 12 && pts.length > 3; i += 1) {
    if (!outlineSelfIntersects(pts)) return pts;
    pts = pts.slice(1);
  }
  return outlineSelfIntersects(pts) ? null : pts;
};

// A curved wall's face coordinates run in DEVELOPED arc-length units (curvedWallPickUv maps hits
// with u ∈ [0, radius·sweep]), so the usable face length is the developed length — the chord
// (lengthMm) is always shorter and would reject shapes on the far part of a deep curve.
export const wallFaceLengthMm = (wall: SceneWallState): number =>
  isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)
    ? resolveArc(wall.geomArcRadiusMm ?? 0, wall.geomArcSweepDeg ?? 1).arcLengthMm
    : wall.lengthMm;

export const wallHeightAtMm = (wall: SceneWallState, xMm: number): number => {
  const heightEnd = wall.heightEndMm ?? wall.heightMm;
  const faceLength = wallFaceLengthMm(wall);
  if (faceLength <= 0) return wall.heightMm;
  const ratio = Math.min(1, Math.max(0, xMm / faceLength));
  return wall.heightMm + (heightEnd - wall.heightMm) * ratio;
};

export const featureFitsWall = (wall: SceneWallState, outline: FeatureOutlinePoint[]): boolean => {
  if (outline.length < 3) return false;
  const bounds = outlineBoundsMm(outline);
  if (bounds.maxX - bounds.minX < MIN_FEATURE_SIZE_MM / 2) return false;
  if (bounds.maxZ - bounds.minZ < MIN_FEATURE_SIZE_MM / 2) return false;
  if (bounds.minX < FEATURE_EDGE_MARGIN_MM) return false;
  if (bounds.maxX > wallFaceLengthMm(wall) - FEATURE_EDGE_MARGIN_MM) return false;
  if (bounds.minZ < FEATURE_EDGE_MARGIN_MM / 2) return false;
  const topLimit =
    Math.min(wallHeightAtMm(wall, bounds.minX), wallHeightAtMm(wall, bounds.maxX)) -
    FEATURE_EDGE_MARGIN_MM;
  return bounds.maxZ <= topLimit;
};

export const boundsOverlapMm = (a: FeatureBounds, b: FeatureBounds, gapMm: number): boolean =>
  a.minX < b.maxX + gapMm &&
  a.maxX > b.minX - gapMm &&
  a.minZ < b.maxZ + gapMm &&
  a.maxZ > b.minZ - gapMm;

export const boundsContainMm = (outer: FeatureBounds, inner: FeatureBounds): boolean =>
  outer.minX <= inner.minX &&
  outer.maxX >= inner.maxX &&
  outer.minZ <= inner.minZ &&
  outer.maxZ >= inner.maxZ;

export const featureBoundsMm = (feature: SceneWallFeature): FeatureBounds =>
  outlineBoundsMm(featureOutlineMm(feature));

export const outlineFitsRect = (
  outline: FeatureOutlinePoint[],
  lengthMm: number,
  depthMm: number,
  marginMm: number,
): boolean => {
  if (outline.length < 3) return false;
  const bounds = outlineBoundsMm(outline);
  if (bounds.maxX - bounds.minX < MIN_FEATURE_SIZE_MM / 2) return false;
  if (bounds.maxZ - bounds.minZ < MIN_FEATURE_SIZE_MM / 2) return false;
  return (
    bounds.minX >= marginMm &&
    bounds.maxX <= lengthMm - marginMm &&
    bounds.minZ >= marginMm &&
    bounds.maxZ <= depthMm - marginMm
  );
};

export const FLUSH_DEPTH_MM = 1;

export type ComposedFeatureKind = 'plug' | 'protrude' | 'outline' | 'none';

export interface ComposedFeature {
  feature: SceneWallFeature;
  outline: FeatureOutlinePoint[];
  bounds: FeatureBounds;
  kind: ComposedFeatureKind;
  cut: boolean;
}

export const composeSurfaceFeatures = (
  features: SceneWallFeature[],
  fits: (outline: FeatureOutlinePoint[]) => boolean,
  openingBounds: FeatureBounds[],
  thicknessMm: number,
): ComposedFeature[] => {
  const area = (b: FeatureBounds) => (b.maxX - b.minX) * (b.maxZ - b.minZ);
  const candidates = features
    .map((feature) => {
      const outline = featureOutlineMm(feature);
      return { feature, outline, bounds: outlineBoundsMm(outline) };
    })
    .filter((c) => fits(c.outline))
    .sort((a, b) => area(b.bounds) - area(a.bounds));
  const acceptedCuts: FeatureBounds[] = [...openingBounds];
  const composed: ComposedFeature[] = [];
  for (const { feature, outline, bounds } of candidates) {
    const isFlush = feature.mode === 'recess' && feature.depthMm < FLUSH_DEPTH_MM;
    const effectiveHole =
      feature.mode === 'hole' ||
      (feature.mode === 'recess' && feature.depthMm >= thicknessMm - FLUSH_DEPTH_MM * 5);
    const wantsCut = !isFlush && (feature.mode === 'hole' || feature.mode === 'recess');
    if (feature.mode === 'protrude') {
      composed.push({ feature, outline, bounds, kind: 'protrude', cut: false });
      continue;
    }
    if (isFlush) {
      composed.push({ feature, outline, bounds, kind: 'outline', cut: false });
      continue;
    }
    if (wantsCut) {
      const containedIn = acceptedCuts.find((b) => boundsContainMm(b, bounds));
      const partialOverlap =
        !containedIn && acceptedCuts.some((b) => boundsOverlapMm(bounds, b, 0));
      if (containedIn) {
        composed.push({
          feature,
          outline,
          bounds,
          kind: effectiveHole ? 'none' : 'plug',
          cut: false,
        });
        continue;
      }
      if (partialOverlap) {
        composed.push({ feature, outline, bounds, kind: 'outline', cut: false });
        continue;
      }
      acceptedCuts.push(bounds);
      composed.push({
        feature,
        outline,
        bounds,
        kind: effectiveHole ? 'none' : 'plug',
        cut: true,
      });
    }
  }
  return composed;
};
