import { useQuery } from '@tanstack/react-query';
import { wizardApi } from '../api/wizardApi';
import type { ProjectTemplateSummaryDto, WizardEnclosureCategory } from '../model/enclosure.types';

export const wizardTemplateKeys = {
  all: ['glass-enclosure', 'wizard', 'templates'] as const,
  byCategory: (category: WizardEnclosureCategory | null) =>
    [...wizardTemplateKeys.all, category] as const,
};

export const useSystemTemplatesQuery = (category: WizardEnclosureCategory | null) =>
  useQuery<ProjectTemplateSummaryDto[]>({
    queryKey: wizardTemplateKeys.byCategory(category),
    queryFn: () => wizardApi.listTemplates(category),
    enabled: Boolean(category),
    staleTime: 5 * 60 * 1000,
  });
