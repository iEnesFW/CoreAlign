import { useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { CanvasPanel } from '../designer/panels';
import { useDesignerStore } from '../model/designerStore';
import { SceneDataExporter } from './SceneDataExporter';
import {
  DEFAULT_FIXTURE,
  FIXTURE_SCENES,
  fixtureColors,
  fixtureGlassTypes,
  fixtureProfileSystems,
} from './fixtures';

// WHY: dev-only visual-verification playground. Renders the REAL 3D designer canvas against a
// backend-free, deterministic fixture scene (injected straight into the store via applyScene) with
// mock catalogs — so the agent can screenshot the canvas and read window.__CAD_SCENE__() without a
// backend, DB, auth, or a seeded project. Registered only under import.meta.env.DEV (see App.tsx).

const noop = () => undefined;

export function GlassFixturePlayground() {
  const [params] = useSearchParams();
  const sceneKey = params.get('scene') ?? DEFAULT_FIXTURE;
  const applyScene = useDesignerStore((s) => s.applyScene);

  const scene = useMemo(
    () => FIXTURE_SCENES[sceneKey] ?? FIXTURE_SCENES[DEFAULT_FIXTURE],
    [sceneKey],
  );

  useEffect(() => {
    // Render exactly like the real designer (default quality + appearance) so the glass reads the
    // same way the user sees it — a real browser captures it fine.
    applyScene(scene);
  }, [applyScene, scene]);

  return (
    <div className="fixed inset-0 bg-white dark:bg-slate-950">
      <div
        data-testid="fixture-scene-key"
        data-scene={sceneKey}
        className="pointer-events-none absolute left-2 top-2 z-10 rounded bg-black/70 px-2 py-1 text-xs text-white"
      >
        fixture: {sceneKey}
      </div>
      <SceneDataExporter />
      <div className="h-full w-full">
        <CanvasPanel
          view="3d"
          profileSystems={fixtureProfileSystems}
          glassTypes={fixtureGlassTypes}
          colors={fixtureColors}
          onAddRunFromPlan={noop}
          onUpdateRunGeometry={noop}
          onSelectConnectionCandidate={noop}
        />
      </div>
    </div>
  );
}
