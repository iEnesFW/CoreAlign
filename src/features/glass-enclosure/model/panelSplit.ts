import { MIN_PANEL_MM } from './panelResize';
import type { SceneHardwareItem, ScenePanelState } from './project.types';

const clampHalf = (value: number, half: number) => Math.max(-half, Math.min(half, value));

// WHY: offsetXmm is centre-relative (absX = panelCentre + offsetXmm, ±width/2); each split half gets a new centre, so hardware must be re-homed and re-expressed against it.
const partitionHardware = (
  hardware: SceneHardwareItem[],
  panelWidthMm: number,
  leftWidthMm: number,
): { left: SceneHardwareItem[]; right: SceneHardwareItem[] } => {
  const rightWidthMm = panelWidthMm - leftWidthMm;
  const leftHalf = leftWidthMm / 2;
  const rightHalf = rightWidthMm / 2;
  const left: SceneHardwareItem[] = [];
  const right: SceneHardwareItem[] = [];
  for (const hw of hardware) {
    const distanceFromLeftEdge = hw.offsetXmm + panelWidthMm / 2;
    if (distanceFromLeftEdge <= leftWidthMm) {
      left.push({ ...hw, offsetXmm: clampHalf(distanceFromLeftEdge - leftHalf, leftHalf) });
    } else {
      right.push({
        ...hw,
        offsetXmm: clampHalf(distanceFromLeftEdge - leftWidthMm - rightHalf, rightHalf),
      });
    }
  }
  return { left, right };
};

// Split the panel spanning cutMm into two (left keeps the original id, right is new), reindexing the
// run and partitioning hardware to the correct half. Returns null when the cut leaves less than
// MIN_PANEL_MM on a side. Pure: the new id is injected so it is deterministic under test.
export const splitPanelsAtLength = (
  panels: ScenePanelState[],
  cutMm: number,
  makeId: () => string,
): ScenePanelState[] | null => {
  let acc = 0;
  for (let i = 0; i < panels.length; i += 1) {
    const panel = panels[i];
    const start = acc;
    const end = acc + panel.widthMm;
    if (cutMm >= start + MIN_PANEL_MM && cutMm <= end - MIN_PANEL_MM) {
      const leftWidth = Math.round(cutMm - start);
      const rightWidth = panel.widthMm - leftWidth;
      const { left: leftHardware, right: rightHardware } = partitionHardware(
        panel.hardware,
        panel.widthMm,
        leftWidth,
      );
      const left: ScenePanelState = { ...panel, widthMm: leftWidth, hardware: leftHardware };
      const right: ScenePanelState = {
        ...panel,
        id: makeId(),
        widthMm: rightWidth,
        hardware: rightHardware,
      };
      return [...panels.slice(0, i), left, right, ...panels.slice(i + 1)].map((p, idx) => ({
        ...p,
        panelIndex: idx,
      }));
    }
    acc = end;
  }
  return null;
};
