import { apiClient } from '@/shared/api/apiClient';
import { safeRequest, type SafeResult } from '@/shared/lib/safeRequest';
import type {
  EnclosureCategory,
  EnclosureSubtype,
  GeometryMode,
  MountingTopology,
  ConnectorKind,
} from '@/features/glass-enclosure/model/project.types';

export type MarketplaceSortBy = 'Popularity' | 'Newest' | 'Rating' | 'Alphabetical';

export type ProjectTemplateVisibility = 'Private' | 'Pending' | 'Public' | 'Rejected';

export interface MarketplaceTemplateSummaryDto {
  id: string;
  code: string;
  displayNameKey: string;
  category: EnclosureCategory;
  subtype: EnclosureSubtype;
  thumbnailUrl: string | null;
  descriptionKey: string | null;
  submittedByTenantId: string | null;
  publishedAtUtc: string | null;
  downloadCount: number;
  averageRating: number | null;
  reviewCount: number;
}

export interface MarketplaceTemplateDetailDto {
  id: string;
  code: string;
  displayNameKey: string;
  category: EnclosureCategory;
  subtype: EnclosureSubtype;
  geometryMode: GeometryMode;
  mountingTopology: MountingTopology;
  defaultConnectorKind: ConnectorKind;
  roofPitchDeg: number | null;
  ridgeHeightMm: number | null;
  eaveHeightMm: number | null;
  thumbnailUrl: string | null;
  descriptionKey: string | null;
  metadataJson: string | null;
  visibility: ProjectTemplateVisibility;
  submittedByTenantId: string | null;
  submittedAtUtc: string | null;
  publishedAtUtc: string | null;
  downloadCount: number;
  averageRating: number | null;
  reviewCount: number;
  rejectionReason: string | null;
  runPresetCount: number;
}

export interface MarketplaceSubmissionDto {
  id: string;
  code: string;
  displayNameKey: string;
  visibility: ProjectTemplateVisibility;
  submittedAtUtc: string | null;
  publishedAtUtc: string | null;
  rejectionReason: string | null;
  downloadCount: number;
}

export interface MarketplaceReviewDto {
  id: string;
  templateId: string;
  reviewerUserId: string;
  ratingStars: number;
  commentMd: string | null;
  reviewedAtUtc: string;
}

export interface InstallMarketplaceResultDto {
  installedTemplateId: string;
}

export interface MarketplaceListParams {
  category?: EnclosureCategory;
  sortBy?: MarketplaceSortBy;
  skip?: number;
  take?: number;
}

export interface SubmitMarketplacePayload {
  tenantTemplateId: string;
}

export interface RateMarketplacePayload {
  templateId: string;
  ratingStars: number;
  commentMd?: string | null;
}

export interface PublishMarketplacePayload {
  templateId: string;
}

export interface RejectMarketplacePayload {
  templateId: string;
  reason: string;
}

const BASE = '/marketplace';
const SUBMISSIONS_BASE = '/my-marketplace-submissions';
const ADMIN_BASE = '/platform/marketplace';

const unwrap = <T>(response: { data: T }): T => response.data;

export const marketplaceApi = {
  list: (
    params: MarketplaceListParams = {},
  ): Promise<SafeResult<MarketplaceTemplateSummaryDto[]>> =>
    safeRequest(
      apiClient
        .get<MarketplaceTemplateSummaryDto[]>(`${BASE}/templates`, {
          params: {
            category: params.category,
            sortBy: params.sortBy ?? 'Popularity',
            skip: params.skip ?? 0,
            take: params.take ?? 20,
          },
        })
        .then(unwrap),
    ),

  detail: (id: string): Promise<SafeResult<MarketplaceTemplateDetailDto>> =>
    safeRequest(
      apiClient
        .get<MarketplaceTemplateDetailDto>(`${BASE}/templates/${encodeURIComponent(id)}`)
        .then(unwrap),
    ),

  reviews: (id: string, skip = 0, take = 20): Promise<SafeResult<MarketplaceReviewDto[]>> =>
    safeRequest(
      apiClient
        .get<MarketplaceReviewDto[]>(`${BASE}/templates/${encodeURIComponent(id)}/reviews`, {
          params: { skip, take },
        })
        .then(unwrap),
    ),

  install: (id: string): Promise<SafeResult<InstallMarketplaceResultDto>> =>
    safeRequest(
      apiClient
        .post<InstallMarketplaceResultDto>(`${BASE}/templates/${encodeURIComponent(id)}/install`)
        .then(unwrap),
    ),

  rate: (payload: RateMarketplacePayload): Promise<SafeResult<MarketplaceReviewDto>> =>
    safeRequest(
      apiClient
        .post<MarketplaceReviewDto>(
          `${BASE}/templates/${encodeURIComponent(payload.templateId)}/review`,
          payload,
        )
        .then(unwrap),
    ),

  listMySubmissions: (): Promise<SafeResult<MarketplaceSubmissionDto[]>> =>
    safeRequest(apiClient.get<MarketplaceSubmissionDto[]>(SUBMISSIONS_BASE).then(unwrap)),

  submit: (payload: SubmitMarketplacePayload): Promise<SafeResult<MarketplaceSubmissionDto>> =>
    safeRequest(apiClient.post<MarketplaceSubmissionDto>(SUBMISSIONS_BASE, payload).then(unwrap)),

  listPending: (): Promise<SafeResult<MarketplaceSubmissionDto[]>> =>
    safeRequest(apiClient.get<MarketplaceSubmissionDto[]>(`${ADMIN_BASE}/pending`).then(unwrap)),

  publish: (payload: PublishMarketplacePayload): Promise<SafeResult<MarketplaceSubmissionDto>> =>
    safeRequest(
      apiClient.post<MarketplaceSubmissionDto>(`${ADMIN_BASE}/publish`, payload).then(unwrap),
    ),

  reject: (payload: RejectMarketplacePayload): Promise<SafeResult<MarketplaceSubmissionDto>> =>
    safeRequest(
      apiClient.post<MarketplaceSubmissionDto>(`${ADMIN_BASE}/reject`, payload).then(unwrap),
    ),
};
