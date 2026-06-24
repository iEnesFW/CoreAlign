import { Component, type ErrorInfo, type ReactNode } from 'react';
import i18n from '@/app/i18n';
import { reportClientError } from '@/shared/lib/clientErrorReporter';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    reportClientError({
      message: `${error.name}: ${error.message}`,
      severity: 'Error',
      component: 'ErrorBoundary',
      stack: error.stack,
      context: { componentStack: info.componentStack },
    });
  }

  handleReload = (): void => {
    window.location.reload();
  };

  render(): ReactNode {
    if (!this.state.hasError) return this.props.children;

    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-white px-4 py-10 text-center dark:bg-slate-900">
        <div className="w-full max-w-md">
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {i18n.t('errors.boundary.title')}
          </h1>
          <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
            {i18n.t('errors.boundary.description')}
          </p>
          <button
            type="button"
            onClick={this.handleReload}
            className="mt-6 rounded-xl bg-sky-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-sky-500"
          >
            {i18n.t('errors.boundary.reload')}
          </button>
        </div>
      </div>
    );
  }
}
