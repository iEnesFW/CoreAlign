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

export const minArcRadiusMm = (lengthMm: number) => Math.ceil(lengthMm / Math.PI);

export const effectiveArcRadiusMm = (lengthMm: number, radiusMm: number) =>
  Math.max(radiusMm, minArcRadiusMm(lengthMm));

export interface ArcEndLocal {
  xMm: number;
  yMm: number;
  tangentDeg: number;
}

export const arcEndLocal = (lengthMm: number, radiusMm: number, sweepSign: number): ArcEndLocal => {
  const direction = sweepSign < 0 ? -1 : 1;
  const radius = effectiveArcRadiusMm(lengthMm, radiusMm);
  const sweepRad = Math.min(lengthMm / radius, MAX_SWEEP_RAD);
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
  lengthM: number,
  radiusM: number,
  sweepSign: number,
  panelWidthsM: number[],
): ArcLayout => {
  const direction: 1 | -1 = sweepSign < 0 ? -1 : 1;
  // Guard against zero/NaN radius which would otherwise feed Infinity/NaN
  // vertices into the geometry and silently produce a broken/invisible mesh.
  const safeRadiusM = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const sweepRad = Math.min(lengthM / safeRadiusM, MAX_SWEEP_RAD);
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

export const deriveArcFromRadius = (arcLengthMm: number, radiusMm: number): ArcDerived => {
  const radius = effectiveArcRadiusMm(arcLengthMm, radiusMm);
  const sweepRad = Math.min(arcLengthMm / radius, MAX_SWEEP_RAD);
  return {
    radiusMm: Math.round(radius),
    sweepDeg: (sweepRad * 180) / Math.PI,
    chordMm: Math.round(2 * radius * Math.sin(sweepRad / 2)),
    sagittaMm: Math.round(radius * (1 - Math.cos(sweepRad / 2))),
    arcLengthMm: Math.round(arcLengthMm),
  };
};

export const deriveArcFromSweep = (arcLengthMm: number, sweepDeg: number): ArcDerived => {
  const clampedDeg = Math.min(180, Math.max(1, Math.abs(sweepDeg)));
  const radius = arcLengthMm / ((clampedDeg * Math.PI) / 180);
  return deriveArcFromRadius(arcLengthMm, radius);
};

export const deriveArcFromChordSagitta = (chordMm: number, sagittaMm: number): ArcDerived => {
  // Guard a zero sagitta (a straight chord) which would divide by zero.
  const sagitta = Math.max(0.001, Math.abs(sagittaMm));
  const radius = sagitta / 2 + (chordMm * chordMm) / (8 * sagitta);
  const sweepRad = 2 * Math.asin(Math.min(1, chordMm / (2 * radius)));
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
