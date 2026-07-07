import { MIN_PANEL_MM } from './panelResize';
import type { ScenePanelState } from './project.types';

// Split the panel that spans cutMm into two (left keeps the original id + hardware, right is new),
// reindexing the whole run. Returns null when the cut does not land inside a panel with room for
// both halves (each ≥ MIN_PANEL_MM) — the caller then leaves the run untouched. Pure: the new id is
// injected so it is deterministic under test.
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
      const left: ScenePanelState = { ...panel, widthMm: leftWidth };
      const right: ScenePanelState = { ...panel, id: makeId(), widthMm: rightWidth, hardware: [] };
      return [...panels.slice(0, i), left, right, ...panels.slice(i + 1)].map((p, idx) => ({
        ...p,
        panelIndex: idx,
      }));
    }
    acc = end;
  }
  return null;
};
