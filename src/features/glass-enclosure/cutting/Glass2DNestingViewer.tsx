import { useTranslation } from 'react-i18next';
import { AlertTriangle, Printer } from 'lucide-react';
import type { Glass2DNestingReportDto, Glass2DPlacedSheetDto } from '../model/engineering.types';
import {
  panelShapeToken,
  placedPanelPolygonPoints,
  type PanelShapeToken,
} from './placedPanelOutline';

const SHAPE_KEY: Record<PanelShapeToken, string> = {
  raked: 'GlassEnclosure.Cutting.Shape.Raked',
  arched: 'GlassEnclosure.Cutting.Shape.Arched',
  rounded: 'GlassEnclosure.Cutting.Shape.Rounded',
  ellipse: 'GlassEnclosure.Cutting.Shape.Ellipse',
  polygon: 'GlassEnclosure.Cutting.Shape.Polygon',
};

interface Glass2DNestingViewerProps {
  report: Glass2DNestingReportDto | null;
}

const PALETTE = [
  '#3b82f6',
  '#10b981',
  '#f59e0b',
  '#ef4444',
  '#8b5cf6',
  '#06b6d4',
  '#ec4899',
  '#84cc16',
];

const colorForPanel = (panelId: string) => {
  let hash = 0;
  for (let i = 0; i < panelId.length; i += 1) {
    hash = (hash * 31 + panelId.charCodeAt(i)) % 1_000_000;
  }
  return PALETTE[hash % PALETTE.length];
};

export function Glass2DNestingViewer({ report }: Glass2DNestingViewerProps) {
  const { t } = useTranslation();

  if (!report) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
        {t('GlassEnclosure.Cutting.Nesting.NoReport')}
      </div>
    );
  }

  const totalWasteM2 = report.totalWasteAreaMm2 / 1_000_000;

  return (
    <section className="flex flex-col gap-4">
      <header className="flex flex-col items-start justify-between gap-2 sm:flex-row sm:items-center">
        <div>
          <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
            {t('GlassEnclosure.Cutting.Nesting.Title')}
          </h3>
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {report.algorithm} · {report.heuristic}
          </p>
        </div>
        <button
          type="button"
          onClick={() => window.print()}
          className="inline-flex items-center gap-1.5 rounded-md border border-slate-300 px-2 py-1 text-xs text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
        >
          <Printer size={12} />
          {t('GlassEnclosure.Cutting.Nesting.PrintExport')}
        </button>
      </header>

      <dl className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <Stat label={t('GlassEnclosure.Cutting.Nesting.Algorithm')} value={report.algorithm} />
        <Stat
          label={t('GlassEnclosure.Cutting.Nesting.SheetsUsed')}
          value={report.sheetsUsed.toString()}
        />
        <Stat
          label={t('GlassEnclosure.Cutting.Nesting.Utilization')}
          value={`${report.totalUtilizationPercent.toFixed(1)}%`}
        />
        <Stat
          label={t('GlassEnclosure.Cutting.Nesting.Waste')}
          value={`${totalWasteM2.toFixed(2)} m²`}
        />
      </dl>

      {report.unplacedPanels.length > 0 && <UnplacedList unplaced={report.unplacedPanels} />}

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        {report.sheets.map((sheet) => (
          <SheetCard key={sheet.sheetId} sheet={sheet} />
        ))}
      </div>
    </section>
  );
}

const Stat = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
    <dt className="truncate text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </dt>
    <dd className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">{value}</dd>
  </div>
);

const UnplacedList = ({ unplaced }: { unplaced: Glass2DNestingReportDto['unplacedPanels'] }) => {
  const { t } = useTranslation();
  return (
    <div className="rounded border border-danger-500/40 bg-danger-50 p-2 text-xs text-danger-700 dark:bg-danger-950/30 dark:text-danger-300">
      <div className="mb-1 flex items-center gap-1 font-semibold">
        <AlertTriangle size={12} />
        {t('GlassEnclosure.Cutting.Nesting.UnplacedPanels', { count: unplaced.length })}
      </div>
      <ul className="space-y-0.5 pl-4 font-mono">
        {unplaced.map((u, idx) => (
          <li key={`${u.panelId}-${idx}`}>
            {u.label} · {u.widthMm}×{u.heightMm} mm · {u.reason}
          </li>
        ))}
      </ul>
    </div>
  );
};

const SheetCard = ({ sheet }: { sheet: Glass2DPlacedSheetDto }) => {
  const { t } = useTranslation();
  const wasteM2 = sheet.wasteAreaMm2 / 1_000_000;
  return (
    <div className="rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
      <div className="mb-1 flex items-center justify-between text-xs text-slate-600 dark:text-slate-300">
        <span className="font-mono">
          #{sheet.sheetIndex} · {sheet.sheetWidthMm}×{sheet.sheetHeightMm} mm
        </span>
        <span className="flex items-center gap-2">
          <span className="font-mono text-success-600 dark:text-success-400">
            {sheet.utilizationPercent.toFixed(1)}%
          </span>
          <span className="font-mono text-warning-600 dark:text-warning-400">
            ↳ {wasteM2.toFixed(2)} m²
          </span>
        </span>
      </div>
      <svg
        viewBox={`0 0 ${sheet.sheetWidthMm} ${sheet.sheetHeightMm}`}
        className="block w-full rounded border border-slate-300 bg-slate-50 dark:border-slate-600 dark:bg-slate-900"
        preserveAspectRatio="xMidYMid meet"
        style={{ aspectRatio: `${sheet.sheetWidthMm} / ${sheet.sheetHeightMm}` }}
      >
        {sheet.panels.map((p, i) => {
          const fill = colorForPanel(p.panelId);
          const fontSize = Math.max(60, Math.min(p.widthMm, p.heightMm) / 8);
          const polygon = placedPanelPolygonPoints(p);
          const token = panelShapeToken(p.shape);
          return (
            <g key={`${p.panelId}-${i}`}>
              {token && p.shape && (
                <title>
                  {t(SHAPE_KEY[token])} · {(p.shape.netAreaMm2 / 1_000_000).toFixed(3)} m²
                </title>
              )}
              {polygon ? (
                <polygon
                  points={polygon}
                  fill={fill}
                  fillOpacity={0.5}
                  stroke={fill}
                  strokeWidth={6}
                />
              ) : (
                <rect
                  x={p.x}
                  y={p.y}
                  width={p.widthMm}
                  height={p.heightMm}
                  fill={fill}
                  fillOpacity={0.5}
                  stroke={fill}
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
                {p.widthMm}×{p.heightMm}
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
