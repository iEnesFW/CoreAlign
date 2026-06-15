import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import type { ApiResponse } from '@/shared/types/api';
import type { GlassProjectDto } from '../../model/project.types';
import type {
  BackendEnclosureCategory,
  CreateGlassProjectInput,
  CreateProjectFromTemplateInput,
  EnclosurePresetDto,
  ProjectTemplateSummaryDto,
  WizardEnclosureCategory,
} from '../model/enclosure.types';
import { WIZARD_TO_BACKEND_CATEGORY_MAP } from '../model/enclosure.types';
import type { ProjectMeta, QuickDimensionsInput, QuickRunDimensions } from '../model/wizardStore';

const toBackendCategory = (
  category: WizardEnclosureCategory | null,
): BackendEnclosureCategory | undefined =>
  category ? WIZARD_TO_BACKEND_CATEGORY_MAP[category] : undefined;

export interface CreateProjectInput {
  category: WizardEnclosureCategory;
  templateId: string | null;
  meta: ProjectMeta;
  quickDims: QuickDimensionsInput;
  runLabelPrefix?: string;
}

export interface CreateProjectResult {
  projectId: string;
  project: GlassProjectDto;
}

const DEFAULT_CURRENCY = 'TRY';

const ensureMeta = (meta: ProjectMeta) => {
  if (!meta.customerId) {
    throw new Error('GlassEnclosure.NewProjectWizard.Validation.CustomerRequired');
  }
  const projectName = meta.name.trim();
  if (!projectName) {
    throw new Error('GlassEnclosure.NewProjectWizard.Validation.ProjectNameRequired');
  }
  return { customerId: meta.customerId, projectName };
};

const buildDirectCreatePayload = (input: CreateProjectInput): CreateGlassProjectInput => {
  const { customerId, projectName } = ensureMeta(input.meta);
  return {
    customerId,
    projectName,
    siteAddressLine1: input.meta.addressText.trim() || null,
    notes: input.meta.notes.trim() || null,
    currency: DEFAULT_CURRENCY,
  };
};

const buildFromTemplatePayload = (
  input: CreateProjectInput,
): Omit<CreateProjectFromTemplateInput, 'templateId'> => {
  const { customerId, projectName } = ensureMeta(input.meta);
  return {
    customerId,
    projectName,
    currency: DEFAULT_CURRENCY,
  };
};

export const wizardApi = {
  listPresets: () =>
    apiClient
      .get<ApiResponse<EnclosurePresetDto[]>>('/glass-enclosure/projects/presets')
      .then((r) => r.data),

  listTemplates: (category: WizardEnclosureCategory | null) => {
    const backendCategory = toBackendCategory(category);
    return apiClient
      .get<ApiResponse<ProjectTemplateSummaryDto[]>>('/glass-enclosure/projects/templates', {
        params: backendCategory ? { category: backendCategory } : undefined,
      })
      .then((r) => (r.data.isSuccess && r.data.data ? r.data.data : []));
  },

  createProject: async (input: CreateProjectInput) => {
    const post = input.templateId
      ? apiClient.post<ApiResponse<GlassProjectDto>>(
          `/glass-enclosure/projects/from-template/${input.templateId}`,
          buildFromTemplatePayload(input),
        )
      : apiClient.post<ApiResponse<GlassProjectDto>>(
          '/glass-enclosure/projects',
          buildDirectCreatePayload(input),
        );

    const [envelope, error] = await safeRequest(post.then((r) => r.data));

    if (error || !envelope?.data) {
      return [null, error ?? new Error('GlassEnclosure.NewProjectWizard.Create.Error')] as const;
    }
    const result: CreateProjectResult = {
      projectId: envelope.data.id,
      project: envelope.data,
    };
    return [result, null] as const;
  },

  addRun: (
    projectId: string,
    run: QuickRunDimensions,
    label: string,
    placement: { originX: number; originY: number; rotationDeg: number },
  ) =>
    safeRequest(
      apiClient
        .post<ApiResponse<{ id: string }>>(`/glass-enclosure/projects/${projectId}/runs`, {
          lengthMm: run.widthMm,
          heightMm: run.heightMm,
          panelCount: run.panelCount,
          label,
          originX: placement.originX,
          originY: placement.originY,
          rotationDeg: placement.rotationDeg,
        })
        .then((r) => r.data),
    ),
};
