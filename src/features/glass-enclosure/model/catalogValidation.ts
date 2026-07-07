import type { GlassTypeDto, ProfileSystemDto } from './glassEnclosure.types';
import type { SceneRunState } from './project.types';

export const runViolatesCatalog = (
  run: SceneRunState,
  systemMap: Map<string, ProfileSystemDto>,
  glassMap: Map<string, GlassTypeDto>,
): boolean => {
  const sys = systemMap.get(run.profileSystemId);
  if (!sys) return false;
  const heightM = run.heightMm / 1000;
  const overHeight = sys.maxPanelHeightMm > 0 && run.heightMm > sys.maxPanelHeightMm;
  const overWidth =
    sys.maxPanelWidthMm > 0 && run.panels.some((p) => p.widthMm > sys.maxPanelWidthMm);
  const overWeight =
    sys.maxPanelWeightKg > 0 &&
    run.panels.some((p) => {
      const glass = glassMap.get(p.glassTypeId);
      if (!glass || glass.weightKgPerM2 <= 0) return false;
      return (p.widthMm / 1000) * heightM * glass.weightKgPerM2 > sys.maxPanelWeightKg;
    });
  // The brand→profile→glass→opening chain exists in the data model; enforce it: a panel's glass
  // thickness must be one the profile carries, and its opening must be one the profile supports.
  // An empty support list means the profile declares no constraint (do not flag).
  const unsupportedThickness =
    sys.supportedGlassThicknesses.length > 0 &&
    run.panels.some((p) => {
      const glass = glassMap.get(p.glassTypeId);
      return glass ? !sys.supportedGlassThicknesses.includes(glass.thicknessMm) : false;
    });
  const unsupportedOpening =
    sys.supportedOpenings.length > 0 &&
    run.panels.some((p) => !sys.supportedOpenings.includes(p.openingType));
  return overHeight || overWidth || overWeight || unsupportedThickness || unsupportedOpening;
};

export const countCatalogViolations = (
  runs: SceneRunState[],
  profileSystems: ProfileSystemDto[],
  glassTypes: GlassTypeDto[],
): number => {
  const systemMap = new Map(profileSystems.map((s) => [s.id, s]));
  const glassMap = new Map(glassTypes.map((g) => [g.id, g]));
  return runs.reduce(
    (count, run) => count + (runViolatesCatalog(run, systemMap, glassMap) ? 1 : 0),
    0,
  );
};
