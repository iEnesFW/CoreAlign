import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { settingsApi } from '../api/settingsApi';
import type {
  CreateEmailTemplateRequest,
  SettingUpsertItem,
  UpdateCompanyProfileRequest,
  UpdateEmailTemplateRequest,
} from '../model/settings.types';

export {
  DOCUMENT_SEQUENCES_KEY,
  useConfigureDocumentSequence,
  useDocumentSequencesQuery,
} from '@/shared/document-sequences';

const COMPANY_KEY = ['settings', 'company'] as const;
const PARAMS_KEY = (category?: string) => ['settings', 'parameters', category ?? 'all'] as const;
const TEMPLATES_KEY = ['settings', 'email-templates'] as const;

export const NUMBER_FORMAT_CATEGORY = 'NumberFormat';
export const DECIMAL_PLACES_KEY = 'DecimalPlaces';
export const DEFAULT_DECIMAL_PLACES = 2;
const MAX_DECIMAL_PLACES = 6;

const invalidate = (qc: ReturnType<typeof useQueryClient>) => {
  qc.invalidateQueries({ queryKey: ['settings'] });
};

export const useCompanyProfileQuery = () =>
  useQuery({
    queryKey: COMPANY_KEY,
    queryFn: () => settingsApi.getCompany(),
    staleTime: 5 * 60 * 1000,
  });

export const useUpdateCompanyProfile = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateCompanyProfileRequest) => settingsApi.updateCompany(request),
    onSuccess: () => invalidate(qc),
  });
};

export const useParametersQuery = (category?: string) =>
  useQuery({
    queryKey: PARAMS_KEY(category),
    queryFn: () => settingsApi.getParameters(category),
    staleTime: 60 * 1000,
  });

export const useUpsertParameters = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (items: SettingUpsertItem[]) => settingsApi.upsertParameters(items),
    onSuccess: () => invalidate(qc),
  });
};

export const useDecimalPlaces = (): number => {
  const query = useParametersQuery(NUMBER_FORMAT_CATEGORY);
  const raw = query.data?.data?.find((s) => s.key === DECIMAL_PLACES_KEY)?.value;
  const parsed = raw ? Number.parseInt(raw, 10) : NaN;
  if (Number.isNaN(parsed)) return DEFAULT_DECIMAL_PLACES;
  return Math.min(Math.max(parsed, 0), MAX_DECIMAL_PLACES);
};

export const useDeleteParameter = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ category, key }: { category: string; key: string }) =>
      settingsApi.deleteParameter(category, key),
    onSuccess: () => invalidate(qc),
  });
};

export const useEmailTemplatesQuery = () =>
  useQuery({
    queryKey: TEMPLATES_KEY,
    queryFn: () => settingsApi.getEmailTemplates(),
    staleTime: 5 * 60 * 1000,
  });

export const useCreateEmailTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: CreateEmailTemplateRequest) => settingsApi.createEmailTemplate(req),
    onSuccess: () => invalidate(qc),
  });
};

export const useUpdateEmailTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (req: UpdateEmailTemplateRequest) => settingsApi.updateEmailTemplate(req),
    onSuccess: () => invalidate(qc),
  });
};

export const useDeleteEmailTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => settingsApi.deleteEmailTemplate(id),
    onSuccess: () => invalidate(qc),
  });
};
