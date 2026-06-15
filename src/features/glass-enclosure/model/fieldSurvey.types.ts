export type FieldSurveyStatus = 'InProgress' | 'Submitted' | 'Approved' | 'Rejected';

export interface FieldSurveyDto {
  id: string;
  projectId: string;
  surveyedByUserId: string;
  surveyedAtUtc: string;
  gpsLat: number | null;
  gpsLng: number | null;
  floorNumber: number | null;
  buildingHeightM: number | null;
  slopeTopMm: number | null;
  slopeBottomMm: number | null;
  slopeLeftMm: number | null;
  slopeRightMm: number | null;
  rawMeasurementsJson: string;
  obstaclesJson: string;
  photoUrlsJson: string;
  annotatedPhotoUrlsJson: string;
  status: FieldSurveyStatus;
  appliedAtUtc: string | null;
  notes: string | null;
}

export interface CreateFieldSurveyInput {
  projectId: string;
  gpsLat?: number | null;
  gpsLng?: number | null;
  floorNumber?: number | null;
  buildingHeightM?: number | null;
  notes?: string | null;
}

export interface UpdateFieldSurveyInput {
  slopeTopMm: number | null;
  slopeBottomMm: number | null;
  slopeLeftMm: number | null;
  slopeRightMm: number | null;
  rawMeasurementsJson: string;
  obstaclesJson: string;
  photoUrlsJson: string;
  annotatedPhotoUrlsJson: string;
  notes: string | null;
}

export interface FieldSurveyApplyResultDto {
  projectId: string;
  surveyId: string;
  runsUpdated: number;
  maxSlopeAdjustmentMm: number;
  toleranceTopMm: number;
  toleranceSideMm: number;
}

export interface FieldSurveyUploadResultDto {
  url: string;
  contentType: string;
  sizeBytes: number;
}

export interface RawMeasurement {
  label: string;
  valueMm: number;
  source: 'manual' | 'laser';
  capturedAt: string;
}

export interface ObstacleNote {
  id: string;
  kind: 'pipe' | 'radiator' | 'window' | 'door' | 'other';
  description: string;
  approximateXMm: number;
  approximateYMm: number;
}
