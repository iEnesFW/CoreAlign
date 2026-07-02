import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowRight, Search } from 'lucide-react';
import { useCommandPaletteStore } from '../model/paletteStore';
import { useMultiEntitySearch } from '../hooks/useMultiEntitySearch';
import { NAV_COMMANDS } from '../model/navCommands';
import type { PaletteKind } from '../model/paletteTypes';

interface FlatItem {
  id: string;
  label: string;
  sublabel?: string;
  to: string;
}

export const CommandPalette = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const close = useCommandPaletteStore((s) => s.close);

  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);
  const [lastQuery, setLastQuery] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  if (query !== lastQuery) {
    setLastQuery(query);
    setActiveIndex(0);
  }

  const { groups, flat, isFetching, shouldSearch } = useMultiEntitySearch(query, true);

  const navItems = useMemo<FlatItem[]>(
    () =>
      NAV_COMMANDS.map((n) => ({
        id: n.key,
        label: t(n.labelKey, { defaultValue: n.key }),
        to: n.to,
      })),
    [t],
  );

  const items: FlatItem[] = shouldSearch ? flat : navItems;
  const indexById = useMemo(() => new Map(items.map((it, i) => [it.id, i])), [items]);

  const go = (to: string) => {
    close();
    navigate(to);
  };

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIndex((i) => Math.min(i + 1, items.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const it = items[activeIndex];
      if (it) go(it.to);
    }
  };

  const groupLabel = (kind: PaletteKind) =>
    t(`CommandPalette.group.${kind}s` as const, { defaultValue: kind });

  return (
    <div
      className="fixed inset-0 z-[60] flex items-start justify-center bg-black/40 px-4 pt-[14vh]"
      role="presentation"
      onClick={close}
    >
      <div
        className="w-full max-w-xl overflow-hidden rounded-xl border border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center gap-2 border-b border-slate-200 px-3 dark:border-slate-800">
          <Search size={16} className="shrink-0 text-slate-400" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={onKeyDown}
            placeholder={t('CommandPalette.placeholder', {
              defaultValue: 'Müşteri, sipariş, fatura, ürün, teklif ara…',
            })}
            className="w-full bg-transparent py-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none dark:text-slate-100"
            aria-label={t('CommandPalette.placeholder', { defaultValue: 'Ara' })}
          />
        </div>

        <div className="max-h-[50vh] overflow-y-auto py-1">
          {!shouldSearch && (
            <Section title={t('CommandPalette.quickNav', { defaultValue: 'Hızlı gezinme' })}>
              {navItems.map((it) => (
                <Row
                  key={it.id}
                  label={it.label}
                  active={indexById.get(it.id) === activeIndex}
                  onSelect={() => go(it.to)}
                  onHover={() => setActiveIndex(indexById.get(it.id) ?? 0)}
                />
              ))}
            </Section>
          )}

          {shouldSearch && isFetching && flat.length === 0 && (
            <div className="px-3 py-6 text-center text-xs text-slate-500">
              {t('CommandPalette.searching', { defaultValue: 'Aranıyor…' })}
            </div>
          )}

          {shouldSearch && !isFetching && flat.length === 0 && (
            <div className="px-3 py-6 text-center text-xs text-slate-500">
              {t('CommandPalette.noResults', {
                defaultValue: '"{{query}}" için sonuç yok',
                query: query.trim(),
              })}
            </div>
          )}

          {shouldSearch &&
            groups.map((group) => (
              <Section key={group.kind} title={groupLabel(group.kind)}>
                {group.results.map((r) => (
                  <Row
                    key={r.id}
                    label={r.label}
                    sublabel={r.sublabel}
                    active={indexById.get(r.id) === activeIndex}
                    onSelect={() => go(r.to)}
                    onHover={() => setActiveIndex(indexById.get(r.id) ?? 0)}
                  />
                ))}
              </Section>
            ))}
        </div>
      </div>
    </div>
  );
};

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <div className="py-1">
    <div className="px-3 py-1 text-[10px] font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500">
      {title}
    </div>
    {children}
  </div>
);

const Row = ({
  label,
  sublabel,
  active,
  onSelect,
  onHover,
}: {
  label: string;
  sublabel?: string;
  active: boolean;
  onSelect: () => void;
  onHover: () => void;
}) => (
  <button
    type="button"
    onClick={onSelect}
    onMouseMove={onHover}
    className={`flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm transition ${
      active
        ? 'bg-primary-50 text-primary-800 dark:bg-primary-500/15 dark:text-primary-200'
        : 'text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800/60'
    }`}
  >
    <span className="min-w-0">
      <span className="block truncate">{label}</span>
      {sublabel && (
        <span className="block truncate text-[11px] text-slate-400 dark:text-slate-500">
          {sublabel}
        </span>
      )}
    </span>
    {active && <ArrowRight size={13} className="shrink-0 text-primary-500" />}
  </button>
);
