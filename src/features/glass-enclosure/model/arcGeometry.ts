export interface ArcChord {
  midX: number;
  midZ: number;
  yawRad: number;
  chordM: number;
}

export interface ArcBoundary {
  x: number;
  z: number;
  tangentRad: number;
}

export interface ArcPanelSpan {
  phiStart: number;
  phiEnd: number;
  phiMid: number;
}

export interface ArcLayout {
  sweepRad: number;
  direction: 1 | -1;
  panelChords: ArcChord[];
  panelSpans: ArcPanelSpan[];
  barSegments: ArcChord[];
  boundaries: ArcBoundary[];
  apex: { x: number; z: number };
}

const MAX_SWEEP_RAD = Math.PI * 2;
const BAR_SEGMENT_STEP_RAD = 0.1;
const MIN_BAR_SEGMENTS = 12;

// CHORD-INVARIANT model: lengthMm is the CHORD (the straight span between the two endpoints) and
// stays fixed when curving; the arc (radius + signed sweep) is an overlay. The tightest radius for
// a given chord is a half-circle (radius = chord/2) — no circular arc passes through both endpoints
// below that. A deeper "wrap" past a semicircle comes from a larger SWEEP, not a smaller radius
// (major arcs also have radius ≥ chord/2).
export const minArcRadiusMm = (chordMm: number) => Math.max(1, Math.ceil(chordMm / 2));

export const effectiveArcRadiusMm = (chordMm: number, radiusMm: number) =>
  Math.max(radiusMm, minArcRadiusMm(chordMm));

const MIN_SWEEP_RAD = 0.0001;

export interface ResolvedArc {
  radiusMm: number;
  radiusM: number;
  sweepRad: number;
  direction: 1 | -1;
  arcLengthMm: number;
  arcLengthM: number;
}

// Resolve an arc's render parameters from the canonical (chord, signed sweep) pair. The radius and
// developed arc length are DERIVED so the rendered chord always equals chordMm — the entity keeps
// lengthMm (the chord) as the user's invariant span and the curve is a pure overlay.
export const resolveArc = (chordMm: number, sweepDeg: number): ResolvedArc => {
  const sweepRad = Math.min(
    MAX_SWEEP_RAD,
    Math.max(MIN_SWEEP_RAD, (Math.abs(sweepDeg) * Math.PI) / 180),
  );
  const direction: 1 | -1 = sweepDeg < 0 ? -1 : 1;
  const radiusMm = chordMm / (2 * Math.sin(sweepRad / 2));
  const arcLengthMm = radiusMm * sweepRad;
  return {
    radiusMm,
    radiusM: radiusMm / 1000,
    sweepRad,
    direction,
    arcLengthMm,
    arcLengthM: arcLengthMm / 1000,
  };
};

// Recover the chord of an arc from its stored radius + sweep. Correct for BOTH chord-invariant data
// and legacy data whose lengthMm was the developed arc length, so it doubles as an idempotent
// migration: chord = 2·r·sin(sweep/2). Returns the input fallback when there is no usable arc.
export const arcChordFromRadiusSweep = (
  fallbackMm: number,
  radiusMm: number | null | undefined,
  sweepDeg: number | null | undefined,
): number => {
  if (!radiusMm || radiusMm <= 0 || !sweepDeg) return fallbackMm;
  const sweepRad = Math.min(
    MAX_SWEEP_RAD,
    Math.max(MIN_SWEEP_RAD, (Math.abs(sweepDeg) * Math.PI) / 180),
  );
  const chord = Math.round(2 * radiusMm * Math.sin(sweepRad / 2));
  return chord > 0 ? chord : fallbackMm;
};

export interface ArcEndLocal {
  xMm: number;
  yMm: number;
  tangentDeg: number;
}

// Endpoint of the arc relative to its start, in the pre-rotation plan frame. Fed the DERIVED arc
// length + radius (from resolveArc), so the returned endpoint sits exactly chordMm from the origin.
export const arcEndLocal = (
  arcLengthMm: number,
  radiusMm: number,
  sweepSign: number,
): ArcEndLocal => {
  const direction = sweepSign < 0 ? -1 : 1;
  const radius = Math.max(0.001, radiusMm);
  const sweepRad = Math.min(arcLengthMm / radius, MAX_SWEEP_RAD);
  return {
    xMm: radius * Math.sin(sweepRad),
    yMm: direction * radius * (1 - Math.cos(sweepRad)),
    tangentDeg: direction * sweepRad * (180 / Math.PI),
  };
};

const arcPoint = (radiusM: number, direction: number, phi: number) => ({
  x: radiusM * Math.sin(phi),
  z: direction * radiusM * (1 - Math.cos(phi)),
});

const tangentAt = (direction: number, phi: number) =>
  Math.atan2(direction * Math.sin(phi), Math.cos(phi));

const chordBetween = (radiusM: number, direction: number, phiA: number, phiB: number): ArcChord => {
  const a = arcPoint(radiusM, direction, phiA);
  const b = arcPoint(radiusM, direction, phiB);
  const dx = b.x - a.x;
  const dz = b.z - a.z;
  return {
    midX: (a.x + b.x) / 2,
    midZ: (a.z + b.z) / 2,
    yawRad: Math.atan2(dz, dx),
    chordM: Math.hypot(dx, dz),
  };
};

export const computeArcLayout = (
  arcLengthM: number,
  radiusM: number,
  sweepSign: number,
  panelWidthsM: number[],
): ArcLayout => {
  const direction: 1 | -1 = sweepSign < 0 ? -1 : 1;
  const safeRadiusM = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const sweepRad = Math.min(arcLengthM / safeRadiusM, MAX_SWEEP_RAD);
  const totalWidth = panelWidthsM.reduce((sum, w) => sum + w, 0);
  const shares =
    totalWidth > 0 && panelWidthsM.length > 0 ? panelWidthsM.map((w) => w / totalWidth) : [1];

  const boundaries: ArcBoundary[] = [];
  const panelChords: ArcChord[] = [];
  const panelSpans: ArcPanelSpan[] = [];
  const barSegments: ArcChord[] = [];

  let phi = 0;
  boundaries.push({ ...arcPoint(safeRadiusM, direction, 0), tangentRad: tangentAt(direction, 0) });
  for (const share of shares) {
    const phiEnd = phi + share * sweepRad;
    panelChords.push(chordBetween(safeRadiusM, direction, phi, phiEnd));
    panelSpans.push({ phiStart: phi, phiEnd, phiMid: (phi + phiEnd) / 2 });
    phi = phiEnd;
    boundaries.push({
      ...arcPoint(safeRadiusM, direction, phiEnd),
      tangentRad: tangentAt(direction, phiEnd),
    });
  }

  const segmentCount = Math.max(MIN_BAR_SEGMENTS, Math.ceil(sweepRad / BAR_SEGMENT_STEP_RAD));
  for (let i = 0; i < segmentCount; i += 1) {
    const phiA = (i / segmentCount) * sweepRad;
    const phiB = ((i + 1) / segmentCount) * sweepRad;
    barSegments.push(chordBetween(safeRadiusM, direction, phiA, phiB));
  }

  return {
    sweepRad,
    direction,
    panelChords,
    panelSpans,
    barSegments,
    boundaries,
    apex: arcPoint(safeRadiusM, direction, sweepRad / 2),
  };
};

export const arcPointAt = (radiusM: number, direction: number, phi: number) =>
  arcPoint(radiusM, direction, phi);

export interface ArcDerived {
  radiusMm: number;
  sweepDeg: number;
  chordMm: number;
  sagittaMm: number;
  arcLengthMm: number;
}

// Set the curve by RADIUS while keeping the chord fixed. radius ≥ chord/2; the resulting sweep is
// the MINOR arc (≤ 180°). A deeper curve (> 180°) is set via the bow drag or the sweep input.
export const deriveArcFromRadius = (chordMm: number, radiusMm: number): ArcDerived => {
  const radius = effectiveArcRadiusMm(chordMm, radiusMm);
  const sweepRad = 2 * Math.asin(Math.min(1, chordMm / (2 * radius)));
  return {
    radiusMm: Math.round(radius),
    sweepDeg: (sweepRad * 180) / Math.PI,
    chordMm: Math.round(chordMm),
    sagittaMm: Math.round(radius * (1 - Math.cos(sweepRad / 2))),
    arcLengthMm: Math.round(radius * sweepRad),
  };
};

// Set the curve by SWEEP angle while keeping the chord fixed (radius derived). Allows past a
// half-circle (up to ~350°) so a curve can be driven deeper than 180°.
export const deriveArcFromSweep = (chordMm: number, sweepDeg: number): ArcDerived => {
  const clampedDeg = Math.min(350, Math.max(1, Math.abs(sweepDeg)));
  const sweepRad = (clampedDeg * Math.PI) / 180;
  const radius = chordMm / (2 * Math.sin(sweepRad / 2));
  return {
    radiusMm: Math.round(radius),
    sweepDeg: clampedDeg,
    chordMm: Math.round(chordMm),
    sagittaMm: Math.round(radius * (1 - Math.cos(sweepRad / 2))),
    arcLengthMm: Math.round(radius * sweepRad),
  };
};

export const deriveArcFromChordSagitta = (chordMm: number, sagittaMm: number): ArcDerived => {
  const sagitta = Math.max(0.001, Math.abs(sagittaMm));
  const radius = sagitta / 2 + (chordMm * chordMm) / (8 * sagitta);
  const minorSweep = 2 * Math.asin(Math.min(1, chordMm / (2 * radius)));
  // Once the bow exceeds the half-chord the arc is the MAJOR arc (> 180°): the chord then subtends
  // 2π − minorSweep. Without this the sweep saturated at 180° and the curve could not go deeper.
  const sweepRad = Math.min(
    MAX_SWEEP_RAD,
    sagitta > chordMm / 2 ? 2 * Math.PI - minorSweep : minorSweep,
  );
  const arcLengthMm = radius * sweepRad;
  return {
    radiusMm: Math.round(radius),
    sweepDeg: (sweepRad * 180) / Math.PI,
    chordMm: Math.round(chordMm),
    sagittaMm: Math.round(sagittaMm),
    arcLengthMm: Math.round(arcLengthMm),
  };
};

export const facetJointAngleDeg = (sweepDeg: number, panelCount: number) =>
  panelCount > 0 ? sweepDeg / panelCount : sweepDeg;

export interface BowArc {
  geomArcRadiusMm: number | null;
  geomArcSweepDeg: number | null;
  rotationDeg: number;
  lengthMm: number;
  arcLengthMm: number;
}

// Turn a perpendicular "bow" (signed sagitta in the chord's +90° across direction) into the arc
// params for a run/wall whose CHORD stays fixed (same start + end). rotationDeg is rolled back by
// dir·sweep/2 so the arc's chord still points along chordDeg — i.e. the body bows WITHOUT rotating
// (the previous bug). The dir/sign convention matches the autofill arc (arcCornerEdge): the bulge
// sits on the OPPOSITE side of the sweep sign. Under the straighten threshold it returns straight.
export const arcFromBow = (
  chordMm: number,
  chordDeg: number,
  sagittaMm: number,
  straightenMm = 25,
): BowArc => {
  if (Math.abs(sagittaMm) < straightenMm) {
    return {
      geomArcRadiusMm: null,
      geomArcSweepDeg: null,
      rotationDeg: Math.round(chordDeg * 10) / 10,
      lengthMm: Math.round(chordMm),
      arcLengthMm: Math.round(chordMm),
    };
  }
  const dir = sagittaMm >= 0 ? -1 : 1;
  const d = deriveArcFromChordSagitta(chordMm, Math.abs(sagittaMm));
  return {
    geomArcRadiusMm: d.radiusMm,
    geomArcSweepDeg: Math.round(dir * d.sweepDeg * 10) / 10,
    rotationDeg: Math.round((chordDeg - dir * (d.sweepDeg / 2)) * 10) / 10,
    // lengthMm is the CHORD (invariant span) — NOT the developed arc length (the old ballooning bug).
    lengthMm: Math.round(chordMm),
    arcLengthMm: d.arcLengthMm,
  };
};

// The current signed bow (sagitta in the +90° across direction) of an existing arc, so a re-adjust
// handle starts at the apex. Inverse of arcFromBow's sign rule (bulge opposite the sweep sign).
export const bowFromArc = (chordMm: number, radiusMm: number, sweepSignDeg: number): number => {
  const r = Math.max(radiusMm, chordMm / 2);
  const minorSag = r - Math.sqrt(Math.max(0, r * r - (chordMm / 2) ** 2));
  // A major arc (> 180°) bulges to the FAR apex (2r − minorSag), not the near one — so a re-grab
  // handle on an already-deep curve starts at its real apex instead of a shallow phantom point.
  const sag = Math.abs(sweepSignDeg) > 180 ? 2 * r - minorSag : minorSag;
  return (sweepSignDeg < 0 ? 1 : -1) * sag;
};

// Samples the circular arc between two chord endpoints (plan mm) that bulges by `sagittaMm` toward
// the chord's +90° across direction. This is the SAME arc the bow commit produces, so a drag
// preview drawn from these points matches the committed result instead of approximating it with a
// parabola (which diverged badly once the bow passed a half-circle). Returns the straight chord for
// a negligible bow.
export const bowArcPlanPoints = (
  startX: number,
  startY: number,
  endX: number,
  endY: number,
  sagittaMm: number,
  segments = 48,
): { x: number; y: number }[] => {
  const dx = endX - startX;
  const dy = endY - startY;
  const chord = Math.hypot(dx, dy) || 1;
  if (Math.abs(sagittaMm) < 1) {
    return [
      { x: startX, y: startY },
      { x: endX, y: endY },
    ];
  }
  const acrossX = -dy / chord;
  const acrossY = dx / chord;
  const midX = (startX + endX) / 2;
  const midY = (startY + endY) / 2;
  const half = chord / 2;
  const center = (sagittaMm * sagittaMm - half * half) / (2 * sagittaMm);
  const cx = midX + acrossX * center;
  const cy = midY + acrossY * center;
  const r = Math.hypot(startX - cx, startY - cy);
  const a0 = Math.atan2(startY - cy, startX - cx);
  const a1 = Math.atan2(endY - cy, endX - cx);
  const apexX = midX + acrossX * sagittaMm;
  const apexY = midY + acrossY * sagittaMm;
  const aApex = Math.atan2(apexY - cy, apexX - cx);
  const twoPi = Math.PI * 2;
  const mod = (a: number) => ((a % twoPi) + twoPi) % twoPi;
  const ccwSweep = mod(a1 - a0);
  // Sweep in the direction that passes through the apex (minor or major arc).
  const sweep = mod(aApex - a0) <= ccwSweep ? ccwSweep : ccwSweep - twoPi;
  const pts: { x: number; y: number }[] = [];
  for (let i = 0; i <= segments; i += 1) {
    const ang = a0 + sweep * (i / segments);
    pts.push({ x: cx + r * Math.cos(ang), y: cy + r * Math.sin(ang) });
  }
  return pts;
};
