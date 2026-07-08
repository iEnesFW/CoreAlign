const clampMm = (value: number, limit: number) => Math.min(limit, Math.max(-limit, value));

// The glass pane is shorter than the run by the top+bottom profile (DEFAULT_PROFILE_CROSS_SECTION
// height, 60mm), or a per-panel heightMm override. Hardware Y-offsets must clamp to THIS, not the
// run height — otherwise a piece can sit ~30mm above/below the actual glass.
const FRAME_HEIGHT_MM = 60;
export const glassClampHeightMm = (
  panelHeightMm: number | null | undefined,
  runHeightMm: number,
): number => panelHeightMm ?? Math.max(1, runHeightMm - FRAME_HEIGHT_MM);

export interface HardwarePlacementInput {
  offsetXmm: number;
  offsetYmm: number;
  widthMm: number;
  heightMm: number;
}

export const clampHardwareOffsets = (
  panelWidthMm: number,
  runHeightMm: number,
  item: HardwarePlacementInput,
): { offsetXmm: number; offsetYmm: number } => {
  const edgeX = Math.max(0, panelWidthMm / 2 - item.widthMm / 2);
  const edgeY = Math.max(0, runHeightMm / 2 - item.heightMm / 2);
  return {
    offsetXmm: clampMm(item.offsetXmm, edgeX),
    offsetYmm: clampMm(item.offsetYmm, edgeY),
  };
};
