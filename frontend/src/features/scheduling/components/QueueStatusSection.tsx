import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Clock, UserCheck } from 'lucide-react';
import { useEffect, useState } from 'react';
import { fetchMyQueueStatus, type PatientQueueStatus } from '../api/schedulingApi';

function formatTime(isoString: string | null): string {
  if (!isoString) return 'N/A';
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });
}

function formatWaitTime(minutes: number): string {
  if (minutes < 1) return 'Just arrived';
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
}

export function QueueStatusSection() {
  const [status, setStatus] = useState<PatientQueueStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [notInQueue, setNotInQueue] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const loadStatus = async () => {
      const result = await fetchMyQueueStatus();
      if (cancelled) return;

      if (result.success) {
        setStatus(result.data);
        setNotInQueue(false);
      } else if ('notInQueue' in result && result.notInQueue) {
        setNotInQueue(true);
      }
      setLoading(false);
    };

    void loadStatus();

    // Poll every 30 seconds for updates
    const interval = setInterval(() => {
      void loadStatus();
    }, 30_000);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  if (loading) {
    return (
      <Card>
        <CardContent className="p-4">
          <Skeleton className="mb-2 h-5 w-32" />
          <Skeleton className="h-4 w-48" />
        </CardContent>
      </Card>
    );
  }

  // Don't render anything if patient is not in queue
  if (notInQueue || !status) {
    return null;
  }

  const isInProgress = status.status === 'InProgress';

  return (
    <Card className={isInProgress ? 'border-green-200 bg-green-50' : 'border-blue-200 bg-blue-50'}>
      <CardContent className="p-4">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className={`rounded-full p-2 ${isInProgress ? 'bg-green-100' : 'bg-blue-100'}`}>
              {isInProgress ? (
                <UserCheck className="h-5 w-5 text-green-600" />
              ) : (
                <Clock className="h-5 w-5 text-blue-600" />
              )}
            </div>
            <div>
              <h3 className="font-semibold">
                {isInProgress ? 'You\'re Being Seen' : 'You\'re in the Queue'}
              </h3>
              <p className="text-sm text-muted-foreground">
                {status.appointmentType} with {status.providerName}
              </p>
            </div>
          </div>
          <Badge variant={isInProgress ? 'default' : 'secondary'} className={isInProgress ? 'bg-green-600' : ''}>
            {isInProgress ? 'In Progress' : `Position #${status.position}`}
          </Badge>
        </div>

        {!isInProgress && (
          <div className="mt-3 flex items-center gap-4 text-sm">
            <div className="flex items-center gap-1.5">
              <Clock className="h-4 w-4 text-muted-foreground" />
              <span>Arrived at {formatTime(status.arrivalTime)}</span>
            </div>
            <div className="text-muted-foreground">
              Wait time: <span className="font-medium text-foreground">{formatWaitTime(status.waitDurationMinutes)}</span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
