import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Checkbox } from '@/shared/ui/Checkbox/Checkbox';
import { Button } from '@/shared/ui/Button/Button';
import { cn } from '@/shared/lib/cn';
import type { AppUser } from '@/features/users/model/user.types';
import type { DunningSetting } from '@/features/dunning/model/dunning.types';
import { RecipientMultiSelect } from './RecipientMultiSelect';

interface DunningTypeRowProps {
  setting: DunningSetting;
  users: AppUser[];
  onSave: (next: DunningSetting) => Promise<unknown>;
  isSaving: boolean;
}

export const DunningTypeRow = ({ setting, users, onSave, isSaving }: DunningTypeRowProps) => {
  const { t } = useTranslation();
  const [draft, setDraft] = useState<DunningSetting>(setting);

  const dirty = useMemo(() => JSON.stringify(draft) !== JSON.stringify(setting), [draft, setting]);
  const channelMissing = draft.isEnabled && !draft.sendInApp && !draft.sendEmail;
  const canSave = dirty && !channelMissing && !isSaving;

  const patch = (p: Partial<DunningSetting>) => setDraft((d) => ({ ...d, ...p }));

  return (
    <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800/50">
      <header className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t(`Dunning.types.${draft.type}.title`)}
          </h3>
          <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">
            {t(`Dunning.types.${draft.type}.description`)}
          </p>
        </div>
        <Checkbox
          id={`enabled-${draft.type}`}
          checked={draft.isEnabled}
          onChange={(e) => patch({ isEnabled: e.target.checked })}
          label={t('Dunning.fields.enabled')}
        />
      </header>

      <div
        className={cn(
          'mt-4 grid gap-4 transition-opacity sm:grid-cols-2',
          !draft.isEnabled && 'opacity-50',
        )}
      >
        <fieldset className="flex flex-col gap-2" disabled={!draft.isEnabled}>
          <legend className="text-xs font-medium text-slate-600 dark:text-slate-400">
            {t('Dunning.fields.channels')}
          </legend>
          <Checkbox
            id={`inapp-${draft.type}`}
            checked={draft.sendInApp}
            onChange={(e) => patch({ sendInApp: e.target.checked })}
            label={t('Dunning.channels.inApp')}
          />
          <Checkbox
            id={`email-${draft.type}`}
            checked={draft.sendEmail}
            onChange={(e) => patch({ sendEmail: e.target.checked })}
            label={t('Dunning.channels.email')}
          />
          {channelMissing && (
            <p role="alert" className="text-xs font-medium text-danger-600 dark:text-danger-400">
              {t('Dunning.validation.atLeastOneChannel')}
            </p>
          )}
        </fieldset>

        <RecipientMultiSelect
          users={users}
          selectedIds={draft.recipientUserIds}
          onChange={(ids) => patch({ recipientUserIds: ids })}
          disabled={!draft.isEnabled}
        />
      </div>

      <footer className="mt-4 flex justify-end">
        <Button
          type="button"
          disabled={!canSave}
          isLoading={isSaving}
          onClick={() => onSave(draft)}
        >
          {t('Dunning.actions.save')}
        </Button>
      </footer>
    </section>
  );
};
