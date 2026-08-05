import type {
  CornerRadiiMm,
  EdgeArcMap,
  SceneWallFeature,
  SceneWallOpening,
  SceneWallState,
  WallEdgeNotch,
} from '../../model/project.types';

// WHY a structural signature instead of object identity: the wall geometry memo used to key on the
// `features` / `openings` / `cornerRadiiMm` / `geomEdgeArc` REFERENCES, so any path that reproduces
// the scene with fresh identities — undo/redo and the autofill commit both `structuredClone` — made
// the memo miss and replayed the curved-band CSG chain, which costs hundreds of milliseconds per
// hole and seconds at a wide sweep. Serialising a handful of numbers costs microseconds.
//
// The field order here is FIXED and written out by hand rather than left to JSON key order, so two
// walls that are geometrically identical always produce the same string even when their objects
// were built by different code paths (server DTO vs local edit vs clone).

export type WallGeometryFields = Pick<
  SceneWallState,
  | 'bendAngleDeg'
  | 'bendAtMm'
  | 'cornerNotchMm'
  | 'cornerRadiiMm'
  | 'edgeNotchMm'
  | 'features'
  | 'geomArcRadiusMm'
  | 'geomArcSweepDeg'
  | 'geomEdgeArc'
  | 'heightEndMm'
  | 'heightMm'
  | 'lengthMm'
  | 'openings'
  | 'thicknessMm'
>;

const num = (value: number | null | undefined): string =>
  value === null || value === undefined ? '' : String(value);

const corners = (value: CornerRadiiMm | null | undefined): string =>
  value ? `${num(value.tl)},${num(value.tr)},${num(value.br)},${num(value.bl)}` : '';

const edgeArc = (value: EdgeArcMap | null | undefined): string =>
  value ? `${num(value.front)},${num(value.right)},${num(value.back)},${num(value.left)}` : '';

const notches = (value: WallEdgeNotch[] | null | undefined): string =>
  (value ?? [])
    .map((n) => `${n.edge}:${num(n.offsetMm)}:${num(n.widthMm)}:${num(n.depthMm)}`)
    .join(';');

const openings = (value: SceneWallOpening[] | null | undefined): string =>
  (value ?? [])
    .map(
      (o) => `${o.kind}:${num(o.offsetMm)}:${num(o.sillMm)}:${num(o.widthMm)}:${num(o.heightMm)}`,
    )
    .join(';');

const features = (value: SceneWallFeature[] | null | undefined): string =>
  (value ?? [])
    .map((f) =>
      [
        f.shape,
        f.mode,
        String(f.side),
        num(f.offsetMm),
        num(f.centerZMm),
        num(f.widthMm),
        num(f.heightMm),
        num(f.depthMm),
        num(f.sides),
        (f.points ?? []).map((p) => `${num(p.x)}|${num(p.z)}`).join('~'),
      ].join(':'),
    )
    .join(';');

export const wallGeometrySignature = (wall: WallGeometryFields): string =>
  [
    num(wall.lengthMm),
    num(wall.heightMm),
    num(wall.heightEndMm),
    num(wall.thicknessMm),
    num(wall.geomArcRadiusMm),
    num(wall.geomArcSweepDeg),
    num(wall.bendAtMm),
    num(wall.bendAngleDeg),
    corners(wall.cornerRadiiMm),
    corners(wall.cornerNotchMm),
    edgeArc(wall.geomEdgeArc),
    notches(wall.edgeNotchMm),
    openings(wall.openings),
    features(wall.features),
  ].join('/');
