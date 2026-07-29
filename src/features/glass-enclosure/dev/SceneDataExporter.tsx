import { useEffect } from 'react';
import { useDesignerStore } from '../model/designerStore';
import { isRealArc, radiusFromChordSweep, resolveArc } from '../model/arcGeometry';

// WHY: dev/E2E-only bridge that exposes the AUTHORITATIVE designer scene (the zustand store's exact
// authored geometry — mm-precise radius/sweep/rotation/shape) on window for headless geometric-
// invariant checks and the agent screenshot verification loop. Deliberately NOT a three.js Box3
// readback (that loses arc/shape/rotation semantics to an axis-aligned world box). Never active in
// production: the hook is installed only under import.meta.env.DEV or an explicit window.__E2E__ flag.

interface CadSceneWindow {
  __E2E__?: boolean;
  __CAD_SCENE__?: () => unknown;
  __CAD_STORE__?: () => unknown;
}

const isEnabled = (): boolean => {
  if (import.meta.env.DEV) return true;
  return typeof window !== 'undefined' && Boolean((window as CadSceneWindow).__E2E__);
};

export function SceneDataExporter() {
  useEffect(() => {
    if (!isEnabled() || typeof window === 'undefined') return;
    const target = window as CadSceneWindow;
    target.__CAD_SCENE__ = () => {
      const state = useDesignerStore.getState();
      const scene = state.scene;
      const runs = scene.runs ?? [];
      const walls = scene.walls ?? [];
      return {
        schemaVersion: scene.metadata?.schemaVersion ?? null,
        projectId: state.projectId,
        quality: state.quality,
        runCount: runs.length,
        wallCount: walls.length,
        // Full authored geometry — consumers read exactly what they assert on.
        scene,
        derived: {
          // Resolved arc math per curved run (radius/sweep/arcLength/direction) so an invariant
          // check can compare the render against the same numbers the geometry tests use.
          arcs: runs
            .filter((run) => isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg))
            .map((run) => ({
              runId: run.id,
              ...resolveArc(
                radiusFromChordSweep(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg),
                run.geomArcSweepDeg ?? 1,
              ),
            })),
        },
      };
    };
    // WHY a WRITE handle next to the read one: the designer's real defects live in the interaction
    // and geometry layers (where a body settles, whether a move is blocked, what a carve does to a
    // wall), and none of those are reachable from a unit test or from reading the scene. Driving
    // the STORE ACTIONS the UI calls exercises exactly that code and is re-runnable, so the same
    // matrix can be replayed after a fix. Same DEV/__E2E__ gate as the reader — never in production.
    target.__CAD_STORE__ = () => useDesignerStore.getState();
    return () => {
      delete target.__CAD_SCENE__;
      delete target.__CAD_STORE__;
    };
  }, []);

  return null;
}
