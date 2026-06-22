import { Component, Fragment, type ErrorInfo, type ReactNode } from 'react';
import { logger } from '@/shared/lib/logger';

interface CanvasErrorBoundaryProps {
  children: ReactNode;
  fallbackLabel: string;
  retryLabel: string;
}

interface CanvasErrorBoundaryState {
  hasError: boolean;
  remountKey: number;
}

export class CanvasErrorBoundary extends Component<
  CanvasErrorBoundaryProps,
  CanvasErrorBoundaryState
> {
  state: CanvasErrorBoundaryState = { hasError: false, remountKey: 0 };

  static getDerivedStateFromError(): Partial<CanvasErrorBoundaryState> {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    logger.error('glass-designer.canvas-crash', {
      message: error.message,
      componentStack: info.componentStack,
    });
  }

  handleRetry = () => this.setState((s) => ({ hasError: false, remountKey: s.remountKey + 1 }));

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex h-full w-full flex-col items-center justify-center gap-3 bg-slate-100 p-6 text-center dark:bg-slate-950">
          <p className="max-w-xs text-sm text-slate-600 dark:text-slate-300">
            {this.props.fallbackLabel}
          </p>
          <button
            type="button"
            onClick={this.handleRetry}
            className="rounded-md bg-primary-600 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-primary-700"
          >
            {this.props.retryLabel}
          </button>
        </div>
      );
    }
    return <Fragment key={this.state.remountKey}>{this.props.children}</Fragment>;
  }
}
