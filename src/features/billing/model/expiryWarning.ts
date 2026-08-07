import type { TenantModuleDto } from './billing.types';

/** Matches the backend's urgent threshold: three days out, the reminder becomes a popup. */
export const POPUP_THRESHOLD_DAYS = 3;

/** Matches ModuleExpiryThresholds.WindowDays — the widest window the reminder job announces. */
export const REMINDER_WINDOW_DAYS = 15;

const MS_PER_DAY = 86_400_000;

export interface ExpiringSoonModule {
  moduleId: string;
  name: string;
  endUtc: string;
  daysLeft: number;
}

export const daysUntil = (endUtc: string, now: Date): number =>
  Math.ceil((new Date(endUtc).getTime() - now.getTime()) / MS_PER_DAY);

/**
 * An expired grant is deliberately excluded: the module is already gone, so a "3 days left" popup
 * would be a lie. Perpetual grants (no end date) can never expire, and a grant whose start date is
 * still ahead is not active yet — the backend job skips both for the same reason.
 */
export const expiringSoon = (
  modules: TenantModuleDto[],
  now: Date,
  thresholdDays = POPUP_THRESHOLD_DAYS,
): ExpiringSoonModule[] =>
  modules
    .filter((m) => m.isCurrentlyActive && Boolean(m.endUtc))
    .map((m) => ({
      moduleId: m.moduleId,
      name: m.name,
      endUtc: m.endUtc as string,
      daysLeft: daysUntil(m.endUtc as string, now),
    }))
    .filter((m) => m.daysLeft > 0 && m.daysLeft <= thresholdDays)
    .sort((a, b) => a.daysLeft - b.daysLeft);

/**
 * The dismissal is keyed by the exact set of modules AND their end dates, so buying an extension
 * (or a second module falling into the window) surfaces the popup again instead of staying hidden
 * behind yesterday's dismissal. The date keeps it to once per day at most.
 */
export const dismissalKey = (modules: ExpiringSoonModule[], now: Date): string =>
  [
    now.toISOString().slice(0, 10),
    ...modules.map((m) => `${m.moduleId}:${m.endUtc.slice(0, 10)}`).sort(),
  ].join('|');
