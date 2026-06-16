import { cn } from '@/lib/utils';
import type { QueueStatus } from '../api/schedulingApi';

const STATUS_CONFIG: Record<QueueStatus, { label: string; className: string }> =
  {
    Scheduled: {
      label: 'Scheduled',
      className: 'bg-blue-50 text-blue-800',
    },
    Confirmed: {
      label: 'Confirmed',
      className: 'bg-emerald-50 text-emerald-800',
    },
    Arrived: {
      label: 'Arrived',
      className: 'bg-amber-50 text-amber-800',
    },
    Waiting: {
      label: 'Waiting',
      className: 'bg-amber-50 text-amber-800',
    },
    InProgress: {
      label: 'In Progress',
      className: 'bg-blue-50 text-primary',
    },
    Completed: {
      label: 'Done',
      className: 'bg-green-50 text-green-800',
    },
    Cancelled: {
      label: 'Cancelled',
      className: 'bg-slate-100 text-slate-600',
    },
    Left: {
      label: 'Left',
      className: 'bg-red-50 text-destructive',
    },
    NoShow: {
      label: 'No-Show',
      className: 'bg-red-50 text-destructive',
    },
    Rescheduled: {
      label: 'Rescheduled',
      className: 'bg-purple-50 text-purple-800',
    },
  };

interface StatusBadgeProps {
  status: QueueStatus;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = STATUS_CONFIG[status];

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold',
        config.className,
        className,
      )}
    >
      {config.label}
    </span>
  );
}
