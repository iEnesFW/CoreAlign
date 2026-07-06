import type { SceneWallState } from './project.types';

const MIN_LEG_MM = 100;
// WHY: a mathematically-exact butt still reads as penetrating — the trig residue of a rotated
// footprint (cos(−90°) ≈ 6e-17) leaks ~1e-14 into the SAT projections, so the "overlap ≤ 0"
// separation never fires and the extent jumps to the max-axis overlap. One invisible millimetre
// of clearance makes the joint numerically separable.
const JOINT_CLEARANCE_MM = 1;

export interface BendLegs {
  legA: SceneWallState;
  legB: SceneWallState;
}

const normalizeDeg = (deg: number) => {
  let d = deg % 360;
  if (d > 180) d -= 360;
  if (d < -180) d += 360;
  return d;
};

// Openings/features are assigned to a leg purely by their centre offset, so an opening whose
// span [offset - w/2, offset + w/2] crosses the cut line would land wholesale on one leg and be
// clipped or misplaced. Reject any split/bend that crosses an opening or feature (a butt exactly
// at an edge, |offset - cut| === w/2, is fine — it falls cleanly to one side).
const spanCrossesCut = (
  items: readonly { offsetMm: number; widthMm?: number }[] | undefined,
  cutMm: number,
): boolean => (items ?? []).some((it) => Math.abs(it.offsetMm - cutMm) < (it.widthMm ?? 0) / 2);

export const wallSplitCrossesOpening = (wall: SceneWallState, cutMm: number): boolean =>
  spanCrossesCut(wall.openings, cutMm) || spanCrossesCut(wall.features, cutMm);

// Converts a bend gesture into TWO grouped straight walls instead of a single mitred L solid —
// each leg is then a normal wall, so stretch / arc / freehand all work on both sides for free.
// Joint detail: leg A extends e = (t/2)·min(tan(|bend|/2), 1) past the centreline corner so the
// outer wedge is covered, and leg B starts e along its own direction so its start face butts
// against leg A's flank (a clean butt at 90°, no coplanar-overlap z-fighting). e is capped under
// the collision joint tolerance so the touching legs never read as a penetration.
export const computeBendLegs = (
  wall: SceneWallState,
  bendAtMm: number,
  bendAngleDeg: number,
): BendLegs | null => {
  if (Math.abs(bendAngleDeg) < 1) return null;
  const bendAt = Math.round(bendAtMm);
  const thickness = wall.thicknessMm;
  const extension = Math.round(
    (thickness / 2) * Math.min(Math.tan((Math.abs(bendAngleDeg) * Math.PI) / 360), 1),
  );
  const startOffset = extension + JOINT_CLEARANCE_MM;
  const legBLength = wall.lengthMm - bendAt - startOffset;
  if (bendAt < MIN_LEG_MM || legBLength < MIN_LEG_MM) return null;
  if (wallSplitCrossesOpening(wall, bendAt)) return null;
  const rad = (wall.rotationDeg * Math.PI) / 180;
  const rotB = normalizeDeg(wall.rotationDeg - bendAngleDeg);
  const radB = (rotB * Math.PI) / 180;
  const cornerX = wall.originX + bendAt * Math.cos(rad);
  const cornerY = wall.originY + bendAt * Math.sin(rad);
  const heightEnd = wall.heightEndMm ?? null;
  const ratio = bendAt / wall.lengthMm;
  const heightAtBend =
    heightEnd === null
      ? wall.heightMm
      : Math.round(wall.heightMm + (heightEnd - wall.heightMm) * ratio);
  const openings = wall.openings ?? [];
  const features = wall.features ?? [];
  const groupId = wall.groupId ?? crypto.randomUUID();
  const legA: SceneWallState = {
    ...wall,
    groupId,
    lengthMm: bendAt + extension,
    heightEndMm: heightEnd === null ? null : heightAtBend,
    bendAtMm: null,
    bendAngleDeg: null,
    openings: openings.filter((o) => o.offsetMm <= bendAt),
    features: features.filter((f) => f.offsetMm <= bendAt),
  };
  const legB: SceneWallState = {
    ...wall,
    groupId,
    id: crypto.randomUUID(),
    originX: Math.round(cornerX + startOffset * Math.cos(radB)),
    originY: Math.round(cornerY + startOffset * Math.sin(radB)),
    rotationDeg: rotB,
    lengthMm: legBLength,
    heightMm: heightAtBend,
    heightEndMm: heightEnd,
    bendAtMm: null,
    bendAngleDeg: null,
    openings: openings
      .filter((o) => o.offsetMm > bendAt)
      .map((o) => ({ ...o, offsetMm: Math.max(0, o.offsetMm - bendAt - startOffset) })),
    features: features
      .filter((f) => f.offsetMm > bendAt)
      .map((f) => ({ ...f, offsetMm: Math.max(0, f.offsetMm - bendAt - startOffset) })),
  };
  return { legA, legB };
};
