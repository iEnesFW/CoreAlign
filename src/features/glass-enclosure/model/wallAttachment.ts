import { arcEndLocal, isRealArc, radiusFromChordSweep } from './arcGeometry';
import type { SceneRunState, SceneWallState } from './project.types';

const ATTACH_BAND_MM = 80;
const TWO_PI = Math.PI * 2;

const runEndPoint = (run: SceneRunState): { x: number; y: number } => {
  const rad = (run.rotationDeg * Math.PI) / 180;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  if (isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)) {
    // CHORD-INVARIANT: the end sits at the fixed chord endpoint, derived from the stored radius+sweep.
    const e = arcEndLocal(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1);
    return {
      x: run.originX + e.xMm * cos - e.yMm * sin,
      y: run.originY + e.xMm * sin + e.yMm * cos,
    };
  }
  return { x: run.originX + run.lengthMm * cos, y: run.originY + run.lengthMm * sin };
};

const toLocal = (wall: SceneWallState, x: number, y: number) => {
  const radians = (wall.rotationDeg * Math.PI) / 180;
  const dx = x - wall.originX;
  const dy = y - wall.originY;
  return {
    along: dx * Math.cos(radians) + dy * Math.sin(radians),
    across: -dx * Math.sin(radians) + dy * Math.cos(radians),
  };
};

const pointAttached = (wall: SceneWallState, x: number, y: number) => {
  const local = toLocal(wall, x, y);
  const band = wall.thicknessMm / 2 + ATTACH_BAND_MM;
  // ARC wall: the real band curves away from the straight origin+rotation axis by R·(1−cos φ), so
  // the box test above never matches a run filling a mid-arc hole — the glass then stays behind
  // when the wall moves. Test radial deviation from the arc's centre (at (0, dir·R) in the local
  // start-tangent frame, mirroring buildArcWallFootprint) plus the polar angle within the sweep.
  if (isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)) {
    const sweepDeg = wall.geomArcSweepDeg ?? 0;
    const dir = sweepDeg < 0 ? -1 : 1;
    const r = radiusFromChordSweep(wall.lengthMm, wall.geomArcRadiusMm, sweepDeg);
    if (r <= 0) return false;
    const sweepRad = (Math.abs(sweepDeg) * Math.PI) / 180;
    const radial = Math.abs(Math.hypot(local.along, local.across - dir * r) - r);
    if (radial > band) return false;
    let phi = Math.atan2(local.along, r - dir * local.across);
    if (phi < 0) phi += TWO_PI;
    const angBand = ATTACH_BAND_MM / Math.max(1, r);
    return phi <= sweepRad + angBand || phi >= TWO_PI - angBand;
  }
  return (
    local.along >= -ATTACH_BAND_MM &&
    local.along <= wall.lengthMm + ATTACH_BAND_MM &&
    Math.abs(local.across) <= band
  );
};

export const findAttachedRunIds = (wall: SceneWallState, runs: SceneRunState[]): string[] => {
  const attached: string[] = [];
  for (const run of runs) {
    const end = runEndPoint(run);
    if (pointAttached(wall, run.originX, run.originY) && pointAttached(wall, end.x, end.y)) {
      attached.push(run.id);
    }
  }
  return attached;
};

export const findAttachedWallIds = (run: SceneRunState, walls: SceneWallState[]): string[] => {
  const end = runEndPoint(run);
  const attached: string[] = [];
  for (const wall of walls) {
    if (pointAttached(wall, run.originX, run.originY) && pointAttached(wall, end.x, end.y)) {
      attached.push(wall.id);
    }
  }
  return attached;
};

// PERSISTENT cam↔host bond resolvers. The geometric derivation above only catches a run while both
// its endpoints sit inside the wall's attach band; once the glass drifts out (a mis-tracked move,
// an inspector edit) the bond is lost forever. When a run carries an explicit hostWallId (set on
// autofill/hole-fill) we honour it in ADDITION to geometry, so a bonded run travels with its host
// even after drifting. Both are pure — no store/three access — so they are unit-testable and a
// drop-in for the geometric variants (identical output until hostWallId is populated).

export const resolveAttachedRunIds = (wall: SceneWallState, runs: SceneRunState[]): string[] => {
  const ids = new Set<string>(findAttachedRunIds(wall, runs));
  for (const run of runs) {
    if (run.hostWallId === wall.id) ids.add(run.id);
  }
  return [...ids];
};

export const resolveAttachedWallIds = (run: SceneRunState, walls: SceneWallState[]): string[] => {
  const geometric = findAttachedWallIds(run, walls);
  // Only honour an explicit host that still exists (a deleted wall must fall back to geometry).
  if (run.hostWallId && walls.some((w) => w.id === run.hostWallId)) {
    return geometric.includes(run.hostWallId) ? geometric : [run.hostWallId, ...geometric];
  }
  return geometric;
};
