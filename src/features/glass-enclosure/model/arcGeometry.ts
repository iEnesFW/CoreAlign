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

// Cap at 359°, never a full 360°: at exactly 2π the chord 2r·sin(sweep/2) collapses to 0, the two
// ends coincide and the body degenerates to zero length. deriveArcFromSweep already clamps to 359°;
// this keeps every radius+sweep render/derivation path (resolveArc, computeArcLayout, …) in step.
const MAX_SWEEP_RAD = (359 * Math.PI) / 180;
const BAR_SEGMENT_STEP_RAD = 0.1;
const MIN_BAR_SEGMENTS = 12;

// CHORD-INVARIANT model: lengthMm is the CHORD — the straight span between the two FIXED endpoints —
// and stays FIXED when curving. The arc is stored as (geomArcRadiusMm, geomArcSweepDeg) and bows
// BETWEEN those fixed ends, so the ends never move. The radius is DERIVED from chord+sweep, and the
// sweep is free across 1–359° (a minor ≤180° and a major >180° arc share the same two endpoints;
// a full 360° is excluded — it collapses the chord to zero). The developed glass length
// (= radius·sweep) is derived. The tightest radius is a half-circle (chord/2, at 180°); a deeper
// curve comes from a larger sweep, not a smaller radius.
export const minArcRadiusMm = (chordMm: number) => Math.max(1, Math.ceil(chordMm / 2));

// A run/wall/slab is a REAL arc only when it has BOTH a radius and a non-negligible sweep. Anything
// else (a "half-arc" — radius set but sweep null/0, e.g. from old persisted data) must be treated as
// STRAIGHT, never rendered as a degenerate band (which the wall's [-π/2,0,0] mesh pitch lays flat).
export const isRealArc = (radiusMm?: number | null, sweepDeg?: number | null): boolean =>
  Boolean(radiusMm && radiusMm > 0 && Math.abs(sweepDeg ?? 0) >= 0.1);

export interface ResolvedArc {
  radiusMm: number;
  radiusM: number;
  sweepRad: number;
  direction: 1 | -1;
  arcLengthMm: number;
  arcLengthM: number;
}

// Resolve an arc's render parameters from the stored (radius, sweep) — the arc's true geometry. The
// sweep magnitude is used DIRECTLY (so the rendered arc is exactly what was committed) and its sign
// carries the bulge direction; the developed arc length is derived (= radius·sweep). The chord
// (lengthMm) is held by the handles, not needed here.
export const resolveArc = (radiusMm: number, sweepDeg: number): ResolvedArc => {
  const radius = Math.max(1, radiusMm);
  const sweepRad = Math.min(MAX_SWEEP_RAD, Math.max(0, (Math.abs(sweepDeg) * Math.PI) / 180));
  const direction: 1 | -1 = sweepDeg < 0 ? -1 : 1;
  const arcLengthMm = radius * sweepRad;
  return {
    // WHY: NOT rounded. radiusM and arcLengthMm are exact, so rounding this one field made the
    // consumers that measure with radiusMm (end/apex probes, snap targets) disagree with the band
    // drawn at radiusM by up to half a millimetre times the sweep — a split-brain in the resolver
    // itself. Round at the display site, not in the geometry.
    radiusMm: radius,
    radiusM: radius / 1000,
    sweepRad,
    direction,
    arcLengthMm,
    arcLengthM: arcLengthMm / 1000,
  };
};

// Recover the CHORD (span between the fixed ends) from the stored radius+sweep: chord =
// 2·radius·sin(sweep/2). Doubles as an idempotent migration of lengthMm to the chord (it held the
// developed arc length under the old arc-length model). Falls back when there is no arc (a straight
// run's lengthMm already IS its chord). keepWithinMm keeps the STORED value when it already agrees
// within rounding — the radius is persisted as an integer, so re-deriving on every refetch would
// clobber an exactly field-measured chord (3000 → 2999/3001) while healing nothing.
export const chordFromRadiusSweep = (
  fallbackMm: number,
  radiusMm: number | null | undefined,
  sweepDeg: number | null | undefined,
  keepWithinMm = 0,
): number => {
  if (!radiusMm || radiusMm <= 0 || !sweepDeg) return fallbackMm;
  const sweepRad = Math.min(MAX_SWEEP_RAD, (Math.abs(sweepDeg) * Math.PI) / 180);
  const chord = Math.round(2 * radiusMm * Math.sin(sweepRad / 2));
  if (chord <= 0) return fallbackMm;
  return Math.abs(chord - fallbackMm) <= keepWithinMm ? fallbackMm : chord;
};

// The DEVELOPED length (physical glass span, radius·sweep) of a body — panel widths divide THIS,
// not the chord: on a curved run every pane's real glass is longer than its chord share (×1.11 at
// 90°, ×1.57 at 180°). Falls back to the given straight length when there is no real arc.
export const developedLengthMm = (
  fallbackMm: number,
  radiusMm?: number | null,
  sweepDeg?: number | null,
): number =>
  isRealArc(radiusMm, sweepDeg)
    ? Math.round(resolveArc(radiusMm ?? 0, sweepDeg ?? 1).arcLengthMm)
    : fallbackMm;

export interface ArcEndLocal {
  xMm: number;
  yMm: number;
  tangentDeg: number;
}

// Endpoint of the arc relative to its start, in the pre-rotation plan frame, from the stored
// (radius, sweep). The chord from the origin to this point is 2·radius·sin(sweep/2) = lengthMm.
export const arcEndLocal = (radiusMm: number, sweepDeg: number): ArcEndLocal => {
  const direction = sweepDeg < 0 ? -1 : 1;
  const radius = Math.max(0.001, radiusMm);
  const sweepRad = Math.min((Math.abs(sweepDeg) * Math.PI) / 180, MAX_SWEEP_RAD);
  return {
    xMm: radius * Math.sin(sweepRad),
    yMm: direction * radius * (1 - Math.cos(sweepRad)),
    tangentDeg: direction * sweepRad * (180 / Math.PI),
  };
};

// Clamp a stored radius up to the chord-invariant floor (a half-circle, chord/2) — defensive for the
// 2D/plan renderers that read geomArcRadiusMm directly.
export const effectiveArcRadiusMm = (chordMm: number, radiusMm: number) =>
  Math.max(minArcRadiusMm(chordMm), radiusMm);

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

// Set the curve by RADIUS while keeping the CHORD fixed. The minor sweep = 2·asin(chord/2r); the
// radius can't be tighter than a half-circle (chord/2), so it's clamped up to that floor. Sweeps
// past 180° aren't reachable by radius alone (the radius is ambiguous between the minor and major
// arc) — use deriveArcFromSweep for those.
export const deriveArcFromRadius = (chordMm: number, radiusMm: number): ArcDerived => {
  const radius = Math.max(minArcRadiusMm(chordMm), radiusMm);
  const sweepRad = 2 * Math.asin(Math.min(1, chordMm / (2 * radius)));
  return {
    radiusMm: Math.round(radius),
    sweepDeg: (sweepRad * 180) / Math.PI,
    chordMm: Math.round(chordMm),
    sagittaMm: Math.round(radius - Math.sqrt(Math.max(0, radius * radius - (chordMm / 2) ** 2))),
    arcLengthMm: Math.round(radius * sweepRad),
  };
};

// Set the curve by SWEEP angle (1–359°) while keeping the CHORD fixed. radius = chord/(2·sin(sweep/2))
// — valid for BOTH a minor (≤180°) and a major (>180°) arc spanning the same two fixed endpoints, so
// the curve can go as deep as the user wants without the ends moving.
export const deriveArcFromSweep = (chordMm: number, sweepDeg: number): ArcDerived => {
  const sign = sweepDeg < 0 ? -1 : 1;
  const clampedDeg = Math.min(359, Math.max(1, Math.abs(sweepDeg)));
  const sweepRad = (clampedDeg * Math.PI) / 180;
  const radius = chordMm / (2 * Math.sin(sweepRad / 2));
  return {
    radiusMm: Math.round(radius),
    sweepDeg: sign * clampedDeg,
    chordMm: Math.round(chordMm),
    sagittaMm: Math.round(bowFromArc(chordMm, radius, sign * clampedDeg)),
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
  const straight: BowArc = {
    geomArcRadiusMm: null,
    geomArcSweepDeg: null,
    rotationDeg: Math.round(chordDeg * 100) / 100,
    lengthMm: Math.round(chordMm),
    arcLengthMm: Math.round(chordMm),
  };
  if (Math.abs(sagittaMm) < straightenMm) return straight;
  const dir = sagittaMm >= 0 ? -1 : 1;
  const d = deriveArcFromChordSagitta(chordMm, Math.abs(sagittaMm));
  // WHY: quantize the sweep FIRST, then derive radius + rotation roll from the QUANTIZED value —
  // rolling with the unrounded sweep while storing the rounded one made repeated bow commits
  // re-measure a drifted chord (tens of mm per shallow commit near straight).
  const sweepStoredDeg = Math.round(d.sweepDeg * 10) / 10;
  if (sweepStoredDeg < 0.5) return straight;
  const sweepStoredRad = Math.min(MAX_SWEEP_RAD, (sweepStoredDeg * Math.PI) / 180);
  const radius = chordMm / (2 * Math.sin(sweepStoredRad / 2));
  return {
    geomArcRadiusMm: Math.round(radius),
    geomArcSweepDeg: dir * sweepStoredDeg,
    rotationDeg: Math.round((chordDeg - dir * (sweepStoredDeg / 2)) * 100) / 100,
    lengthMm: Math.round(chordMm),
    arcLengthMm: Math.round(radius * sweepStoredRad),
  };
};

// Corner/end-handle commit (CHORD-INVARIANT). Dragging an end changes the CHORD (the span). Keep the
// sweep angle (the curl shape) and re-derive the radius for the new chord; lengthMm = the new chord.
export const arcFromCornerResize = (
  chordMm: number,
  sweepDeg: number,
): { lengthMm: number; geomArcRadiusMm: number } => {
  const sweepRad = Math.min(MAX_SWEEP_RAD, Math.max(0.0001, (Math.abs(sweepDeg) * Math.PI) / 180));
  const radius = chordMm / (2 * Math.sin(sweepRad / 2));
  return { lengthMm: Math.round(chordMm), geomArcRadiusMm: Math.round(radius) };
};

// Read-time radius from the AUTHORITATIVE chord + stored sweep: the persisted radius is integer-
// rounded (and legacy rows carry drifted values), so consumers that must render the chord at
// exactly lengthMm re-derive it. Falls back to the stored radius when the pair is not a real arc.
export const radiusFromChordSweep = (
  chordMm: number,
  radiusMm?: number | null,
  sweepDeg?: number | null,
): number => {
  if (!isRealArc(radiusMm, sweepDeg)) return radiusMm ?? 0;
  const sweepRad = Math.min(MAX_SWEEP_RAD, (Math.abs(sweepDeg ?? 0) * Math.PI) / 180);
  const sin = Math.sin(sweepRad / 2);
  if (sin < 1e-6 || chordMm <= 0) return radiusMm ?? 0;
  return chordMm / (2 * sin);
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
