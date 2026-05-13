import { Toaster as SonnerToaster } from 'sonner';

export const AppToaster = () => (
  <SonnerToaster
    position="top-right"
    richColors
    closeButton
    expand
    toastOptions={{ duration: 4000 }}
  />
);
