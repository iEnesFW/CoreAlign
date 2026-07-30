import { useTranslation } from 'react-i18next';
import { FileDown, RotateCw } from 'lucide-react';
import type {
  CuttingPattern1DDto,
  CuttingReportDto,
  CuttingSheet2DDto,
} from '../model/engineering.types';
import {
  panelShapeToken,
  placedPanelPolygonMm,
  placedPanelPolygonPoints,
  type PanelShapeToken,
} from '../cutting/placedPanelOutline';

const SHAPE_KEY: Record<PanelShapeToken, string> = {
  raked: 'GlassEnclosure.Cutting.Shape.Raked',
  arched: 'GlassEnclosure.Cutting.Shape.Arched',
  rounded: 'GlassEnclosure.Cutting.Shape.Rounded',
  ellipse: 'GlassEnclosure.Cutting.Shape.Ellipse',
  polygon: 'GlassEnclosure.Cutting.Shape.Polygon',
};

interface CuttingReportViewProps {
  report: CuttingReportDto | null;
  onRegenerate: () => void;
  isGenerating: boolean;
}

export function CuttingReportView({ report, onRegenerate, isGenerating }: CuttingReportViewProps) {
  const { t, i18n } = useTranslation();
  const dateFormatter = new Intl.DateTimeFormat(i18n.language, {
    dateStyle: 'short',
    timeStyle: 'short',
  });

  return (
    <section className="flex flex-col gap-4 p-4">
      <header className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.Cutting.Title')}
          </h2>
          {report && (
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {dateFormatter.format(new Date(report.generatedAtUtc))}
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={onRegenerate}
          disabled={isGenerating}
          className="inline-flex items-center gap-1.5 rounded-md bg-primary-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
        >
          <RotateCw size={14} className={isGenerating ? 'animate-spin' : ''} />
          {t('GlassEnclosure.Cutting.Regenerate')}
        </button>
      </header>

      {!report && (
        <div className="rounded-lg border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
          {t('GlassEnclosure.Cutting.NoReport')}
        </div>
      )}

      {report && (
        <>
          <ProfileSection report={report} />
          <GlassSection report={report} />
        </>
      )}
    </section>
  );
}

const ProfileSection = ({ report }: { report: CuttingReportDto }) => {
  const { t } = useTranslation();
  const r = report.profile1D;
  return (
    <section className="space-y-2">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Cutting.Profile1D')}
        </h3>
        <button
          type="button"
          className="inline-flex items-center gap-1 text-xs text-primary-600 hover:underline"
          onClick={() => downloadCsv(r, 'profile-cutting-plan')}
        >
          <FileDown size={12} />
          {t('GlassEnclosure.Cutting.ExportCsv')}
        </button>
      </div>

      <Stats
        stats={[
          { label: t('GlassEnclosure.Cutting.StockBar'), value: `${r.stockBarLengthMm} mm` },
          { label: t('GlassEnclosure.Cutting.Bars'), value: r.totalBars.toString() },
          { label: t('GlassEnclosure.Cutting.Cuts'), value: r.totalCuts.toString() },
          {
            label: t('GlassEnclosure.Cutting.Utilization'),
            value: `${r.utilizationPercent.toFixed(1)}%`,
          },
          { label: t('GlassEnclosure.Cutting.Waste'), value: `${r.totalWasteMm} mm` },
        ]}
      />

      <div className="space-y-2">
        {(r.patterns ?? []).map((pattern) => (
          <BarVisualizer key={pattern.barIndex} pattern={pattern} />
        ))}
      </div>
    </section>
  );
};

const BarVisualizer = ({ pattern }: { pattern: CuttingPattern1DDto }) => {
  const { t } = useTranslation();
  const total = pattern.stockBarLengthMm;
  return (
    <div className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
      <div className="mb-1 flex items-center justify-between text-xs text-slate-600 dark:text-slate-300">
        <span className="font-mono">
          Bar #{pattern.barIndex} · {total} mm
        </span>
        <span className="text-warning-600 dark:text-warning-400">↳ {pattern.wasteMm} mm fire</span>
      </div>
      <div className="relative h-7 w-full overflow-hidden rounded bg-slate-100 dark:bg-slate-900">
        {(pattern.cuts ?? []).map((cut) => {
          const left = (cut.offsetMm / total) * 100;
          const width = (cut.lengthMm / total) * 100;
          const pieceCount = cut.pieceCount ?? 1;
          const pieceIndex = cut.pieceIndex ?? 1;
          const spliced = pieceCount > 1;
          const spliceLabel = t('GlassEnclosure.Cutting.SplicePiece', {
            index: pieceIndex,
            count: pieceCount,
          });
          return (
            <div
              key={`${cut.label}-${cut.offsetMm}`}
              className="absolute top-0 h-full border-r border-slate-300 bg-gradient-to-b from-primary-400/40 to-primary-500/60 px-1 text-[10px] font-mono text-primary-900 dark:from-primary-400/30 dark:to-primary-500/50 dark:text-primary-100"
              style={{ left: `${left}%`, width: `${width}%` }}
              title={
                spliced
                  ? `${cut.label} · ${cut.lengthMm}mm · ${spliceLabel}`
                  : `${cut.label} · ${cut.lengthMm}mm`
              }
            >
              <span className="truncate">{cut.lengthMm}</span>
              {spliced && (
                <span className="ml-1 rounded bg-warning-500/20 px-1 text-[9px] font-semibold text-warning-800 dark:text-warning-300">
                  {spliceLabel}
                </span>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

const GlassSection = ({ report }: { report: CuttingReportDto }) => {
  const { t } = useTranslation();
  const r = report.glass2D;
  return (
    <section className="space-y-2">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Cutting.Glass2D')}
          {r.guillotineOnly && (
            <span className="ml-2 rounded bg-purple-500/10 px-1.5 py-0.5 text-[10px] font-medium text-purple-700 dark:bg-purple-500/20 dark:text-purple-300">
              {t('GlassEnclosure.Cutting.Guillotine', { defaultValue: 'Guillotine' })}
            </span>
          )}
        </h3>
        <button
          type="button"
          className="inline-flex items-center gap-1 text-xs text-primary-600 hover:underline"
          onClick={() => downloadDxf(r)}
        >
          <FileDown size={12} />
          {t('GlassEnclosure.Cutting.ExportDxf')}
        </button>
      </div>

      <Stats
        stats={[
          {
            label: t('GlassEnclosure.Cutting.Jumbo'),
            value: `${r.sheetWidthMm} × ${r.sheetHeightMm} mm`,
          },
          { label: t('GlassEnclosure.Cutting.Sheets'), value: r.totalSheets.toString() },
          {
            label: t('GlassEnclosure.Cutting.Utilization'),
            value: `${r.utilizationPercent.toFixed(1)}%`,
          },
          {
            label: t('GlassEnclosure.Cutting.Waste'),
            value: `${(r.totalWasteMm2 / 1_000_000).toFixed(2)} m²`,
          },
        ]}
      />

      {(r.groups?.length ?? 0) > 1 && (
        <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
              <tr>
                <th scope="col" className="px-2 py-1 font-medium">
                  {t('GlassEnclosure.Cutting.GlassGroup')}
                </th>
                <th scope="col" className="px-2 py-1 font-medium">
                  {t('GlassEnclosure.Cutting.Sheets')}
                </th>
                <th scope="col" className="px-2 py-1 font-medium">
                  {t('GlassEnclosure.Cutting.Utilization')}
                </th>
                <th scope="col" className="px-2 py-1 font-medium">
                  {t('GlassEnclosure.Cutting.Waste')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(r.groups ?? []).map((g) => (
                <tr
                  key={g.groupKey ?? 'default'}
                  className="border-t border-slate-200 dark:border-slate-700"
                >
                  <td className="px-2 py-1 font-mono text-slate-800 dark:text-slate-100">
                    {g.groupKey ?? '—'}
                  </td>
                  <td className="px-2 py-1 font-mono">{g.totalSheets}</td>
                  <td className="px-2 py-1 font-mono">{g.utilizationPercent.toFixed(1)}%</td>
                  <td className="px-2 py-1 font-mono">
                    {(g.totalWasteMm2 / 1_000_000).toFixed(2)} m²
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(r.unplaced?.length ?? 0) > 0 && (
        <div className="rounded border border-danger-500/40 bg-danger-50 p-2 text-xs text-danger-700 dark:bg-danger-950/30 dark:text-danger-300">
          {t('GlassEnclosure.Cutting.UnplacedWarning', {
            count: r.unplaced?.length ?? 0,
            defaultValue: `${r.unplaced?.length ?? 0} unplaced`,
          })}
        </div>
      )}

      {(r.sheets?.length ?? 0) === 0 ? (
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Cutting.NoReport')}
        </p>
      ) : (
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {r.sheets.map((sheet) => (
            <SheetVisualizer key={sheet.sheetIndex} sheet={sheet} />
          ))}
        </div>
      )}
    </section>
  );
};

const SheetVisualizer = ({ sheet }: { sheet: CuttingSheet2DDto }) => {
  const { t } = useTranslation();
  const wasteM2 = sheet.wasteMm2 / 1_000_000;
  return (
    <div className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
      <div className="mb-1 flex items-center justify-between text-xs text-slate-600 dark:text-slate-300">
        <span className="font-mono">
          Jumbo #{sheet.sheetIndex} · {sheet.widthMm} × {sheet.heightMm} mm
          {sheet.groupKey && ` · ${sheet.groupKey}`}
        </span>
        <span className="text-warning-600 dark:text-warning-400">
          ↳ {wasteM2.toFixed(2)} m² fire
        </span>
      </div>
      <svg
        viewBox={`0 0 ${sheet.widthMm} ${sheet.heightMm}`}
        className="block w-full rounded border border-slate-300 bg-slate-50 dark:border-slate-600 dark:bg-slate-900"
        preserveAspectRatio="xMidYMid meet"
        style={{ aspectRatio: `${sheet.widthMm} / ${sheet.heightMm}` }}
      >
        {(sheet.placements ?? []).map((p, i) => {
          const fontSize = Math.max(80, Math.min(p.widthMm, p.heightMm) / 8);
          const polygon = placedPanelPolygonPoints(p);
          const token = panelShapeToken(p.shape);
          return (
            <g key={`${p.label}-${i}`}>
              {token && p.shape && (
                <title>
                  {t(SHAPE_KEY[token])} · {(p.shape.netAreaMm2 / 1_000_000).toFixed(3)} m²
                </title>
              )}
              {polygon ? (
                <polygon
                  points={polygon}
                  fill="rgba(59, 130, 246, 0.45)"
                  stroke="#1e3a8a"
                  strokeWidth={6}
                />
              ) : (
                <rect
                  x={p.x}
                  y={p.y}
                  width={p.widthMm}
                  height={p.heightMm}
                  fill="rgba(59, 130, 246, 0.45)"
                  stroke="#1e3a8a"
                  strokeWidth={6}
                />
              )}
              <text
                x={p.x + p.widthMm / 2}
                y={p.y + p.heightMm / 2}
                textAnchor="middle"
                dominantBaseline="middle"
                fontSize={fontSize}
                fill="currentColor"
                className="text-slate-900 dark:text-slate-100"
                fontFamily="ui-monospace, monospace"
              >
                {p.widthMm} × {p.heightMm}
                {p.rotated && ' ⟲'}
              </text>
              {token && (
                <text
                  x={p.x + p.widthMm / 2}
                  y={p.y + p.heightMm / 2 + fontSize * 1.25}
                  textAnchor="middle"
                  dominantBaseline="middle"
                  fontSize={fontSize * 0.8}
                  fill="currentColor"
                  className="text-slate-900 dark:text-slate-100"
                  fontFamily="ui-monospace, monospace"
                >
                  {t(SHAPE_KEY[token])}
                </text>
              )}
            </g>
          );
        })}
      </svg>
    </div>
  );
};

const Stats = ({ stats }: { stats: { label: string; value: string }[] }) => (
  <dl className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">
    {stats.map((s) => (
      <div
        key={s.label}
        className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800"
      >
        <dt className="truncate text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {s.label}
        </dt>
        <dd className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
          {s.value}
        </dd>
      </div>
    ))}
  </dl>
);

const downloadCsv = (r: CuttingReportDto['profile1D'], filename: string) => {
  const lines: string[] = ['bar_no,position_mm,length_mm,label,piece_index,piece_count'];
  for (const pattern of r.patterns ?? []) {
    for (const cut of pattern.cuts ?? []) {
      lines.push(
        `${pattern.barIndex},${cut.offsetMm},${cut.lengthMm},${cut.label},${cut.pieceIndex ?? 1},${cut.pieceCount ?? 1}`,
      );
    }
  }
  const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8' });
  triggerDownload(blob, `${filename}.csv`);
};

const downloadDxf = (r: CuttingReportDto['glass2D']) => {
  const parts: string[] = ['0', 'SECTION', '2', 'ENTITIES'];
  for (const sheet of r.sheets ?? []) {
    for (const p of sheet.placements ?? []) {
      const x = p.x;
      const y = sheet.heightMm - p.y - p.heightMm;
      // WHY: a raked/arched/elliptical/polygon panel is NOT its blank rectangle — cutting the DXF
      // as a rect wastes the offcut and mis-cuts the piece. The viewer already draws the true
      // silhouette from the same helper; the export used to be the only rectangle-only consumer.
      const outline = placedPanelPolygonMm(p);
      const points = outline
        ? // The helper works in SVG (y-down) sheet space; DXF is y-up. Close the loop explicitly.
          [...outline, outline[0]].map((pt) => [pt.x, sheet.heightMm - pt.y] as const)
        : ([
            [x, y],
            [x + p.widthMm, y],
            [x + p.widthMm, y + p.heightMm],
            [x, y + p.heightMm],
            [x, y],
          ] as const);
      parts.push(
        '0',
        'LWPOLYLINE',
        '8',
        `SHEET-${sheet.sheetIndex}`,
        '90',
        points.length.toString(),
        '70',
        '1',
      );
      for (const [px, py] of points) {
        parts.push('10', px.toString(), '20', py.toString());
      }
    }
  }
  parts.push('0', 'ENDSEC', '0', 'EOF');
  const blob = new Blob([parts.join('\n')], { type: 'application/dxf' });
  triggerDownload(blob, 'glass-cutting-plan.dxf');
};

const triggerDownload = (blob: Blob, filename: string) => {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
};
