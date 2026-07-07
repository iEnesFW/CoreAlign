const clampMm = (value: number, limit: number) => Math.min(limit, Math.max(-limit, value));

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
