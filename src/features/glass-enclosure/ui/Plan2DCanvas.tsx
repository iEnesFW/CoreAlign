import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BrickWall, Minus, MoveDiagonal2, Pencil, Plus, Square } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { resolveWallHoles } from '../model/wallHoleGeometry';
import { arcEndLocal, isRealArc, resolveArc } from '../model/arcGeometry';
import { snapAngleDeg } from '../model/angleSnap';
import type { SceneRunState, SceneSlabState, SceneWallState } from '../model/project.types';

interface Plan2DCanvasProps {
  onAddRun?: (start: { x: number; y: number }, end: { x: number; y: number }) => void;
  onUpdateRunGeometry?: (
    runId: string,
    geometry: {
      lengthMm: number;
      originX: number;
      originY: number;
      rotationDeg: number;
    },
  ) => void;
  onSelectConnectionCandidate?: (runAId: string, runBId: string) => void;
}

type ViewportState = {
  panX: number;
  panY: number;
  zoom: number;
};

type Vec = { x: number; y: number };

type DragState =
  | { mode: 'idle' }
  | { mode: 'pan'; lastClient: Vec }
  | { mode: 'draw'; start: Vec; current: Vec }
  | { mode: 'move-endpoint'; runId: string; endpoint: 'start' | 'end'; current: Vec };

const SNAP_MM = 50;
const GRID_MAJOR_MM = 1000;
const GRID_MINOR_MM = 100;
const WALL_HEIGHT_MM = 2600;
const WALL_THICKNESS_MM = 200;

const angleDeg = (dx: number, dy: number) => {
  const rad = Math.atan2(dy, dx);
  return snapAngleDeg((rad * 180) / Math.PI);
};

const lenMm = (a: Vec, b: Vec) => Math.round(Math.hypot(b.x - a.x, b.y - a.y));

const snapToGrid = (value: number) => Math.round(value / SNAP_MM) * SNAP_MM;

const isArcRun = (run: SceneRunState) => isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg);

const rotateVec = (xMm: number, yMm: number, rotationDeg: number): Vec => {
  const cos = Math.cos((rotationDeg * Math.PI) / 180);
  const sin = Math.sin((rotationDeg * Math.PI) / 180);
  return { x: xMm * cos - yMm * sin, y: xMm * sin + yMm * cos };
};

const runEndLocal = (run: SceneRunState): Vec => {
  if (!isArcRun(run)) return { x: run.lengthMm, y: 0 };
  const arc = arcEndLocal(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1);
  return { x: arc.xMm, y: arc.yMm };
};

type OpeningSpan = { id: string; kind: 'window' | 'door'; fromMm: number; toMm: number };

// WHY: this used to be a third, hand-inlined copy of the opening clamp — and it omitted the head
// re-anchor, so the 2D plan and the carved 3D wall disagreed about which openings even exist.
// resolveWallHoles is the one source of truth for what the wall actually carves.
const wallOpeningSpans = (wall: SceneWallState): OpeningSpan[] => {
  const kindById = new Map((wall.openings ?? []).map((o) => [o.id, o.kind]));
  return resolveWallHoles(wall)
    .holes.filter((hole) => hole.source === 'opening')
    .map((hole) => ({
      id: hole.id,
      kind: kindById.get(hole.id) ?? 'window',
      fromMm: hole.uStartMm,
      toMm: hole.uStartMm + hole.uWidthMm,
    }));
};

type ArcRenderParams = { radiusMm: number; largeArcFlag: 0 | 1; sweepFlag: 0 | 1 };

const arcRenderParams = (run: SceneRunState): ArcRenderParams | null => {
  if (!isArcRun(run)) return null;
  // RAW stored radius — exactly what the 3D renderer, snap targets and collision footprints use,
  // so the 2D plan can never draw an arc the other systems disagree with.
  const radiusMm = resolveArc(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1).radiusMm;
  // CHORD-INVARIANT: the sweep is stored directly (a major arc >180° sets the SVG large-arc flag).
  const sweepRad = Math.min((Math.abs(run.geomArcSweepDeg ?? 0) * Math.PI) / 180, Math.PI * 2);
  return {
    radiusMm,
    largeArcFlag: sweepRad > Math.PI ? 1 : 0,
    sweepFlag: (run.geomArcSweepDeg ?? 1) < 0 ? 0 : 1,
  };
};

export function Plan2DCanvas({
  onAddRun,
  onUpdateRunGeometry,
  onSelectConnectionCandidate,
}: Plan2DCanvasProps) {
  const { t } = useTranslation();
  const containerRef = useRef<HTMLDivElement | null>(null);
  const svgRef = useRef<SVGSVGElement | null>(null);
  const [size, setSize] = useState({ width: 800, height: 500 });
  const [viewport, setViewport] = useState<ViewportState>({ panX: 0, panY: 0, zoom: 0.05 });
  const [drag, setDrag] = useState<DragState>({ mode: 'idle' });
  const [tool, setTool] = useState<'select' | 'draw' | 'wall'>('select');

  const runs = useDesignerStore((s) => s.scene.runs);
  const walls = useDesignerStore((s) => s.scene.walls);
  const slabs = useDesignerStore((s) => s.scene.slabs);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const selectedRunId = useDesignerStore((s) => s.selection.runId);
  const selectedWallId = useDesignerStore((s) =>
    s.selection.kind === 'wall' ? (s.selection.wallId ?? null) : null,
  );
  const selectedSlabId = useDesignerStore((s) =>
    s.selection.kind === 'slab' ? (s.selection.slabId ?? null) : null,
  );

  useEffect(() => {
    const element = containerRef.current;
    if (!element) return;
    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        setSize({ width: entry.contentRect.width, height: entry.contentRect.height });
      }
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const screenToWorld = useCallback(
    (clientX: number, clientY: number): Vec => {
      const rect = containerRef.current?.getBoundingClientRect();
      if (!rect) return { x: 0, y: 0 };
      const sx = clientX - rect.left;
      const sy = clientY - rect.top;
      return {
        x: (sx - size.width / 2 - viewport.panX) / viewport.zoom,
        y: (sy - size.height / 2 - viewport.panY) / viewport.zoom,
      };
    },
    [size.width, size.height, viewport.panX, viewport.panY, viewport.zoom],
  );

  const runsAsSegments = useMemo(
    () =>
      runs.map((run) => {
        const start: Vec = { x: run.originX, y: run.originY };
        const localEnd = runEndLocal(run);
        const rotated = rotateVec(localEnd.x, localEnd.y, run.rotationDeg);
        const end: Vec = {
          x: run.originX + rotated.x,
          y: run.originY + rotated.y,
        };
        return { run, start, end };
      }),
    [runs],
  );

  const wallsAsSegments = useMemo(
    () =>
      (walls ?? []).map((wall) => {
        const start: Vec = { x: wall.originX, y: wall.originY };
        const rotated = rotateVec(wall.lengthMm, 0, wall.rotationDeg);
        const end: Vec = { x: wall.originX + rotated.x, y: wall.originY + rotated.y };
        return { wall, start, end };
      }),
    [walls],
  );

  const slabsAsPolygons = useMemo(
    () =>
      (slabs ?? []).map((slab) => {
        const origin: Vec = { x: slab.originX, y: slab.originY };
        const alongLength = rotateVec(slab.lengthMm, 0, slab.rotationDeg);
        const alongDepth = rotateVec(0, slab.depthMm, slab.rotationDeg);
        const points: Vec[] = [
          origin,
          { x: origin.x + alongLength.x, y: origin.y + alongLength.y },
          {
            x: origin.x + alongLength.x + alongDepth.x,
            y: origin.y + alongLength.y + alongDepth.y,
          },
          { x: origin.x + alongDepth.x, y: origin.y + alongDepth.y },
        ];
        return { slab, points };
      }),
    [slabs],
  );

  const adjacencyHits = useMemo(() => {
    const matches: { runA: SceneRunState; runB: SceneRunState; pointA: Vec; pointB: Vec }[] = [];
    const threshold = 250;
    for (let i = 0; i < runsAsSegments.length; i += 1) {
      const a = runsAsSegments[i];
      for (let j = i + 1; j < runsAsSegments.length; j += 1) {
        const b = runsAsSegments[j];
        const pairs: [Vec, Vec][] = [
          [a.start, b.start],
          [a.start, b.end],
          [a.end, b.start],
          [a.end, b.end],
        ];
        for (const [pa, pb] of pairs) {
          if (Math.hypot(pa.x - pb.x, pa.y - pb.y) <= threshold) {
            matches.push({ runA: a.run, runB: b.run, pointA: pa, pointB: pb });
            break;
          }
        }
      }
    }
    return matches;
  }, [runsAsSegments]);

  const handlePointerDown = (event: React.PointerEvent<SVGSVGElement>) => {
    if (event.button === 1 || (event.button === 0 && event.shiftKey)) {
      setDrag({ mode: 'pan', lastClient: { x: event.clientX, y: event.clientY } });
      event.currentTarget.setPointerCapture(event.pointerId);
      return;
    }
    if ((tool === 'draw' || tool === 'wall') && event.button === 0) {
      const world = screenToWorld(event.clientX, event.clientY);
      const snapped = { x: snapToGrid(world.x), y: snapToGrid(world.y) };
      setDrag({ mode: 'draw', start: snapped, current: snapped });
      event.currentTarget.setPointerCapture(event.pointerId);
      return;
    }
  };

  const handlePointerMove = (event: React.PointerEvent<SVGSVGElement>) => {
    if (drag.mode === 'pan') {
      const dx = event.clientX - drag.lastClient.x;
      const dy = event.clientY - drag.lastClient.y;
      setViewport((vp) => ({ ...vp, panX: vp.panX + dx, panY: vp.panY + dy }));
      setDrag({ mode: 'pan', lastClient: { x: event.clientX, y: event.clientY } });
      return;
    }
    if (drag.mode === 'draw') {
      const world = screenToWorld(event.clientX, event.clientY);
      const snapped = { x: snapToGrid(world.x), y: snapToGrid(world.y) };
      setDrag({ ...drag, current: snapped });
      return;
    }
    if (drag.mode === 'move-endpoint') {
      const world = screenToWorld(event.clientX, event.clientY);
      const snapped = { x: snapToGrid(world.x), y: snapToGrid(world.y) };
      setDrag({ ...drag, current: snapped });
      return;
    }
  };

  const handlePointerUp = (event: React.PointerEvent<SVGSVGElement>) => {
    if (drag.mode === 'draw') {
      const length = lenMm(drag.start, drag.current);
      if (length >= 100) {
        if (tool === 'wall') {
          const state = useDesignerStore.getState();
          const midX = (drag.start.x + drag.current.x) / 2;
          const midY = (drag.start.y + drag.current.y) / 2;
          let heightMm = WALL_HEIGHT_MM;
          let bestDist = Number.POSITIVE_INFINITY;
          for (const run of state.scene.runs) {
            const dist = Math.hypot(run.originX - midX, run.originY - midY);
            if (dist < bestDist) {
              bestDist = dist;
              heightMm = run.heightMm + 200;
            }
          }
          state.addWall({
            id: crypto.randomUUID(),
            originX: drag.start.x,
            originY: drag.start.y,
            lengthMm: length,
            rotationDeg: angleDeg(drag.current.x - drag.start.x, drag.current.y - drag.start.y),
            heightMm,
            thicknessMm: WALL_THICKNESS_MM,
          });
        } else if (onAddRun) {
          onAddRun(drag.start, drag.current);
        }
      }
      setTool('select');
    }
    if (drag.mode === 'move-endpoint') {
      const segment = runsAsSegments.find((s) => s.run.id === drag.runId);
      if (segment && onUpdateRunGeometry) {
        const start = drag.endpoint === 'start' ? drag.current : segment.start;
        const end = drag.endpoint === 'end' ? drag.current : segment.end;
        const dx = end.x - start.x;
        const dy = end.y - start.y;
        onUpdateRunGeometry(drag.runId, {
          lengthMm: Math.max(100, Math.round(Math.hypot(dx, dy))),
          originX: start.x,
          originY: start.y,
          rotationDeg: angleDeg(dx, dy),
        });
      }
    }
    setDrag({ mode: 'idle' });
    event.currentTarget.releasePointerCapture(event.pointerId);
  };

  const handleWheel = useCallback((event: WheelEvent) => {
    event.preventDefault();
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect) return;
    const factor = event.deltaY < 0 ? 1.15 : 1 / 1.15;
    const cx = event.clientX - rect.left - rect.width / 2;
    const cy = event.clientY - rect.top - rect.height / 2;
    setViewport((vp) => {
      const next = Math.min(0.5, Math.max(0.005, vp.zoom * factor));
      return {
        panX: cx - ((cx - vp.panX) * next) / vp.zoom,
        panY: cy - ((cy - vp.panY) * next) / vp.zoom,
        zoom: next,
      };
    });
  }, []);

  useEffect(() => {
    const svg = svgRef.current;
    if (!svg) return;
    svg.addEventListener('wheel', handleWheel, { passive: false });
    return () => svg.removeEventListener('wheel', handleWheel);
  }, [handleWheel]);

  const handleFit = () => {
    const points: Vec[] = [
      ...runsAsSegments.flatMap(({ start, end }) => [start, end]),
      ...wallsAsSegments.flatMap(({ start, end }) => [start, end]),
      ...slabsAsPolygons.flatMap(({ points: slabPoints }) => slabPoints),
    ];
    if (points.length === 0) {
      setViewport({ panX: 0, panY: 0, zoom: 0.05 });
      return;
    }
    let minX = Infinity;
    let minY = Infinity;
    let maxX = -Infinity;
    let maxY = -Infinity;
    for (const p of points) {
      minX = Math.min(minX, p.x);
      minY = Math.min(minY, p.y);
      maxX = Math.max(maxX, p.x);
      maxY = Math.max(maxY, p.y);
    }
    const pad = 1000;
    minX -= pad;
    minY -= pad;
    maxX += pad;
    maxY += pad;
    const worldW = Math.max(1, maxX - minX);
    const worldH = Math.max(1, maxY - minY);
    const zoom = Math.min(size.width / worldW, size.height / worldH, 0.5);
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;
    setViewport({ panX: -centerX * zoom, panY: -centerY * zoom, zoom });
  };

  const transform = `translate(${size.width / 2 + viewport.panX} ${size.height / 2 + viewport.panY}) scale(${viewport.zoom})`;
  const gridSpacingMajor = GRID_MAJOR_MM * viewport.zoom;
  const gridSpacingMinor = GRID_MINOR_MM * viewport.zoom;
  const showMinorGrid = gridSpacingMinor > 5;

  return (
    <div
      ref={containerRef}
      className="relative h-full w-full overflow-hidden bg-slate-100 dark:bg-slate-950"
    >
      <svg
        ref={svgRef}
        className="h-full w-full"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        style={{
          touchAction: 'none',
          cursor: tool !== 'select' ? 'crosshair' : drag.mode === 'pan' ? 'grabbing' : 'default',
        }}
      >
        <defs>
          <pattern
            id="grid-minor"
            width={gridSpacingMinor}
            height={gridSpacingMinor}
            patternUnits="userSpaceOnUse"
          >
            <path
              d={`M ${gridSpacingMinor} 0 L 0 0 0 ${gridSpacingMinor}`}
              fill="none"
              stroke="currentColor"
              strokeWidth="0.5"
              className="text-slate-300/40 dark:text-slate-700/30"
            />
          </pattern>
          <pattern
            id="grid-major"
            width={gridSpacingMajor}
            height={gridSpacingMajor}
            patternUnits="userSpaceOnUse"
          >
            {showMinorGrid && <rect width="100%" height="100%" fill="url(#grid-minor)" />}
            <path
              d={`M ${gridSpacingMajor} 0 L 0 0 0 ${gridSpacingMajor}`}
              fill="none"
              stroke="currentColor"
              strokeWidth="1"
              className="text-slate-400/60 dark:text-slate-600/60"
            />
          </pattern>
        </defs>
        <rect width="100%" height="100%" fill="url(#grid-major)" />
        <g transform={transform}>
          <line x1={-50000} x2={50000} y1={0} y2={0} stroke="#94a3b8" strokeWidth={20} />
          <line x1={0} x2={0} y1={-50000} y2={50000} stroke="#94a3b8" strokeWidth={20} />

          {slabsAsPolygons.map(({ slab, points }) => (
            <SlabShape
              key={slab.id}
              slab={slab}
              points={points}
              selected={selectedSlabId === slab.id}
              selectable={tool === 'select'}
              onSelect={() =>
                setSelection({
                  kind: 'slab',
                  runId: null,
                  panelId: null,
                  connectionId: null,
                  hardwareId: null,
                  wallId: null,
                  slabId: slab.id,
                })
              }
            />
          ))}

          {wallsAsSegments.map(({ wall, start, end }) => (
            <WallSegment
              key={wall.id}
              wall={wall}
              start={start}
              end={end}
              selected={selectedWallId === wall.id}
              selectable={tool === 'select'}
              onSelect={() =>
                setSelection({
                  kind: 'wall',
                  runId: null,
                  panelId: null,
                  connectionId: null,
                  hardwareId: null,
                  wallId: wall.id,
                })
              }
            />
          ))}

          {runsAsSegments.map(({ run, start, end }) => (
            <RunSegment
              key={run.id}
              run={run}
              start={start}
              end={end}
              selected={selectedRunId === run.id}
              zoom={viewport.zoom}
              onSelect={() =>
                setSelection({ kind: 'run', runId: run.id, panelId: null, connectionId: null })
              }
              onGrabEndpoint={(endpoint, point) =>
                setDrag({ mode: 'move-endpoint', runId: run.id, endpoint, current: point })
              }
              previewPoint={
                drag.mode === 'move-endpoint' && drag.runId === run.id ? drag.current : null
              }
              previewEndpoint={
                drag.mode === 'move-endpoint' && drag.runId === run.id ? drag.endpoint : null
              }
            />
          ))}

          {adjacencyHits.map(({ runA, runB, pointA }, i) => (
            <g
              key={`adj-${i}`}
              transform={`translate(${pointA.x} ${pointA.y})`}
              onClick={(e) => {
                e.stopPropagation();
                onSelectConnectionCandidate?.(runA.id, runB.id);
              }}
              className="cursor-pointer"
            >
              <circle
                r={120}
                fill="rgba(245, 158, 11, 0.18)"
                stroke="#f59e0b"
                strokeWidth={20}
                strokeDasharray="60 30"
              />
              <circle r={40} fill="#f59e0b" />
            </g>
          ))}

          {drag.mode === 'draw' && (
            <g>
              <line
                x1={drag.start.x}
                y1={drag.start.y}
                x2={drag.current.x}
                y2={drag.current.y}
                stroke="#2563eb"
                strokeWidth={60}
                strokeDasharray="120 60"
              />
              <circle cx={drag.start.x} cy={drag.start.y} r={80} fill="#2563eb" />
              <circle cx={drag.current.x} cy={drag.current.y} r={80} fill="#2563eb" />
              <text
                x={(drag.start.x + drag.current.x) / 2}
                y={(drag.start.y + drag.current.y) / 2 - 180}
                textAnchor="middle"
                fill="#1e3a8a"
                fontSize={300}
                fontFamily="ui-monospace, monospace"
              >
                {lenMm(drag.start, drag.current)} mm
              </text>
            </g>
          )}
        </g>
      </svg>

      <div className="absolute left-3 top-3 flex flex-col gap-1.5 rounded-lg border border-slate-200 bg-white/95 p-1.5 shadow dark:border-slate-700 dark:bg-slate-900/95">
        <ToolButton
          active={tool === 'select'}
          onClick={() => setTool('select')}
          icon={<MoveDiagonal2 size={16} />}
          label={t('GlassEnclosure.Plan2D.Select')}
        />
        <ToolButton
          active={tool === 'draw'}
          onClick={() => setTool('draw')}
          icon={<Pencil size={16} />}
          label={t('GlassEnclosure.Plan2D.Draw')}
        />
        <ToolButton
          active={tool === 'wall'}
          onClick={() => setTool('wall')}
          icon={<BrickWall size={16} />}
          label={t('GlassEnclosure.Plan2D.WallMode', { defaultValue: 'Duvar çiz' })}
        />
        <button
          type="button"
          onClick={handleFit}
          className="rounded p-1.5 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          aria-label={t('GlassEnclosure.Plan2D.Fit')}
          title={t('GlassEnclosure.Plan2D.Fit')}
        >
          <Square size={16} />
        </button>
        <button
          type="button"
          onClick={() => setViewport((vp) => ({ ...vp, zoom: Math.min(0.5, vp.zoom * 1.2) }))}
          className="rounded p-1.5 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          aria-label={t('GlassEnclosure.Plan2D.ZoomIn')}
          title={t('GlassEnclosure.Plan2D.ZoomIn')}
        >
          <Plus size={16} />
        </button>
        <button
          type="button"
          onClick={() => setViewport((vp) => ({ ...vp, zoom: Math.max(0.005, vp.zoom / 1.2) }))}
          className="rounded p-1.5 text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          aria-label={t('GlassEnclosure.Plan2D.ZoomOut')}
          title={t('GlassEnclosure.Plan2D.ZoomOut')}
        >
          <Minus size={16} />
        </button>
      </div>

      <div className="absolute bottom-3 left-3 rounded-md bg-white/90 px-2 py-1 text-xs font-mono text-slate-600 shadow dark:bg-slate-900/90 dark:text-slate-300">
        {Math.round(viewport.zoom * 1000) / 10}% · snap {SNAP_MM}mm
      </div>
      {tool === 'draw' && (
        <div className="absolute bottom-3 right-3 rounded-md bg-primary-600 px-3 py-1.5 text-xs font-medium text-white shadow">
          {t('GlassEnclosure.Plan2D.DrawHint')}
        </div>
      )}
    </div>
  );
}

interface WallSegmentProps {
  wall: SceneWallState;
  start: Vec;
  end: Vec;
  selected: boolean;
  selectable: boolean;
  onSelect: () => void;
}

function WallSegment({ wall, start, end, selected, selectable, onSelect }: WallSegmentProps) {
  const direction = rotateVec(1, 0, wall.rotationDeg);
  const openings = wallOpeningSpans(wall);
  return (
    <g
      className={selectable ? 'cursor-pointer' : undefined}
      onClick={selectable ? onSelect : undefined}
    >
      <line
        x1={start.x}
        y1={start.y}
        x2={end.x}
        y2={end.y}
        stroke={selected ? '#1d4ed8' : '#475569'}
        strokeWidth={wall.thicknessMm}
        strokeOpacity={0.85}
        strokeLinecap="butt"
      />
      {openings.map((opening) => (
        <line
          key={opening.id}
          x1={start.x + direction.x * opening.fromMm}
          y1={start.y + direction.y * opening.fromMm}
          x2={start.x + direction.x * opening.toMm}
          y2={start.y + direction.y * opening.toMm}
          stroke={opening.kind === 'door' ? '#f8fafc' : '#cbd5e1'}
          strokeWidth={wall.thicknessMm}
          strokeDasharray={opening.kind === 'door' ? '160 90' : undefined}
          strokeLinecap="butt"
        />
      ))}
    </g>
  );
}

interface SlabShapeProps {
  slab: SceneSlabState;
  points: Vec[];
  selected: boolean;
  selectable: boolean;
  onSelect: () => void;
}

function SlabShape({ slab, points, selected, selectable, onSelect }: SlabShapeProps) {
  return (
    <polygon
      points={points.map((p) => `${p.x},${p.y}`).join(' ')}
      fill="#94a3b8"
      fillOpacity={slab.kind === 'roof' ? 0.25 : 0.18}
      stroke={selected ? '#1d4ed8' : '#94a3b8'}
      strokeOpacity={selected ? 1 : 0.5}
      strokeWidth={selected ? 60 : 20}
      className={selectable ? 'cursor-pointer' : undefined}
      onClick={selectable ? onSelect : undefined}
    />
  );
}

interface RunSegmentProps {
  run: SceneRunState;
  start: Vec;
  end: Vec;
  selected: boolean;
  zoom: number;
  onSelect: () => void;
  onGrabEndpoint: (endpoint: 'start' | 'end', point: Vec) => void;
  previewPoint: Vec | null;
  previewEndpoint: 'start' | 'end' | null;
}

function RunSegment({
  run,
  start,
  end,
  selected,
  zoom,
  onSelect,
  onGrabEndpoint,
  previewPoint,
  previewEndpoint,
}: RunSegmentProps) {
  const displayStart = previewEndpoint === 'start' && previewPoint ? previewPoint : start;
  const displayEnd = previewEndpoint === 'end' && previewPoint ? previewPoint : end;
  const stroke = selected ? '#2563eb' : '#0f172a';
  const fillColor = selected ? '#dbeafe' : '#cbd5e1';
  const labelMidX = (displayStart.x + displayEnd.x) / 2;
  const labelMidY = (displayStart.y + displayEnd.y) / 2;
  const fontSize = Math.max(180, 24 / zoom);
  const endpointRadius = Math.max(60, 12 / zoom);
  const arc = arcRenderParams(run);

  return (
    <g onClick={onSelect} className="cursor-pointer">
      {arc ? (
        <path
          d={`M ${displayStart.x} ${displayStart.y} A ${arc.radiusMm} ${arc.radiusMm} 0 ${arc.largeArcFlag} ${arc.sweepFlag} ${displayEnd.x} ${displayEnd.y}`}
          fill="none"
          stroke={stroke}
          strokeWidth={selected ? 90 : 60}
        />
      ) : (
        <line
          x1={displayStart.x}
          y1={displayStart.y}
          x2={displayEnd.x}
          y2={displayEnd.y}
          stroke={stroke}
          strokeWidth={selected ? 90 : 60}
        />
      )}
      <circle
        cx={displayStart.x}
        cy={displayStart.y}
        r={endpointRadius}
        fill={fillColor}
        stroke={stroke}
        strokeWidth={20}
        onPointerDown={(e) => {
          e.stopPropagation();
          onGrabEndpoint('start', displayStart);
        }}
      />
      <circle
        cx={displayEnd.x}
        cy={displayEnd.y}
        r={endpointRadius}
        fill={fillColor}
        stroke={stroke}
        strokeWidth={20}
        onPointerDown={(e) => {
          e.stopPropagation();
          onGrabEndpoint('end', displayEnd);
        }}
      />
      <text
        x={labelMidX}
        y={labelMidY - 200}
        textAnchor="middle"
        fill={stroke}
        fontSize={fontSize}
        fontFamily="ui-monospace, monospace"
      >
        {run.label} · {run.lengthMm} mm
      </text>
    </g>
  );
}

function ToolButton({
  active,
  onClick,
  icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded p-1.5 transition ${
        active
          ? 'bg-primary-600 text-white'
          : 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800'
      }`}
      aria-pressed={active}
      title={label}
    >
      {icon}
    </button>
  );
}
