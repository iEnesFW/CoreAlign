import { arcCommitKeepingEnds } from '../geometry/arcCommit';
import { bodyDevelopedLengthMm } from '../geometry/curvature';
import type {
  CornerRadiiMm,
  EdgeArcMap,
  SceneSlabState,
  SceneSurfaceState,
  SceneWallState,
} from './project.types';

/**
 * Horizontal mirror ("Yatay aynala") for the bodies that carry one.
 *
 * A mirror has to reverse EVERY asymmetry the body carries, not just the obvious ones. The three
 * that were being missed made the button look broken:
 *  - the ARC: negating the sweep alone is not enough on a rolled body — `rotationDeg` is the start
 *    tangent, so it must be re-rolled or the curve bulges the same way and nothing visibly happens.
 *  - the ON-FACE offsets: on a curved wall these live along the DEVELOPED band (radius x sweep),
 *    so mirroring them about the chord pushes them off the band.
 *  - the EDGE bows: `geomEdgeArc` left/right and polygon `edgeArcs` handedness.
 *
 * A body with none of those (a plain rectangle with no openings) mirrors to itself — that is
 * geometrically correct, not a bug.
 */

const swapRadii = (r?: CornerRadiiMm | null): CornerRadiiMm => ({
  tl: r?.tr,
  tr: r?.tl,
  bl: r?.br,
  br: r?.bl,
});

/**
 * The two END edges trade places; front/back are the long faces and stay put — the same left/right
 * swap {@link swapRadii} applies to the corners.
 */
const swapEdgeArc = (arc?: EdgeArcMap | null): EdgeArcMap | null => {
  if (!arc) return null;
  return { front: arc.front, back: arc.back, left: arc.right, right: arc.left };
};

export const mirrorWallPatch = (wall: SceneWallState): Partial<SceneWallState> => {
  const faceLengthMm = bodyDevelopedLengthMm(wall);
  const arcPatch = arcCommitKeepingEnds(wall, { kind: 'flip' }).patch ?? {};
  const heightEnd = wall.heightEndMm;
  const slopeSwap =
    heightEnd !== null && heightEnd !== undefined
      ? { heightMm: heightEnd, heightEndMm: wall.heightMm }
      : {};
  return {
    ...arcPatch,
    cornerRadiiMm: swapRadii(wall.cornerRadiiMm),
    geomEdgeArc: swapEdgeArc(wall.geomEdgeArc),
    ...slopeSwap,
    openings: (wall.openings ?? []).map((o) => ({
      ...o,
      offsetMm: faceLengthMm - o.offsetMm,
    })),
    features: (wall.features ?? []).map((f) => ({
      ...f,
      offsetMm: faceLengthMm - f.offsetMm,
      points: f.points ? f.points.map((p) => ({ x: -p.x, z: p.z })) : f.points,
    })),
  };
};

export const mirrorSlabPatch = (slab: SceneSlabState): Partial<SceneSlabState> => {
  const faceLengthMm = bodyDevelopedLengthMm(slab);
  // Slabs use the SYMMETRIC pose (the mesh builder owns the mirror), so the flip only negates the
  // sweep — rolling rotationDeg here would swing the whole deck.
  const arcPatch = arcCommitKeepingEnds(slab, { kind: 'flip' }, { pose: 'symmetric' }).patch ?? {};
  return {
    ...arcPatch,
    cornerRadiiMm: swapRadii(slab.cornerRadiiMm),
    geomEdgeArc: swapEdgeArc(slab.geomEdgeArc),
    features: (slab.features ?? []).map((f) => ({
      ...f,
      offsetMm: faceLengthMm - f.offsetMm,
      points: f.points ? f.points.map((p) => ({ x: -p.x, z: p.z })) : f.points,
    })),
  };
};

export const mirrorSurfacePatch = (surface: SceneSurfaceState): Partial<SceneSurfaceState> => {
  const count = surface.points.length;
  if (count === 0) return {};
  const cx = surface.points.reduce((sum, p) => sum + p.x, 0) / count;
  // WHY negate the sagittae: mirroring reverses each edge's handedness, so the same stored sagitta
  // now resolves to the opposite perpendicular — an outward bow would come back as an inward dent
  // and the surface would change SHAPE instead of mirroring. Vertex order is preserved, so edge i
  // is still edge i; only the sign flips.
  return {
    points: surface.points.map((p) => ({ x: Math.round(2 * cx - p.x), y: p.y })),
    edgeArcs: surface.edgeArcs
      ? surface.edgeArcs.map((s) => (typeof s === 'number' ? -s : s))
      : surface.edgeArcs,
  };
};
