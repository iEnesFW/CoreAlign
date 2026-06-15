import { useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantBrandingApi } from '../api/tenantBrandingApi';

export const useUploadTenantLogo = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => tenantBrandingApi.uploadLogo(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['settings', 'company'] });
    },
  });
};
