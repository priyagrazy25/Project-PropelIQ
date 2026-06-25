import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { AlertTriangle, RefreshCw, WifiOff } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { toast } from 'sonner';
import type { QueueEntry, QueueStatus } from '../api/schedulingApi';
import {
  fetchQueueEntries,
  markArrival,
  updateQueueEntryStatus,
} from '../api/schedulingApi';
import { QueuePatientRow } from '../components/QueuePatientRow';
import type { QueueUpdateEvent } from '../hooks/useQueueSignalR';
import { useQueueSignalR } from '../hooks/useQueueSignalR';

type TabFilter = 'all' | 'Waiting' | 'InProgress' | 'Completed';

const TABS: { key: TabFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'Waiting', label: 'Waiting' },
  { key: 'InProgress', label: 'In Progress' },
  { key: 'Completed', label: 'Completed' },
];

export function QueueManagementPage() {
  const [entries, setEntries] = useState<QueueEntry[]>([]);
  const [averageWaitMinutes, setAverageWaitMinutes] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabFilter>('all');
  const [updatingIds, setUpdatingIds] = useState<Set<string>>(new Set());
  const location = useLocation();

  // Determine the correct base path for navigation
  const isAdminRoute = location.pathname.startsWith('/management');
  const dashboardPath = isAdminRoute ? '/management/dashboard' : '/staff/dashboard';
  const walkInPath = isAdminRoute ? '/management/walk-in' : '/staff/walk-in';

  const loadQueue = useCallback(async (showSkeleton = false) => {
    if (showSkeleton) setLoading(true);
    setError(null);

    const result = await fetchQueueEntries();

    if (result.success) {
      setEntries(result.data.entries);
      setAverageWaitMinutes(result.data.averageWaitMinutes);
    } else {
      setError(result.error.message);
    }

    setLoading(false);
  }, []);

  // eslint-disable-next-line react-hooks/exhaustive-deps, react-hooks/set-state-in-effect -- intentional mount-only fetch
  useEffect(() => {
    void loadQueue(true);
  }, []);

  const handleQueueUpdate = useCallback((event: QueueUpdateEvent) => {
    setEntries((prev) => {
      if (event.action === 'added') {
        const exists = prev.some((e) => e.id === event.entry.id);
        if (exists) {
          return prev.map((e) => (e.id === event.entry.id ? event.entry : e));
        }
        return [...prev, event.entry];
      }
      if (event.action === 'updated') {
        return prev.map((e) => (e.id === event.entry.id ? event.entry : e));
      }
      if (event.action === 'removed') {
        return prev.filter((e) => e.id !== event.entry.id);
      }
      return prev;
    });
  }, []);

  const handleReconnected = useCallback(() => {
    void loadQueue();
  }, [loadQueue]);

  const { connectionState } = useQueueSignalR({
    onQueueUpdate: handleQueueUpdate,
    onReconnected: handleReconnected,
    enabled: true,
  });

  const handleTransition = useCallback(
    async (entryId: string, newStatus: QueueStatus) => {
      const entry = entries.find((e) => e.id === entryId);
      if (!entry) return;

      // Optimistic update
      setEntries((prev) =>
        prev.map((e) => (e.id === entryId ? { ...e, status: newStatus } : e)),
      );
      setUpdatingIds((prev) => new Set(prev).add(entryId));

      // Use markArrival API when transitioning from Scheduled/Confirmed to Waiting
      const result =
        (entry.status === 'Scheduled' || entry.status === 'Confirmed') &&
        newStatus === 'Waiting'
          ? await markArrival(entryId, entry.rowVersion)
          : await updateQueueEntryStatus(entryId, newStatus, entry.rowVersion);

      setUpdatingIds((prev) => {
        const next = new Set(prev);
        next.delete(entryId);
        return next;
      });

      if (result.success) {
        // Replace with server-confirmed entry (updated rowVersion), preserving
        // the display position — the server returns position=0 for single-entry
        // updates since position is only meaningful in the full queue context.
        setEntries((prev) =>
          prev.map((e) => (e.id === entryId ? { ...result.data, position: e.position } : e)),
        );
      } else {
        // Rollback optimistic update
        setEntries((prev) => prev.map((e) => (e.id === entryId ? entry : e)));
        if (result.error.status === 409) {
          toast.error(result.error.message);
          void loadQueue();
        } else {
          toast.error(result.error.message);
        }
      }
    },
    [entries, loadQueue],
  );

  const filteredEntries = useMemo(() => {
    if (activeTab === 'all') return entries;
    if (activeTab === 'Waiting') {
      return entries.filter(
        (e) =>
          e.status === 'Waiting' ||
          e.status === 'Scheduled' ||
          e.status === 'Confirmed' ||
          e.status === 'Arrived',
      );
    }
    return entries.filter((e) => e.status === activeTab);
  }, [entries, activeTab]);

  const tabCounts = useMemo(() => {
    const waiting = entries.filter(
      (e) =>
        e.status === 'Waiting' ||
        e.status === 'Scheduled' ||
        e.status === 'Confirmed' ||
        e.status === 'Arrived',
    ).length;
    const inProgress = entries.filter((e) => e.status === 'InProgress').length;
    const completed = entries.filter(
      (e) =>
        e.status === 'Completed' ||
        e.status === 'Left' ||
        e.status === 'NoShow' ||
        e.status === 'Cancelled' ||
        e.status === 'Rescheduled',
    ).length;
    return {
      all: entries.length,
      Waiting: waiting,
      InProgress: inProgress,
      Completed: completed,
    };
  }, [entries]);

  // Skeleton loading state per UXR-501
  if (loading) {
    return (
      <div>
        <nav
          className="flex items-center gap-2 text-sm mb-6"
          aria-label="Breadcrumb"
        >
          <NavLink
            to="/staff/dashboard"
            className="text-primary hover:underline"
          >
            Dashboard
          </NavLink>
          <span className="text-muted-foreground" aria-hidden="true">
            &rsaquo;
          </span>
          <span aria-current="page">Queue Management</span>
        </nav>
        <h1 className="text-[32px] font-bold mb-6">Queue Management</h1>
        <div
          className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6"
          aria-busy="true"
          aria-label="Loading queue statistics"
        >
          {Array.from({ length: 4 }, (_, i) => (
            <Card key={i} className="p-5" aria-hidden="true">
              <Skeleton className="h-8 w-16 mb-1" />
              <Skeleton className="h-4 w-24" />
            </Card>
          ))}
        </div>
        <Skeleton className="h-10 w-80 mb-5" />
        <Card>
          <div className="space-y-0">
            <div className="bg-muted/50 px-4 py-3 flex gap-4">
              {Array.from({ length: 7 }, (_, i) => (
                <Skeleton key={i} className="h-4 flex-1" />
              ))}
            </div>
            {Array.from({ length: 6 }, (_, i) => (
              <div
                key={i}
                className="px-4 py-3 flex gap-4 border-b border-border"
              >
                {Array.from({ length: 7 }, (_, j) => (
                  <Skeleton key={j} className="h-4 flex-1" />
                ))}
              </div>
            ))}
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div>
      {/* Breadcrumb */}
      <nav
        className="flex items-center gap-2 text-sm mb-6"
        aria-label="Breadcrumb"
      >
        <NavLink to={dashboardPath} className="text-primary hover:underline">
          Dashboard
        </NavLink>
        <span className="text-muted-foreground" aria-hidden="true">
          &rsaquo;
        </span>
        <span aria-current="page">Queue Management</span>
      </nav>

      {/* Page Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-[32px] font-bold">Queue Management</h1>
        <div className="flex items-center gap-4">
          <span className="inline-flex items-center gap-2 rounded-full bg-green-50 px-3 py-1 text-xs font-semibold text-green-800">
            <span
              className="h-2 w-2 rounded-full bg-green-800 animate-pulse"
              aria-hidden="true"
            />
            Live
          </span>
          <NavLink
            to={walkInPath}
            className="inline-flex items-center justify-center h-10 px-4 rounded-md bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors no-underline"
          >
            + Walk-In
          </NavLink>
        </div>
      </div>

      {/* Reconnecting Banner */}
      {connectionState === 'reconnecting' && (
        <div
          className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 mb-4"
          role="status"
          aria-live="polite"
        >
          <WifiOff className="h-4 w-4 shrink-0" />
          Reconnecting to real-time updates…
        </div>
      )}

      {/* Error Banner */}
      {error && (
        <div
          className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive mb-5"
          role="alert"
          aria-live="assertive"
        >
          <AlertTriangle
            className="h-4 w-4 mt-0.5 shrink-0"
            aria-hidden="true"
          />
          <span className="flex-1">{error}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => void loadQueue(true)}
          >
            <RefreshCw className="h-3 w-3 mr-1" aria-hidden="true" />
            Retry
          </Button>
        </div>
      )}

      {/* KPI Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <Card className="p-5 shadow-sm">
          <div className="text-[28px] font-bold text-primary">
            {tabCounts.Waiting}
          </div>
          <div className="text-[13px] text-muted-foreground mt-1">Waiting</div>
        </Card>
        <Card className="p-5 shadow-sm">
          <div className="text-[28px] font-bold text-secondary">
            {tabCounts.InProgress}
          </div>
          <div className="text-[13px] text-muted-foreground mt-1">
            In Progress
          </div>
        </Card>
        <Card className="p-5 shadow-sm">
          <div className="text-[28px] font-bold text-green-800">
            {tabCounts.Completed}
          </div>
          <div className="text-[13px] text-muted-foreground mt-1">
            Completed Today
          </div>
        </Card>
        <Card className="p-5 shadow-sm">
          <div className="text-[28px] font-bold text-amber-500">
            {averageWaitMinutes} min
          </div>
          <div className="text-[13px] text-muted-foreground mt-1">
            Avg Wait Time
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <div
        className="flex gap-1 border-b-2 border-border mb-5"
        role="tablist"
        aria-label="Queue filter tabs"
      >
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.key}
            className={cn(
              'px-4 py-3 text-sm font-medium text-muted-foreground border-b-2 border-transparent -mb-0.5 cursor-pointer transition-colors bg-transparent',
              activeTab === tab.key &&
                'text-primary border-b-primary font-semibold',
            )}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.label}{' '}
            <span
              className={cn(
                'inline-flex items-center justify-center min-w-5 h-5 px-1.5 rounded-full text-[11px] font-semibold ml-1',
                activeTab === tab.key
                  ? 'bg-blue-50 text-primary'
                  : 'bg-muted text-muted-foreground',
              )}
            >
              {tabCounts[tab.key]}
            </span>
          </button>
        ))}
      </div>

      {/* Queue Table */}
      {filteredEntries.length === 0 && !error ? (
        <Card className="p-8 text-center">
          <p className="text-sm text-muted-foreground">
            No patients in the queue
            {activeTab !== 'all'
              ? ` with status "${TABS.find((t) => t.key === activeTab)?.label}"`
              : ''}
            .
          </p>
        </Card>
      ) : (
        <Card className="shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table
              className="w-full border-collapse"
              role="grid"
              aria-label="Queue entries"
            >
              <thead>
                <tr>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    #
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Patient
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Type
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Provider
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Status
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Wait Time
                  </th>
                  <th
                    scope="col"
                    className="text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground px-4 py-3 bg-muted/50 border-b border-border"
                  >
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody>
                {filteredEntries.map((entry) => (
                  <QueuePatientRow
                    key={entry.id}
                    entry={entry}
                    loading={updatingIds.has(entry.id)}
                    onTransition={(id, status) =>
                      void handleTransition(id, status)
                    }
                  />
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
