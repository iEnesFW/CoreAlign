/**
 * The surface a glass pane presents to everything mounted on it.
 *
 * WHY this exists: hardware, fittings and their clamps each derived their own idea of where the
 * glass is. On a curved run the built-in handle stepped to the pane edge inside a FLAT chord frame
 * and left the cylinder by up to **353 mm** (R3 m, 60°, single pane), while user-placed hardware —
 * anchored on the cylinder — sat correctly on the same pane. Same pane, two answers.
 *
 * The contract: ONE frame function, and a FLAT pane is the degenerate case of the curved one
 * (`curve === null` → yaw 0, tangent +X, normal +Z, curvature 0). There is no second code path to
 * drift out of sync.
 *
 * Coordinates are pane-local: `u` runs along the pane's DEVELOPED width from its centre, `v` along
 * its height from the glass centre, `n` along the outward surface normal from the glass mid-plane.
 */

export interface PaneCurvature {
  radiusM: number;
  direction: 1 | -1;
  /** Angle of the pane's own mid-point on the run's arc. */
  phiMid: number;
}

export interface PaneSurface {
  /** The width actually DRAWN — the joint allowance is already deducted. */
  widthMm: number;
  heightMm: number;
  thicknessMm: number;
  /** Y of the pane group's base in its parent frame, metres. */
  baseYm: number;
  /** null = flat. A flat pane is the degenerate curved pane, not a separate case. */
  curve: PaneCurvature | null;
}

export interface SurfaceOffsetMm {
  /** Along the developed width, from the pane centre. */
  uMm: number;
  /** Along the height, from the glass centre. */
  vMm: number;
  /** Along the outward normal, from the glass mid-plane. */
  nMm: number;
}

export interface SurfaceFrame {
  /** Full position in the pane's PARENT frame, metres. */
  positionM: [number, number, number];
  /** Rotation about Y that aligns local +X with the surface tangent. Zero on a flat pane. */
  yawRad: number;
  /** Signed curvature along the width, 1/m. Zero on a flat pane. */
  kappaPerM: number;
}

const MM = 1000;

/** The point on the run's arc at `phi`, in the pane's parent frame (metres). */
const arcPointM = (curve: PaneCurvature, phi: number) => ({
  x: curve.radiusM * Math.sin(phi),
  z: curve.direction * curve.radiusM * (1 - Math.cos(phi)),
});

/**
 * THE mounting frame. Every piece of hardware, every built-in fitting and every clamp reads its
 * position from here — flat and curved alike.
 */
export const paneSurfaceFrame = (s: PaneSurface, offset: SurfaceOffsetMm): SurfaceFrame => {
  const y = s.baseYm + s.heightMm / 2 / MM + offset.vMm / MM;
  if (!s.curve) {
    return {
      positionM: [offset.uMm / MM, y, offset.nMm / MM],
      yawRad: 0,
      kappaPerM: 0,
    };
  }
  const { radiusM, direction, phiMid } = s.curve;
  // `u` is DEVELOPED arc length, so it converts to angle by dividing by the radius — the same
  // relation the curved pane mesh uses to lay its glass out.
  const phi = phiMid + offset.uMm / MM / radiusM;
  const anchor = arcPointM(s.curve, phi);
  const tangentYaw = Math.atan2(direction * Math.sin(phi), Math.cos(phi));
  // WHY the TANGENT frame's own +Z and not the arc's outward normal: `nMm` must stay CONTINUOUS
  // with the flat pane. As the sweep goes to zero the tangent frame becomes the flat frame, so a
  // positive `nMm` keeps meaning the same world side; anchoring it to the arc's outward normal
  // instead would flip every mounted piece to the other face the instant a straight run picked up
  // a 1° bow. Under rotation [0, -tangentYaw, 0] the local +Z maps to (-sin yaw, 0, cos yaw).
  const nM = offset.nMm / MM;
  return {
    positionM: [anchor.x - nM * Math.sin(tangentYaw), y, anchor.z + nM * Math.cos(tangentYaw)],
    yawRad: -tangentYaw,
    kappaPerM: direction / radiusM,
  };
};

/**
 * A straight drag measured in the tangent plane, converted to the DEVELOPED coordinate the surface
 * stores.
 *
 * WHY: the drag runs on a flat plane but the committed number is read back as arc length. On a
 * 1 m radius a single 500 mm drag over-shot by 36 mm; the 5 mm snap hid the small cases.
 */
export const developedFromTangentMm = (tangentMm: number, s: PaneSurface): number => {
  if (!s.curve) return tangentMm;
  const rMm = s.curve.radiusM * MM;
  return rMm * Math.atan(tangentMm / rMm);
};

/**
 * How far the centre of a straight chord of `spanMm` falls from the surface. Zero on a flat pane —
 * this is the measure that decides whether a piece must BEND rather than merely be posed.
 */
export const surfaceSagittaMm = (spanMm: number, s: PaneSurface): number => {
  if (!s.curve || spanMm <= 0) return 0;
  const rMm = s.curve.radiusM * MM;
  const half = Math.min(rMm, spanMm / 2);
  return rMm - Math.sqrt(Math.max(0, rMm * rMm - half * half));
};

/**
 * The furthest a piece of `itemWidthMm` × `itemHeightMm` may sit from the pane centre and still be
 * entirely ON the glass.
 *
 * WHY it lives here: the clamp used the raw stored panel width while the renderer drew the pane
 * 12 mm narrower — so a piece snapped to the edge sat 6 mm off the glass on every straight run, and
 * 11 mm on a faceted arc. Clamp and render now read the same surface.
 */
export const paneHalfSpanMm = (
  s: PaneSurface,
  itemWidthMm: number,
  itemHeightMm: number,
): { uMm: number; vMm: number } => ({
  uMm: Math.max(0, s.widthMm / 2 - itemWidthMm / 2),
  vMm: Math.max(0, s.heightMm / 2 - itemHeightMm / 2),
});

/** Where a piece should sit along the normal to rest ON the outer glass face. */
export const seatOnFaceMm = (glassThicknessMm: number, itemDepthMm: number): number =>
  Math.round((glassThicknessMm + itemDepthMm) / 2);

export interface SurfaceSegment {
  /** Local X within the mounting frame at the piece's centre, metres. */
  xM: number;
  /** Local Z (the face direction) within that frame, metres. */
  zM: number;
  /** Local yaw so the segment lies flat on the surface. */
  yawRad: number;
  /** Developed length this segment covers, mm. */
  spanMm: number;
}

/**
 * Break a piece of hardware that spans `spanMm` along the surface into segments that FOLLOW it,
 * expressed in the piece's own mounting frame.
 *
 * WHY: a rigid box is right for a lock and wrong for a 600 mm drip profile — on a 2 m radius that
 * box misses the glass by 22 mm at its ends, on a 1 m radius by 45 mm. Posing is not enough for
 * anything long; it has to BEND. Segments are derived from the same `paneSurfaceFrame` the glass
 * uses, so a bent piece can never disagree with the pane it sits on.
 *
 * A flat surface returns a single full-span segment at the origin — the rigid box, unchanged.
 */
export const surfaceSegmentsLocal = (
  s: PaneSurface,
  centre: SurfaceOffsetMm,
  spanMm: number,
  maxSegmentMm = 60,
): SurfaceSegment[] => {
  if (!s.curve || spanMm <= 0) return [{ xM: 0, zM: 0, yawRad: 0, spanMm }];
  const count = Math.max(1, Math.ceil(spanMm / maxSegmentMm));
  if (count === 1) return [{ xM: 0, zM: 0, yawRad: 0, spanMm }];
  const origin = paneSurfaceFrame(s, centre);
  const cos = Math.cos(origin.yawRad);
  const sin = Math.sin(origin.yawRad);
  const step = spanMm / count;
  const out: SurfaceSegment[] = [];
  for (let i = 0; i < count; i += 1) {
    const du = -spanMm / 2 + step * (i + 0.5);
    const f = paneSurfaceFrame(s, { ...centre, uMm: centre.uMm + du });
    const dx = f.positionM[0] - origin.positionM[0];
    const dz = f.positionM[2] - origin.positionM[2];
    out.push({
      // Undo the origin frame's yaw so the offsets are expressed in the piece's own axes.
      xM: dx * cos - dz * sin,
      zM: dx * sin + dz * cos,
      yawRad: f.yawRad - origin.yawRad,
      spanMm: step,
    });
  }
  return out;
};

/**
 * Whether a piece is a PROFILE that must follow the glass, or a BLOCK that only needs posing on it.
 *
 * The split is physical, not a stored flag: a piece conforms when it is an extrusion AND long
 * enough on THIS curve to leave the surface by more than a millimetre. So a vertical 12 mm gasket
 * never bends, a 600 mm drip profile on a 2 m radius does, and widening a handle to 800 mm makes it
 * start conforming with no data change and no migration.
 */
const CONFORMABLE_KINDS = new Set<string>([
  'GasketStrip',
  'DripProfile',
  'Vent',
  'Louver',
  'Handle',
  'PullHandle',
]);
const BEND_TOLERANCE_MM = 1;

export const hardwareBends = (item: { kind: string; widthMm: number }, s: PaneSurface): boolean =>
  CONFORMABLE_KINDS.has(item.kind) && surfaceSagittaMm(item.widthMm, s) > BEND_TOLERANCE_MM;
