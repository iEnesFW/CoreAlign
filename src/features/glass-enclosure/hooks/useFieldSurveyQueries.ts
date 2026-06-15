import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { glassFieldSurveysApi } from '../api/glassFieldSurveysApi';
import { glassProjectKeys } from './projectKeys';

const surveyKeys = {
  byProject: (projectId: string | null) =>
    ['glass-field-surveys', 'by-project', projectId] as const,
  detail: (id: string | null) => ['glass-field-surveys', 'detail', id] as const,
};

export const fieldSurveyKeys = surveyKeys;

const invalidate = (qc: ReturnType<typeof useQueryClient>, projectId: string | null) => {
  if (projectId) {
    qc.invalidateQueries({ queryKey: surveyKeys.byProject(projectId) });
    qc.invalidateQueries({ queryKey: glassProjectKeys.detail(projectId) });
  }
};

export const useFieldSurveysByProjectQuery = (projectId: string | null) =>
  useQuery({
    queryKey: surveyKeys.byProject(projectId),
    queryFn: () => glassFieldSurveysApi.listByProject(projectId as string),
    enabled: projectId !== null,
  });

export const useFieldSurveyQuery = (id: string | null) =>
  useQuery({
    queryKey: surveyKeys.detail(id),
    queryFn: () => glassFieldSurveysApi.getById(id as string),
    enabled: id !== null,
  });

export const useCreateFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: glassFieldSurveysApi.create,
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useUpdateFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      input,
    }: {
      id: string;
      input: Parameters<typeof glassFieldSurveysApi.update>[1];
      projectId: string;
    }) => glassFieldSurveysApi.update(id, input),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useSubmitFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id }: { id: string; projectId: string }) => glassFieldSurveysApi.submit(id),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useApproveFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      applyToProject,
    }: {
      id: string;
      applyToProject: boolean;
      projectId: string;
    }) => glassFieldSurveysApi.approve(id, applyToProject),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useRejectFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string | null; projectId: string }) =>
      glassFieldSurveysApi.reject(id, reason),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useApplyFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id }: { id: string; projectId: string }) => glassFieldSurveysApi.apply(id),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useDeleteFieldSurveyMutation = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id }: { id: string; projectId: string }) => glassFieldSurveysApi.remove(id),
    onSuccess: (_, vars) => invalidate(qc, vars.projectId),
  });
};

export const useUploadSurveyPhotoMutation = () =>
  useMutation({
    mutationFn: ({ surveyId, file }: { surveyId: string; file: File }) =>
      glassFieldSurveysApi.uploadPhoto(surveyId, file),
  });
