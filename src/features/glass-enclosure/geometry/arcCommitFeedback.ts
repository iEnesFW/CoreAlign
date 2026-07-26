import type { TFunction } from 'i18next';
import { queueToast } from '@/shared/api/toastQueue';
import { arcCommitKeepingEnds } from './arcCommit';
import type { ArcCommitInput, ArcCommitOptions, ArcCommitPatch } from './arcCommit';
import type { CurvablePose } from './curvature';

/**
 * The one place a curvature edit turns into a patch AND a user-visible reason when it cannot.
 *
 * WHY: the handles used to refuse a too-tight radius by silently snapping back, which read as
 * "the stretch handle is broken" on tight arcs. Every caller gets the same guard and the same
 * wording for free — a new arc entry point cannot forget it.
 */
export const commitArcOrWarn = (
  body: CurvablePose,
  input: ArcCommitInput,
  t: TFunction,
  options?: ArcCommitOptions,
): ArcCommitPatch | null => {
  const { patch, rejection, radiusMm } = arcCommitKeepingEnds(body, input, options);
  if (patch) return patch;
  if (rejection === 'radiusTooSmall') {
    queueToast({
      dedupeKey: 'glass-arc-radius-too-small',
      variant: 'warning',
      description: t('GlassEnclosure.Designer.Arc.RadiusTooSmall', {
        defaultValue:
          'Bu ölçüler {{r}} mm yarıçap üretiyor — minimum 100 mm. Kirişi büyütün veya oku küçültün.',
        r: radiusMm ?? 0,
      }),
    });
  }
  return null;
};
