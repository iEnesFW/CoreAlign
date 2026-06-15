import { useMutation } from '@tanstack/react-query';
import { consentApi, type CaptureConsentInput } from './api';

export const useCaptureConsentMutation = () =>
  useMutation({
    mutationFn: (input: CaptureConsentInput) => consentApi.capture(input),
    onError: () => undefined,
  });
