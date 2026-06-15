import { useTranslation } from 'react-i18next';
import { CheckCircle2, AlertCircle, HelpCircle, MinusCircle } from 'lucide-react';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import type { ProviderHealthStatus } from '../providers.types';

interface Props {
  status: ProviderHealthStatus | null;
  isConfigured?: boolean;
}

type Tone = {
  variant: BadgeVariant;
  icon: typeof CheckCircle2;
  labelKey: string;
};

const TONE: Record<string, Tone> = {
  Healthy: { variant: 'success', icon: CheckCircle2, labelKey: 'Admin.Providers.Status.Healthy' },
  Degraded: { variant: 'warning', icon: AlertCircle, labelKey: 'Admin.Providers.Status.Degraded' },
  Unhealthy: { variant: 'error', icon: AlertCircle, labelKey: 'Admin.Providers.Status.Unhealthy' },
  NotConfigured: {
    variant: 'neutral',
    icon: MinusCircle,
    labelKey: 'Admin.Providers.Status.NotConfigured',
  },
  Unknown: { variant: 'neutral', icon: HelpCircle, labelKey: 'Admin.Providers.Status.Unknown' },
};

export const ProviderHealthBadge = ({ status, isConfigured = true }: Props) => {
  const { t } = useTranslation();
  const resolved = !isConfigured ? 'NotConfigured' : (status ?? 'Unknown');
  const tone = TONE[resolved] ?? TONE.Unknown;
  const Icon = tone.icon;
  return (
    <Badge variant={tone.variant} pill className="gap-1 normal-case">
      <Icon size={11} />
      <span>{t(tone.labelKey)}</span>
    </Badge>
  );
};
