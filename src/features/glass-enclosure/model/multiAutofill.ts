import { RUN_PLAN_THICKNESS_MM, penetratesAny } from '../scene/interaction/planCollision';
import type { PlanFootprint } from '../scene/interaction/planCollision';
import { deriveArcFromChordSagitta } from './arcGeometry';
import type { SceneRunState, SceneWallState } from './project.types';
import type { OpenEdge } from './wallAutofill';

export interface GapEdge extends OpenEdge {
  cornerGroup?: number;
}

// How a corner gap between two free wall ends is bridged:
// - 'auto'     L legs around the corner, falling back to a straight connector;
// - 'straight' a single straight run end-to-end;
// - 'L'        L legs only (skip the pair if they don't fit);
// - 'arc'      a single curved run bulging around the outside corner.
export type CornerFillMode = 'auto' | 'straight' | 'L' | 'arc';

const DEG2RAD = Math.PI / 180;
const RAD2DEG = 180 / Math.PI;
const ENDPOINT_JOIN_TOLERANCE_MM = 150;
const MIN_GAP_MM = 300;
const MAX_GAP_MM = 60000;
const MIN_RUN_MM = 300;
const CORNER_ANGLE_TOLERANCE_DEG = 60;
const OUTWARD_TOLERANCE_MM = 100;
const OUTWARD_DOT_MIN = -0.35;
const TRIM_ITERATIONS = 16;
const GAP_RUN_ID = 'gap-run-candidate';

interface WallEndpoint {
  wall: SceneWallState;
  x: number;
  y: number;
  outwardDeg: number;
  heightMm: number;
  baseZMm: number;
}

interface EdgeCandidate {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm: number;
  baseZMm: number;
}

const roundDeg = (deg: number) => Math.round(deg * 100) / 100;

const normalizeDeg = (deg: number) => ((deg % 360) + 360) % 360;

const angleDiffDeg = (a: number, b: number) => {
  const d = Math.abs((((a - b) % 180) + 180) % 180);
  return Math.min(d, 180 - d);
};

const capsuleFootprint = (
  ownerId: string,
  startX: number,
  startY: number,
  lengthMm: number,
  rotationDeg: number,
  halfWidthMm: number,
  zMinMm: number,
  zMaxMm: number,
): PlanFootprint => {
  const rad = rotationDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  const inset = Math.min(halfWidthMm, lengthMm / 2);
  return {
    ownerId,
    x1: startX + inset * cos,
    y1: startY + inset * sin,
    x2: startX + (lengthMm - inset) * cos,
    y2: startY + (lengthMm - inset) * sin,
    halfWidthMm,
    zMinMm,
    zMaxMm,
  };
};

const wallBlocker = (wall: SceneWallState): PlanFootprint => {
  const zMin = wall.geomZ ?? 0;
  return capsuleFootprint(
    wall.id,
    wall.originX,
    wall.originY,
    wall.lengthMm,
    wall.rotationDeg,
    wall.thicknessMm / 2,
    zMin,
    zMin + Math.max(wall.heightMm, wall.heightEndMm ?? wall.heightMm),
  );
};

const runBlocker = (run: SceneRunState): PlanFootprint => {
  const zMin = run.geomZ ?? 0;
  return capsuleFootprint(
    run.id,
    run.originX,
    run.originY,
    run.lengthMm,
    run.rotationDeg,
    RUN_PLAN_THICKNESS_MM / 2,
    zMin,
    zMin + run.heightMm,
  );
};

const wallEndpoints = (wall: SceneWallState): [WallEndpoint, WallEndpoint] => {
  const rad = wall.rotationDeg * DEG2RAD;
  const baseZMm = wall.geomZ ?? 0;
  return [
    {
      wall,
      x: wall.originX,
      y: wall.originY,
      outwardDeg: normalizeDeg(wall.rotationDeg + 180),
      heightMm: wall.heightMm,
      baseZMm,
    },
    {
      wall,
      x: wall.originX + wall.lengthMm * Math.cos(rad),
      y: wall.originY + wall.lengthMm * Math.sin(rad),
      outwardDeg: normalizeDeg(wall.rotationDeg),
      heightMm: wall.heightEndMm ?? wall.heightMm,
      baseZMm,
    },
  ];
};

const lineIntersection = (
  p1: { x: number; y: number },
  dir1Deg: number,
  p2: { x: number; y: number },
  dir2Deg: number,
): { x: number; y: number } | null => {
  const d1x = Math.cos(dir1Deg * DEG2RAD);
  const d1y = Math.sin(dir1Deg * DEG2RAD);
  const d2x = Math.cos(dir2Deg * DEG2RAD);
  const d2y = Math.sin(dir2Deg * DEG2RAD);
  const denom = d1x * d2y - d1y * d2x;
  if (Math.abs(denom) < 1e-9) return null;
  const t = ((p2.x - p1.x) * d2y - (p2.y - p1.y) * d2x) / denom;
  return { x: p1.x + d1x * t, y: p1.y + d1y * t };
};

const edgeFootprint = (edge: EdgeCandidate, startTrimMm: number, endTrimMm: number) => {
  const rad = edge.rotationDeg * DEG2RAD;
  return capsuleFootprint(
    GAP_RUN_ID,
    edge.originX + startTrimMm * Math.cos(rad),
    edge.originY + startTrimMm * Math.sin(rad),
    edge.lengthMm - startTrimMm - endTrimMm,
    edge.rotationDeg,
    RUN_PLAN_THICKNESS_MM / 2,
    edge.baseZMm,
    edge.baseZMm + Math.max(1, edge.heightMm),
  );
};

const edgePenetrates = (
  edge: EdgeCandidate,
  startTrimMm: number,
  endTrimMm: number,
  blockers: PlanFootprint[],
) => {
  if (edge.lengthMm - startTrimMm - endTrimMm < MIN_RUN_MM) return true;
  return penetratesAny(edgeFootprint(edge, startTrimMm, endTrimMm), blockers);
};

const findMinTrim = (
  edge: EdgeCandidate,
  blockers: PlanFootprint[],
  fromStart: boolean,
  otherTrimMm: number,
): number | null => {
  const check = (trim: number) =>
    fromStart
      ? edgePenetrates(edge, trim, otherTrimMm, blockers)
      : edgePenetrates(edge, otherTrimMm, trim, blockers);
  if (!check(0)) return 0;
  const maxTrim = edge.lengthMm - otherTrimMm - MIN_RUN_MM;
  if (maxTrim <= 0 || check(maxTrim)) return null;
  let lo = 0;
  let hi = maxTrim;
  for (let i = 0; i < TRIM_ITERATIONS; i += 1) {
    const mid = (lo + hi) / 2;
    if (check(mid)) lo = mid;
    else hi = mid;
  }
  return Math.ceil(hi);
};

const buildTrimmed = (
  edge: EdgeCandidate,
  startTrimMm: number,
  endTrimMm: number,
  blockers: PlanFootprint[],
): EdgeCandidate | null => {
  const lengthMm = Math.round(edge.lengthMm - startTrimMm - endTrimMm);
  if (lengthMm < MIN_RUN_MM) return null;
  const rad = edge.rotationDeg * DEG2RAD;
  const trimmed: EdgeCandidate = {
    originX: Math.round(edge.originX + startTrimMm * Math.cos(rad)),
    originY: Math.round(edge.originY + startTrimMm * Math.sin(rad)),
    rotationDeg: edge.rotationDeg,
    lengthMm,
    heightMm: edge.heightMm,
    baseZMm: edge.baseZMm,
  };
  if (edgePenetrates(trimmed, 0, 0, blockers)) return null;
  return trimmed;
};

const trimWithOrder = (
  edge: EdgeCandidate,
  blockers: PlanFootprint[],
  startFirst: boolean,
): EdgeCandidate | null => {
  const first = findMinTrim(edge, blockers, startFirst, 0);
  if (first === null) return null;
  const second = findMinTrim(edge, blockers, !startFirst, first);
  if (second === null) return null;
  const startTrim = startFirst ? first : second;
  const endTrim = startFirst ? second : first;
  return buildTrimmed(edge, startTrim, endTrim, blockers);
};

const trimEdge = (edge: EdgeCandidate, blockers: PlanFootprint[]): EdgeCandidate | null => {
  const startFirst = trimWithOrder(edge, blockers, true);
  const endFirst = trimWithOrder(edge, blockers, false);
  if (!startFirst) return endFirst;
  if (!endFirst) return startFirst;
  return endFirst.lengthMm > startFirst.lengthMm ? endFirst : startFirst;
};

const edgeBetween = (
  from: { x: number; y: number },
  to: { x: number; y: number },
  heightMm: number,
  baseZMm: number,
): EdgeCandidate | null => {
  const lengthMm = Math.hypot(to.x - from.x, to.y - from.y);
  if (lengthMm < MIN_RUN_MM) return null;
  return {
    originX: from.x,
    originY: from.y,
    rotationDeg: roundDeg(normalizeDeg(Math.atan2(to.y - from.y, to.x - from.x) * RAD2DEG)),
    lengthMm,
    heightMm,
    baseZMm,
  };
};

interface CornerLeg {
  edge: EdgeCandidate;
  ownWallId: string;
}

const cornerCandidates = (a: WallEndpoint, b: WallEndpoint): CornerLeg[] | null => {
  if (angleDiffDeg(a.wall.rotationDeg, b.wall.rotationDeg) < 90 - CORNER_ANGLE_TOLERANCE_DEG) {
    return null;
  }
  const corner = lineIntersection(a, a.outwardDeg, b, b.outwardDeg);
  if (!corner) return null;
  const aDist = Math.hypot(corner.x - a.x, corner.y - a.y);
  const bDist = Math.hypot(corner.x - b.x, corner.y - b.y);
  if (aDist > MAX_GAP_MM || bDist > MAX_GAP_MM) return null;
  const outwardA =
    (corner.x - a.x) * Math.cos(a.outwardDeg * DEG2RAD) +
    (corner.y - a.y) * Math.sin(a.outwardDeg * DEG2RAD);
  const outwardB =
    (corner.x - b.x) * Math.cos(b.outwardDeg * DEG2RAD) +
    (corner.y - b.y) * Math.sin(b.outwardDeg * DEG2RAD);
  if (outwardA < -OUTWARD_TOLERANCE_MM || outwardB < -OUTWARD_TOLERANCE_MM) return null;
  // Each corner leg extends its OWN wall outward, so it inherits that wall's top
  // height and base elevation — a leg must reach the top of the wall it belongs to,
  // not the shorter of the pair (which would leave a mixed-height corner misaligned).
  // Endpoints arrive nearest-corner refined (same as straight/arc) so a thick-wall L meets the
  // walls' closest corners; only x/y are slid, so each leg still runs along its wall's outward ray.
  const legs: CornerLeg[] = [];
  const legA = edgeBetween(a, corner, a.heightMm, a.baseZMm);
  if (legA) legs.push({ edge: legA, ownWallId: a.wall.id });
  const legB = edgeBetween(corner, b, b.heightMm, b.baseZMm);
  if (legB) legs.push({ edge: legB, ownWallId: b.wall.id });
  return legs.length > 0 ? legs : null;
};

// PARALLEL / near-parallel free ends can't form an outward-ray corner, but they CAN form an L:
// run one leg outward along a wall's axis until it is level with the partner, turn 90°, and run
// the perpendicular leg across the offset to the partner's free end. Two right-angle legs, never
// a diagonal. (Used as the 'L'-mode fallback when cornerCandidates rejects the pair.)
const parallelCornerCandidates = (a: WallEndpoint, b: WallEndpoint): CornerLeg[] | null => {
  const tryCandidate = (own: WallEndpoint, partner: WallEndpoint): CornerLeg[] | null => {
    const axis = own.outwardDeg * DEG2RAD;
    const ux = Math.cos(axis);
    const uy = Math.sin(axis);
    const dx = partner.x - own.x;
    const dy = partner.y - own.y;
    const along = dx * ux + dy * uy; // axis-leg length (signed: + is outward from the wall)
    const across = Math.hypot(dx - along * ux, dy - along * uy); // perpendicular-leg length
    // The axis leg must travel OUTWARD; a non-positive projection would run it back over the wall.
    if (along < MIN_RUN_MM || across < MIN_RUN_MM) return null;
    const corner = { x: own.x + ux * along, y: own.y + uy * along };
    const legOwn = edgeBetween(own, corner, own.heightMm, own.baseZMm);
    const legCross = edgeBetween(corner, partner, partner.heightMm, partner.baseZMm);
    if (!legOwn || !legCross) return null;
    return [
      { edge: legOwn, ownWallId: own.wall.id },
      { edge: legCross, ownWallId: partner.wall.id },
    ];
  };
  return tryCandidate(a, b) ?? tryCandidate(b, a);
};

// A single curved run from A to B that bulges toward the outside corner (the
// intersection of the two walls' outward rays). The run-arc convention (arcGeometry):
// a run placed at A heading `rotationDeg` whose local chord subtends `dir·sweep/2`,
// so rotationDeg = chordAngle − dir·sweep/2 lands the far end exactly on B.
const arcCornerEdge = (a: WallEndpoint, b: WallEndpoint): GapEdge | null => {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const chordMm = Math.hypot(dx, dy);
  if (chordMm < MIN_RUN_MM) return null;
  const chordDeg = normalizeDeg(Math.atan2(dy, dx) * RAD2DEG);
  const corner = lineIntersection(a, a.outwardDeg, b, b.outwardDeg);
  const nx = -dy / chordMm;
  const ny = dx / chordMm;
  const signed = corner ? (corner.x - a.x) * nx + (corner.y - a.y) * ny : chordMm * 0.35;
  // WHY: arcEndLocal bulges local +y for dir=+1, but the chord normal used for `signed`
  // is rotated −dir·sweep/2 relative to that, so matching the outside corner needs the
  // opposite sign (verified by the apex-side test, not intuition).
  const dir = signed >= 0 ? -1 : 1;
  const sagittaMm = Math.min(Math.max(Math.abs(signed), chordMm * 0.1), chordMm * 0.5);
  const derived = deriveArcFromChordSagitta(chordMm, sagittaMm);
  return {
    originX: Math.round(a.x),
    originY: Math.round(a.y),
    rotationDeg: roundDeg(normalizeDeg(chordDeg - dir * (derived.sweepDeg / 2))),
    // lengthMm is the developed glass length (the curved pane bridging the gap).
    lengthMm: derived.arcLengthMm,
    heightMm: Math.round(Math.min(a.heightMm, b.heightMm)),
    geomZ: Math.round(Math.min(a.baseZMm, b.baseZMm)),
    geomArcRadiusMm: derived.radiusMm,
    geomArcSweepDeg: roundDeg(dir * derived.sweepDeg),
    arcGlassBent: true,
  };
};

// Slide an endpoint along its wall's END FACE (the thickness-wide segment across the wall
// axis, centred on the centreline end) to the point NEAREST the partner. For thin walls this
// stays on the centreline (the projection clamps to ~0); for thick / cube walls it lands on
// the near corner so the infill bridges the walls' closest corners, not their centres.
const refineEndpointToFace = (ep: WallEndpoint, partner: WallEndpoint): WallEndpoint => {
  const rad = ep.wall.rotationDeg * DEG2RAD;
  const ax = -Math.sin(rad);
  const ay = Math.cos(rad);
  const half = ep.wall.thicknessMm / 2;
  const t = Math.max(-half, Math.min(half, (partner.x - ep.x) * ax + (partner.y - ep.y) * ay));
  return { ...ep, x: ep.x + t * ax, y: ep.y + t * ay };
};

const connectorLeavesOutward = (a: WallEndpoint, b: WallEndpoint): boolean => {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const len = Math.hypot(dx, dy);
  if (len < 1e-6) return false;
  const ux = dx / len;
  const uy = dy / len;
  const dotA = ux * Math.cos(a.outwardDeg * DEG2RAD) + uy * Math.sin(a.outwardDeg * DEG2RAD);
  const dotB = -ux * Math.cos(b.outwardDeg * DEG2RAD) - uy * Math.sin(b.outwardDeg * DEG2RAD);
  return dotA >= OUTWARD_DOT_MIN && dotB >= OUTWARD_DOT_MIN;
};

export const computeMultiWallGapRuns = (
  selectedWalls: SceneWallState[],
  allWalls: SceneWallState[],
  existingRuns: SceneRunState[],
  mode: CornerFillMode = 'auto',
): GapEdge[] => {
  if (selectedWalls.length < 2) return [];
  const allEndpoints = allWalls.flatMap(wallEndpoints);
  const free = selectedWalls
    .flatMap(wallEndpoints)
    .filter(
      (point) =>
        !allEndpoints.some(
          (other) =>
            other.wall.id !== point.wall.id &&
            Math.hypot(other.x - point.x, other.y - point.y) <= ENDPOINT_JOIN_TOLERANCE_MM,
        ),
    );
  const wallBlockers = allWalls.map(wallBlocker);
  const gapRunBlockers: PlanFootprint[] = existingRuns.map(runBlocker);
  const pairs: { i: number; j: number; distance: number }[] = [];
  for (let i = 0; i < free.length; i += 1) {
    for (let j = i + 1; j < free.length; j += 1) {
      if (free[j].wall.id === free[i].wall.id) continue;
      // Gate the gap on the CENTRELINE distance between the free ends (the real gap). A thick
      // perpendicular corner whose near FACES nearly touch collapses both refined endpoints to
      // the same corner point (refined distance ≈ 0), which wrongly failed MIN_GAP and dropped a
      // perfectly valid L corner before any leg was tried.
      const centreDistance = Math.hypot(free[j].x - free[i].x, free[j].y - free[i].y);
      if (centreDistance < MIN_GAP_MM || centreDistance > MAX_GAP_MM) continue;
      if (!connectorLeavesOutward(free[i], free[j])) continue;
      // Rank by NEAREST-CORNER distance (each end slid to its near face) so the greedy pass
      // joins the corners that are actually closest — not centreline ends that can mis-rank for
      // thick walls and pick a far corner.
      const ai = refineEndpointToFace(free[i], free[j]);
      const bj = refineEndpointToFace(free[j], free[i]);
      const distance = Math.hypot(bj.x - ai.x, bj.y - ai.y);
      pairs.push({ i, j, distance });
    }
  }
  pairs.sort((a, b) => a.distance - b.distance);
  const edges: GapEdge[] = [];
  const used = new Set<number>();
  let cornerGroup = 0;
  for (const pair of pairs) {
    if (used.has(pair.i) || used.has(pair.j)) continue;
    // Bridge the walls' NEAREST corners (not their centrelines): slide each gap endpoint
    // along its own end face toward the other wall. Decides the attach points for every mode.
    const a = refineEndpointToFace(free[pair.i], free[pair.j]);
    const b = refineEndpointToFace(free[pair.j], free[pair.i]);

    if (mode === 'arc') {
      // A bent-glass run rounding the corner; bulge sits in empty space outside the
      // walls, so it is placed directly (no trim) and blocked by its straight chord.
      const arcEdge = arcCornerEdge(a, b);
      if (!arcEdge) continue;
      used.add(pair.i);
      used.add(pair.j);
      gapRunBlockers.push(
        capsuleFootprint(
          `gap-run-${edges.length}`,
          a.x,
          a.y,
          Math.hypot(b.x - a.x, b.y - a.y),
          normalizeDeg(Math.atan2(b.y - a.y, b.x - a.x) * RAD2DEG),
          RUN_PLAN_THICKNESS_MM / 2,
          arcEdge.geomZ ?? 0,
          (arcEdge.geomZ ?? 0) + Math.max(1, arcEdge.heightMm ?? 1),
        ),
      );
      edges.push(arcEdge);
      continue;
    }

    // Corner legs use each wall's own height (set inside cornerCandidates); the
    // straight single-run fallback can only carry one height, so it uses the shorter.
    const straightHeightMm = Math.round(Math.min(a.heightMm, b.heightMm));
    const straightBlockers: PlanFootprint[] = [
      ...wallBlockers.filter((w) => w.ownerId !== a.wall.id && w.ownerId !== b.wall.id),
      ...gapRunBlockers,
    ];
    // An L leg extends one wall TO the corner it shares with the other (partner) wall, so it
    // legitimately reaches right up to the partner — excluding the partner from the leg's
    // trimming blockers stops the partner wall's own footprint from trimming the leg to nothing
    // (the bug that made thick/close perpendicular walls return no L fill). Other walls + already
    // placed gap runs still block.
    const legBlockers = (ownWallId: string, partnerWallId: string): PlanFootprint[] => [
      ...wallBlockers.filter((w) => w.ownerId !== ownWallId && w.ownerId !== partnerWallId),
      ...gapRunBlockers,
    ];
    // 'straight' skips corner legs entirely; 'auto'/'L' try the L legs first. Like arc + straight,
    // the legs prefer the NEAREST-corner refined endpoints (a/b) so an L bridges the walls' actual
    // closest corners, not their centrelines (thick walls used to misalign). Fall back to the
    // centreline ends when refinement collapses the legs to nothing — for very thick walls the two
    // near corners can coincide, which would otherwise make L mode silently emit no fill.
    // Perpendicular pairs go through cornerCandidates; an 'L' on PARALLEL walls falls back to the
    // right-angle parallel-L generator (kept out of 'auto' so it still bridges collinear straight).
    const corner =
      mode === 'straight'
        ? null
        : (cornerCandidates(a, b) ??
          cornerCandidates(free[pair.i], free[pair.j]) ??
          (mode === 'L'
            ? (parallelCornerCandidates(a, b) ??
              parallelCornerCandidates(free[pair.i], free[pair.j]))
            : null));
    let isCorner = corner !== null;
    let trimmed = (corner ?? [])
      .map((leg) =>
        trimEdge(
          leg.edge,
          legBlockers(leg.ownWallId, leg.ownWallId === a.wall.id ? b.wall.id : a.wall.id),
        ),
      )
      .filter((candidate): candidate is EdgeCandidate => candidate !== null);
    // 'L' is corner-legs ONLY — never a single straight, because a straight between two
    // perpendicular walls is a wrong-looking diagonal. 'auto' (and the default) still fall back
    // to a straight connector for collinear/near-parallel walls.
    if (trimmed.length === 0 && mode !== 'L') {
      isCorner = false;
      // A flat connector run can only carry one elevation; bridge the pair at the
      // lower of the two wall bases so it still reaches both ends.
      const straightBaseZ = Math.min(a.baseZMm, b.baseZMm);
      const straight = edgeBetween(a, b, straightHeightMm, straightBaseZ);
      const trimmedStraight = straight ? trimEdge(straight, straightBlockers) : null;
      trimmed = trimmedStraight ? [trimmedStraight] : [];
    }
    if (trimmed.length === 0) continue;
    used.add(pair.i);
    used.add(pair.j);
    const group = isCorner && trimmed.length > 1 ? (cornerGroup += 1) : undefined;
    for (const candidate of trimmed) {
      gapRunBlockers.push(
        capsuleFootprint(
          `gap-run-${edges.length}`,
          candidate.originX,
          candidate.originY,
          candidate.lengthMm,
          candidate.rotationDeg,
          RUN_PLAN_THICKNESS_MM / 2,
          candidate.baseZMm,
          candidate.baseZMm + Math.max(1, candidate.heightMm),
        ),
      );
      edges.push({
        originX: Math.round(candidate.originX),
        originY: Math.round(candidate.originY),
        rotationDeg: candidate.rotationDeg,
        lengthMm: Math.round(candidate.lengthMm),
        heightMm: candidate.heightMm,
        geomZ: Math.round(candidate.baseZMm),
        cornerGroup: group,
      });
    }
  }
  return edges;
};
