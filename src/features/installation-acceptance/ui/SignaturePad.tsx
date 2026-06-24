import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eraser, Save } from 'lucide-react';

interface Props {
  onCapture: (dataUrl: string, customerName: string) => void;
  disabled?: boolean;
}

export const SignaturePad = ({ onCapture, disabled }: Props) => {
  const { t } = useTranslation();
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const drawingRef = useRef<boolean>(false);
  const [customerName, setCustomerName] = useState<string>('');
  const [hasDrawn, setHasDrawn] = useState<boolean>(false);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.strokeStyle = '#1e293b';
    ctx.lineWidth = 2;
    ctx.lineCap = 'round';
  }, []);

  const getCoords = (e: React.PointerEvent<HTMLCanvasElement>): { x: number; y: number } => {
    const canvas = canvasRef.current;
    if (!canvas) return { x: 0, y: 0 };
    const rect = canvas.getBoundingClientRect();
    return {
      x: ((e.clientX - rect.left) * canvas.width) / rect.width,
      y: ((e.clientY - rect.top) * canvas.height) / rect.height,
    };
  };

  const handleDown = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (disabled) return;
    const ctx = canvasRef.current?.getContext('2d');
    if (!ctx) return;
    const { x, y } = getCoords(e);
    ctx.beginPath();
    ctx.moveTo(x, y);
    drawingRef.current = true;
  };

  const handleMove = (e: React.PointerEvent<HTMLCanvasElement>) => {
    if (!drawingRef.current) return;
    const ctx = canvasRef.current?.getContext('2d');
    if (!ctx) return;
    const { x, y } = getCoords(e);
    ctx.lineTo(x, y);
    ctx.stroke();
    setHasDrawn(true);
  };

  const handleUp = () => {
    drawingRef.current = false;
  };

  const clear = () => {
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext('2d');
    if (!canvas || !ctx) return;
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    setHasDrawn(false);
  };

  const capture = () => {
    const canvas = canvasRef.current;
    if (!canvas || !hasDrawn || !customerName.trim()) return;
    onCapture(canvas.toDataURL('image/png'), customerName.trim());
  };

  return (
    <div className="flex flex-col gap-3">
      <input
        type="text"
        value={customerName}
        onChange={(e) => setCustomerName(e.target.value)}
        disabled={disabled}
        placeholder={t('InstallationAcceptance.SignaturePad.CustomerNamePlaceholder')}
        className="w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
      />
      <p className="text-xs text-slate-600 dark:text-slate-400">
        {t('InstallationAcceptance.SignaturePad.Instructions')}
      </p>
      <canvas
        ref={canvasRef}
        width={600}
        height={240}
        onPointerDown={handleDown}
        onPointerMove={handleMove}
        onPointerUp={handleUp}
        onPointerLeave={handleUp}
        className="touch-none rounded border-2 border-dashed border-slate-300 bg-white dark:border-slate-700"
      />
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={clear}
          disabled={disabled || !hasDrawn}
          className="flex items-center gap-1 rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
        >
          <Eraser className="size-4" />
          {t('InstallationAcceptance.SignaturePad.Clear')}
        </button>
        <button
          type="button"
          onClick={capture}
          disabled={disabled || !hasDrawn || !customerName.trim()}
          className="flex items-center gap-1 rounded bg-primary-600 px-3 py-2 text-sm text-white disabled:opacity-50"
        >
          <Save className="size-4" />
          {t('InstallationAcceptance.SignaturePad.Save')}
        </button>
      </div>
    </div>
  );
};
