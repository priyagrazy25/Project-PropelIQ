import { cn } from '@/lib/utils';
import { AlertTriangle, CheckCircle, Loader2 } from 'lucide-react';
import type { AutosaveStatus } from '../hooks/useAutosave';

interface AutosaveIndicatorProps {
  status: AutosaveStatus;
  lastSavedAt: Date | null;
}

const statusConfig: Record<
  AutosaveStatus,
  { label: string; icon: React.ReactNode; className: string }
> = {
  idle: { label: '', icon: null, className: '' },
  saving: {
    label: 'Saving…',
    icon: <Loader2 className="h-4 w-4 animate-spin" />,
    className: 'text-muted-foreground',
  },
  saved: {
    label: 'Saved',
    icon: <CheckCircle className="h-4 w-4" />,
    className: 'text-green-600',
  },
  error: {
    label: 'Saved locally',
    icon: <AlertTriangle className="h-4 w-4" />,
    className: 'text-amber-600',
  },
};

export function AutosaveIndicator({
  status,
  lastSavedAt,
}: AutosaveIndicatorProps) {
  if (status === 'idle' && !lastSavedAt) return null;

  const config = statusConfig[status];

  const timeLabel =
    status === 'idle' && lastSavedAt
      ? `Last saved ${lastSavedAt.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}`
      : config.label;

  return (
    <div
      className={cn(
        'flex items-center gap-1.5 text-sm transition-opacity',
        config.className,
        status === 'idle' && 'text-muted-foreground',
      )}
      role="status"
      aria-live="polite"
    >
      {status !== 'idle' && config.icon}
      <span>{timeLabel}</span>
    </div>
  );
}
