import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { AlertTriangle, RefreshCw, X } from 'lucide-react';
import { type ReactNode } from 'react';

interface ErrorBannerProps {
  message: ReactNode;
  onRetry?: () => void;
  onDismiss?: () => void;
  className?: string;
}

export function ErrorBanner({
  message,
  onRetry,
  onDismiss,
  className,
}: ErrorBannerProps) {
  return (
    <div
      className={cn(
        'flex items-start gap-2 rounded-md border border-destructive/30 bg-[var(--surface-danger)] px-4 py-3 text-sm text-destructive mb-5',
        className,
      )}
      role="alert"
      aria-live="assertive"
    >
      <AlertTriangle className="h-4 w-4 mt-0.5 shrink-0" aria-hidden="true" />
      <span className="flex-1">{message}</span>
      <div className="flex items-center gap-2 shrink-0">
        {onRetry && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-auto px-2 py-1 text-destructive hover:text-destructive hover:bg-destructive/10 underline"
            onClick={onRetry}
          >
            <RefreshCw className="h-3 w-3 mr-1" />
            Retry
          </Button>
        )}
        {onDismiss && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-6 w-6 text-destructive hover:text-destructive hover:bg-destructive/10"
            onClick={onDismiss}
            aria-label="Dismiss error"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  );
}
