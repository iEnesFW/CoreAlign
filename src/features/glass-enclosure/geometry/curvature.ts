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
