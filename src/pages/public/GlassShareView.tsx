import { useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Canvas } from '@react-three/fiber';
import { ContactShadows, Environment, OrbitControls, PerspectiveCamera } from '@react-three/drei';
import { Check, X } from 'lucide-react';
import axios from 'axios';
import { RunGroup } from '@/features/glass-enclosure/scene/builders/RunGroup';
import type { ApiResponse } from '@/shared/types/api';
import type { SceneState } from '@/features/glass-enclosure/model/project.types';

interface ShareViewerProjectDto {
  projectId: string;
  code: string;
  projectName: string;
  customerName: string | null;
  status: string;
  currency: string;
  grandTotal: number;
  version: number;
  sceneJson: string;
  validUntilUtc: string;
  alreadyDecided: boolean;
}

interface ShareViewerActionResultDto {
  accepted: boolean;
  rejected: boolean;
  decidedAtUtc: string;
}

const publicClient = axios.create({ baseURL: '/api/v1', withCredentials: false });

export function GlassShareView() {
  const { token } = useParams<{ token: string }>();
  const { t, i18n } = useTranslation();
  const [state, setState] = useState<{
    loading: boolean;
    error: string | null;
    project: ShareViewerProjectDto | null;
    decision: 'pending' | 'accepting' | 'rejecting' | 'done';
    decidedAt: string | null;
    accepted: boolean | null;
  }>({
    loading: true,
    error: null,
    project: null,
    decision: 'pending',
    decidedAt: null,
    accepted: null,
  });
  const [rejectReason, setRejectReason] = useState('');
  const signatureRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    if (!token) return;
    let cancelled = false;
    publicClient
      .get<ApiResponse<ShareViewerProjectDto>>(`/share/glass/${token}`)
      .then((r) => {
        if (cancelled) return;
        setState((s) => ({
          ...s,
          loading: false,
          project: r.data.data,
          decision: r.data.data?.alreadyDecided ? 'done' : 'pending',
        }));
      })
      .catch((err) => {
        if (cancelled) return;
        setState((s) => ({
          ...s,
          loading: false,
          error: err?.response?.data?.error?.message ?? 'load_failed',
        }));
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  const scene = useMemo<SceneState | null>(() => {
    if (!state.project) return null;
    try {
      return JSON.parse(state.project.sceneJson) as SceneState;
    } catch {
      return null;
    }
  }, [state.project]);

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(i18n.language, {
        style: 'currency',
        currency: state.project?.currency ?? 'TRY',
        maximumFractionDigits: 0,
      }),
    [i18n.language, state.project?.currency],
  );

  const handleDecision = async (accept: boolean) => {
    if (!token) return;
    setState((s) => ({ ...s, decision: accept ? 'accepting' : 'rejecting' }));
    const signatureDataUrl = accept ? (signatureRef.current?.toDataURL('image/png') ?? null) : null;
    try {
      const response = await publicClient.post<ApiResponse<ShareViewerActionResultDto>>(
        `/share/glass/${token}/action`,
        { accept, reason: accept ? null : rejectReason || null, signatureDataUrl },
      );
      const data = response.data.data;
      if (!data) return;
      setState((s) => ({
        ...s,
        decision: 'done',
        decidedAt: data.decidedAtUtc,
        accepted: data.accepted,
      }));
    } catch (err) {
      const message = (err as { response?: { data?: { error?: { message?: string } } } })?.response
        ?.data?.error?.message;
      setState((s) => ({ ...s, decision: 'pending', error: message ?? 'action_failed' }));
    }
  };

  if (state.loading) {
    return <FullScreen message={t('Common.Loading')} />;
  }

  if (state.error || !state.project || !scene) {
    return <FullScreen message={t('GlassEnclosure.Share.NotFound')} tone="error" />;
  }

  return (
    <div className="flex min-h-screen flex-col bg-slate-100 dark:bg-slate-950">
      <header className="border-b border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-4">
          <div>
            <h1 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
              {t('GlassEnclosure.Share.Title')}
            </h1>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {state.project.code} · {state.project.projectName}
            </p>
          </div>
          <div className="text-right">
            <div className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Share.GrandTotal')}
            </div>
            <div className="font-mono text-2xl font-bold text-emerald-700 dark:text-emerald-300">
              {currencyFormatter.format(state.project.grandTotal)}
            </div>
          </div>
        </div>
      </header>

      <main className="mx-auto grid w-full max-w-5xl flex-1 gap-4 p-4 md:grid-cols-3">
        <section
          className="rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900 md:col-span-2"
          style={{ minHeight: 460 }}
        >
          <Canvas shadows dpr={[1, 2]} gl={{ antialias: true }}>
            <color attach="background" args={['#f1f5f9']} />
            <PerspectiveCamera
              makeDefault
              position={[3.5, 2.6, 4.5]}
              fov={45}
              near={0.1}
              far={100}
            />
            <OrbitControls
              enableDamping
              makeDefault
              target={[0, 1.2, 0]}
              minDistance={1.5}
              maxDistance={15}
              minPolarAngle={Math.PI / 6}
              maxPolarAngle={Math.PI / 2.05}
            />
            <ambientLight intensity={0.5} />
            <directionalLight position={[5, 8, 3]} intensity={1.1} castShadow />
            <Environment preset="apartment" />
            <ContactShadows
              position={[0, 0.001, 0]}
              opacity={0.4}
              scale={15}
              blur={2.5}
              far={4}
              resolution={1024}
              color="#0f172a"
            />
            {scene.runs.map((run) => (
              <RunGroup
                key={run.id}
                run={run}
                glassTypes={new Map()}
                quality="high"
                showAnnotations={false}
                selectedPanelId={null}
                selectedRunId={null}
                selectedHardwareId={null}
                onSelectRun={() => undefined}
                onSelectPanel={() => undefined}
                onSelectHardware={() => undefined}
              />
            ))}
          </Canvas>
        </section>

        <aside className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
          {state.decision === 'done' ? (
            <DecisionBanner accepted={state.accepted === true} decidedAt={state.decidedAt} />
          ) : (
            <DecisionPanel
              onAccept={() => handleDecision(true)}
              onReject={() => handleDecision(false)}
              rejectReason={rejectReason}
              setRejectReason={setRejectReason}
              signatureRef={signatureRef}
              busy={state.decision !== 'pending'}
            />
          )}

          <dl className="mt-4 space-y-1 text-xs">
            <DlRow
              label={t('GlassEnclosure.Share.Customer')}
              value={state.project.customerName ?? '—'}
            />
            <DlRow label={t('GlassEnclosure.Share.Status')} value={state.project.status} />
            <DlRow label={t('GlassEnclosure.Share.Version')} value={`v${state.project.version}`} />
            <DlRow
              label={t('GlassEnclosure.Share.ValidUntil')}
              value={new Date(state.project.validUntilUtc).toLocaleString(i18n.language)}
            />
          </dl>
        </aside>
      </main>

      <footer className="border-t border-slate-200 bg-white px-4 py-3 text-center text-[10px] text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
        {t('GlassEnclosure.Share.Footer')}
      </footer>
    </div>
  );
}

export default GlassShareView;

const FullScreen = ({ message, tone }: { message: string; tone?: 'error' }) => (
  <div
    className={`flex min-h-screen items-center justify-center text-sm ${tone === 'error' ? 'text-red-600' : 'text-slate-500'}`}
  >
    {message}
  </div>
);

const DlRow = ({ label, value }: { label: string; value: string }) => (
  <div className="flex justify-between text-xs">
    <dt className="text-slate-500 dark:text-slate-400">{label}</dt>
    <dd className="font-mono text-slate-700 dark:text-slate-300">{value}</dd>
  </div>
);

const DecisionBanner = ({
  accepted,
  decidedAt,
}: {
  accepted: boolean;
  decidedAt: string | null;
}) => {
  const { t, i18n } = useTranslation();
  const dt = decidedAt ? new Date(decidedAt).toLocaleString(i18n.language) : '';
  return (
    <div
      className={`rounded-md border p-3 text-sm ${
        accepted
          ? 'border-emerald-500/60 bg-emerald-50 text-emerald-700 dark:border-emerald-500/40 dark:bg-emerald-950/30 dark:text-emerald-300'
          : 'border-red-500/60 bg-red-50 text-red-700 dark:border-red-500/40 dark:bg-red-950/30 dark:text-red-300'
      }`}
    >
      <div className="mb-1 flex items-center gap-2 font-semibold">
        {accepted ? <Check size={16} /> : <X size={16} />}
        {accepted ? t('GlassEnclosure.Share.Accepted') : t('GlassEnclosure.Share.Rejected')}
      </div>
      <div className="text-xs opacity-80">{dt}</div>
    </div>
  );
};

const DecisionPanel = ({
  onAccept,
  onReject,
  rejectReason,
  setRejectReason,
  signatureRef,
  busy,
}: {
  onAccept: () => void;
  onReject: () => void;
  rejectReason: string;
  setRejectReason: (value: string) => void;
  signatureRef: React.MutableRefObject<HTMLCanvasElement | null>;
  busy: boolean;
}) => {
  const { t } = useTranslation();
  const startDrawing = useRef(false);

  const handlePointerDown = (e: React.PointerEvent<HTMLCanvasElement>) => {
    startDrawing.current = true;
    const canvas = signatureRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const rect = canvas.getBoundingClientRect();
    ctx.beginPath();
    ctx.moveTo(e.clientX - rect.left, e.clientY - rect.top);
  };

  const handlePointerMove = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!startDrawing.current) return;
    const canvas = signatureRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    const rect = canvas.getBoundingClientRect();
    ctx.lineTo(e.clientX - rect.left, e.clientY - rect.top);
    ctx.strokeStyle = '#0f172a';
    ctx.lineWidth = 2;
    ctx.lineCap = 'round';
    ctx.stroke();
  };

  const handlePointerUp = () => {
    startDrawing.current = false;
  };

  const clearSignature = () => {
    const canvas = signatureRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
  };

  return (
    <div className="space-y-3">
      <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
        {t('GlassEnclosure.Share.Decide')}
      </h2>
      <div className="rounded border border-slate-300 bg-white p-1 dark:border-slate-700 dark:bg-slate-950">
        <div className="text-[10px] text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Share.Signature')}
        </div>
        <canvas
          ref={signatureRef}
          width={320}
          height={120}
          onPointerDown={handlePointerDown}
          onPointerMove={handlePointerMove}
          onPointerUp={handlePointerUp}
          onPointerLeave={handlePointerUp}
          className="block w-full touch-none rounded border border-dashed border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-900"
          style={{ touchAction: 'none' }}
        />
        <button
          type="button"
          onClick={clearSignature}
          className="mt-1 text-[10px] text-slate-500 underline hover:text-slate-700 dark:hover:text-slate-300"
        >
          {t('GlassEnclosure.Share.ClearSignature')}
        </button>
      </div>
      <button
        type="button"
        onClick={onAccept}
        disabled={busy}
        className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
      >
        <Check size={16} />
        {t('GlassEnclosure.Share.Accept')}
      </button>
      <div className="border-t border-slate-200 pt-2 dark:border-slate-700">
        <label className="text-xs text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Share.RejectReason')}
        </label>
        <textarea
          value={rejectReason}
          onChange={(e) => setRejectReason(e.target.value)}
          rows={2}
          className="mt-1 w-full rounded border border-slate-300 bg-white p-2 text-xs dark:border-slate-700 dark:bg-slate-950"
          placeholder={t('GlassEnclosure.Share.RejectReasonPlaceholder')}
        />
        <button
          type="button"
          onClick={onReject}
          disabled={busy}
          className="mt-2 inline-flex w-full items-center justify-center gap-2 rounded-md border border-red-500/50 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50 dark:hover:bg-red-950/30"
        >
          <X size={16} />
          {t('GlassEnclosure.Share.Reject')}
        </button>
      </div>
    </div>
  );
};
