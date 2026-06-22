import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Box, Download, FileText, Ruler } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import { useGlassTypesQuery, useProfileSystemsQuery } from '../hooks/useGlassEnclosureQueries';
import { countCatalogViolations } from '../model/catalogValidation';
import {
  downloadTextFile,
  exportSceneGlb,
  printPlanSvg,
  sceneToDxf,
  sceneToPlanSvg,
} from '../model/sceneExport';

const sanitize = (name: string) => name.replace(/[^\w.-]+/g, '_').slice(0, 60) || 'design';

export function ExportMenu() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const exportScene = useDesignerStore((s) => s.exportScene);
  const runs = useDesignerStore((s) => s.scene.runs);
  const projectName = useDesignerStore((s) => s.project?.projectName ?? 'design');
  const profileSystems = useProfileSystemsQuery().data?.data;
  const glassTypes = useGlassTypesQuery().data?.data;
  const catalogReady = Array.isArray(profileSystems) && Array.isArray(glassTypes);

  const catalogViolations = useMemo(
    () => countCatalogViolations(runs, profileSystems ?? [], glassTypes ?? []),
    [runs, profileSystems, glassTypes],
  );

  const confirmIfOutOfCatalog = (): boolean => {
    if (!catalogReady)
      return window.confirm(
        t('GlassEnclosure.Designer.Export.CatalogNotLoadedConfirm', {
          defaultValue:
            'Katalog yüklenemedi; üretilebilirlik doğrulanamadı. Yine de dışa aktarılsın mı?',
        }),
      );
    if (catalogViolations === 0) return true;
    return window.confirm(
      t('GlassEnclosure.Designer.Export.CatalogConfirm', {
        defaultValue:
          '{{count}} hat katalog limitini aşıyor (üretilemez). Yine de dışa aktarılsın mı?',
        count: catalogViolations,
      }),
    );
  };

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    };
    window.addEventListener('mousedown', onDown);
    return () => window.removeEventListener('mousedown', onDown);
  }, [open]);

  const base = sanitize(projectName);

  const onDxf = () => {
    if (!confirmIfOutOfCatalog()) return;
    downloadTextFile(`${base}.dxf`, 'application/dxf', sceneToDxf(exportScene()));
    setOpen(false);
  };
  const onGlb = () => {
    if (!confirmIfOutOfCatalog()) return;
    exportSceneGlb(`${base}.glb`);
    setOpen(false);
  };
  const onPdf = () => {
    if (!confirmIfOutOfCatalog()) return;
    printPlanSvg(sceneToPlanSvg(exportScene(), projectName));
    setOpen(false);
  };

  const label = (key: string, defaultValue: string) =>
    t(`GlassEnclosure.Designer.Export.${key}`, { defaultValue });

  const itemClass =
    'flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs text-slate-700 transition hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800';

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="inline-flex h-8 items-center gap-1.5 rounded-md border border-slate-300 bg-white px-2 text-xs font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
      >
        <Download size={14} />
        {label('Title', 'Dışa aktar')}
      </button>
      {open && (
        <div className="absolute right-0 z-30 mt-1 w-44 overflow-hidden rounded-md border border-slate-200 bg-white shadow-lg dark:border-slate-700 dark:bg-slate-900">
          {catalogViolations > 0 && (
            <p className="border-b border-rose-200 bg-rose-50 px-3 py-1.5 text-[10px] font-medium text-rose-700 dark:border-rose-900/50 dark:bg-rose-950/30 dark:text-rose-300">
              {t('GlassEnclosure.Designer.Export.CatalogWarn', {
                defaultValue: '⚠ {{count}} hat katalog limitini aşıyor',
                count: catalogViolations,
              })}
            </p>
          )}
          <button type="button" onClick={onDxf} className={itemClass}>
            <Ruler size={14} /> {label('Dxf', 'DXF (2B plan)')}
          </button>
          <button type="button" onClick={onGlb} className={itemClass}>
            <Box size={14} /> {label('Glb', 'glTF/GLB (3B)')}
          </button>
          <button type="button" onClick={onPdf} className={itemClass}>
            <FileText size={14} /> {label('Pdf', 'PDF (yazdır)')}
          </button>
        </div>
      )}
    </div>
  );
}
