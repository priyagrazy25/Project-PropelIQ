import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Loader2, MoreHorizontal } from 'lucide-react';
import type { QueueStatus } from '../api/schedulingApi';

interface StatusTransitionActionsProps {
  status: QueueStatus;
  loading: boolean;
  onTransition: (newStatus: QueueStatus) => void;
}

export function StatusTransitionActions({
  status,
  loading,
  onTransition,
}: StatusTransitionActionsProps) {
  // Terminal statuses - no further transitions
  if (
    status === 'Completed' ||
    status === 'Left' ||
    status === 'NoShow' ||
    status === 'Cancelled' ||
    status === 'Rescheduled'
  ) {
    return null;
  }

  return (
    <div className="flex items-center gap-1">
      {(status === 'Scheduled' || status === 'Confirmed') && (
        <Button
          size="sm"
          className="h-7 px-2 text-xs"
          disabled={loading}
          onClick={() => onTransition('Waiting')}
          aria-label="Mark patient as arrived"
        >
          {loading ? (
            <Loader2 className="h-3 w-3 animate-spin" aria-hidden="true" />
          ) : (
            'Mark Arrived'
          )}
        </Button>
      )}

      {(status === 'Waiting' || status === 'Arrived') && (
        <Button
          size="sm"
          className="h-7 px-2 text-xs"
          disabled={loading}
          onClick={() => onTransition('InProgress')}
          aria-label="Start visit"
        >
          {loading ? (
            <Loader2 className="h-3 w-3 animate-spin" aria-hidden="true" />
          ) : (
            'Start'
          )}
        </Button>
      )}

      {status === 'InProgress' && (
        <Button
          size="sm"
          className="h-7 px-2 text-xs bg-secondary text-secondary-foreground hover:bg-secondary/80"
          disabled={loading}
          onClick={() => onTransition('Completed')}
          aria-label="Complete visit"
        >
          {loading ? (
            <Loader2 className="h-3 w-3 animate-spin" aria-hidden="true" />
          ) : (
            'Complete'
          )}
        </Button>
      )}

      <DropdownMenu>
        <DropdownMenuTrigger
          className="inline-flex items-center justify-center h-7 w-7 p-0 rounded-md text-sm font-medium hover:bg-muted disabled:pointer-events-none disabled:opacity-50"
          disabled={loading}
          aria-label="More actions"
        >
          <MoreHorizontal className="h-4 w-4" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {(status === 'Waiting' || status === 'Arrived') && (
            <DropdownMenuItem onClick={() => onTransition('NoShow')}>
              Mark No-Show
            </DropdownMenuItem>
          )}
          <DropdownMenuItem onClick={() => onTransition('Left')}>
            Mark Left
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
