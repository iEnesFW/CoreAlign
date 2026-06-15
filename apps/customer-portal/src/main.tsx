import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { App } from './App';
import { initSentry, withSentryProfiler } from './observability/sentry';

initSentry();

const RootApp = withSentryProfiler(App);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RootApp />
  </StrictMode>,
);
