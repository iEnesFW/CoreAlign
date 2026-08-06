import { isRealArc, radiusFromChordSweep, resolveArc } from './arcGeometry';
import { serializePanelPolygonPoints } from './panelPolygon';
import {
  composeSurfaceFeatures,
  featureBoundsMm,
  featureFitsWall,
  featureOutlineMm,
  outlineBoundsMm,
} from './wallFeatureGeometry';
import type {
  PanelShapeKind,
  SceneWallFeature,
  SceneWallOpening,
  SceneWallState,
} from './project.types';

export const OPENING_SIDE_MARGIN_MM = 5;
export const OPENING_BOTTOM_MARGIN_MM = 1;
export const OPENING_TOP_MARGIN_MM = 10;
export const MIN_HOLE_MM = 20;
export const OPENING_GAP_MM = 50;

export interface WallOpeningRectMm {
  x0: number;
  x1: number;
  y0: number;
  y1: number;
  hasSill: boolean;
}

// WHY: the wall body is extruded with these margins, so this rect — not the raw record — IS the
// hole. Autofill and the renderer must both read it or the glass will not match what was carved.
export const clampOpeningRectMm = (
  opening: SceneWallOpening,
  lengthMm: number,
  heightStartMm: number,
  heightEndMm: number,
): WallOpeningRectMm | null => {
  const halfW = opening.widthMm / 2;
  const x0 = Math.max(OPENING_SIDE_MARGIN_MM, opening.offsetMm - halfW);
  const x1 = Math.min(lengthMm - OPENING_SIDE_MARGIN_MM, opening.offsetMm + halfW);
  if (x1 - x0 < MIN_HOLE_MM) return null;
  const slope = lengthMm > 0 ? (heightEndMm - heightStartMm) / lengthMm : 0;
  const topLimit =
    Math.min(heightStartMm + slope * x0, heightStartMm + slope * x1) - OPENING_TOP_MARGIN_MM;
  let y0 = Math.max(OPENING_BOTTOM_MARGIN_MM, opening.sillMm);
  let y1 = Math.min(topLimit, opening.sillMm + opening.heightMm);
  if (y1 - y0 < MIN_HOLE_MM) {
    y0 = Math.max(OPENING_BOTTOM_MARGIN_MM, topLimit - opening.heightMm);
    y1 = Math.min(topLimit, y0 + opening.heightMm);
  }
  if (y1 - y0 < MIN_HOLE_MM) return null;
  return { x0, x1, y0, y1, hasSill: opening.sillMm > 0 };
};

export type WallHoleSkipReason =
  | 'bentWall'
  | 'arcOpening'
  | 'sideFace'
  | 'notCarved'
  | 'tooSmall'
  | 'alreadyFilled'
  | 'approximated';

export interface WallHoleSkip {
  source: 'opening' | 'feature';
  id: string;
  reason: WallHoleSkipReason;
}

export interface ResolvedWallHole {
  source: 'opening' | 'feature';
  id: string;
  // Along the wall FACE: developed arc-length u on a curved wall, plain x on a straight one.
  uStartMm: number;
  uWidthMm: number;
  // Wall-local vertical band (the caller adds the wall's own geomZ).
  zBottomMm: number;
  zHeightMm: number;
  shape: { shapeKind: PanelShapeKind; shapePointsJson: string | null } | null;
}

export interface ResolvedWallHoles {
  holes: ResolvedWallHole[];
  skipped: WallHoleSkip[];
}

const isThroughHole = (feature: SceneWallFeature, thicknessMm: number): boolean =>
  feature.mode === 'hole' || (feature.mode === 'recess' && feature.depthMm >= thicknessMm - 5);

const isRoundShape = (feature: SceneWallFeature): boolean =>
  feature.shape === 'circle' || feature.shape === 'ellipse';

const isFrontBack = (feature: SceneWallFeature): boolean =>
  feature.side === 1 || feature.side === -1;

// Map a shaped wall hole (the feature silhouette) onto a glass PANEL shape so the fill glass
// matches the hole instead of a rectangle that overflows it.
export const featurePanelShape = (
  feature: SceneWallFeature,
): { shapeKind: PanelShapeKind; shapePointsJson: string | null } | null => {
  if (feature.shape === 'rect') return null;
  if (feature.shape === 'circle' || feature.shape === 'ellipse') {
    return { shapeKind: 'ellipse', shapePointsJson: null };
  }
  const outline = featureOutlineMm({
    shape: feature.shape,
    offsetMm: feature.offsetMm,
    centerZMm: feature.centerZMm,
    widthMm: feature.widthMm,
    heightMm: feature.heightMm,
    sides: feature.sides,
    points: feature.points,
  });
  // Feature outline is absolute (wall-face u,z); a panel polygon is local, bottom-centred, y-up.
  // WHY the shift is BOUNDS-relative and not offset/centerZ-relative: the pane autofill creates is
  // sized from the hole's OUTLINE BOUNDS, but the points used to be shifted by the feature's
  // NOMINAL box centre. Those two agree for symmetric shapes only — an inscribed pentagon is
  // narrower than its box AND its bounds centre sits above the nominal centre, so the glass
  // silhouette floated off the pane and its top vertex landed OUTSIDE the pane box. Same after a
  // 'free' feature's stored width/height was edited (the outline does not scale with them).
  const bounds = outlineBoundsMm(outline);
  const centreXMm = (bounds.minX + bounds.maxX) / 2;
  const pts = outline.map((p) => ({
    x: Math.round(p.x - centreXMm),
    y: Math.round(p.z - bounds.minZ),
  }));
  return { shapeKind: 'polygon', shapePointsJson: serializePanelPolygonPoints(pts) };
};

// The wall arc, resolved the way every renderer/collision consumer resolves it: the stored radius
// is integer-rounded (and legacy rows drifted), so it is re-derived from the chord + sweep.
export const resolveWallArc = (wall: SceneWallState) =>
  isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)
    ? resolveArc(
        radiusFromChordSweep(wall.lengthMm, wall.geomArcRadiusMm, wall.geomArcSweepDeg),
        wall.geomArcSweepDeg ?? 1,
      )
    : null;

// The single source of truth for "what holes does this wall actually have". Mirrors every decision
// the wall builder makes when it carves the body, so glass produced from this can never describe a
// hole the wall does not have (nor miss one it does).
export const resolveWallHoles = (wall: SceneWallState): ResolvedWallHoles => {
  const openings = wall.openings ?? [];
  const features = wall.features ?? [];
  const skipped: WallHoleSkip[] = [];

  // Bent (L) wall: the builder defers every opening and feature, so nothing is carved.
  if (wall.bendAngleDeg && Math.abs(wall.bendAngleDeg) >= 1) {
    for (const o of openings) skipped.push({ source: 'opening', id: o.id, reason: 'bentWall' });
    for (const f of features) {
      if (isThroughHole(f, wall.thicknessMm)) {
        skipped.push({ source: 'feature', id: f.id, reason: 'bentWall' });
      }
    }
    return { holes: [], skipped };
  }

  const holes: ResolvedWallHole[] = [];

  // Curved wall: the builder carves FEATURES into the band (applyCurvedWallFeatures) but never
  // reads wall.openings — an opening on a curved wall is not a hole, so it must not be glazed.
  const wallArc = resolveWallArc(wall);
  if (wallArc) {
    for (const o of openings) skipped.push({ source: 'opening', id: o.id, reason: 'arcOpening' });
    // WHY: the band is built from the CHORD-derived radius (the stored one is integer-rounded and
    // legacy rows drifted), so the carved face runs to that developed length — not the raw one.
    const faceLengthMm = wallArc.arcLengthMm;
    for (const feature of features) {
      // WHY: applyCurvedWallFeatures perforates the band ONLY for mode 'hole' — a recess is a
      // partial-radial pocket at any depth (rNear is floored at innerR + 0.001), so even a
      // full-thickness recess keeps a membrane. The straight builder's "deep recess counts as a
      // through hole" promotion does not apply here.
      if (feature.mode !== 'hole') {
        if (isThroughHole(feature, wall.thicknessMm)) {
          skipped.push({ source: 'feature', id: feature.id, reason: 'notCarved' });
        }
        continue;
      }
      if (!isFrontBack(feature)) {
        skipped.push({ source: 'feature', id: feature.id, reason: 'sideFace' });
        continue;
      }
      // WHY: the CSG cutter is built from featureOutlineMm, so a polygon/triangle hole is carved at
      // its OUTLINE bounds, not the nominal width×height box (a hexagon is only 0.866× as tall, an
      // odd n-gon is narrower AND off-centre). Sizing the pane from the nominal box put glass over
      // solid band — the very split this module exists to close.
      const bounds = featureBoundsMm(feature);
      const rawStartMm = bounds.minX;
      const rawWidthMm = bounds.maxX - bounds.minX;
      const rawHeightMm = bounds.maxZ - bounds.minZ;
      const uStartMm = Math.max(0, rawStartMm);
      const uWidthMm = Math.min(rawWidthMm - (uStartMm - rawStartMm), faceLengthMm - uStartMm);
      // The band also ends vertically: anything above the wall top or below its base is not carved.
      const zBottomMm = Math.max(0, bounds.minZ);
      const zHeightMm = Math.min(wall.heightMm, bounds.maxZ) - zBottomMm;
      if (uWidthMm <= 0 || zHeightMm <= 0) {
        skipped.push({ source: 'feature', id: feature.id, reason: 'notCarved' });
        continue;
      }
      // WHY: a clipped hole is smaller than the feature, so the feature's silhouette no longer
      // describes this pane — shipping it would cut glass past the band. Fall back to a rectangle
      // and say so, rather than emit an outline that overflows.
      const clipped = uWidthMm < rawWidthMm - 0.5 || zHeightMm < rawHeightMm - 0.5;
      const shape = clipped ? null : featurePanelShape(feature);
      if (clipped ? feature.shape !== 'rect' : isRoundShape(feature)) {
        skipped.push({ source: 'feature', id: feature.id, reason: 'approximated' });
      }
      holes.push({
        source: 'feature',
        id: feature.id,
        uStartMm,
        uWidthMm,
        zBottomMm,
        zHeightMm,
        shape,
      });
    }
    return { holes, skipped };
  }

  // Straight wall: openings are extruded holes (clamped), features go through the fits + overlap
  // composition. Both gates are replayed here so the fill sees exactly the carved set.
  const heightEndMm = wall.heightEndMm ?? wall.heightMm;
  const openingBounds: { minX: number; maxX: number; minZ: number; maxZ: number }[] = [];
  const sorted = [...openings].sort((a, b) => a.offsetMm - b.offsetMm);
  let lastRightMm = Number.NEGATIVE_INFINITY;
  for (const opening of sorted) {
    const leftMm = opening.offsetMm - opening.widthMm / 2;
    if (leftMm < lastRightMm + OPENING_GAP_MM) {
      skipped.push({ source: 'opening', id: opening.id, reason: 'notCarved' });
      continue;
    }
    const rect = clampOpeningRectMm(opening, wall.lengthMm, wall.heightMm, heightEndMm);
    if (!rect) {
      skipped.push({ source: 'opening', id: opening.id, reason: 'tooSmall' });
      continue;
    }
    lastRightMm = opening.offsetMm + opening.widthMm / 2;
    openingBounds.push({
      minX: leftMm,
      maxX: lastRightMm,
      minZ: opening.sillMm,
      maxZ: opening.sillMm + opening.heightMm,
    });
    holes.push({
      source: 'opening',
      id: opening.id,
      uStartMm: rect.x0,
      uWidthMm: rect.x1 - rect.x0,
      zBottomMm: rect.y0,
      zHeightMm: rect.y1 - rect.y0,
      shape: null,
    });
  }

  for (const feature of features) {
    if (!isThroughHole(feature, wall.thicknessMm)) continue;
    if (!isFrontBack(feature)) {
      skipped.push({ source: 'feature', id: feature.id, reason: 'sideFace' });
    }
  }
  const frontBack = features.filter(isFrontBack);
  const composed = composeSurfaceFeatures(
    frontBack,
    (outline) => featureFitsWall(wall, outline),
    openingBounds,
    wall.thicknessMm,
  );
  const composedById = new Map(composed.map((c) => [c.feature.id, c]));
  for (const feature of frontBack) {
    if (!isThroughHole(feature, wall.thicknessMm)) continue;
    const entry = composedById.get(feature.id);
    // cut === true && kind === 'none' is exactly the builder's "this became a through hole".
    if (!entry || !entry.cut || entry.kind !== 'none') {
      skipped.push({ source: 'feature', id: feature.id, reason: 'notCarved' });
      continue;
    }
    holes.push({
      source: 'feature',
      id: feature.id,
      uStartMm: entry.bounds.minX,
      uWidthMm: entry.bounds.maxX - entry.bounds.minX,
      zBottomMm: entry.bounds.minZ,
      zHeightMm: entry.bounds.maxZ - entry.bounds.minZ,
      shape: featurePanelShape(feature),
    });
    if (isRoundShape(feature)) {
      skipped.push({ source: 'feature', id: feature.id, reason: 'approximated' });
    }
  }

  return { holes, skipped };
};
