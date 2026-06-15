import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Star, Upload, Trash2, ArrowUp, ArrowDown, ImageIcon } from 'lucide-react';
import { toast } from 'sonner';
import {
  useDeleteProductImage,
  useProductImagesQuery,
  useUpdateProductImage,
  useUploadProductImage,
} from '@/features/products/hooks/useProductImages';
import type { ProductImage } from '@/features/products/api/productImagesApi';

interface ProductImagesTabProps {
  productId: string;
}

const MAX_BYTES = 5 * 1024 * 1024;
const MAX_IMAGES = 10;
const ALLOWED_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];

const sortImages = (images: ProductImage[]): ProductImage[] =>
  [...images].sort((a, b) => {
    if (a.isPrimary !== b.isPrimary) return a.isPrimary ? -1 : 1;
    if (a.displayOrder !== b.displayOrder) return a.displayOrder - b.displayOrder;
    return a.uploadedAtUtc.localeCompare(b.uploadedAtUtc);
  });

export const ProductImagesTab = ({ productId }: ProductImagesTabProps) => {
  const { t } = useTranslation();
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const query = useProductImagesQuery(productId);
  const upload = useUploadProductImage(productId);
  const update = useUpdateProductImage(productId);
  const remove = useDeleteProductImage(productId);

  const images = useMemo(() => sortImages(query.data ?? []), [query.data]);
  const canUpload = images.length < MAX_IMAGES;

  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    for (const file of Array.from(files)) {
      if (!ALLOWED_TYPES.includes(file.type)) {
        toast.error(t('Products.Images.invalidType', { defaultValue: 'Unsupported file type' }));
        continue;
      }
      if (file.size > MAX_BYTES) {
        toast.error(t('Products.Images.tooLarge', { defaultValue: 'File exceeds 5 MB' }));
        continue;
      }
      try {
        await upload.mutateAsync({ file });
        toast.success(t('Products.Images.uploaded', { defaultValue: 'Image uploaded' }));
      } catch {
        toast.error(t('Products.Images.uploadFailed', { defaultValue: 'Upload failed' }));
      }
    }
    if (inputRef.current) inputRef.current.value = '';
  };

  const setPrimary = async (image: ProductImage) => {
    setPendingId(image.id);
    try {
      await update.mutateAsync({
        imageId: image.id,
        payload: {
          altText: image.altText,
          displayOrder: image.displayOrder,
          isPrimary: true,
        },
      });
    } finally {
      setPendingId(null);
    }
  };

  const move = async (image: ProductImage, direction: -1 | 1) => {
    const next = Math.max(0, image.displayOrder + direction);
    setPendingId(image.id);
    try {
      await update.mutateAsync({
        imageId: image.id,
        payload: {
          altText: image.altText,
          displayOrder: next,
          isPrimary: image.isPrimary,
        },
      });
    } finally {
      setPendingId(null);
    }
  };

  const saveAlt = async (image: ProductImage, altText: string) => {
    setPendingId(image.id);
    try {
      await update.mutateAsync({
        imageId: image.id,
        payload: {
          altText,
          displayOrder: image.displayOrder,
          isPrimary: image.isPrimary,
        },
      });
    } finally {
      setPendingId(null);
    }
  };

  const remove1 = async (image: ProductImage) => {
    setPendingId(image.id);
    try {
      await remove.mutateAsync(image.id);
    } finally {
      setPendingId(null);
    }
  };

  return (
    <section className="space-y-3" data-testid="product-images-tab">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xs font-semibold text-slate-800 dark:text-slate-100">
            {t('Products.Images.title', { defaultValue: 'Images' })}
          </h3>
          <p className="text-[11px] text-slate-500 dark:text-slate-400">
            {t('Products.Images.subtitle', {
              defaultValue: 'Up to {{max}} images, JPG/PNG/WebP, 5 MB each.',
              max: MAX_IMAGES,
            })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <input
            ref={inputRef}
            type="file"
            accept={ALLOWED_TYPES.join(',')}
            multiple
            className="hidden"
            onChange={(e) => void handleFiles(e.target.files)}
          />
          <button
            type="button"
            disabled={!canUpload || upload.isPending}
            onClick={() => inputRef.current?.click()}
            className="inline-flex items-center gap-1.5 rounded-[5px] bg-indigo-600 text-white text-[11px] font-semibold px-2.5 py-1.5 hover:bg-indigo-500 disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <Upload className="h-3.5 w-3.5" />
            {t('Products.Images.upload', { defaultValue: 'Upload' })}
          </button>
        </div>
      </div>

      {query.isLoading && (
        <p className="text-[11px] text-slate-500">
          {t('Products.Images.loading', { defaultValue: 'Loading images…' })}
        </p>
      )}

      {!query.isLoading && images.length === 0 && (
        <div className="flex flex-col items-center justify-center gap-2 rounded-[5px] border border-dashed border-slate-200 dark:border-slate-700 py-10 text-slate-400">
          <ImageIcon className="h-6 w-6" />
          <p className="text-[11px]">
            {t('Products.Images.empty', { defaultValue: 'No images yet' })}
          </p>
        </div>
      )}

      <ul className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
        {images.map((image, index) => (
          <li
            key={image.id}
            className="group relative rounded-[5px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 overflow-hidden"
            data-testid={`product-image-${image.id}`}
          >
            <div className="aspect-square bg-slate-100 dark:bg-slate-800 flex items-center justify-center">
              <img
                src={image.publicUrl}
                alt={image.altText ?? ''}
                className="object-cover w-full h-full"
                loading="lazy"
              />
            </div>
            {image.isPrimary && (
              <span className="absolute top-1.5 start-1.5 inline-flex items-center gap-1 rounded-[3px] bg-amber-500/90 text-white text-[9px] font-bold px-1.5 py-0.5">
                <Star className="h-3 w-3" />
                {t('Products.Images.primary', { defaultValue: 'Primary' })}
              </span>
            )}
            <div className="p-2 space-y-2">
              <label className="block text-[10px] text-slate-500 dark:text-slate-400">
                {t('Products.Images.alt', { defaultValue: 'Alt text' })}
              </label>
              <input
                type="text"
                defaultValue={image.altText ?? ''}
                onBlur={(e) => {
                  if ((image.altText ?? '') !== e.target.value) {
                    void saveAlt(image, e.target.value);
                  }
                }}
                aria-label={t('Products.Images.alt', { defaultValue: 'Alt text' })}
                className="w-full text-[11px] px-2 py-1 rounded-[3px] border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900"
              />
              <div className="flex items-center justify-between text-[10px] text-slate-500">
                <span>#{index + 1}</span>
                <span>{Math.round(image.sizeBytes / 1024)} KB</span>
              </div>
              <div className="flex items-center justify-between gap-1">
                <button
                  type="button"
                  onClick={() => void setPrimary(image)}
                  disabled={image.isPrimary || pendingId === image.id}
                  aria-label={t('Products.Images.markPrimary', { defaultValue: 'Mark as primary' })}
                  className="p-1 rounded-[3px] hover:bg-slate-100 dark:hover:bg-slate-800 text-amber-500 disabled:opacity-40"
                >
                  <Star className="h-3.5 w-3.5" />
                </button>
                <button
                  type="button"
                  onClick={() => void move(image, -1)}
                  disabled={index === 0 || pendingId === image.id}
                  aria-label={t('Products.Images.moveUp', { defaultValue: 'Move earlier' })}
                  className="p-1 rounded-[3px] hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40"
                >
                  <ArrowUp className="h-3.5 w-3.5" />
                </button>
                <button
                  type="button"
                  onClick={() => void move(image, 1)}
                  disabled={index === images.length - 1 || pendingId === image.id}
                  aria-label={t('Products.Images.moveDown', { defaultValue: 'Move later' })}
                  className="p-1 rounded-[3px] hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40"
                >
                  <ArrowDown className="h-3.5 w-3.5" />
                </button>
                <button
                  type="button"
                  onClick={() => void remove1(image)}
                  disabled={pendingId === image.id}
                  aria-label={t('Products.Images.delete', { defaultValue: 'Delete' })}
                  className="p-1 rounded-[3px] hover:bg-red-50 dark:hover:bg-red-500/10 text-red-500 disabled:opacity-40"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
};
