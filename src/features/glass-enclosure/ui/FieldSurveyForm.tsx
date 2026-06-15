import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Camera,
  Check,
  ClipboardCheck,
  MapPin,
  Plus,
  Ruler,
  Trash2,
  Upload,
  X,
} from 'lucide-react';
import {
  useApproveFieldSurveyMutation,
  useFieldSurveysByProjectQuery,
  useCreateFieldSurveyMutation,
  useRejectFieldSurveyMutation,
  useSubmitFieldSurveyMutation,
  useUpdateFieldSurveyMutation,
  useUploadSurveyPhotoMutation,
} from '../hooks/useFieldSurveyQueries';
import type { FieldSurveyDto, ObstacleNote, RawMeasurement } from '../model/fieldSurvey.types';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { queueToast } from '@/shared/api/toastQueue';
import { logger } from '@/shared/lib/logger';

interface FieldSurveyFormProps {
  projectId: string;
  defaultFloorNumber: number | null;
  defaultBuildingHeightM: number | null;
}

interface SurveyDraft {
  slopeTopMm: number | null;
  slopeBottomMm: number | null;
  slopeLeftMm: number | null;
  slopeRightMm: number | null;
  rawMeasurements: RawMeasurement[];
  obstacles: ObstacleNote[];
  photoUrls: string[];
  annotatedPhotoUrls: string[];
  notes: string;
}

const OBSTACLE_KINDS: ObstacleNote['kind'][] = ['pipe', 'radiator', 'window', 'door', 'other'];

const MAX_PHOTO_BYTES = 25 * 1024 * 1024;
const ALLOWED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/heic', 'image/heif'];

const safeParse = <T,>(json: string, fallback: T): T => {
  if (!json) return fallback;
  try {
    const parsed = JSON.parse(json);
    if (Array.isArray(fallback) && !Array.isArray(parsed)) return fallback;
    return parsed as T;
  } catch {
    return fallback;
  }
};

const buildDraft = (survey: FieldSurveyDto | null): SurveyDraft => ({
  slopeTopMm: survey?.slopeTopMm ?? null,
  slopeBottomMm: survey?.slopeBottomMm ?? null,
  slopeLeftMm: survey?.slopeLeftMm ?? null,
  slopeRightMm: survey?.slopeRightMm ?? null,
  rawMeasurements: safeParse<RawMeasurement[]>(survey?.rawMeasurementsJson ?? '[]', []),
  obstacles: safeParse<ObstacleNote[]>(survey?.obstaclesJson ?? '[]', []),
  photoUrls: safeParse<string[]>(survey?.photoUrlsJson ?? '[]', []),
  annotatedPhotoUrls: safeParse<string[]>(survey?.annotatedPhotoUrlsJson ?? '[]', []),
  notes: survey?.notes ?? '',
});

export function FieldSurveyForm({
  projectId,
  defaultFloorNumber,
  defaultBuildingHeightM,
}: FieldSurveyFormProps) {
  const { t, i18n } = useTranslation();
  const surveysQuery = useFieldSurveysByProjectQuery(projectId);
  const createMutation = useCreateFieldSurveyMutation();
  const updateMutation = useUpdateFieldSurveyMutation();
  const submitMutation = useSubmitFieldSurveyMutation();
  const approveMutation = useApproveFieldSurveyMutation();
  const rejectMutation = useRejectFieldSurveyMutation();
  const uploadMutation = useUploadSurveyPhotoMutation();

  const surveys = useMemo<FieldSurveyDto[]>(
    () => surveysQuery.data?.data ?? [],
    [surveysQuery.data?.data],
  );
  const [selectedSurveyId, setSelectedSurveyId] = useState<string | null>(null);
  const activeSurvey = useMemo<FieldSurveyDto | null>(() => {
    if (selectedSurveyId) {
      const picked = surveys.find((s) => s.id === selectedSurveyId);
      if (picked) return picked;
    }
    const inProgress = surveys.find((s) => s.status === 'InProgress');
    if (inProgress) return inProgress;
    return surveys[0] ?? null;
  }, [surveys, selectedSurveyId]);
  const activeSurveyId = activeSurvey?.id ?? null;

  const [draft, setDraft] = useState<SurveyDraft>(() => buildDraft(activeSurvey));
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const dateFormatter = useMemo(
    () => new Intl.DateTimeFormat(i18n.language, { dateStyle: 'short', timeStyle: 'short' }),
    [i18n.language],
  );

  const draftSurveyRef = useRef<string | null>(activeSurveyId);
  if (draftSurveyRef.current !== activeSurveyId) {
    draftSurveyRef.current = activeSurveyId;
    setDraft(buildDraft(activeSurvey));
  }

  const isEditable = activeSurvey === null || activeSurvey.status === 'InProgress';
  const canSubmit = activeSurvey?.status === 'InProgress';
  const canApprove = activeSurvey?.status === 'Submitted';

  const handleCreate = async () => {
    let gpsLat: number | null = null;
    let gpsLng: number | null = null;
    let gpsUnavailable = false;
    if ('geolocation' in navigator) {
      try {
        const position = await new Promise<GeolocationPosition>((resolve, reject) => {
          navigator.geolocation.getCurrentPosition(resolve, reject, {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 30000,
          });
        });
        gpsLat = position.coords.latitude;
        gpsLng = position.coords.longitude;
      } catch {
        gpsUnavailable = true;
      }
    } else {
      gpsUnavailable = true;
    }
    if (gpsUnavailable) {
      logger.info('field-survey.gps-unavailable', { projectId });
      queueToast({
        dedupeKey: 'field-survey:gps-unavailable',
        description: t('GlassEnclosure.Survey.GpsUnavailable'),
        variant: 'warning',
      });
    }
    const [result] = await safeRequestWithNotify(
      createMutation.mutateAsync({
        projectId,
        gpsLat,
        gpsLng,
        floorNumber: defaultFloorNumber,
        buildingHeightM: defaultBuildingHeightM,
        notes: null,
      }),
      { successMessage: t('GlassEnclosure.Survey.Created') },
    );
    if (result?.data) setSelectedSurveyId(result.data.id);
  };

  const handleSave = async (): Promise<boolean> => {
    if (!activeSurvey) return false;
    const [, error] = await safeRequestWithNotify(
      updateMutation.mutateAsync({
        id: activeSurvey.id,
        projectId,
        input: {
          slopeTopMm: draft.slopeTopMm,
          slopeBottomMm: draft.slopeBottomMm,
          slopeLeftMm: draft.slopeLeftMm,
          slopeRightMm: draft.slopeRightMm,
          rawMeasurementsJson: JSON.stringify(draft.rawMeasurements),
          obstaclesJson: JSON.stringify(draft.obstacles),
          photoUrlsJson: JSON.stringify(draft.photoUrls),
          annotatedPhotoUrlsJson: JSON.stringify(draft.annotatedPhotoUrls),
          notes: draft.notes,
        },
      }),
      { successMessage: t('GlassEnclosure.Survey.Saved') },
    );
    return error === null;
  };

  const handleSubmit = async () => {
    if (!activeSurvey) return;
    const saved = await handleSave();
    if (!saved) return;
    await safeRequestWithNotify(submitMutation.mutateAsync({ id: activeSurvey.id, projectId }), {
      successMessage: t('GlassEnclosure.Survey.Submitted'),
    });
  };

  const handleApprove = async (applyToProject: boolean) => {
    if (!activeSurvey) return;
    const [result] = await safeRequestWithNotify(
      approveMutation.mutateAsync({ id: activeSurvey.id, applyToProject, projectId }),
      { successMessage: t('GlassEnclosure.Survey.Approved') },
    );
    const applied = result?.data;
    if (applyToProject && applied) {
      window.alert(
        t('GlassEnclosure.Survey.AppliedInfo', {
          runs: applied.runsUpdated,
          mm: applied.maxSlopeAdjustmentMm,
          obstacles: draft.obstacles.length,
          defaultValue: `${applied.runsUpdated} runs updated · ${applied.maxSlopeAdjustmentMm} mm slope adjustment · ${draft.obstacles.length} obstacle(s) recorded (informational only)`,
        }),
      );
    }
  };

  const handleReject = async () => {
    if (!activeSurvey) return;
    const reason = window.prompt(t('GlassEnclosure.Survey.RejectPrompt')) || null;
    await safeRequestWithNotify(
      rejectMutation.mutateAsync({ id: activeSurvey.id, reason, projectId }),
      { successMessage: t('GlassEnclosure.Survey.Rejected') },
    );
  };

  const handlePhotoUpload = async (file: File) => {
    if (!activeSurvey) return;
    if (!ALLOWED_PHOTO_TYPES.includes(file.type)) {
      queueToast({
        dedupeKey: 'field-survey:photo-type',
        description: t('GlassEnclosure.Survey.PhotoTypeError'),
        variant: 'error',
      });
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      queueToast({
        dedupeKey: 'field-survey:photo-size',
        description: t('GlassEnclosure.Survey.PhotoSizeError'),
        variant: 'error',
      });
      return;
    }
    const [result] = await safeRequestWithNotify(
      uploadMutation.mutateAsync({ surveyId: activeSurvey.id, file }),
      { successMessage: t('GlassEnclosure.Survey.PhotoUploaded') },
    );
    const uploaded = result?.data;
    if (uploaded) {
      setDraft((d) => ({ ...d, photoUrls: [...d.photoUrls, uploaded.url] }));
    }
  };

  const addManualMeasurement = (label: string, valueMm: number) => {
    setDraft((d) => ({
      ...d,
      rawMeasurements: [
        ...d.rawMeasurements,
        { label, valueMm, source: 'manual', capturedAt: new Date().toISOString() },
      ],
    }));
  };

  const addObstacle = () => {
    setDraft((d) => ({
      ...d,
      obstacles: [
        ...d.obstacles,
        {
          id: crypto.randomUUID(),
          kind: 'other',
          description: '',
          approximateXMm: 0,
          approximateYMm: 0,
        },
      ],
    }));
  };

  const updateObstacle = (id: string, patch: Partial<ObstacleNote>) => {
    setDraft((d) => ({
      ...d,
      obstacles: d.obstacles.map((o) => (o.id === id ? { ...o, ...patch } : o)),
    }));
  };

  const removeObstacle = (id: string) => {
    setDraft((d) => ({ ...d, obstacles: d.obstacles.filter((o) => o.id !== id) }));
  };

  return (
    <section className="space-y-4 p-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.Survey.Title')}
          </h2>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Survey.Subtitle')}
          </p>
        </div>
        {activeSurvey ? (
          <div className="flex flex-wrap items-center gap-1.5">
            <StatusBadge status={activeSurvey.status} />
            {activeSurvey.appliedAtUtc && (
              <span className="rounded bg-violet-100 px-2 py-0.5 text-xs font-medium text-violet-700 dark:bg-violet-950/40 dark:text-violet-300">
                {t('GlassEnclosure.Survey.AppliedBadge', {
                  date: dateFormatter.format(new Date(activeSurvey.appliedAtUtc)),
                })}
              </span>
            )}
            <button
              type="button"
              onClick={handleCreate}
              disabled={createMutation.isPending}
              className="inline-flex items-center gap-1.5 rounded-md border border-blue-500 px-2 py-1 text-xs font-medium text-blue-600 hover:bg-blue-50 disabled:opacity-50 dark:hover:bg-blue-950/30"
            >
              <Plus size={12} />
              {t('GlassEnclosure.Survey.New')}
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={handleCreate}
            disabled={createMutation.isPending}
            className="inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            <Plus size={14} />
            {t('GlassEnclosure.Survey.New')}
          </button>
        )}
      </header>

      {surveys.length > 1 && (
        <div className="flex flex-wrap gap-1.5">
          {surveys.map((s) => (
            <button
              key={s.id}
              type="button"
              onClick={() => setSelectedSurveyId(s.id)}
              className={`rounded border px-2 py-0.5 text-[10px] ${
                s.id === activeSurveyId
                  ? 'border-blue-500 bg-blue-50 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300'
                  : 'border-slate-300 text-slate-600 dark:border-slate-700 dark:text-slate-300'
              }`}
            >
              {dateFormatter.format(new Date(s.surveyedAtUtc))} · {s.status}
            </button>
          ))}
        </div>
      )}

      {!activeSurvey && (
        <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
          <MapPin size={16} className="mx-auto mb-2" />
          {t('GlassEnclosure.Survey.NoActive')}
        </div>
      )}

      {activeSurvey && (
        <>
          <section className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Survey.Slope')}
            </h3>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <SlopeField
                label={t('GlassEnclosure.Survey.SlopeTop')}
                value={draft.slopeTopMm}
                onChange={(v) => setDraft((d) => ({ ...d, slopeTopMm: v }))}
                disabled={!isEditable}
              />
              <SlopeField
                label={t('GlassEnclosure.Survey.SlopeBottom')}
                value={draft.slopeBottomMm}
                onChange={(v) => setDraft((d) => ({ ...d, slopeBottomMm: v }))}
                disabled={!isEditable}
              />
              <SlopeField
                label={t('GlassEnclosure.Survey.SlopeLeft')}
                value={draft.slopeLeftMm}
                onChange={(v) => setDraft((d) => ({ ...d, slopeLeftMm: v }))}
                disabled={!isEditable}
              />
              <SlopeField
                label={t('GlassEnclosure.Survey.SlopeRight')}
                value={draft.slopeRightMm}
                onChange={(v) => setDraft((d) => ({ ...d, slopeRightMm: v }))}
                disabled={!isEditable}
              />
            </div>
            <p className="mt-2 text-[10px] text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Survey.SlopeHint')}
            </p>
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.Measurements')}
              </h3>
              {isEditable && (
                <button
                  type="button"
                  onClick={() => {
                    const label = window.prompt(t('GlassEnclosure.Survey.MeasureLabel')) || '';
                    const valueStr = window.prompt(t('GlassEnclosure.Survey.MeasureValue')) || '';
                    const value = parseInt(valueStr, 10);
                    if (label && !Number.isNaN(value)) addManualMeasurement(label, value);
                  }}
                  className="inline-flex items-center gap-1 rounded bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-700 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-200"
                >
                  <Ruler size={12} />
                  {t('GlassEnclosure.Survey.AddMeasurement')}
                </button>
              )}
            </div>
            {draft.rawMeasurements.length === 0 ? (
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.NoMeasurements')}
              </p>
            ) : (
              <ul className="space-y-1 text-xs">
                {draft.rawMeasurements.map((m, idx) => (
                  <li key={idx} className="flex items-center justify-between">
                    <span className="text-slate-600 dark:text-slate-300">{m.label}</span>
                    <span className="font-mono text-slate-800 dark:text-slate-100">
                      {m.valueMm} mm
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.Obstacles')}
              </h3>
              {isEditable && (
                <button
                  type="button"
                  onClick={addObstacle}
                  className="inline-flex items-center gap-1 rounded bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-700 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-200"
                >
                  <Plus size={12} />
                  {t('GlassEnclosure.Survey.AddObstacle')}
                </button>
              )}
            </div>
            <p className="mb-2 rounded bg-blue-50 px-2 py-1 text-[11px] text-blue-700 dark:bg-blue-950/40 dark:text-blue-300">
              {t('GlassEnclosure.Survey.ObstaclesInfo', {
                defaultValue: 'Recorded for reference only — not applied to the drawing or cost.',
              })}
            </p>
            {draft.obstacles.length === 0 ? (
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.NoObstacles')}
              </p>
            ) : (
              <ul className="space-y-2 text-xs">
                {draft.obstacles.map((o) => (
                  <li key={o.id} className="grid grid-cols-[auto_1fr_auto] items-center gap-2">
                    <select
                      value={o.kind}
                      disabled={!isEditable}
                      onChange={(e) =>
                        updateObstacle(o.id, { kind: e.target.value as ObstacleNote['kind'] })
                      }
                      className="rounded border border-slate-300 bg-white px-1 py-0.5 dark:border-slate-700 dark:bg-slate-900"
                    >
                      {OBSTACLE_KINDS.map((k) => (
                        <option key={k} value={k}>
                          {t(`GlassEnclosure.Survey.ObstacleKind.${k}` as never)}
                        </option>
                      ))}
                    </select>
                    <input
                      type="text"
                      value={o.description}
                      disabled={!isEditable}
                      onChange={(e) => updateObstacle(o.id, { description: e.target.value })}
                      placeholder={t('GlassEnclosure.Survey.ObstaclePlaceholder')}
                      className="rounded border border-slate-300 bg-white px-1 py-0.5 dark:border-slate-700 dark:bg-slate-900"
                    />
                    {isEditable && (
                      <button
                        type="button"
                        onClick={() => removeObstacle(o.id)}
                        className="text-red-600 hover:text-red-700"
                      >
                        <Trash2 size={12} />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.Photos')}
              </h3>
              {isEditable && (
                <>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/heic,image/heif"
                    capture="environment"
                    className="hidden"
                    onChange={(e) => {
                      const file = e.target.files?.[0];
                      if (file) handlePhotoUpload(file);
                      if (fileInputRef.current) fileInputRef.current.value = '';
                    }}
                  />
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    className="inline-flex items-center gap-1 rounded bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-700 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-200"
                  >
                    <Camera size={12} />
                    {t('GlassEnclosure.Survey.UploadPhoto')}
                  </button>
                </>
              )}
            </div>
            {draft.photoUrls.length === 0 ? (
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {t('GlassEnclosure.Survey.NoPhotos')}
              </p>
            ) : (
              <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                {draft.photoUrls.map((url, idx) => (
                  <li
                    key={url}
                    className="relative aspect-square overflow-hidden rounded border border-slate-200 dark:border-slate-700"
                  >
                    <img src={url} alt={`survey-${idx}`} className="h-full w-full object-cover" />
                    {isEditable && (
                      <button
                        type="button"
                        onClick={() =>
                          setDraft((d) => ({
                            ...d,
                            photoUrls: d.photoUrls.filter((u) => u !== url),
                          }))
                        }
                        className="absolute end-1 top-1 rounded-full bg-red-600 p-0.5 text-white"
                      >
                        <X size={10} />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Survey.Notes')}
            </h3>
            <textarea
              rows={3}
              value={draft.notes}
              disabled={!isEditable}
              onChange={(e) => setDraft((d) => ({ ...d, notes: e.target.value }))}
              className="w-full rounded border border-slate-300 bg-white p-2 text-xs dark:border-slate-700 dark:bg-slate-900"
              placeholder={t('GlassEnclosure.Survey.NotesPlaceholder')}
            />
          </section>

          <div className="flex flex-wrap gap-2">
            {isEditable && (
              <>
                <button
                  type="button"
                  onClick={handleSave}
                  disabled={updateMutation.isPending}
                  className="inline-flex items-center gap-1.5 rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                  <Upload size={14} />
                  {t('GlassEnclosure.Survey.SaveDraft')}
                </button>
                {canSubmit && (
                  <button
                    type="button"
                    onClick={handleSubmit}
                    disabled={submitMutation.isPending}
                    className="inline-flex items-center gap-1.5 rounded-md bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                  >
                    <ClipboardCheck size={14} />
                    {t('GlassEnclosure.Survey.Submit')}
                  </button>
                )}
              </>
            )}
            {canApprove && (
              <>
                <button
                  type="button"
                  onClick={() => handleApprove(true)}
                  disabled={approveMutation.isPending}
                  className="inline-flex items-center gap-1.5 rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
                >
                  <Check size={14} />
                  {t('GlassEnclosure.Survey.ApproveAndApply')}
                </button>
                <button
                  type="button"
                  onClick={() => handleApprove(false)}
                  disabled={approveMutation.isPending}
                  className="inline-flex items-center gap-1.5 rounded-md border border-emerald-500 px-3 py-1.5 text-sm font-medium text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/30"
                >
                  {t('GlassEnclosure.Survey.ApproveOnly')}
                </button>
                <button
                  type="button"
                  onClick={handleReject}
                  disabled={rejectMutation.isPending}
                  className="inline-flex items-center gap-1.5 rounded-md border border-red-500 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
                >
                  <X size={14} />
                  {t('GlassEnclosure.Survey.Reject')}
                </button>
              </>
            )}
          </div>
        </>
      )}
    </section>
  );
}

const SLOPE_MIN_MM = -500;
const SLOPE_MAX_MM = 500;

const SlopeField = ({
  label,
  value,
  onChange,
  disabled,
}: {
  label: string;
  value: number | null;
  onChange: (v: number | null) => void;
  disabled: boolean;
}) => (
  <label className="flex flex-col gap-1 text-xs text-slate-600 dark:text-slate-400">
    <span className="uppercase tracking-wide">{label} (mm)</span>
    <input
      type="number"
      step="0.5"
      min={SLOPE_MIN_MM}
      max={SLOPE_MAX_MM}
      value={value ?? ''}
      disabled={disabled}
      onChange={(e) => {
        if (e.target.value === '') {
          onChange(null);
          return;
        }
        const parsed = Number(e.target.value);
        if (Number.isNaN(parsed)) {
          onChange(null);
          return;
        }
        onChange(Math.min(SLOPE_MAX_MM, Math.max(SLOPE_MIN_MM, parsed)));
      }}
      className="rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900"
    />
  </label>
);

const StatusBadge = ({ status }: { status: FieldSurveyDto['status'] }) => {
  const { t } = useTranslation();
  const map: Record<FieldSurveyDto['status'], string> = {
    InProgress: 'bg-amber-100 text-amber-700 dark:bg-amber-950/40 dark:text-amber-300',
    Submitted: 'bg-blue-100 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300',
    Approved: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300',
    Rejected: 'bg-red-100 text-red-700 dark:bg-red-950/40 dark:text-red-300',
  };
  return (
    <span className={`rounded px-2 py-0.5 text-xs font-medium ${map[status]}`}>
      {t(`GlassEnclosure.Survey.Status.${status}` as never)}
    </span>
  );
};
