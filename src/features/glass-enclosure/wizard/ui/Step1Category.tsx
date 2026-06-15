import { useTranslation } from 'react-i18next';
import {
  Anchor,
  Building,
  DoorOpen,
  Droplet,
  Fence,
  Home,
  Pencil,
  Sun,
  type LucideIcon,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useUxMode } from '@/features/persona/hooks/useUxMode';
import { useWizardStore } from '../model/wizardStore';
import { useEnclosurePresetsQuery } from '../hooks/useEnclosurePresetsQuery';
import { ENCLOSURE_PRESET_CATALOG, type EnclosurePresetEntry } from '../model/presetCatalog';
import type { WizardEnclosureCategory } from '../model/enclosure.types';

const ICON_MAP: Record<string, LucideIcon> = {
  Balcony: Home,
  Sun: Sun,
  Droplet: Droplet,
  Fence: Fence,
  DoorOpen: DoorOpen,
  Building: Building,
  Anchor: Anchor,
  Pencil: Pencil,
};

const EMOJI_MAP: Record<WizardEnclosureCategory, string> = {
  Balcony: '🏠',
  Greenhouse: '🌱',
  ShowerCabin: '🚿',
  Balustrade: '🪟',
  FramelessDoor: '🚪',
  CurtainWall: '🏢',
  SpiderFacade: '🕸️',
  FreeForm: '✏️',
};

const TITLE_DEFAULTS: Record<WizardEnclosureCategory, string> = {
  Balcony: 'Balkon Camlama',
  Greenhouse: 'Sera / Kış Bahçesi',
  ShowerCabin: 'Duşakabin',
  Balustrade: 'Korkuluk',
  FramelessDoor: 'Çerçevesiz Kapı',
  CurtainWall: 'Giydirme Cephe',
  SpiderFacade: 'Örümcek Cephe',
  FreeForm: 'Özel Tasarım',
};

const DESCRIPTION_DEFAULTS: Record<WizardEnclosureCategory, string> = {
  Balcony: 'Apartman ve teras balkonları için sürgü, katlama veya ısıcam.',
  Greenhouse: 'Düz veya eğimli çatılı, kış bahçesi ve sera.',
  ShowerCabin: 'Köşe, U veya niş tipi duşakabinler.',
  Balustrade: 'Cam korkuluk, merdiven ve balkon kenarı.',
  FramelessDoor: 'Çerçevesiz cam kapı ve giriş bölmeleri.',
  CurtainWall: 'Bina dış cephesi için kaset sistemli giydirme cephe.',
  SpiderFacade: 'Lobi ve giriş için örümcek bağlantılı yapısal cam cephe.',
  FreeForm: 'Kullanıcı tanımlı polygon ile özel tasarım.',
};

const SUBTITLE_DEFAULTS: Record<WizardEnclosureCategory, string> = {
  Balcony: 'TPH / Folding model · inline geometri',
  Greenhouse: 'Çatı eğim açısı parametrik · cam çatı',
  ShowerCabin: '8/10 mm temperli · köşe braketi',
  Balustrade: '12-19 mm yapısal cam · TRSB',
  FramelessDoor: 'Patch fitting · 10-12 mm temperli',
  CurtainWall: 'Kaset sistemi · 1.5×3 m panel · alüminyum profil',
  SpiderFacade: '4 noktalı tutucu · min. 12 mm yapısal cam',
  FreeForm: 'Boş başlangıç · polygon vertices ≥ 3',
};

const SKELETON_KEYS = ['p1', 'p2', 'p3', 'p4', 'p5'] as const;

const renderIconBlock = (entry: EnclosurePresetEntry, mode: 'Simple' | 'Pro') => {
  if (mode === 'Simple') {
    return (
      <span className="text-4xl" aria-hidden>
        {EMOJI_MAP[entry.category]}
      </span>
    );
  }
  const Icon = ICON_MAP[entry.iconKey] ?? Home;
  return (
    <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 text-white shadow-md shadow-indigo-500/20">
      <Icon size={22} />
    </span>
  );
};

export const Step1Category = () => {
  const { t } = useTranslation();
  const setCategory = useWizardStore((s) => s.setCategory);
  const setStep = useWizardStore((s) => s.setStep);
  const selected = useWizardStore((s) => s.category);
  const mode = useUxMode();
  const presetsQuery = useEnclosurePresetsQuery();

  const presetViews = presetsQuery.data ?? [];
  const entries: EnclosurePresetEntry[] = presetViews.length
    ? presetViews.map((v) => v.catalog)
    : ENCLOSURE_PRESET_CATALOG.slice();

  const handlePick = (category: WizardEnclosureCategory) => {
    setCategory(category);
    setStep(2);
  };

  return (
    <section className="space-y-4">
      <header className="space-y-1">
        <h3 className="text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('GlassEnclosure.NewProjectWizard.Step1.Title', {
            defaultValue: 'Hangi mekan için tasarlayalım?',
          })}
        </h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {mode === 'Simple'
            ? t('GlassEnclosure.NewProjectWizard.Step1.HintSimple', {
                defaultValue: 'Sana en yakın görüneni seç, gerisini birlikte halledelim.',
              })
            : t('GlassEnclosure.NewProjectWizard.Step1.HintPro', {
                defaultValue: 'Kategori seçimi geometri ve mounting topolojisini belirler.',
              })}
        </p>
      </header>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        {presetsQuery.isLoading &&
          SKELETON_KEYS.map((key) => (
            <div
              key={key}
              className="h-36 animate-pulse rounded-xl border border-slate-200 bg-slate-100 dark:border-slate-800 dark:bg-slate-800/50"
            />
          ))}

        {!presetsQuery.isLoading &&
          entries.map((entry) => {
            const isActive = selected === entry.category;
            return (
              <button
                key={entry.category}
                type="button"
                onClick={() => handlePick(entry.category)}
                className={cn(
                  'group flex flex-col items-start gap-2 rounded-xl border p-3 text-left transition-all',
                  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500',
                  isActive
                    ? 'border-indigo-500 bg-indigo-50/60 ring-2 ring-indigo-500/40 dark:bg-indigo-500/10'
                    : 'border-slate-200 bg-white hover:border-indigo-300 hover:shadow-sm dark:border-slate-800 dark:bg-slate-900 dark:hover:border-indigo-700',
                )}
              >
                {renderIconBlock(entry, mode)}
                <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                  {t(`${entry.i18nKey}.Title`, {
                    defaultValue: TITLE_DEFAULTS[entry.category],
                  })}
                </span>
                <span className="text-[11px] leading-snug text-slate-500 dark:text-slate-400">
                  {t(`${entry.i18nKey}.Description`, {
                    defaultValue: DESCRIPTION_DEFAULTS[entry.category],
                  })}
                </span>
                {mode === 'Pro' && (
                  <span className="mt-1 inline-flex rounded-md bg-slate-100 px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                    {t(`${entry.i18nKey}.Subtitle`, {
                      defaultValue: SUBTITLE_DEFAULTS[entry.category],
                    })}
                  </span>
                )}
              </button>
            );
          })}
      </div>
    </section>
  );
};

export default Step1Category;
