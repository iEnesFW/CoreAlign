import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import '@/features/auth/authBridgeSetup';
import { App } from './App';
import { initSentry, withSentryProfiler } from './observability/sentry';
import { installGlobalErrorReporting } from '@/shared/lib/clientErrorReporter';
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary';

initSentry();
installGlobalErrorReporting();

const RootApp = withSentryProfiler(App);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <RootApp />
    </ErrorBoundary>
  </StrictMode>,
);
