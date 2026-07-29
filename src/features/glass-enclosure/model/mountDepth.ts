/**
 * How deep a glass assembly sits THROUGH its host wall — the third axis.
 *
 * WHY this exists: the glass that fills a wall hole matched the hole exactly in width and height,
 * yet visibly failed to seat. The reason was the axis nobody modelled. A carved opening runs the
 * FULL wall thickness (the shape's hole is punched before a single full-depth extrude), but every
 * solid put back into it was a fixed 50 mm section centred on the wall centreline. On a 200 mm wall
 * that leaves a **75 mm open reveal on each face** — the "small gap, doesn't sit in the opening".
 *
 * The model: the frame/profile fills the opening depth minus a thin, deliberate shadow gap on each
 * face, centred in the thickness. The GLASS PANE keeps its real thickness and stays centred — a
 * pane is thin; what fills a reveal in a real window is the frame, not the glass.
 */

// The deliberate setback per face: a hairline shadow that keeps the frame from z-fighting the wall
// surface it sits in, without reading as a gap. Was 10 mm, which users saw as the glass failing to
// meet the hole edge. Per-run overridable via `mountShadowGapMm` (0 = dead flush).
export const SHADOW_GAP_MM = 1;

// What a run that is NOT hosted by a wall keeps: today's free-standing profile section. A run
// standing in the open must not balloon to 180 mm just because a hosted one does.
export const FREE_STANDING_DEPTH_MM = 50;

// Below this a "wall" is thinner than the shadow gaps would consume; fill it edge to edge instead
// of returning a negative depth.
const MIN_MOUNT_DEPTH_MM = 10;

export interface MountDepthOverride {
  mountDepthMm?: number | null;
  mountOffsetMm?: number | null;
  mountShadowGapMm?: number | null;
}

export interface ResolvedMountDepth {
  /** Depth the frame/profile occupies along the wall normal, mm. */
  depthMm: number;
  /** Signed shift of the assembly from the wall's mid-thickness plane, mm. +Z is the front face. */
  offsetMm: number;
  /** Open reveal left at the front face, mm. */
  frontGapMm: number;
  /** Open reveal left at the back face, mm. */
  backGapMm: number;
  /** True when the depth came from a host wall rather than the free-standing default. */
  hosted: boolean;
}

/**
 * Resolve the mount depth for a run.
 *
 * @param hostThicknessMm the host wall's thickness, or null/undefined when the run stands free.
 * @param override optional per-run values (blob-only scene fields; absent for every existing run).
 */
export const resolveMountDepth = (
  hostThicknessMm: number | null | undefined,
  override?: MountDepthOverride,
): ResolvedMountDepth => {
  const hosted = typeof hostThicknessMm === 'number' && hostThicknessMm > 0;
  const thickness = hosted ? hostThicknessMm : FREE_STANDING_DEPTH_MM;
  const gap = Math.max(0, override?.mountShadowGapMm ?? (hosted ? SHADOW_GAP_MM : 0));

  const derived = Math.max(MIN_MOUNT_DEPTH_MM, thickness - 2 * gap);
  const depthMm = Math.max(MIN_MOUNT_DEPTH_MM, override?.mountDepthMm ?? derived);
  // Clamp the offset so the assembly can never poke out through either face — an override that
  // asks for more than the wall can hold is honoured up to the surface, not beyond it.
  const slack = Math.max(0, (thickness - depthMm) / 2);
  const requested = override?.mountOffsetMm ?? 0;
  const offsetMm = Math.max(-slack, Math.min(slack, requested));

  return {
    depthMm,
    offsetMm,
    frontGapMm: thickness / 2 - (offsetMm + depthMm / 2),
    backGapMm: thickness / 2 + (offsetMm - depthMm / 2),
    hosted,
  };
};

/**
 * The profile cross-section a run should draw: the catalogue face dimension across the run's own
 * plane, and the resolved mount depth through the wall.
 *
 * `Bar` reads `{ width, height }` as `{ Z extent, Y extent }` (boxGeometry [length, height, width]),
 * so `width` is the axis that must follow the wall thickness.
 */
export const mountedSection = (
  faceMm: number,
  mount: ResolvedMountDepth,
): { width: number; height: number } => ({ width: mount.depthMm, height: faceMm });
