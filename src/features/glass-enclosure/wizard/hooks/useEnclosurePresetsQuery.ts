import { useQuery } from '@tanstack/react-query';
import { wizardApi } from '../api/wizardApi';
import { ENCLOSURE_PRESET_CATALOG, type EnclosurePresetEntry } from '../model/presetCatalog';
import type { EnclosurePresetDto } from '../model/enclosure.types';

export const wizardPresetKeys = {
  all: ['glass-enclosure', 'wizard', 'presets'] as const,
};

export interface EnclosurePresetView {
  catalog: EnclosurePresetEntry;
  remote: EnclosurePresetDto | null;
}

const buildPresetViews = (remote: EnclosurePresetDto[]): EnclosurePresetView[] => {
  const lookup = new Map<string, EnclosurePresetDto>();
  for (const item of remote) {
    lookup.set(item.subtype, item);
  }
  return ENCLOSURE_PRESET_CATALOG.map((entry) => ({
    catalog: entry,
    remote: lookup.get(entry.category) ?? null,
  }));
};

export const useEnclosurePresetsQuery = () =>
  useQuery<EnclosurePresetView[]>({
    queryKey: wizardPresetKeys.all,
    queryFn: async () => {
      const response = await wizardApi.listPresets();
      const remote = response.isSuccess && response.data ? response.data : [];
      return buildPresetViews(remote);
    },
    staleTime: 10 * 60 * 1000,
    placeholderData: () => buildPresetViews([]),
  });
