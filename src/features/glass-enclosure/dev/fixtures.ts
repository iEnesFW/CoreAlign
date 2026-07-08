import type { SceneState } from '../model/project.types';

// WHY: deterministic, reproducible fixture scenes for the dev visual-verification playground. The
// catalog ids (profile system / glass type / color) are injected from the REAL catalogs fetched
// after the dev auto-login, so the glass renders exactly like the normal designer (mock catalog ids
// did not resolve to a renderable material). Fixed geometry + fixed run/panel ids keep captures and
// window.__CAD_SCENE__() reproducible.

export interface FixtureCatalogIds {
  profileSystemId: string;
  glassTypeId: string;
  colorId: string | null;
}

const base = (): Pick<
  SceneState,
  'connections' | 'walls' | 'slabs' | 'surfaces' | 'camera' | 'metadata'
> => ({
  connections: [],
  walls: [],
  slabs: [],
  surfaces: [],
  camera: null,
  metadata: { schemaVersion: 3, savedAt: '2026-01-01T00:00:00.000Z' },
});

// WHY: arc-bent run with one triangle-polygon pane — reproduces the arc shaped hole-fill so the
// silhouette frame (buildCurvedShapedFrameGeometry) can be visually verified.
const arcHolefillTriangle = (ids: FixtureCatalogIds): SceneState => ({
  ...base(),
  runs: [
    {
      id: 'fixture-run-arc-tri',
      orderIndex: 0,
      label: 'Arc hole-fill triangle',
      lengthMm: 2052,
      heightMm: 2200,
      originX: 0,
      originY: 0,
      rotationDeg: 0,
      profileSystemId: ids.profileSystemId,
      colorId: ids.colorId,
      hasTopDrip: true,
      hasBottomThreshold: false,
      geomArcRadiusMm: 3000,
      geomArcSweepDeg: 40,
      arcGlassBent: true,
      panels: [
        {
          id: 'fixture-panel-tri',
          panelIndex: 0,
          widthMm: 2052,
          openingType: 'Fixed',
          glassTypeId: ids.glassTypeId,
          hasHandle: false,
          hasLock: false,
          hasBrushSeal: false,
          hardware: [],
          heightMm: 2200,
          shapeKind: 'polygon',
          shapePointsJson: JSON.stringify([
            { x: -1026, y: 0 },
            { x: 1026, y: 0 },
            { x: 0, y: 2200 },
          ]),
        },
      ],
    },
  ],
});

// WHY: baseline sanity render — three rectangular panes; the middle pane carries a handle + lock so
// hardware rendering is visible too.
const straightRun = (ids: FixtureCatalogIds): SceneState => ({
  ...base(),
  runs: [
    {
      id: 'fixture-run-straight',
      orderIndex: 0,
      label: 'Straight run',
      lengthMm: 3000,
      heightMm: 2200,
      originX: 0,
      originY: 0,
      rotationDeg: 0,
      profileSystemId: ids.profileSystemId,
      colorId: ids.colorId,
      hasTopDrip: true,
      hasBottomThreshold: false,
      panels: [0, 1, 2].map((i) => ({
        id: `fixture-panel-straight-${i}`,
        panelIndex: i,
        widthMm: 1000,
        openingType: 'Fixed' as const,
        glassTypeId: ids.glassTypeId,
        hasHandle: i === 1,
        hasLock: i === 1,
        hasBrushSeal: false,
        hardware: [],
      })),
    },
  ],
});

const BUILDERS: Record<string, (ids: FixtureCatalogIds) => SceneState> = {
  'arc-holefill-triangle': arcHolefillTriangle,
  'straight-run': straightRun,
};

export const FIXTURE_KEYS = Object.keys(BUILDERS);
export const DEFAULT_FIXTURE = 'arc-holefill-triangle';

export const buildFixtureScene = (key: string, ids: FixtureCatalogIds): SceneState =>
  (BUILDERS[key] ?? BUILDERS[DEFAULT_FIXTURE])(ids);
