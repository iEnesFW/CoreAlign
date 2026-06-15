import { Loader2 } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export const Spinner = ({ className, size = 16 }: { className?: string; size?: number }) => (
  <Loader2 className={cn('animate-spin text-slate-400', className)} size={size} />
);
