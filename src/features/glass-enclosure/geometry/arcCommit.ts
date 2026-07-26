import {
  deriveArcFromChordSagitta,
  deriveArcFromRadius,
  deriveArcFromSweep,
  minArcRadiusMm,
} from '../model/arcGeometry';
import {
  STRAIGHT_SWEEP_EPSILON_DEG,
  chordDirectionDeg,
  isCurved,
  quantizeSweepDeg,
  rotationForChord,
} from './curvature';
import type { CurvablePose, PoseConvention } from './curvature';

export const MIN_SERVER_RADIUS_MM = 100;
export const DEFAULT_BOW_DEADZONE_MM = 25;
// The default curl a "curved" placement drops in. It is a starting shape, not a constraint — the
// bow handle and the inspector re-curve it freely.
export const PLACEMENT_SWEEP_DEG = 60;

export type ArcCommitInput =
  | { kind: 'radius'; radiusMm: number }
  | { kind: 'sweep'; sweepDeg: number }
  | { kind: 'chordSagitta'; chordMm: number; sagittaMm: number }
  | { kind: 'bow'; sagittaMm: number }
  | { kind: 'chordResize'; chordMm: number }
  | { kind: 'straighten' }
  | { kind: 'flip' };

export interface ArcCommitOptions {
  // Runs/walls roll rotationDeg to the start tangent; slabs keep the axis. See PoseConvention.
  pose?: PoseConvention;
  // A bow drag shallower than this snaps back to straight, so a nudge on a straight body does not
  // store a 0.6° phantom arc the user cannot see but every downstream consumer treats as curved.
  bowDeadzoneMm?: number;
  // Force which side the body bulges (sweep sign). Without it every mode except `bow` keeps the
  // side the body already has — which is what an edit like "make the radius 2 m" should do, but
  // not what a "curve left / curve right" button means.
  bulge?: 1 | -1;
}

export interface ArcCommitPatch {
  lengthMm?: number;
  rotationDeg: number;
  geomArcRadiusMm: number | null;
  geomArcSweepDeg: number | null;
}

export type ArcCommitRejection = 'radiusTooSmall' | 'invalidInput' | 'notCurved';

export interface ArcCommitResult {
  patch: ArcCommitPatch | null;
  rejection?: ArcCommitRejection;
  radiusMm?: number;
}

const straighten = (body: CurvablePose, pose: PoseConvention): ArcCommitResult => ({
  patch: {
    rotationDeg: rotationForChord(chordDirectionDeg(body, pose), null, pose),
    geomArcRadiusMm: null,
    geomArcSweepDeg: null,
  },
});

/**
 * Build a curvature patch that leaves BOTH ENDPOINTS EXACTLY WHERE THEY ARE.
 *
 * This is the single writer for `geomArcRadiusMm` / `geomArcSweepDeg`. Every entry point — the bow
 * handle, the end handle, the radius field, the sweep field, the field-survey chord+rise, straighten
 * and flip — must go through it, for runs, walls and slabs alike.
 *
 * WHY it exists: the inspector commits used to write a sweep WITHOUT re-rolling `rotationDeg`.
 * Since the renderer reads `rotationDeg` as the START TANGENT, changing the sweep by S rotated the
 * whole body by S/2 and swung its far end by metres — the reported "width, height, position, size,
 * everything changes". The maths was never wrong; the POSE was.
 *
 * Order matters: the sweep is quantised FIRST, then the radius and the rotation roll are derived
 * from the QUANTISED sweep. Deriving from the unrounded value and storing the rounded one makes
 * repeated commits re-measure a drifted chord.
 */
export const arcCommitKeepingEnds = (
  body: CurvablePose,
  input: ArcCommitInput,
  options: ArcCommitOptions = {},
): ArcCommitResult => {
  const pose = options.pose ?? 'rolled';
  const bowDeadzoneMm = options.bowDeadzoneMm ?? DEFAULT_BOW_DEADZONE_MM;
  const chordDeg = chordDirectionDeg(body, pose);
  const currentSweep = body.geomArcSweepDeg ?? 0;
  const sweepSign = currentSweep < 0 ? -1 : 1;

  if (input.kind === 'straighten') return straighten(body, pose);

  if (input.kind === 'flip') {
    if (!isCurved(body)) return { patch: null, rejection: 'notCurved' };
    // WHY: flipping must re-roll too. Negating the sweep alone leaves rotationDeg rolled for the
    // OLD direction, so the mirrored body pivots around its start instead of bulging the other way.
    const flipped = -currentSweep;
    return {
      patch: {
        rotationDeg: rotationForChord(chordDeg, flipped, pose),
        geomArcRadiusMm: body.geomArcRadiusMm ?? null,
        geomArcSweepDeg: flipped,
      },
    };
  }

  const chordMm =
    input.kind === 'chordSagitta' || input.kind === 'chordResize' ? input.chordMm : body.lengthMm;
  if (!(chordMm > 0)) return { patch: null, rejection: 'invalidInput' };

  let unsignedSweepDeg: number;
  switch (input.kind) {
    case 'radius': {
      if (!(input.radiusMm > 0)) return straighten(body, pose);
      const floored = Math.max(minArcRadiusMm(chordMm), input.radiusMm);
      unsignedSweepDeg = deriveArcFromRadius(chordMm, floored).sweepDeg;
      break;
    }
    case 'sweep': {
      if (!(input.sweepDeg > 0)) return { patch: null, rejection: 'invalidInput' };
      unsignedSweepDeg = deriveArcFromSweep(chordMm, input.sweepDeg).sweepDeg;
      break;
    }
    case 'chordSagitta': {
      if (!(input.sagittaMm > 0)) return { patch: null, rejection: 'invalidInput' };
      unsignedSweepDeg = deriveArcFromChordSagitta(chordMm, input.sagittaMm).sweepDeg;
      break;
    }
    case 'bow': {
      if (Math.abs(input.sagittaMm) < bowDeadzoneMm) return straighten(body, pose);
      unsignedSweepDeg = deriveArcFromChordSagitta(chordMm, Math.abs(input.sagittaMm)).sweepDeg;
      break;
    }
    case 'chordResize': {
      // Dragging an end changes the SPAN and keeps the curl ANGLE — the radius re-derives for the
      // new chord. A straight body just gets its new length.
      if (!isCurved(body)) {
        return {
          patch: {
            lengthMm: Math.round(chordMm),
            rotationDeg: rotationForChord(chordDeg, null, pose),
            geomArcRadiusMm: null,
            geomArcSweepDeg: null,
          },
        };
      }
      unsignedSweepDeg = Math.abs(currentSweep);
      break;
    }
  }

  const quantized = quantizeSweepDeg(unsignedSweepDeg);
  if (quantized < STRAIGHT_SWEEP_EPSILON_DEG) return straighten(body, pose);

  // Derive the radius from the QUANTISED sweep so the stored triple (chord, radius, sweep) is
  // self-consistent — otherwise every read re-derives a slightly different chord.
  const sweepRad = (quantized * Math.PI) / 180;
  const radiusMm = chordMm / (2 * Math.sin(sweepRad / 2));
  if (!Number.isFinite(radiusMm) || radiusMm <= 0)
    return { patch: null, rejection: 'invalidInput' };

  const rounded = Math.round(radiusMm);
  if (rounded < MIN_SERVER_RADIUS_MM) {
    return { patch: null, rejection: 'radiusTooSmall', radiusMm: rounded };
  }

  // The bow handle carries its own direction (which side the user dragged) and a caller may force
  // one ("curve left"); every other mode keeps whichever way the body already bulges.
  const direction =
    options.bulge ?? (input.kind === 'bow' ? (input.sagittaMm >= 0 ? -1 : 1) : sweepSign);
  const signedSweepDeg = direction * quantized;

  return {
    patch: {
      lengthMm: Math.round(chordMm),
      rotationDeg: rotationForChord(chordDeg, signedSweepDeg, pose),
      geomArcRadiusMm: rounded,
      geomArcSweepDeg: signedSweepDeg,
    },
    radiusMm: rounded,
  };
};

/**
 * The arc a freshly PLACED curved body is born with, derived the same way an edit would be so the
 * chord runs along the direction the user dragged and the far end lands where the ghost showed it.
 *
 * Returns null for a straight placement (and for a degenerate span), so callers can spread the
 * result straight into their draft.
 */
export const curvedPlacementArc = (
  chordMm: number,
  chordDeg: number,
  sweepDeg: number = PLACEMENT_SWEEP_DEG,
): ArcCommitPatch | null => {
  const { patch } = arcCommitKeepingEnds(
    { lengthMm: chordMm, rotationDeg: chordDeg, geomArcRadiusMm: null, geomArcSweepDeg: null },
    { kind: 'sweep', sweepDeg },
    { bulge: 1 },
  );
  return patch?.geomArcRadiusMm ? patch : null;
};
