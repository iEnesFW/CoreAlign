import { useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { CanvasPanel } from '../designer/panels';
import { useDesignerStore } from '../model/designerStore';
import { useViewerAppearance } from '../model/viewerAppearance';
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
  const setQuality = useDesignerStore((s) => s.setQuality);
  const { setPreset } = useViewerAppearance();

  const scene = useMemo(
    () => FIXTURE_SCENES[sceneKey] ?? FIXTURE_SCENES[DEFAULT_FIXTURE],
    [sceneKey],
  );

  useEffect(() => {
    // WHY: software WebGL (SwiftShader, headless capture) loses its context under shadows + an
    // Environment HDR fetch. The 'low' preset drops shadows/AA and 'plain' sets environment:'none',
    // so the deterministic-capture render stays lightweight and stable.
    setQuality('low');
    setPreset('plain');
    applyScene(scene);
  }, [applyScene, scene, setQuality, setPreset]);

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
