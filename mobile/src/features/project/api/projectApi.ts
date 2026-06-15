import { apiClient } from '@/api/apiClient';

export type GlassRunStatus = 'Planned' | 'Cutting' | 'Ready' | 'Installing' | 'Installed';

export interface GlassRun {
  id: string;
  code: string;
  panelCount: number;
  totalWidthMm: number;
  totalHeightMm: number;
  thicknessMm: number;
  status: GlassRunStatus;
}

export interface ProjectPanel {
  id: string;
  code: string;
  runId: string;
  widthMm: number;
  heightMm: number;
  thicknessMm: number;
  notes: string | null;
}

export interface ProjectDimensionSummary {
  totalGlassArea: number;
  totalPanelCount: number;
  totalRunCount: number;
  largestPanelMm: { width: number; height: number };
}

export interface ProjectSitePhoto {
  id: string;
  url: string;
  caption: string | null;
  takenAt: string;
}

export interface ProjectDetail {
  id: string;
  code: string;
  customerName: string;
  siteAddress: string;
  status: string;
  scheduledStart: string | null;
  scheduledEnd: string | null;
  planImageUrl: string | null;
  runs: GlassRun[];
  panels: ProjectPanel[];
  dimensionSummary: ProjectDimensionSummary;
  sitePhotos: ProjectSitePhoto[];
}

export const projectApi = {
  async getById(id: string): Promise<ProjectDetail> {
    const { data } = await apiClient.get<ProjectDetail>(`/api/v1/projects/${id}`);
    return data;
  },
};
