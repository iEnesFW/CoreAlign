import { arcEndLocal, isRealArc, radiusFromChordSweep, resolveArc } from '../model/arcGeometry';
import type { ResolvedArc } from '../model/arcGeometry';

/**
 * The pose fields every curvable body shares (run, wall, slab). `lengthMm` is the CHORD — the
 * straight span between the two fixed endpoints — and `rotationDeg` is the ROLLED START TANGENT
 * once the body is an arc, not the chord direction.
 */
export interface CurvableShape {
  lengthMm: number;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
}

export interface CurvablePose extends CurvableShape {
  rotationDeg: number;
}

/**
 * How a body's `rotationDeg` relates to its chord once it curves. BOTH keep the two endpoints
 * pinned; they differ only in where the stored angle points.
 *
 * - `rolled` (runs, walls): `rotationDeg` is the START TANGENT — rolled back by half the sweep so
 *   the chord still points along the user's direction. The renderer sweeps from that tangent.
 * - `symmetric` (slabs): `rotationDeg` stays the AXIS direction and the mesh builder bows the band
 *   symmetrically about it (`curvedSlabPlanColumnsMm` owns the mirror). Rolling a slab would
 *   double-apply the turn and swing the deck.
 */
export type PoseConvention = 'rolled' | 'symmetric';

export const SWEEP_QUANTUM_DEG = 0.01;
export const ROTATION_QUANTUM_DEG = 0.01;
export const STRAIGHT_SWEEP_EPSILON_DEG = 0.5;

export const quantizeSweepDeg = (sweepDeg: number): number =>
  Math.round(sweepDeg / SWEEP_QUANTUM_DEG) * SWEEP_QUANTUM_DEG;

export const quantizeRotationDeg = (rotationDeg: number): number =>
  Math.round(rotationDeg / ROTATION_QUANTUM_DEG) * ROTATION_QUANTUM_DEG;

export const isCurved = (body: CurvableShape): boolean =>
  isRealArc(body.geomArcRadiusMm, body.geomArcSweepDeg);

/**
 * The one radius every consumer must read.
 *
 * The persisted radius is integer-rounded (and legacy rows drifted), so reading it raw makes the
 * renderer, the collision footprint, the snap target and the pick surface disagree by millimetres.
 * Re-deriving it from the chord + sweep makes them agree by construction.
 */
export const resolveBodyCurvature = (body: CurvableShape): ResolvedArc | null =>
  isCurved(body)
    ? resolveArc(
        radiusFromChordSweep(body.lengthMm, body.geomArcRadiusMm, body.geomArcSweepDeg),
        body.geomArcSweepDeg ?? 1,
      )
    : null;

/**
 * The direction of the CHORD in world degrees.
 *
 * WHY: on an arc body the stored `rotationDeg` is the start tangent, rolled back by half the sweep
 * so the two endpoints stay pinned. Anything that wants "which way does this body run" must undo
 * that roll — reading `rotationDeg` directly is the single most common way an arc consumer ends up
 * pointing metres away from the real geometry.
 */
export const chordDirectionDeg = (body: CurvablePose, pose: PoseConvention = 'rolled'): number =>
  pose === 'rolled' && isCurved(body)
    ? body.rotationDeg + (body.geomArcSweepDeg ?? 0) / 2
    : body.rotationDeg;

/**
 * The inverse of {@link chordDirectionDeg}: the `rotationDeg` a body must store so that a given
 * chord direction and signed sweep leave both endpoints where they are.
 */
export const rotationForChord = (
  chordDeg: number,
  signedSweepDeg: number | null,
  pose: PoseConvention = 'rolled',
): number =>
  quantizeRotationDeg(pose === 'rolled' ? chordDeg - (signedSweepDeg ?? 0) / 2 : chordDeg);

/**
 * The DEVELOPED face length of a body: the arc length a curved body's surface actually has, the
 * plain length when it is straight.
 *
 * This is the domain of every on-surface `u` coordinate (draw picks, feature offsets, clamps, the
 * CSG cutter). Deriving it from the stored radius instead of the chord returns a length nobody
 * draws, and every stored outline then rescales into the wrong units.
 */
export const bodyDevelopedLengthMm = (body: CurvableShape): number =>
  resolveBodyCurvature(body)?.arcLengthMm ?? body.lengthMm;

/**
 * A body's far endpoint in its OWN local frame (x along `rotationDeg`, y across), in mm.
 *
 * Straight bodies end at (length, 0); an arc ends where its curve ends. Anything that needs "where
 * does this body finish" — snap targets, attachment tests, corner posts, the 2D plan — must read it
 * here so they cannot disagree about the same body.
 */
export const bodyEndLocalMm = (body: CurvableShape): { xMm: number; yMm: number } => {
  const arc = resolveBodyCurvature(body);
  if (!arc) return { xMm: body.lengthMm, yMm: 0 };
  return arcEndLocal(arc.radiusMm, body.geomArcSweepDeg ?? 1);
};

const DEG2RAD = Math.PI / 180;

export interface PlacedBody extends CurvablePose {
  originX: number;
  originY: number;
}

/**
 * The world-space vector from a body's start to its far end — its CHORD.
 *
 * WHY this exists: `lengthMm * dir(rotationDeg)` is the single most repeated arc bug in this
 * module. On an arc body `rotationDeg` is the START TANGENT, so that expression points along the
 * tangent instead of the chord and overshoots by the sagitta — metres wrong at a 90 degree sweep.
 * Paste placement, the array tool, the DXF/plan export, push-resize and the 2D plan all need "which
 * way and how far does this body actually run", and they must all read it from here.
 */
export const bodyChordVectorMm = (body: CurvablePose): { xMm: number; yMm: number } => {
  const end = bodyEndLocalMm(body);
  const rad = body.rotationDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  return {
    xMm: end.xMm * cos - end.yMm * sin,
    yMm: end.xMm * sin + end.yMm * cos,
  };
};

/** A placed body's far endpoint in world mm. */
export const bodyEndWorldMm = (body: PlacedBody): { xMm: number; yMm: number } => {
  const chord = bodyChordVectorMm(body);
  return { xMm: body.originX + chord.xMm, yMm: body.originY + chord.yMm };
};

/** The midpoint of a placed body's chord, in world mm — the point a drop/paste should centre on. */
export const bodyChordMidWorldMm = (body: PlacedBody): { xMm: number; yMm: number } => {
  const chord = bodyChordVectorMm(body);
  return { xMm: body.originX + chord.xMm / 2, yMm: body.originY + chord.yMm / 2 };
};

/**
 * The origin a body must be given so its CHORD MIDPOINT lands on `centre`.
 *
 * Placement code wants "drop this where I clicked"; walking back half the length along
 * `rotationDeg` only does that for a straight body.
 */
export const originForChordCentreMm = (
  centreXMm: number,
  centreYMm: number,
  body: CurvablePose,
): { originX: number; originY: number } => {
  const chord = bodyChordVectorMm(body);
  return {
    originX: Math.round(centreXMm - chord.xMm / 2),
    originY: Math.round(centreYMm - chord.yMm / 2),
  };
};
