import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { AlertCircle, CheckCircle2, Clock, XCircle } from 'lucide-react';
import type { CalendarSyncState } from '../../scheduling/api/schedulingApi';

interface StatusConfig {
  icon: React.ReactNode;
  label: string;
  containerClassName: string;
  textClassName: string;
}

const STATUS_MAP: Record<Exclude<CalendarSyncState, 'idle'>, StatusConfig> = {
  synced: {
    icon: <CheckCircle2 className="h-6 w-6 shrink-0" aria-hidden="true" />,
    label:
      'Calendar connected successfully. Future appointments will be synced automatically.',
    containerClassName: 'border-teal-300 bg-teal-50',
    textClassName: 'text-teal-800',
  },
  pending: {
    icon: <Clock className="h-6 w-6 shrink-0" aria-hidden="true" />,
    label: 'Sync pending',
    containerClassName: 'border-amber-300 bg-amber-50',
    textClassName: 'text-amber-800',
  },
  failed: {
    icon: <XCircle className="h-6 w-6 shrink-0" aria-hidden="true" />,
    label: 'Sync failed',
    containerClassName: 'border-red-300 bg-red-50',
    textClassName: 'text-red-800',
  },
};

interface CalendarSyncStatusProps {
  state: CalendarSyncState;
  message?: string | null;
  onRetry?: () => void;
}

export function CalendarSyncStatus({
  state,
  message,
  onRetry,
}: CalendarSyncStatusProps) {
  if (state === 'idle') return null;

  const config = STATUS_MAP[state];

  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-lg border p-4',
        config.containerClassName,
      )}
      role={state === 'failed' ? 'alert' : 'status'}
      aria-live="polite"
    >
      <span className={config.textClassName}>{config.icon}</span>
      <div className="flex-1">
        <p className={cn('text-sm font-medium', config.textClassName)}>
          {config.label}
        </p>
        {message && (
          <p className={cn('text-sm mt-0.5', config.textClassName)}>
            {message}
          </p>
        )}
      </div>
      {state === 'failed' && onRetry && (
        <Button
          variant="outline"
          size="sm"
          onClick={onRetry}
          aria-label="Retry calendar sync"
        >
          <AlertCircle className="h-3.5 w-3.5 mr-1" aria-hidden="true" />
          Retry
        </Button>
      )}
    </div>
  );
}
