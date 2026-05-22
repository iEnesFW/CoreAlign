import { useId } from 'react';
import { cn } from '@/shared/lib/cn';

type Variant = 'bars' | 'area';

interface Props {
  data: number[];
  width?: number;
  height?: number;
  className?: string;
  variant?: Variant;
  strokeColor?: string;
  fillFrom?: string;
  fillTo?: string;
}

export const Sparkline = ({
  data,
  width = 96,
  height = 28,
  className,
  variant = 'bars',
  strokeColor = '#6366f1',
  fillFrom = 'rgba(99,102,241,0.32)',
  fillTo = 'rgba(99,102,241,0)',
}: Props) => {
  const gradientId = useId();
  const safe = data.length > 0 ? data : [0];
  const max = Math.max(...safe, 1);
  const min = Math.min(...safe, 0);
  const range = max - min || 1;

  if (variant === 'bars') {
    const gap = 2;
    const barWidth = Math.max(1, (width - gap * (safe.length - 1)) / safe.length);
    return (
      <svg
        width={width}
        height={height}
        className={cn('block overflow-visible', className)}
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-hidden
      >
        <defs>
          <linearGradient id={gradientId} x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stopColor={strokeColor} stopOpacity="1" />
            <stop offset="100%" stopColor={strokeColor} stopOpacity="0.35" />
          </linearGradient>
        </defs>
        {safe.map((v, i) => {
          const h = Math.max(2, ((v - min) / range) * height);
          const x = i * (barWidth + gap);
          const y = height - h;
          return (
            <rect
              key={i}
              x={x}
              y={y}
              width={barWidth}
              height={h}
              rx={1}
              fill={`url(#${gradientId})`}
            />
          );
        })}
      </svg>
    );
  }

  const stepX = safe.length > 1 ? width / (safe.length - 1) : width;
  const points = safe.map((v, i) => {
    const x = i * stepX;
    const y = height - ((v - min) / range) * (height - 2) - 1;
    return `${x},${y}`;
  });
  const linePath = `M ${points.join(' L ')}`;
  const areaPath = `${linePath} L ${width},${height} L 0,${height} Z`;

  return (
    <svg
      width={width}
      height={height}
      className={cn('block overflow-visible', className)}
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-hidden
    >
      <defs>
        <linearGradient id={gradientId} x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor={fillFrom} />
          <stop offset="100%" stopColor={fillTo} />
        </linearGradient>
      </defs>
      <path d={areaPath} fill={`url(#${gradientId})`} />
      <path
        d={linePath}
        fill="none"
        stroke={strokeColor}
        strokeWidth={1.5}
        strokeLinejoin="round"
        strokeLinecap="round"
      />
    </svg>
  );
};
