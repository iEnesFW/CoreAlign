import { type BufferGeometry, ExtrudeGeometry, Matrix4, Mesh, Shape, Vector3 } from 'three';
import { CSG } from 'three-csg-ts';
import { logger } from '@/shared/lib/logger';
import { buildCurvedWallFeatureSolid } from './curvedExtrude';
import { featureOutlineMm } from '../../model/wallFeatureGeometry';
import type { FeatureOutlinePoint } from '../../model/wallFeatureGeometry';
import type { SceneWallFeature } from '../../model/project.types';

export type WallFeatureSide = 'front' | 'back' | 'top' | 'bottom' | 'left' | 'right';

const ALL_SIDES: WallFeatureSide[] = ['front', 'back', 'top', 'bottom', 'left', 'right'];

// Old data (and the front/back-only draw path) stored side as 1 (front) / -1 (back). Accept
// both so saved scenes keep working as the model widens to six faces.
export const normalizeWallSide = (
  side: WallFeatureSide | number | null | undefined,
): WallFeatureSide => {
  if (typeof side === 'string' && (ALL_SIDES as string[]).includes(side)) {
    return side as WallFeatureSide;
  }
  return side === -1 ? 'back' : 'front';
};

// A picked triangle's normal (in the wall body's local frame X=length, Y=height, Z=thickness)
// → which of the six faces was clicked. Lets a click create a feature on that exact face.
export const sideFromLocalNormal = (n: { x: number; y: number; z: number }): WallFeatureSide => {
  const ax = Math.abs(n.x);
  const ay = Math.abs(n.y);
  const az = Math.abs(n.z);
  if (az >= ax && az >= ay) return n.z >= 0 ? 'front' : 'back';
  if (ax >= ay) return n.x >= 0 ? 'right' : 'left';
  return n.y >= 0 ? 'top' : 'bottom';
};

export interface WallBoxDims {
  lengthM: number;
  heightM: number;
  thicknessM: number;
}

export interface FaceFrame {
  origin: Vector3; // the face corner at (u=0, v=0), in the wall's local geometry frame (metres)
  uAxis: Vector3; // unit in-plane axis the feature width runs along
  vAxis: Vector3; // unit in-plane axis the feature height runs along
  normal: Vector3; // outward unit normal
  uMaxM: number; // face size along u
  vMaxM: number; // face size along v
  depthM: number; // material depth available behind the face (for a through-cut)
}

// Local geometry frame of a wall body: X∈[0,L] (length), Y∈[0,H] (height), Z∈[-t/2,+t/2]
// (thickness, centred — body is ExtrudeGeometry translated by -t/2). Each face exposes a 2D
// (u,v) coordinate system so a feature outline maps the same way it does on the front today.
export const wallFaceFrame = (side: WallFeatureSide, dims: WallBoxDims): FaceFrame => {
  const { lengthM: L, heightM: H, thicknessM: T } = dims;
  const h = T / 2;
  const X = new Vector3(1, 0, 0);
  const Y = new Vector3(0, 1, 0);
  const Z = new Vector3(0, 0, 1);
  switch (side) {
    case 'front':
      return {
        origin: new Vector3(0, 0, h),
        uAxis: X,
        vAxis: Y,
        normal: Z.clone(),
        uMaxM: L,
        vMaxM: H,
        depthM: T,
      };
    case 'back':
      return {
        origin: new Vector3(0, 0, -h),
        uAxis: X,
        vAxis: Y,
        normal: Z.clone().negate(),
        uMaxM: L,
        vMaxM: H,
        depthM: T,
      };
    case 'top':
      return {
        origin: new Vector3(0, H, -h),
        uAxis: X,
        vAxis: Z,
        normal: Y.clone(),
        uMaxM: L,
        vMaxM: T,
        depthM: H,
      };
    case 'bottom':
      return {
        origin: new Vector3(0, 0, -h),
        uAxis: X,
        vAxis: Z,
        normal: Y.clone().negate(),
        uMaxM: L,
        vMaxM: T,
        depthM: H,
      };
    case 'right':
      return {
        origin: new Vector3(L, 0, -h),
        uAxis: Y,
        vAxis: Z,
        normal: X.clone(),
        uMaxM: H,
        vMaxM: T,
        depthM: L,
      };
    case 'left':
      return {
        origin: new Vector3(0, 0, -h),
        uAxis: Y,
        vAxis: Z,
        normal: X.clone().negate(),
        uMaxM: H,
        vMaxM: T,
        depthM: L,
      };
  }
};

// A cutter/plug solid for a feature, placed on its face. The 2D outline (u,v in mm) is extruded
// along the inward (cut) or outward (protrude) normal and oriented onto the face. Returned in
// the wall's local geometry frame so it can be CSG-combined with the body.
export const buildFaceFeatureGeometry = (
  outlineMm: FeatureOutlinePoint[],
  frame: FaceFrame,
  depthM: number,
  outward: boolean,
): BufferGeometry | null => {
  if (outlineMm.length < 3 || depthM <= 0) return null;
  const shape = new Shape();
  outlineMm.forEach((p, i) => {
    const x = p.x / 1000;
    const y = p.z / 1000;
    if (i === 0) shape.moveTo(x, y);
    else shape.lineTo(x, y);
  });
  shape.closePath();
  const geo = new ExtrudeGeometry(shape, { depth: depthM, bevelEnabled: false });
  // Extrude is local (x→u, y→v, +z→depth). Send +z to the inward (-normal) or outward normal.
  const depthAxis = outward ? frame.normal.clone() : frame.normal.clone().negate();
  const basis = new Matrix4().makeBasis(frame.uAxis, frame.vAxis, depthAxis);
  basis.setPosition(frame.origin);
  geo.applyMatrix4(basis);
  geo.computeVertexNormals();
  return geo;
};

// Build the final wall body: start from the plain box body, SUBTRACT every hole/recess solid
// and UNION every protrusion, on whichever face each feature lives. Pure geometry (no scene
// refs); returns a new BufferGeometry. Falls back to the input body if there are no CSG cuts.
export interface WallFaceFeature {
  outlineMm: FeatureOutlinePoint[];
  side: WallFeatureSide;
  mode: 'hole' | 'recess' | 'protrude';
  depthMm: number;
}

export const applyWallFaceFeatures = (
  body: BufferGeometry,
  features: WallFaceFeature[],
  dims: WallBoxDims,
): BufferGeometry => {
  // A flush recess (depth ≤ 0) is a non-cutting outline — the same way the front/back path
  // renders it as a line. Never carve it (it would leave a stray 2 mm pock on the side face).
  const ops = features.filter(
    (f) => f.outlineMm.length >= 3 && !(f.mode === 'recess' && f.depthMm <= 0),
  );
  if (ops.length === 0) return body;
  let mesh = new Mesh(body);
  mesh.updateMatrix();
  for (const f of ops) {
    const frame = wallFaceFrame(f.side, dims);
    const outward = f.mode === 'protrude';
    const depthM = f.mode === 'hole' ? frame.depthM : Math.max(0.002, f.depthMm / 1000);
    const geo = buildFaceFeatureGeometry(f.outlineMm, frame, depthM, outward);
    if (!geo) continue;
    // WHY: a single degenerate cutter must never break the whole wall mesh — skip it and keep
    // the body built so far, rather than throwing out of the geometry builder.
    try {
      const tool = new Mesh(geo);
      tool.updateMatrix();
      const next = outward ? CSG.union(mesh, tool) : CSG.subtract(mesh, tool);
      // WHY: when a side cut crosses an existing front/back hole, three-csg-ts can return an
      // EMPTY mesh (coplanar/degenerate BSP) WITHOUT throwing — that empty mesh would then make
      // the whole wall vanish and poison every later op. Keep the previous body if the result
      // collapsed to nothing.
      const count = next.geometry.getAttribute('position')?.count ?? 0;
      if (count === 0) {
        logger.error('wall face CSG produced an empty result; keeping prior body', {
          side: f.side,
          mode: f.mode,
        });
        next.geometry.dispose();
      } else {
        if (mesh.geometry !== body) mesh.geometry.dispose();
        mesh = next;
      }
    } catch (error) {
      // Skip a degenerate cutter rather than break the whole wall, but surface why so a real
      // CSG failure isn't invisible.
      logger.error('wall face CSG failed', { side: f.side, mode: f.mode, error });
    }
    geo.dispose();
  }
  mesh.geometry.computeVertexNormals();
  mesh.updateMatrix();
  return mesh.geometry;
};

export interface CurvedWallParams {
  lengthMm: number;
  radiusM: number;
  direction: 1 | -1;
  sweep: number;
  thicknessM: number;
}

// Carve front/back holes & recesses into a CURVED (arc-in-plan) wall body by CSG-subtracting a
// curved cutter that follows the feature outline along the arc (buildCurvedWallFeatureSolid is in
// the same pre-rotation frame as the band). Mirrors applyWallFaceFeatures' empty-result guard so a
// degenerate cut can never collapse the whole wall. Flush recesses (depth <= 0) and protrusions are
// NOT cut here (protrusions are additive meshes; a flush recess is just a non-cutting outline).
export const applyCurvedWallFeatures = (
  body: BufferGeometry,
  features: SceneWallFeature[],
  params: CurvedWallParams,
): BufferGeometry => {
  const { lengthMm, radiusM, direction, sweep, thicknessM } = params;
  const outerR = radiusM + thicknessM / 2;
  const innerR = Math.max(0.001, radiusM - thicknessM / 2);
  const cuts = features.filter(
    (f) =>
      (f.side === 1 || f.side === -1) &&
      (f.mode === 'hole' || (f.mode === 'recess' && f.depthMm > 0)),
  );
  if (cuts.length === 0) return body;
  let mesh = new Mesh(body);
  mesh.updateMatrix();
  for (const f of cuts) {
    const outline = featureOutlineMm(f).map((p) => ({ x: p.x, z: p.z }));
    if (outline.length < 3) continue;
    const depthM = Math.max(0.002, f.depthMm / 1000);
    const isFront = f.side === 1; // front = OUTER (convex) face; back = INNER (concave)
    let rNear: number;
    let rFar: number;
    if (f.mode === 'hole') {
      rNear = innerR - 0.02;
      rFar = outerR + 0.02;
    } else if (isFront) {
      rNear = Math.max(innerR + 0.001, outerR - depthM);
      rFar = outerR + 0.02;
    } else {
      rNear = innerR - 0.02;
      rFar = Math.min(outerR - 0.001, innerR + depthM);
    }
    const cutter = buildCurvedWallFeatureSolid(
      outline,
      lengthMm,
      radiusM,
      direction,
      sweep,
      rNear,
      rFar,
    );
    if ((cutter.attributes.position?.count ?? 0) === 0) {
      cutter.dispose();
      continue;
    }
    try {
      const tool = new Mesh(cutter);
      tool.updateMatrix();
      const next = CSG.subtract(mesh, tool);
      const count = next.geometry.getAttribute('position')?.count ?? 0;
      if (count === 0) {
        logger.error('curved wall CSG produced an empty result; keeping prior body', {
          side: f.side,
          mode: f.mode,
        });
        next.geometry.dispose();
      } else {
        if (mesh.geometry !== body) mesh.geometry.dispose();
        mesh = next;
      }
    } catch (error) {
      logger.error('curved wall feature CSG failed', { side: f.side, mode: f.mode, error });
    }
    cutter.dispose();
  }
  mesh.geometry.computeVertexNormals();
  mesh.updateMatrix();
  return mesh.geometry;
};
