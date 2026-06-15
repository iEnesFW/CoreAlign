import { X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

interface TagChipProps {
  name: string;
  colorHex?: string | null;
  onRemove?: () => void;
  className?: string;
}

const DEFAULT_COLOR = '#6366f1';

const isLightColor = (hex: string): boolean => {
  const normalized = hex.replace('#', '').slice(0, 6);
  if (normalized.length < 6) return false;
  const r = parseInt(normalized.slice(0, 2), 16);
  const g = parseInt(normalized.slice(2, 4), 16);
  const b = parseInt(normalized.slice(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.6;
};

export const TagChip = ({ name, colorHex, onRemove, className }: TagChipProps) => {
  const color = colorHex || DEFAULT_COLOR;
  const textColor = isLightColor(color) ? '#1e293b' : '#ffffff';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-semibold',
        className,
      )}
      style={{ backgroundColor: color, color: textColor }}
    >
      {name}
      {onRemove && (
        <button
          type="button"
          onClick={onRemove}
          className="-mr-0.5 rounded-full p-0.5 transition hover:bg-black/15"
          aria-label={`remove ${name}`}
        >
          <X size={9} />
        </button>
      )}
    </span>
  );
};
