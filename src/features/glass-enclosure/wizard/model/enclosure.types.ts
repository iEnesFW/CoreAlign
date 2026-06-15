export type {
  EnclosureSubtype,
  GeometryMode,
  MountingTopology,
  ConnectorKind,
  EnclosurePresetDto,
  ProjectTemplateSummaryDto,
  ProjectTemplateDetailDto,
  ProjectTemplateRunPresetDto,
  CreateGlassProjectInput,
  CreateProjectFromTemplateInput,
} from '../../model/project.types';

import type { EnclosureCategory } from '../../model/project.types';

export type BackendEnclosureCategory = EnclosureCategory;

export type WizardEnclosureCategory =
  | 'Balcony'
  | 'Greenhouse'
  | 'ShowerCabin'
  | 'Balustrade'
  | 'FramelessDoor'
  | 'CurtainWall'
  | 'SpiderFacade'
  | 'FreeForm';

export const WIZARD_ENCLOSURE_CATEGORIES: ReadonlyArray<WizardEnclosureCategory> = [
  'Balcony',
  'Greenhouse',
  'ShowerCabin',
  'Balustrade',
  'FramelessDoor',
  'CurtainWall',
  'SpiderFacade',
  'FreeForm',
];

export const WIZARD_TO_BACKEND_CATEGORY_MAP: Record<
  WizardEnclosureCategory,
  BackendEnclosureCategory
> = {
  Balcony: 'Vertical',
  Greenhouse: 'HorizontalOrPitched',
  ShowerCabin: 'Functional',
  Balustrade: 'Functional',
  FramelessDoor: 'Special',
  CurtainWall: 'Vertical',
  SpiderFacade: 'Vertical',
  FreeForm: 'Special',
};
