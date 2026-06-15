import type { WizardEnclosureCategory } from './enclosure.types';

export interface EnclosurePresetEntry {
  category: WizardEnclosureCategory;
  iconKey: string;
  i18nKey: string;
  imageUrl?: string;
}

export const ENCLOSURE_PRESET_CATALOG: ReadonlyArray<EnclosurePresetEntry> = [
  {
    category: 'Balcony',
    iconKey: 'Balcony',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.Balcony',
  },
  {
    category: 'Greenhouse',
    iconKey: 'Sun',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.Greenhouse',
  },
  {
    category: 'ShowerCabin',
    iconKey: 'Droplet',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.ShowerCabin',
  },
  {
    category: 'Balustrade',
    iconKey: 'Fence',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.Balustrade',
  },
  {
    category: 'FramelessDoor',
    iconKey: 'DoorOpen',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.FramelessDoor',
  },
  {
    category: 'CurtainWall',
    iconKey: 'Building',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.CurtainWall',
  },
  {
    category: 'SpiderFacade',
    iconKey: 'Anchor',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.SpiderFacade',
  },
  {
    category: 'FreeForm',
    iconKey: 'Pencil',
    i18nKey: 'GlassEnclosure.NewProjectWizard.Preset.FreeForm',
  },
];
