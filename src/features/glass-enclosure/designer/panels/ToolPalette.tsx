import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  BrickWall,
  Circle,
  Egg,
  Eraser,
  Hexagon,
  Home,
  LassoSelect,
  Layers,
  LayoutTemplate,
  Minus,
  MousePointer2,
  Move,
  MoveHorizontal,
  Paintbrush,
  PenTool,
  RectangleHorizontal,
  RotateCw,
  Ruler,
  Save,
  Shapes,
  Spline,
  Square,
  SquareSplitHorizontal,
  Trash2,
  Triangle,
  Wand2,
} from 'lucide-react';
import { PROCEDURAL_MATERIAL_KEYS } from '@/shared/three-engine';
import { queueToast } from '@/shared/api/toastQueue';
import { cn } from '@/shared/lib/cn';
import {
  useDesignerStore,
  type DesignerTool,
  type PenIntent,
  type PenMode,
  type PlacementKind,
  type WallDrawShape,
} from '@/features/glass-enclosure/model/designerStore';
import { useWallAutofill } from '@/features/glass-enclosure/hooks/useWallAutofill';
import { useTemplateInsert } from '@/features/glass-enclosure/hooks/useTemplateInsert';
import { useUserGlassTemplates } from '@/features/glass-enclosure/hooks/useUserGlassTemplates';
import { useColorOptionsQuery } from '@/features/glass-enclosure/hooks/useGlassEnclosureQueries';
import type { GlassTemplateKey } from '@/features/glass-enclosure/model/templates';

interface ToolDef {
  tool: DesignerTool;
  labelKey: string;
  defaultLabel: string;
  shortcut: string;
  Icon: typeof Move;
}

const TOOLS: ToolDef[] = [
  { tool: 'select', labelKey: 'Select', defaultLabel: 'Seç', shortcut: 'V', Icon: MousePointer2 },
  {
    tool: 'multiselect',
    labelKey: 'MultiSelect',
    defaultLabel: 'Çoklu seç (lasso)',
    shortcut: 'L',
    Icon: LassoSelect,
  },
  { tool: 'move', labelKey: 'Move', defaultLabel: 'Taşı', shortcut: 'M', Icon: Move },
  { tool: 'rotate', labelKey: 'Rotate', defaultLabel: 'Döndür', shortcut: 'R', Icon: RotateCw },
  {
    tool: 'stretch',
    labelKey: 'Stretch',
    defaultLabel: 'Genişlet',
    shortcut: 'S',
    Icon: MoveHorizontal,
  },
  { tool: 'draw', labelKey: 'Draw', defaultLabel: 'Yüzeye çiz', shortcut: 'D', Icon: Shapes },
  { tool: 'paint', labelKey: 'Paint', defaultLabel: 'Boya', shortcut: 'B', Icon: Paintbrush },
  { tool: 'erase', labelKey: 'Erase', defaultLabel: 'Sil', shortcut: 'E', Icon: Eraser },
  { tool: 'measure', labelKey: 'Measure', defaultLabel: 'Ölç', shortcut: 'K', Icon: Ruler },
];

const PLACEMENTS: {
  kind: PlacementKind;
  labelKey: string;
  defaultLabel: string;
  shortcut: string;
  Icon: typeof Move;
}[] = [
  {
    kind: 'run',
    labelKey: 'AddRun',
    defaultLabel: 'Hat ekle',
    shortcut: '1',
    Icon: RectangleHorizontal,
  },
  { kind: 'wall', labelKey: 'AddWall', defaultLabel: 'Duvar ekle', shortcut: '2', Icon: BrickWall },
  { kind: 'floor', labelKey: 'AddFloor', defaultLabel: 'Zemin ekle', shortcut: '3', Icon: Square },
  { kind: 'roof', labelKey: 'AddRoof', defaultLabel: 'Çatı ekle', shortcut: '4', Icon: Home },
  { kind: 'pen', labelKey: 'Pen', defaultLabel: 'Kalemle yüzey çiz', shortcut: 'P', Icon: PenTool },
];

const TEMPLATES: { key: GlassTemplateKey; labelKey: string; defaultLabel: string }[] = [
  { key: 'l-walls', labelKey: 'LWalls', defaultLabel: 'L duvar' },
  { key: 'u-walls', labelKey: 'UWalls', defaultLabel: 'U üç duvar' },
  { key: 'room', labelKey: 'Room', defaultLabel: 'Dört duvar (kapalı kutu)' },
  { key: 'gable-roof', labelKey: 'GableRoof', defaultLabel: 'Beşik çatı' },
  { key: 'barrel-roof', labelKey: 'BarrelRoof', defaultLabel: 'Tonoz çatı' },
  { key: 'arc-roof', labelKey: 'ArcRoof', defaultLabel: 'Kavisli çatı (plan arc)' },
  { key: 'arc-run', labelKey: 'ArcRun', defaultLabel: 'Kavisli cam hattı' },
];

const MATERIAL_FALLBACKS: Record<string, string> = {
  wood: 'Ahşap',
  marble: 'Mermer',
  concrete: 'Beton',
  panel: 'Panel',
  grass: 'Çim',
  asphalt: 'Asfalt',
  brick: 'Tuğla',
  plaster: 'Sıva',
};

const DRAW_SHAPES: {
  shape: WallDrawShape;
  labelKey: string;
  defaultLabel: string;
  Icon: typeof Move;
}[] = [
  { shape: 'rect', labelKey: 'ShapeRect', defaultLabel: 'Dikdörtgen', Icon: Square },
  { shape: 'circle', labelKey: 'ShapeCircle', defaultLabel: 'Daire', Icon: Circle },
  { shape: 'ellipse', labelKey: 'ShapeEllipse', defaultLabel: 'Oval', Icon: Egg },
  { shape: 'triangle', labelKey: 'ShapeTriangle', defaultLabel: 'Üçgen', Icon: Triangle },
  { shape: 'polygon', labelKey: 'ShapePolygon', defaultLabel: 'Çokgen', Icon: Hexagon },
  { shape: 'free', labelKey: 'ShapeFree', defaultLabel: 'Serbest çizim', Icon: PenTool },
  {
    shape: 'split',
    labelKey: 'ShapeSplit',
    defaultLabel: 'Duvarı böl',
    Icon: SquareSplitHorizontal,
  },
];

const PEN_INTENTS: {
  intent: PenIntent;
  labelKey: string;
  defaultLabel: string;
  Icon: typeof Move;
}[] = [
  {
    intent: 'opening',
    labelKey: 'PenIntentOpening',
    defaultLabel: 'Açıklık / delik',
    Icon: Square,
  },
  {
    intent: 'glassPanel',
    labelKey: 'PenIntentGlassPanel',
    defaultLabel: 'Cam paneli',
    Icon: Shapes,
  },
  {
    intent: 'divide',
    labelKey: 'PenIntentDivide',
    defaultLabel: 'Panel böl',
    Icon: SquareSplitHorizontal,
  },
];

const PEN_MODES: {
  penMode: PenMode;
  labelKey: string;
  defaultLabel: string;
  Icon: typeof Move;
}[] = [
  {
    penMode: 'clicked',
    labelKey: 'PenModeClicked',
    defaultLabel: 'Nokta nokta',
    Icon: MousePointer2,
  },
  { penMode: 'freehand', labelKey: 'PenModeFreehand', defaultLabel: 'Serbest çizim', Icon: Spline },
];

export function ToolPalette() {
  const { t } = useTranslation();
  const activeTool = useDesignerStore((s) => s.activeTool);
  const placement = useDesignerStore((s) => s.placement);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const drawShape = useDesignerStore((s) => s.drawShape);
  const penIntent = useDesignerStore((s) => s.penIntent);
  const setPenIntent = useDesignerStore((s) => s.setPenIntent);
  const penMode = useDesignerStore((s) => s.penMode);
  const setPenMode = useDesignerStore((s) => s.setPenMode);
  const stackOnDrop = useDesignerStore((s) => s.stackOnDrop);
  const toggleStackOnDrop = useDesignerStore((s) => s.toggleStackOnDrop);
  const setActiveTool = useDesignerStore((s) => s.setActiveTool);
  const setPlacement = useDesignerStore((s) => s.setPlacement);
  const setPaintColor = useDesignerStore((s) => s.setPaintColor);
  const setPaintMaterial = useDesignerStore((s) => s.setPaintMaterial);
  const setDrawShape = useDesignerStore((s) => s.setDrawShape);
  const placementShape = useDesignerStore((s) => s.placementShape);
  const setPlacementShape = useDesignerStore((s) => s.setPlacementShape);
  const { autofill } = useWallAutofill();
  const { insertTemplate } = useTemplateInsert();
  const {
    templates: userTemplates,
    saveCurrentAsTemplate,
    deleteTemplate,
    insertUserTemplate,
  } = useUserGlassTemplates();
  const [templatesOpen, setTemplatesOpen] = useState(false);
  const [savingTemplate, setSavingTemplate] = useState(false);
  const [templateName, setTemplateName] = useState('');
  const colorsQuery = useColorOptionsQuery();
  const colors = colorsQuery.data?.data ?? [];

  const commitSaveTemplate = async () => {
    const saved = await saveCurrentAsTemplate(templateName);
    setSavingTemplate(false);
    setTemplateName('');
    queueToast(
      saved
        ? {
            dedupeKey: 'glass-template-saved',
            variant: 'success',
            description: t('GlassEnclosure.Designer.Templates.Saved', {
              defaultValue: 'Şablon kaydedildi.',
            }),
          }
        : {
            dedupeKey: 'glass-template-save-empty',
            variant: 'warning',
            description: t('GlassEnclosure.Designer.Templates.SaveEmpty', {
              defaultValue: 'Boş çizim veya boş isim şablon olarak kaydedilemez.',
            }),
          },
    );
  };

  const label = (key: string, defaultValue: string) =>
    t(`GlassEnclosure.Designer.Tool.${key}`, { defaultValue });

  const withShortcut = (text: string, shortcut: string) => `${text} (${shortcut})`;

  return (
    <div className="pointer-events-none flex flex-col items-start gap-1.5">
      <div className="pointer-events-auto flex max-w-[18rem] flex-wrap items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
        {TOOLS.map(({ tool, labelKey, defaultLabel, shortcut, Icon }) => (
          <PaletteButton
            key={tool}
            title={withShortcut(label(labelKey, defaultLabel), shortcut)}
            active={activeTool === tool && !placement}
            onClick={() => setActiveTool(tool)}
          >
            <Icon size={15} />
          </PaletteButton>
        ))}
        <PaletteButton
          title={withShortcut(label('Autofill', 'Boşlukları camla doldur'), 'F')}
          active={false}
          onClick={() => void autofill()}
        >
          <Wand2 size={15} />
        </PaletteButton>
        <PaletteButton
          title={label(
            'StackOnDrop',
            'Üst üste bırak — sürüklediğin objeyi üzerine geldiğinin üstüne koyar (Alt ile de olur)',
          )}
          active={stackOnDrop}
          onClick={toggleStackOnDrop}
        >
          <Layers size={15} />
        </PaletteButton>
        <PaletteButton
          title={t('GlassEnclosure.Designer.Templates.Title', {
            defaultValue: 'Şablonlar — tek tıkla hazır kompozisyon ekle',
          })}
          active={templatesOpen}
          onClick={() => setTemplatesOpen((open) => !open)}
        >
          <LayoutTemplate size={15} />
        </PaletteButton>
        <span className="mx-0.5 h-5 w-px bg-slate-300 dark:bg-slate-700" />
        {PLACEMENTS.map(({ kind, labelKey, defaultLabel, shortcut, Icon }) => (
          <PaletteButton
            key={kind}
            title={withShortcut(label(labelKey, defaultLabel), shortcut)}
            active={placement === kind}
            onClick={() => setPlacement(placement === kind ? null : kind)}
          >
            <Icon size={15} />
          </PaletteButton>
        ))}
      </div>
      {templatesOpen && (
        <div className="pointer-events-auto flex flex-col gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
          {TEMPLATES.map((template) => (
            <button
              key={template.key}
              type="button"
              className="rounded px-2 py-1 text-left text-xs font-medium text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
              onClick={() => {
                setTemplatesOpen(false);
                void insertTemplate(template.key);
              }}
            >
              {t(`GlassEnclosure.Designer.Templates.${template.labelKey}`, {
                defaultValue: template.defaultLabel,
              })}
            </button>
          ))}
          <div className="my-0.5 h-px bg-slate-200 dark:bg-slate-700" />
          {userTemplates.length > 0 && (
            <div className="px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
              {t('GlassEnclosure.Designer.Templates.Mine', { defaultValue: 'Şablonlarım' })}
            </div>
          )}
          {userTemplates.map((ut) => (
            <div
              key={ut.id}
              className="group flex items-center gap-1 rounded hover:bg-slate-100 dark:hover:bg-slate-800"
            >
              <button
                type="button"
                className="flex-1 truncate rounded px-2 py-1 text-left text-xs font-medium text-slate-700 dark:text-slate-200"
                onClick={() => {
                  setTemplatesOpen(false);
                  void insertUserTemplate(ut.id);
                }}
              >
                {ut.name}
              </button>
              <button
                type="button"
                className="mr-1 rounded p-1 text-slate-400 opacity-0 transition hover:text-red-500 group-hover:opacity-100"
                title={t('GlassEnclosure.Designer.Templates.Delete', {
                  defaultValue: 'Şablonu sil',
                })}
                onClick={() => void deleteTemplate(ut.id)}
              >
                <Trash2 size={12} />
              </button>
            </div>
          ))}
          {savingTemplate ? (
            <input
              ref={(el) => el?.focus()}
              value={templateName}
              onChange={(event) => setTemplateName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') void commitSaveTemplate();
                else if (event.key === 'Escape') {
                  setSavingTemplate(false);
                  setTemplateName('');
                }
              }}
              placeholder={t('GlassEnclosure.Designer.Templates.NamePlaceholder', {
                defaultValue: 'Şablon adı',
              })}
              className="mx-1 my-0.5 rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-800 outline-none focus:border-primary-400 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            />
          ) : (
            <button
              type="button"
              className="flex items-center gap-1.5 rounded px-2 py-1 text-left text-xs font-medium text-primary-600 transition hover:bg-primary-50 dark:text-primary-400 dark:hover:bg-primary-950/40"
              onClick={() => setSavingTemplate(true)}
            >
              <Save size={12} />
              {t('GlassEnclosure.Designer.Templates.SaveCurrent', {
                defaultValue: 'Bu çizimi şablon kaydet',
              })}
            </button>
          )}
        </div>
      )}
      {placement === 'pen' && (
        <div className="pointer-events-auto flex flex-col items-center gap-1">
          <div className="flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {PEN_INTENTS.map(({ intent, labelKey, defaultLabel, Icon }) => (
              <PaletteButton
                key={intent}
                title={label(labelKey, defaultLabel)}
                active={penIntent === intent}
                onClick={() => setPenIntent(intent)}
              >
                <Icon size={15} />
              </PaletteButton>
            ))}
          </div>
          <div className="flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {PEN_MODES.map(({ penMode: m, labelKey, defaultLabel, Icon }) => (
              <PaletteButton
                key={m}
                title={label(labelKey, defaultLabel)}
                active={penMode === m}
                onClick={() => setPenMode(m)}
              >
                <Icon size={15} />
              </PaletteButton>
            ))}
          </div>
          <span className="rounded bg-slate-900/80 px-2 py-0.5 text-[10px] font-medium text-white">
            {label(
              'PenHint',
              'Zemine ya da bir obje yüzeyine tıkla → köşe ekle · Shift düz · ilk noktaya tıkla / çift tık / Enter bitir · Esc iptal',
            )}
          </span>
        </div>
      )}
      {placement && placement !== 'pen' && (
        <div className="pointer-events-auto flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
          <PaletteButton
            title={label('ShapeFlat', 'Düz')}
            active={placementShape === 'flat'}
            onClick={() => setPlacementShape('flat')}
          >
            <Minus size={15} />
          </PaletteButton>
          <PaletteButton
            title={label('ShapeCurved', 'Yay / kavisli')}
            active={placementShape === 'curved'}
            onClick={() => setPlacementShape('curved')}
          >
            <Spline size={15} />
          </PaletteButton>
        </div>
      )}
      {activeTool === 'draw' && !placement && (
        <div className="pointer-events-auto flex flex-col items-center gap-1">
          <div className="flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {DRAW_SHAPES.map(({ shape, labelKey, defaultLabel, Icon }) => (
              <PaletteButton
                key={shape}
                title={label(labelKey, defaultLabel)}
                active={drawShape === shape}
                onClick={() => setDrawShape(shape)}
              >
                <Icon size={15} />
              </PaletteButton>
            ))}
          </div>
          <span className="rounded bg-slate-900/80 px-2 py-0.5 text-[10px] font-medium text-white">
            {label(
              'DrawHint',
              'Yüzeyde bölge çiz → Genişlet (S) aracıyla girinti/çıkıntı derinliği ver',
            )}
          </span>
        </div>
      )}
      {activeTool === 'paint' && !placement && (
        <div className="pointer-events-auto flex flex-col items-start gap-1">
          <div className="flex max-w-[18rem] flex-wrap items-center gap-1 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {colors.map((color) => (
              <button
                key={color.id}
                type="button"
                title={color.name}
                aria-label={color.name}
                onClick={() => setPaintColor({ id: color.id, hex: color.hexColor })}
                className={cn(
                  'h-6 w-6 rounded border',
                  paintColor?.hex === color.hexColor
                    ? 'border-primary-500 ring-2 ring-primary-400/60'
                    : 'border-slate-300 dark:border-slate-600',
                )}
                style={{ backgroundColor: color.hexColor }}
              />
            ))}
            {colors.length === 0 && (
              <span className="px-2 text-[11px] text-slate-400">
                {label('NoColors', 'Renk kataloğu boş')}
              </span>
            )}
          </div>
          <div className="flex max-w-[18rem] flex-wrap items-center gap-1 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {PROCEDURAL_MATERIAL_KEYS.map((key) => (
              <button
                key={key}
                type="button"
                aria-pressed={paintMaterial === key}
                onClick={() => setPaintMaterial(key)}
                className={cn(
                  'rounded border px-2 py-1 text-[11px] font-medium transition',
                  paintMaterial === key
                    ? 'border-primary-600 bg-primary-600 text-white'
                    : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800',
                )}
              >
                {label(`Material_${key}`, MATERIAL_FALLBACKS[key])}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

const PaletteButton = ({
  title,
  active,
  onClick,
  children,
}: {
  title: string;
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    title={title}
    aria-label={title}
    aria-pressed={active}
    onClick={onClick}
    className={cn(
      'inline-flex h-8 w-8 items-center justify-center rounded-md transition',
      active
        ? 'bg-primary-600 text-white'
        : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
    )}
  >
    {children}
  </button>
);
