import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  AlertTriangle,
  ClipboardList,
  Hash,
  Zap,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector } from '../../../app/hooks';
import { fetchCodeVerificationQueue, fetchOpenConflictsCount } from '../../clinical/api/codingApi';
import { fetchQueueEntries } from '../api/schedulingApi';
import { fetchRiskAssessments, getDateRangeOptions } from '../api/riskApi';

interface DashboardMetrics {
  waitingCount: number;
  pendingCodesCount: number;
  openConflictsCount: number;
  highRiskCount: number;
}

interface RecentActivityItem {
  time: string;
  patientId: string;
  patientName: string;
  action: string;
  status: 'Waiting' | 'Accepted' | 'Resolved' | 'Done' | 'Alert';
}

const STATUS_STYLES: Record<RecentActivityItem['status'], string> = {
  Waiting: 'bg-amber-50 text-amber-700',
  Accepted: 'bg-green-50 text-green-700',
  Resolved: 'bg-blue-50 text-blue-700',
  Done: 'bg-green-50 text-green-700',
  Alert: 'bg-red-50 text-red-700',
};

const DEFAULT_METRICS: DashboardMetrics = {
  waitingCount: 0,
  pendingCodesCount: 0,
  openConflictsCount: 0,
  highRiskCount: 0,
};

function SummaryCardSkeleton() {
  return (
    <Card>
      <CardContent className="pt-5">
        <Skeleton className="mb-2 h-6 w-6" />
        <Skeleton className="mb-1 h-8 w-16" />
        <Skeleton className="h-4 w-24" />
      </CardContent>
    </Card>
  );
}

export function StaffDashboardPage() {
  const fullName = useAppSelector((state) => state.identity.fullName);
  const firstName = fullName?.split(' ')[0] ?? 'Staff';

  const [metrics, setMetrics] = useState<DashboardMetrics>(DEFAULT_METRICS);
  const [recentActivity, setRecentActivity] = useState<RecentActivityItem[]>([]);
  const [loading, setLoading] = useState(true);

  const today = new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  const loadDashboard = useCallback(async () => {
    setLoading(true);

    const riskRange = getDateRangeOptions()[1] ?? getDateRangeOptions()[0];

    const [queueResult, codesResult, riskResult, conflictsResult] = await Promise.all([
      fetchQueueEntries(),
      fetchCodeVerificationQueue({ status: 'Pending' }),
      fetchRiskAssessments({ startDate: riskRange.startDate, endDate: riskRange.endDate }),
      fetchOpenConflictsCount(),
    ]);

    const next: DashboardMetrics = { ...DEFAULT_METRICS };

    if (queueResult.success) {
      next.waitingCount = queueResult.data.waitingCount;
    }

    if (codesResult.success) {
      next.pendingCodesCount = codesResult.data.pendingCount;
    }

    if (riskResult.success) {
      next.highRiskCount = riskResult.data.filter((r) => r.riskLevel === 'High').length;
    }

    if (conflictsResult.success) {
      next.openConflictsCount = conflictsResult.data;
    }

    // Derive recent activity from queue entries (last 5 items by arrival time)
    const activityItems: RecentActivityItem[] = [];
    if (queueResult.success) {
      const sorted = [...queueResult.data.entries]
        .sort((a, b) => new Date(b.arrivalTime).getTime() - new Date(a.arrivalTime).getTime())
        .slice(0, 5);

      for (const entry of sorted) {
        const time = new Date(entry.arrivalTime).toLocaleTimeString('en-US', {
          hour: 'numeric',
          minute: '2-digit',
        });

        let action = 'Arrived';
        let status: RecentActivityItem['status'] = 'Waiting';

        if (entry.status === 'Completed') {
          action = 'Appointment completed';
          status = 'Done';
        } else if (entry.status === 'InProgress') {
          action = 'In progress with provider';
          status = 'Resolved';
        } else if (entry.status === 'Waiting') {
          action = 'Walk-in registered';
          status = 'Waiting';
        } else if (entry.status === 'NoShow') {
          action = 'No-show flagged';
          status = 'Alert';
        }

        activityItems.push({
          time,
          patientId: entry.patientId,
          patientName: entry.patientName,
          action,
          status,
        });
      }
    }

    setRecentActivity(activityItems);
    setMetrics(next);
    setLoading(false);
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const summaryCards = [
    {
      icon: ClipboardList,
      value: metrics.waitingCount,
      label: 'Queue / Waiting',
      to: '/staff/queue',
      valueColor: 'text-amber-500',
    },
    {
      icon: Hash,
      value: metrics.pendingCodesCount,
      label: 'Pending Codes',
      to: '/staff/codes',
      valueColor: 'text-primary',
    },
    {
      icon: Zap,
      value: metrics.openConflictsCount,
      label: 'Open Conflicts',
      to: '/staff/conflicts',
      valueColor: 'text-destructive',
    },
    {
      icon: AlertTriangle,
      value: metrics.highRiskCount,
      label: 'High No-Show Risk',
      to: '/staff/risk',
      valueColor: 'text-amber-700',
    },
  ];

  const quickActions = [
    {
      emoji: '🚶',
      title: 'Register Walk-In',
      description: 'Add a walk-in patient to the queue',
      to: '/staff/walk-in',
    },
    {
      emoji: '👤',
      title: 'Patient Lookup',
      description: 'Search and view complete patient records',
      to: '/staff/patient-view',
    },
    {
      emoji: '📋',
      title: 'Manage Queue',
      description: 'View and manage the real-time queue',
      to: '/staff/queue',
    },
  ];

  return (
    <div className="p-6 md:p-8">
      {/* Page header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-foreground">Staff Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {today} — Welcome back, {firstName}
        </p>
      </div>

      {/* Summary cards */}
      <div className="mb-8 grid grid-cols-2 gap-4 lg:grid-cols-4">
        {loading
          ? Array.from({ length: 4 }).map((_, i) => <SummaryCardSkeleton key={i} />)
          : summaryCards.map((card) => (
              <Link key={card.label} to={card.to} className="no-underline">
                <Card className="cursor-pointer transition-shadow hover:shadow-md">
                  <CardContent className="pt-5">
                    <card.icon className="mb-2 h-6 w-6 text-muted-foreground" aria-hidden />
                    <p className={`text-3xl font-bold ${card.valueColor}`}>{card.value}</p>
                    <p className="mt-1 text-sm text-muted-foreground">{card.label}</p>
                  </CardContent>
                </Card>
              </Link>
            ))}
      </div>

      {/* Quick Actions */}
      <h2 className="mb-4 text-xl font-semibold text-foreground">Quick Actions</h2>
      <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-3">
        {quickActions.map((action) => (
          <Link key={action.title} to={action.to} className="no-underline">
            <Card className="h-full cursor-pointer border transition-colors hover:border-primary">
              <CardContent className="pt-5">
                <p className="mb-1 text-[15px] font-semibold text-foreground">
                  {action.emoji} {action.title}
                </p>
                <p className="text-[13px] text-muted-foreground">{action.description}</p>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Recent Activity */}
      <h2 className="mb-4 text-xl font-semibold text-foreground">Recent Activity</h2>
      <Card>
        {loading ? (
          <CardContent className="space-y-3 pt-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full" />
            ))}
          </CardContent>
        ) : recentActivity.length === 0 ? (
          <CardContent className="py-10 text-center text-sm text-muted-foreground">
            No activity recorded today.
          </CardContent>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full" role="grid" aria-label="Recent staff activity">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Time
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Patient
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Action
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    Status
                  </th>
                </tr>
              </thead>
              <tbody>
                {recentActivity.map((item, idx) => (
                  <tr
                    key={idx}
                    className="border-b last:border-0 hover:bg-muted/30"
                  >
                    <td className="px-4 py-3 text-sm text-foreground">{item.time}</td>
                    <td className="px-4 py-3 text-sm font-medium">
                      <Link
                        to={`/staff/patient-view/${item.patientId}`}
                        className="text-primary hover:underline"
                      >
                        {item.patientName}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-sm text-foreground">{item.action}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-semibold ${STATUS_STYLES[item.status]}`}
                      >
                        {item.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
