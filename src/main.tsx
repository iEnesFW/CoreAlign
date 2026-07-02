import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@fontsource-variable/inter';
import '@fontsource-variable/sora';
import '@/app/i18n/config';
import './index.css';
import '@/features/auth/model/authBridgeSetup';
import '@/features/installation-acceptance/model/offlineExecutors';
import App from './App.tsx';
import { installWindowErrorHandlers } from '@/shared/errors/windowHandlers';
import { registerServiceWorker } from '@/shared/offline/registerServiceWorker';

installWindowErrorHandlers();
registerServiceWorker();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
