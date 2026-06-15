import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@/app/i18n/config';
import './index.css';
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
