import { useAnimatedNumber } from '@/shared/hooks/useAnimatedNumber';

interface Props {
  value: number;
  format: (value: number) => string;
  durationMs?: number;
  className?: string;
}

export const AnimatedNumber = ({ value, format, durationMs = 700, className }: Props) => {
  const animated = useAnimatedNumber(value, durationMs);
  return <span className={className}>{format(animated)}</span>;
};
