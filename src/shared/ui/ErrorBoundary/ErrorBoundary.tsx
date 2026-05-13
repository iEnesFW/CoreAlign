import { Component, type ErrorInfo, type ReactNode } from 'react';
import { logger } from '@/shared/lib/logger';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  info: ErrorInfo | null;
}

const isDev = import.meta.env.DEV;

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null, info: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error, info: null };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    this.setState({ info });
    logger.error('UI ErrorBoundary caught error', error, {
      componentStack: info.componentStack,
    });
  }

  handleRetry = (): void => {
    this.setState({ hasError: false, error: null, info: null });
  };

  handleReload = (): void => {
    window.location.reload();
  };

  render(): ReactNode {
    if (!this.state.hasError) {
      return this.props.children;
    }

    if (this.props.fallback) {
      return this.props.fallback;
    }

    const { error, info } = this.state;

    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-white px-4 py-10 text-center dark:bg-zinc-900">
        <div className="w-full max-w-xl">
          <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100">
            Something went wrong
          </h1>
          <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
            An unexpected error occurred. Try again, or reload the page if the issue persists.
          </p>

          <div className="mt-6 flex items-center justify-center gap-2">
            <button
              type="button"
              onClick={this.handleRetry}
              className="rounded-lg border border-zinc-300 bg-white px-4 py-2 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-700"
            >
              Try again
            </button>
            <button
              type="button"
              onClick={this.handleReload}
              className="rounded-lg bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-zinc-800 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-white"
            >
              Reload page
            </button>
          </div>

          {isDev && error && (
            <details className="mt-8 rounded-lg border border-zinc-200 bg-zinc-50 p-3 text-left text-xs text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900/40 dark:text-zinc-300">
              <summary className="cursor-pointer font-semibold">Developer details</summary>
              <pre className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap break-all">
                {error.name}: {error.message}
                {'\n\n'}
                {error.stack}
                {info?.componentStack ? `\n\nComponent stack:${info.componentStack}` : ''}
              </pre>
            </details>
          )}
        </div>
      </div>
    );
  }
}
