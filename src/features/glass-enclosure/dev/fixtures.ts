import type { ColorOptionDto, GlassTypeDto, ProfileSystemDto } from '../model/glassEnclosure.types';
import type { SceneState } from '../model/project.types';

// WHY: deterministic, backend-free fixtures for the dev visual-verification playground. Fixed ids
// (not random) so screenshots + window.__CAD_SCENE__() are reproducible across runs.

export const FIXTURE_PROFILE_ID = 'fixture-profile-standard';
export const FIXTURE_GLASS_ID = 'fixture-glass-8mm';
export const FIXTURE_COLOR_ID = 'fixture-color-anodized';

export const fixtureProfileSystems: ProfileSystemDto[] = [
  {
    id: FIXTURE_PROFILE_ID,
    code: 'FX-STD',
    name: 'Fixture Standard System',
    brandId: 'fixture-brand',
    brandName: 'Fixture',
    systemType: 'Fixed',
    maxPanelWidthMm: 3000,
    maxPanelHeightMm: 3000,
    maxPanelWeightKg: 200,
    supportedGlassThicknesses: [8, 10, 12],
    supportedOpenings: ['Fixed', 'SlidingLeft', 'SlidingRight', 'Folding', 'Hinged', 'Guillotine'],
    certificationClass: null,
    fireClass: null,
    thermalUValue: null,
    thermalBreakFactor: 1,
    description: null,
    isActive: true,
    items: [],
  },
];

export const fixtureGlassTypes: GlassTypeDto[] = [
  {
    id: FIXTURE_GLASS_ID,
    code: 'FX-8T',
    name: 'Fixture 8mm Tempered',
    thicknessMm: 8,
    structure: 'Tempered',
    glassLayers: [8],
    uValue: 5.8,
    soundDb: 32,
    maxPanelAreaM2: 6,
    allowablePressurePa: 1200,
    weightKgPerM2: 20,
    pricePerM2: 100,
    currency: 'USD',
    linkedProductId: null,
    isActive: true,
  },
];

export const fixtureColors: ColorOptionDto[] = [
  {
    id: FIXTURE_COLOR_ID,
    code: 'FX-ANO',
    name: 'Fixture Anodized',
    ralCode: null,
    hexColor: '#9aa5ad',
    finishType: 'Anodized',
    priceModifierPercent: 0,
    sortOrder: 0,
    isActive: true,
  },
];

const metadata = { schemaVersion: 3, savedAt: '2026-01-01T00:00:00.000Z' };

const emptySceneBase = (): Pick<
  SceneState,
  'connections' | 'walls' | 'slabs' | 'surfaces' | 'camera' | 'metadata'
> => ({
  connections: [],
  walls: [],
  slabs: [],
  surfaces: [],
  camera: null,
  metadata: { ...metadata },
});

// A single arc-bent run whose one pane is a triangle polygon — reproduces the arc shaped hole-fill
// result so the silhouette FRAME (buildCurvedShapedFrameGeometry) can be visually + numerically
// verified. radius 3000mm, sweep 40° → chord ≈ 2052mm; the triangle fills the 2052×2200 cell.
const arcHolefillTriangle: SceneState = {
  ...emptySceneBase(),
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
      profileSystemId: FIXTURE_PROFILE_ID,
      colorId: FIXTURE_COLOR_ID,
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
          glassTypeId: FIXTURE_GLASS_ID,
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
};

// A plain straight run with three rectangular panes — the baseline sanity render (no arc, no shape).
const straightRun: SceneState = {
  ...emptySceneBase(),
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
      profileSystemId: FIXTURE_PROFILE_ID,
      colorId: FIXTURE_COLOR_ID,
      hasTopDrip: true,
      hasBottomThreshold: false,
      panels: [0, 1, 2].map((i) => ({
        id: `fixture-panel-straight-${i}`,
        panelIndex: i,
        widthMm: 1000,
        openingType: 'Fixed' as const,
        glassTypeId: FIXTURE_GLASS_ID,
        hasHandle: false,
        hasLock: false,
        hasBrushSeal: false,
        hardware: [],
      })),
    },
  ],
};

export const FIXTURE_SCENES: Record<string, SceneState> = {
  'arc-holefill-triangle': arcHolefillTriangle,
  'straight-run': straightRun,
};

export const DEFAULT_FIXTURE = 'arc-holefill-triangle';
