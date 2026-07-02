import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Bookmark, BookmarkPlus, Check, Pencil, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import type { SavedView } from '../model/savedView.types';

interface ChipProps {
  view: SavedView;
  isActive: boolean;
  onApply: (view: SavedView) => void;
  onRename: (id: string, name: string) => void;
  onDelete: (id: string) => void;
}

function ViewChip({ view, isActive, onApply, onRename, onDelete }: ChipProps) {
  const { t } = useTranslation();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(view.name);

  const commit = () => {
    const trimmed = name.trim();
    if (trimmed && trimmed !== view.name) onRename(view.id, trimmed);
    setEditing(false);
  };

  if (editing) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full border border-primary-300 bg-white px-2 py-0.5 dark:border-primary-500/40 dark:bg-slate-900">
        <input
          ref={(el) => el?.focus()}
          value={name}
          onChange={(e) => setName(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') commit();
            if (e.key === 'Escape') {
              setName(view.name);
              setEditing(false);
            }
          }}
          className="w-28 bg-transparent text-xs text-slate-800 focus:outline-none dark:text-slate-100"
          aria-label={t('SavedViews.rename', { defaultValue: 'Yeniden adlandır' })}
        />
        <button
          type="button"
          onClick={commit}
          aria-label={t('SavedViews.confirmSave', { defaultValue: 'Kaydet' })}
          className="rounded p-0.5 text-success-600 hover:bg-success-50 dark:hover:bg-success-500/10"
        >
          <Check size={11} />
        </button>
      </span>
    );
  }

  return (
    <span
      className={cn(
        'group inline-flex items-center gap-0.5 rounded-full border px-1 py-0.5 transition',
        isActive
          ? 'border-primary-300 bg-primary-50 dark:border-primary-500/40 dark:bg-primary-500/10'
          : 'border-slate-200 bg-white hover:border-primary-200 dark:border-slate-700 dark:bg-slate-900 dark:hover:border-primary-500/30',
      )}
    >
      <button
        type="button"
        onClick={() => onApply(view)}
        className={cn(
          'max-w-[160px] truncate px-1.5 text-xs font-medium',
          isActive
            ? 'text-primary-700 dark:text-primary-300'
            : 'text-slate-600 dark:text-slate-300',
        )}
      >
        {view.name}
      </button>
      <button
        type="button"
        onClick={() => setEditing(true)}
        aria-label={t('SavedViews.rename', { defaultValue: 'Yeniden adlandır' })}
        className="rounded p-0.5 text-slate-400 opacity-0 transition group-hover:opacity-100 hover:bg-slate-100 hover:text-slate-600 focus-visible:opacity-100 dark:hover:bg-slate-800"
      >
        <Pencil size={10} />
      </button>
      <button
        type="button"
        onClick={() => onDelete(view.id)}
        aria-label={t('SavedViews.delete', { defaultValue: 'Sil' })}
        className="rounded p-0.5 text-slate-400 opacity-0 transition group-hover:opacity-100 hover:bg-danger-50 hover:text-danger-600 focus-visible:opacity-100 dark:hover:bg-danger-500/10"
      >
        <X size={10} />
      </button>
    </span>
  );
}

interface Props {
  views: SavedView[];
  activeViewId: string | null;
  onApply: (view: SavedView) => void;
  onSave: (name: string) => void;
  onRename: (id: string, name: string) => void;
  onDelete: (id: string) => void;
}

export function SavedViewBar({ views, activeViewId, onApply, onSave, onRename, onDelete }: Props) {
  const { t } = useTranslation();
  const [saving, setSaving] = useState(false);
  const [newName, setNewName] = useState('');

  const commitSave = () => {
    const trimmed = newName.trim();
    if (trimmed) {
      onSave(trimmed);
      setNewName('');
      setSaving(false);
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-1.5 rounded-xl border border-slate-200/70 bg-white/70 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/50">
      <span className="inline-flex items-center gap-1 text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-400">
        <Bookmark size={11} />
        {t('SavedViews.label', { defaultValue: 'Kayıtlı görünümler' })}
      </span>
      {views.map((view) => (
        <ViewChip
          key={view.id}
          view={view}
          isActive={view.id === activeViewId}
          onApply={onApply}
          onRename={onRename}
          onDelete={onDelete}
        />
      ))}
      {saving ? (
        <span className="inline-flex items-center gap-1 rounded-full border border-primary-300 bg-white px-2 py-0.5 dark:border-primary-500/40 dark:bg-slate-900">
          <input
            ref={(el) => el?.focus()}
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') commitSave();
              if (e.key === 'Escape') {
                setNewName('');
                setSaving(false);
              }
            }}
            placeholder={t('SavedViews.namePlaceholder', { defaultValue: 'Görünüm adı' })}
            className="w-32 bg-transparent text-xs text-slate-800 placeholder:text-slate-400 focus:outline-none dark:text-slate-100"
          />
          <button
            type="button"
            onClick={commitSave}
            aria-label={t('SavedViews.confirmSave', { defaultValue: 'Kaydet' })}
            className="rounded p-0.5 text-success-600 hover:bg-success-50 dark:hover:bg-success-500/10"
          >
            <Check size={11} />
          </button>
          <button
            type="button"
            onClick={() => {
              setNewName('');
              setSaving(false);
            }}
            aria-label={t('SavedViews.cancel', { defaultValue: 'Vazgeç' })}
            className="rounded p-0.5 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <X size={11} />
          </button>
        </span>
      ) : (
        <button
          type="button"
          onClick={() => setSaving(true)}
          className="inline-flex items-center gap-1 rounded-full border border-dashed border-slate-300 px-2 py-0.5 text-xs font-medium text-slate-500 transition hover:border-primary-300 hover:text-primary-600 dark:border-slate-600 dark:text-slate-400 dark:hover:border-primary-500/40 dark:hover:text-primary-300"
        >
          <BookmarkPlus size={11} />
          {t('SavedViews.save', { defaultValue: 'Görünümü kaydet' })}
        </button>
      )}
    </div>
  );
}
