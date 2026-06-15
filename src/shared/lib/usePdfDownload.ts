import { useCallback, useState } from 'react';
import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

export interface UsePdfDownloadResult {
  download: () => Promise<void>;
  isLoading: boolean;
  error: unknown;
}

export const usePdfDownload = (path: string, fileName: string): UsePdfDownloadResult => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const download = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    const [response, requestError] = await safeRequest(
      apiClient.get<Blob>(path, { responseType: 'blob' }),
    );
    setIsLoading(false);

    if (requestError || !response) {
      setError(requestError);
      logger.warn('PDF download failed', { path, error: String(requestError) });
      return;
    }

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
  }, [path, fileName]);

  return { download, isLoading, error };
};
