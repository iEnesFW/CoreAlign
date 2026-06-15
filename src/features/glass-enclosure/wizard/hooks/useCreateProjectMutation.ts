import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toastApiError, toastApiSuccess } from '@/shared/lib/mutationToast';
import { safeRequest } from '@/shared/lib/safeRequest';
import { glassProjectsApi } from '../../api/glassProjectsApi';
import { glassProjectKeys } from '../../hooks/projectKeys';
import { wizardApi, type CreateProjectInput, type CreateProjectResult } from '../api/wizardApi';

interface CreatedRunRef {
  id: string | null;
  turnDeg: number;
}

const toRadians = (deg: number) => (deg * Math.PI) / 180;

const createRunsAlongHeading = async (
  projectId: string,
  input: CreateProjectInput,
): Promise<CreatedRunRef[]> => {
  const prefix = input.runLabelPrefix ?? 'Run';
  const createdRuns: CreatedRunRef[] = [];
  let x = 0;
  let y = 0;
  let headingDeg = 0;
  for (const [i, run] of input.quickDims.runs.entries()) {
    const turnDeg = run.turnDeg ?? 0;
    headingDeg += turnDeg;
    const [envelope, runError] = await wizardApi.addRun(projectId, run, `${prefix} ${i + 1}`, {
      originX: Math.round(x),
      originY: Math.round(y),
      rotationDeg: headingDeg,
    });
    if (runError) throw runError;
    createdRuns.push({ id: envelope?.data?.id ?? null, turnDeg });
    x += run.widthMm * Math.cos(toRadians(headingDeg));
    y += run.widthMm * Math.sin(toRadians(headingDeg));
  }
  return createdRuns;
};

const createCornerConnections = async (projectId: string, runs: CreatedRunRef[]) => {
  for (let i = 1; i < runs.length; i += 1) {
    const turn = runs[i].turnDeg;
    const runAId = runs[i - 1].id;
    const runBId = runs[i].id;
    if (turn === 0 || !runAId || !runBId) continue;
    await safeRequest(
      glassProjectsApi.addConnection(projectId, {
        runAId,
        runBId,
        jointAngleDeg: Math.abs(turn),
        mitreCutDeg: Math.abs(turn) / 2,
        usesCornerPost: false,
        cornerProfileId: null,
      }),
    );
  }
};

export const useCreateProjectMutation = () => {
  const qc = useQueryClient();
  return useMutation<CreateProjectResult, Error, CreateProjectInput>({
    mutationFn: async (input) => {
      const [created, createError] = await wizardApi.createProject(input);
      if (createError || !created)
        throw createError ?? new Error('GlassEnclosure.NewProjectWizard.Create.Error');

      if (!input.quickDims.skipped && input.quickDims.runs.length > 0) {
        const createdRuns = await createRunsAlongHeading(created.projectId, input);
        await createCornerConnections(created.projectId, createdRuns);
      }

      return created;
    },
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: glassProjectKeys.lists() });
      qc.invalidateQueries({ queryKey: glassProjectKeys.detail(result.projectId) });
      toastApiSuccess('GlassEnclosure.NewProjectWizard.Create.Success');
    },
    onError: (error) => toastApiError(error, 'GlassEnclosure.NewProjectWizard.Create.Error'),
  });
};
