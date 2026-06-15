import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { HelpCircle, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface HelpItem {
  keys?: string;
  text: string;
}

interface HelpGroup {
  title: string;
  items: HelpItem[];
}

interface ShortcutsHelpProps {
  triggerClassName?: string;
  iconSize?: number;
}

const DEFAULT_TRIGGER_CLASS =
  'inline-flex h-8 w-8 items-center justify-center rounded-md border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50 focus-visible:ring-2 focus-visible:ring-blue-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800';

const HelpSection = ({ group }: { group: HelpGroup }) => (
  <section>
    <h3 className="mb-1 text-[11px] font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {group.title}
    </h3>
    <ul className="space-y-1">
      {group.items.map((item) => (
        <li
          key={item.text}
          className="flex items-start gap-2 text-xs text-slate-700 dark:text-slate-200"
        >
          {item.keys && (
            <kbd className="shrink-0 rounded border border-slate-300 bg-slate-100 px-1 py-0.5 font-mono text-[10px] text-slate-600 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300">
              {item.keys}
            </kbd>
          )}
          <span>{item.text}</span>
        </li>
      ))}
    </ul>
  </section>
);

export const ShortcutsHelp = ({ triggerClassName, iconSize = 14 }: ShortcutsHelpProps) => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const titleId = useId();

  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
    };
    const handlePointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('pointerdown', handlePointerDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('pointerdown', handlePointerDown);
    };
  }, [open]);

  const groups: HelpGroup[] = [
    {
      title: t('GlassEnclosure.Designer.Help.Selection.Title', { defaultValue: 'Selection' }),
      items: [
        {
          text: t('GlassEnclosure.Designer.Help.Selection.Click', {
            defaultValue: 'Click an item to select it',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Selection.ClickEmpty', {
            defaultValue: 'Click empty space to clear the selection',
          }),
        },
      ],
    },
    {
      title: t('GlassEnclosure.Designer.Help.Keyboard.Title', { defaultValue: 'Keyboard' }),
      items: [
        {
          keys: 'Ctrl+Z',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Undo', { defaultValue: 'Undo' }),
        },
        {
          keys: 'Ctrl+Y / Ctrl+Shift+Z',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Redo', { defaultValue: 'Redo' }),
        },
        {
          keys: 'Ctrl+S',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Save', { defaultValue: 'Save' }),
        },
        {
          keys: 'Ctrl+C',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Copy', { defaultValue: 'Copy' }),
        },
        {
          keys: 'Ctrl+V',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Paste', { defaultValue: 'Paste' }),
        },
        {
          keys: 'Esc',
          text: t('GlassEnclosure.Designer.Help.Keyboard.Cancel', { defaultValue: 'Cancel' }),
        },
      ],
    },
    {
      title: t('GlassEnclosure.Designer.Help.Tools.Title', { defaultValue: 'Araçlar' }),
      items: [
        {
          keys: 'V / M / R / S',
          text: t('GlassEnclosure.Designer.Help.Tools.Basic', {
            defaultValue: 'Seç · Taşı · Döndür · Genişlet',
          }),
        },
        {
          keys: 'D / B / E',
          text: t('GlassEnclosure.Designer.Help.Tools.Surface', {
            defaultValue: 'Yüzeye çiz · Boya · Sil',
          }),
        },
        {
          keys: 'L',
          text: t('GlassEnclosure.Designer.Help.Tools.Lasso', {
            defaultValue: 'Lasso ile çoklu seçim — Del ile topluca silin',
          }),
        },
        {
          keys: 'Ctrl+Tık',
          text: t('GlassEnclosure.Designer.Help.Tools.CtrlSelect', {
            defaultValue:
              'Öğeleri çoklu seçime ekle/çıkar — sağ üstten hizala, uç uca birleştir, eşitle, araları doldur',
          }),
        },
        {
          keys: 'F',
          text: t('GlassEnclosure.Designer.Help.Tools.Autofill', {
            defaultValue: 'Seçili duvarın boşluklarını camla doldur',
          }),
        },
        {
          keys: '1-4',
          text: t('GlassEnclosure.Designer.Help.Tools.Placement', {
            defaultValue: 'Hat / Duvar / Zemin / Çatı ekle — imlece yapışır, tıklayınca yerleşir',
          }),
        },
        {
          keys: 'Del',
          text: t('GlassEnclosure.Designer.Help.Tools.Delete', {
            defaultValue: 'Seçili öğeyi sil',
          }),
        },
        {
          keys: 'Esc',
          text: t('GlassEnclosure.Designer.Help.Tools.Cancel', {
            defaultValue: 'Aracı ve yerleştirmeyi iptal et',
          }),
        },
      ],
    },
    {
      title: t('GlassEnclosure.Designer.Help.Draw.Title', { defaultValue: 'Yüzeye çizim' }),
      items: [
        {
          text: t('GlassEnclosure.Designer.Help.Draw.Shapes', {
            defaultValue:
              'Şekli (dikdörtgen, daire, oval, üçgen, çokgen, serbest kalem) üst şeritten seçin; çizilen alan yüzeyde bölge olarak işaretlenir',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Draw.Drag', {
            defaultValue:
              'Duvar veya zemin/çatı yüzeyinde sürükleyerek alanı çizin — bölge bağımsız bir katmana dönüşür',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Draw.PushPull', {
            defaultValue:
              'Genişlet aracıyla katman yüzeyini çekip iterek derinliği ayarlayın; tamamen itilen katman boşluğa dönüşür',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Draw.Split', {
            defaultValue: "'Duvarı böl' şekli ile tıkladığınız noktadan duvar ikiye ayrılır",
          }),
        },
      ],
    },
    {
      title: t('GlassEnclosure.Designer.Help.Plan2D.Title', { defaultValue: '2D Plan' }),
      items: [
        {
          text: t('GlassEnclosure.Designer.Help.Plan2D.Draw', {
            defaultValue: 'In draw mode, drag to sketch a run',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Plan2D.Resize', {
            defaultValue: 'Drag the endpoints to resize',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Plan2D.DrawWall', {
            defaultValue: 'In wall mode, drag to draw walls/obstacles',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.Plan2D.Autofill', {
            defaultValue:
              "Select a wall → 'Fill openings with glass' creates runs across open edges",
          }),
        },
      ],
    },
    {
      title: t('GlassEnclosure.Designer.Help.View3D.Title', { defaultValue: '3D' }),
      items: [
        {
          text: t('GlassEnclosure.Designer.Help.View3D.Move', {
            defaultValue: 'Drag the selected hardware with the axis arrows',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.View3D.Orbit', {
            defaultValue: 'Right-click or middle wheel to rotate and zoom',
          }),
        },
        {
          text: t('GlassEnclosure.Designer.Help.View3D.MullionResize', {
            defaultValue: 'Drag a mullion (panel divider) to resize panels',
          }),
        },
      ],
    },
  ];

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-haspopup="dialog"
        className={cn(
          triggerClassName ?? DEFAULT_TRIGGER_CLASS,
          open && 'text-blue-600 dark:text-blue-400',
        )}
        aria-label={t('GlassEnclosure.Designer.Help.Open', {
          defaultValue: 'Usage and shortcuts',
        })}
      >
        <HelpCircle size={iconSize} />
      </button>
      {open && (
        <div
          role="dialog"
          aria-labelledby={titleId}
          className="absolute right-0 top-full z-50 mt-2 max-h-[70vh] w-72 overflow-auto rounded-lg border border-slate-200 bg-white p-3 shadow-xl dark:border-slate-700 dark:bg-slate-900"
        >
          <div className="mb-2 flex items-center justify-between">
            <h2 id={titleId} className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t('GlassEnclosure.Designer.Help.Title', { defaultValue: 'Usage & Shortcuts' })}
            </h2>
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label={t('GlassEnclosure.Designer.Help.Close', { defaultValue: 'Close' })}
              className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
            >
              <X size={14} />
            </button>
          </div>
          <div className="space-y-3">
            {groups.map((group) => (
              <HelpSection key={group.title} group={group} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default ShortcutsHelp;
