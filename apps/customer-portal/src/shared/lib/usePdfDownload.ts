import { useCallback, useState } from 'react';
import { toast } from 'sonner';
import i18n from '@/app/i18n';
import { apiClient } from '@/shared/api/apiClient';

export interface UsePdfDownloadResult {
  download: () => Promise<void>;
  isLoading: boolean;
}

export const usePdfDownload = (path: string, fileName: string): UsePdfDownloadResult => {
  const [isLoading, setIsLoading] = useState(false);

  const download = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await apiClient.get<Blob>(path, { responseType: 'blob' });
      const blob = response.data instanceof Blob ? response.data : new Blob([response.data]);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName;
      anchor.rel = 'noopener';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (error) {
      toast.error(i18n.t('common.errorGeneric'));
      throw error;
    } finally {
      setIsLoading(false);
    }
  }, [path, fileName]);

  return { download, isLoading };
};
