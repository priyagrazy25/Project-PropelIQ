import { cn } from '@/lib/utils';
import { useEffect, useState } from 'react';
import { NavLink } from 'react-router-dom';
import type { QueueEntry, QueueStatus } from '../api/schedulingApi';
import { StatusBadge } from './StatusBadge';
import { StatusTransitionActions } from './StatusTransitionActions';

interface QueuePatientRowProps {
  entry: QueueEntry;
  loading: boolean;
  onTransition: (entryId: string, newStatus: QueueStatus) => void;
}

function formatArrivalTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function computeWaitMinutes(arrivalIso: string): number {
  return Math.max(
    0,
    Math.floor((Date.now() - new Date(arrivalIso).getTime()) / 60_000),
  );
}

function formatWaitDuration(minutes: number): string {
  if (minutes < 1) return '<1 min';
  if (minutes < 60) return `${String(minutes)} min`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m > 0 ? `${String(h)}h ${String(m)}m` : `${String(h)}h`;
}

const LONG_WAIT_THRESHOLD = 30;

export function QueuePatientRow({
  entry,
  loading,
  onTransition,
}: QueuePatientRowProps) {
  const isTerminal =
    entry.status === 'Completed' ||
    entry.status === 'Left' ||
    entry.status === 'NoShow' ||
    entry.status === 'Cancelled' ||
    entry.status === 'Rescheduled';

  const isPreArrival = entry.status === 'Scheduled' || entry.status === 'Confirmed';

  const [waitMinutes, setWaitMinutes] = useState(() =>
    isTerminal || isPreArrival || !entry.arrivalTime
      ? 0
      : computeWaitMinutes(entry.arrivalTime),
  );

  useEffect(() => {
    if (isTerminal || isPreArrival || !entry.arrivalTime) return;

    const id = setInterval(() => {
      setWaitMinutes(computeWaitMinutes(entry.arrivalTime!));
    }, 60_000);

    return () => clearInterval(id);
  }, [entry.arrivalTime, isTerminal, isPreArrival]);

  const isLongWait =
    waitMinutes >= LONG_WAIT_THRESHOLD && !isTerminal && !isPreArrival;

  return (
    <tr className="hover:bg-muted/50 transition-colors duration-150">
      <td className="px-4 py-3 text-sm border-b border-border">
        {entry.position}
      </td>
      <td className="px-4 py-3 border-b border-border">
        <NavLink
          to={`/staff/patient-view/${entry.patientId}`}
          className="text-sm font-medium text-primary hover:underline no-underline"
        >
          {entry.patientName}
        </NavLink>
        {entry.arrivalTime ? (
          <div className="text-xs text-muted-foreground">
            Arrived {formatArrivalTime(entry.arrivalTime)}
          </div>
        ) : (
          <div className="text-xs text-muted-foreground">Not yet arrived</div>
        )}
      </td>
      <td className="px-4 py-3 text-sm border-b border-border">
        {entry.appointmentType}
      </td>
      <td className="px-4 py-3 text-sm border-b border-border">
        {entry.providerName}
      </td>
      <td className="px-4 py-3 border-b border-border">
        <StatusBadge status={entry.status} />
      </td>
      <td className="px-4 py-3 border-b border-border">
        {isTerminal || isPreArrival ? (
          <span className="text-sm text-muted-foreground">&mdash;</span>
        ) : (
          <span
            className={cn(
              'text-[13px]',
              isLongWait
                ? 'text-destructive font-medium'
                : 'text-muted-foreground',
            )}
          >
            {formatWaitDuration(waitMinutes)}
          </span>
        )}
      </td>
      <td className="px-4 py-3 border-b border-border">
        <StatusTransitionActions
          status={entry.status}
          loading={loading}
          onTransition={(newStatus) => onTransition(entry.id, newStatus)}
        />
      </td>
    </tr>
  );
}
