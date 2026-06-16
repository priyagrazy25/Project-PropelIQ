import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Activity,
  AlertTriangle,
  ArrowRight,
  ClipboardList,
  Clock3,
  Shield,
  UserCog,
  Users,
} from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector } from '../../../app/hooks';
import { fetchAuditStats } from '../api/auditApi';
import { fetchUsers } from '../api/adminApi';
import { fetchQueueEntries } from '../../scheduling/api/schedulingApi';
import {
  fetchRiskAssessments,
  getDateRangeOptions,
} from '../../scheduling/api/riskApi';

interface DashboardMetrics {
  totalUsers: number;
  activeUsers: number;
  patients: number;
  admins: number;
  waitingCount: number;
  inProgressCount: number;
  completedCount: number;
  averageWaitMinutes: number;
  highRiskCount: number;
  totalAuditRecords: number;
  uniqueActors: number;
}

interface QueueSnapshotItem {
  id: string;
  patientName: string;
  providerName: string;
  appointmentType: string;
  status: string;
  position: number;
}

const DEFAULT_METRICS: DashboardMetrics = {
  totalUsers: 0,
  activeUsers: 0,
  patients: 0,
  admins: 0,
  waitingCount: 0,
  inProgressCount: 0,
  completedCount: 0,
  averageWaitMinutes: 0,
  highRiskCount: 0,
  totalAuditRecords: 0,
  uniqueActors: 0,
};

function MetricCard({
  title,
  value,
  description,
  icon: Icon,
}: {
  title: string;
  value: string | number;
  description: string;
  icon: typeof Users;
}) {
  return (
    <Card>
      <CardContent className="flex items-start justify-between pt-4">
        <div>
          <p className="text-sm text-muted-foreground">{title}</p>
          <p className="mt-2 text-3xl font-bold text-foreground">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{description}</p>
        </div>
        <div className="rounded-full bg-primary/10 p-3 text-primary">
          <Icon className="h-5 w-5" />
        </div>
      </CardContent>
    </Card>
  );
}

export function AdminDashboardPage() {
  const fullName = useAppSelector((state) => state.identity.fullName);
  const firstName = fullName?.split(' ')[0] ?? 'Admin';

  const [metrics, setMetrics] = useState<DashboardMetrics>(DEFAULT_METRICS);
  const [queueSnapshot, setQueueSnapshot] = useState<QueueSnapshotItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);

    const riskRange = getDateRangeOptions()[1] ?? getDateRangeOptions()[0];

    const [
      totalUsersResult,
      activeUsersResult,
      patientsResult,
      adminsResult,
      queueResult,
      auditStatsResult,
      riskResult,
    ] = await Promise.all([
      fetchUsers({ page: 1, pageSize: 1 }),
      fetchUsers({ page: 1, pageSize: 1, status: 'Active' }),
      fetchUsers({ page: 1, pageSize: 1, role: 'Patient' }),
      fetchUsers({ page: 1, pageSize: 1, role: 'Admin' }),
      fetchQueueEntries(),
      fetchAuditStats(),
      fetchRiskAssessments({
        startDate: riskRange.startDate,
        endDate: riskRange.endDate,
      }),
    ]);

    const nextMetrics = { ...DEFAULT_METRICS };
    let nextError: string | null = null;

    if (totalUsersResult.success) nextMetrics.totalUsers = totalUsersResult.data.totalCount;
    else nextError = totalUsersResult.error.message;

    if (activeUsersResult.success) nextMetrics.activeUsers = activeUsersResult.data.totalCount;
    else nextError ??= activeUsersResult.error.message;

    if (patientsResult.success) nextMetrics.patients = patientsResult.data.totalCount;
    else nextError ??= patientsResult.error.message;

    if (adminsResult.success) nextMetrics.admins = adminsResult.data.totalCount;
    else nextError ??= adminsResult.error.message;

    if (queueResult.success) {
      nextMetrics.waitingCount = queueResult.data.waitingCount;
      nextMetrics.inProgressCount = queueResult.data.inProgressCount;
      nextMetrics.completedCount = queueResult.data.completedCount;
      nextMetrics.averageWaitMinutes = queueResult.data.averageWaitMinutes;
      setQueueSnapshot(queueResult.data.entries.slice(0, 5));
    } else {
      nextError ??= queueResult.error.message;
      setQueueSnapshot([]);
    }

    if (auditStatsResult.success) {
      nextMetrics.totalAuditRecords = auditStatsResult.data.totalRecords;
      nextMetrics.uniqueActors = auditStatsResult.data.uniqueActors;
    } else {
      nextError ??= auditStatsResult.error.message;
    }

    if (riskResult.success) {
      nextMetrics.highRiskCount = riskResult.data.filter(
        (assessment) => assessment.riskLevel === 'High',
      ).length;
    } else {
      nextError ??= riskResult.error.message;
    }

    setMetrics(nextMetrics);
    setError(nextError);
    setLoading(false);
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const quickLinks = useMemo(
    () => [
      {
        to: '/management',
        title: 'Review user access',
        description: 'Create users, adjust roles, and manage active accounts.',
      },
      {
        to: '/management/queue',
        title: 'Monitor today\'s queue',
        description: 'Track waiting, in-progress, and completed visits.',
      },
      {
        to: '/management/audit',
        title: 'Check audit activity',
        description: 'Review user actions and export compliance logs.',
      },
      {
        to: '/management/risk',
        title: 'Review no-show risk',
        description: 'Prioritize outreach for higher-risk appointments.',
      },
    ],
    [],
  );

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-9 w-56" />
          <Skeleton className="h-4 w-80" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 8 }).map((_, index) => (
            <Skeleton key={index} className="h-32 rounded-xl" />
          ))}
        </div>
        <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
          <Skeleton className="h-80 rounded-xl" />
          <Skeleton className="h-80 rounded-xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            Admin Dashboard
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Welcome back, {firstName}. Here is the current platform and operations overview.
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Button asChild variant="outline">
            <Link to="/management/audit">Open Audit Logs</Link>
          </Button>
          <Button asChild>
            <Link to="/management">Manage Users</Link>
          </Button>
        </div>
      </header>

      {error !== null && (
        <div
          className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
          role="status"
        >
          Some dashboard metrics could not be refreshed: {error}
        </div>
      )}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Total users"
          value={metrics.totalUsers}
          description="All registered accounts"
          icon={Users}
        />
        <MetricCard
          title="Active accounts"
          value={metrics.activeUsers}
          description="Users currently enabled"
          icon={UserCog}
        />
        <MetricCard
          title="Patients"
          value={metrics.patients}
          description="Patient role accounts"
          icon={Activity}
        />
        <MetricCard
          title="Administrators"
          value={metrics.admins}
          description="Admin role accounts"
          icon={Shield}
        />
        <MetricCard
          title="Waiting queue"
          value={metrics.waitingCount}
          description="Patients waiting right now"
          icon={ClipboardList}
        />
        <MetricCard
          title="In progress"
          value={metrics.inProgressCount}
          description="Visits currently being handled"
          icon={Clock3}
        />
        <MetricCard
          title="Average wait"
          value={`${metrics.averageWaitMinutes} min`}
          description="Completed visit average today"
          icon={Clock3}
        />
        <MetricCard
          title="High-risk appointments"
          value={metrics.highRiskCount}
          description="Flagged over the next 7 days"
          icon={AlertTriangle}
        />
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <Card>
          <CardHeader>
            <CardTitle>Queue snapshot</CardTitle>
            <CardDescription>
              Live operational view of the first patients in today&apos;s queue.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {queueSnapshot.length === 0 ? (
              <div className="rounded-lg border border-dashed p-6 text-sm text-muted-foreground">
                No active queue entries were returned for today.
              </div>
            ) : (
              <div className="space-y-3">
                {queueSnapshot.map((entry) => (
                  <div
                    key={entry.id}
                    className="flex flex-col gap-3 rounded-lg border p-4 lg:flex-row lg:items-center lg:justify-between"
                  >
                    <div>
                      <p className="font-medium text-foreground">{entry.patientName}</p>
                      <p className="text-sm text-muted-foreground">
                        {entry.providerName} · {entry.appointmentType}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge variant="outline">Position #{entry.position}</Badge>
                      <Badge>{entry.status}</Badge>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Compliance overview</CardTitle>
              <CardDescription>
                Audit and completion signals for today&apos;s operations.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between rounded-lg border p-4">
                <div>
                  <p className="text-sm text-muted-foreground">Audit records</p>
                  <p className="text-2xl font-bold">{metrics.totalAuditRecords}</p>
                </div>
                <Shield className="h-5 w-5 text-primary" />
              </div>
              <div className="flex items-center justify-between rounded-lg border p-4">
                <div>
                  <p className="text-sm text-muted-foreground">Unique actors</p>
                  <p className="text-2xl font-bold">{metrics.uniqueActors}</p>
                </div>
                <Users className="h-5 w-5 text-primary" />
              </div>
              <div className="flex items-center justify-between rounded-lg border p-4">
                <div>
                  <p className="text-sm text-muted-foreground">Completed visits</p>
                  <p className="text-2xl font-bold">{metrics.completedCount}</p>
                </div>
                <ClipboardList className="h-5 w-5 text-primary" />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Quick actions</CardTitle>
              <CardDescription>
                Jump into the highest-value admin workflows.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {quickLinks.map((link) => (
                <Link
                  key={link.to}
                  to={link.to}
                  className="flex items-center justify-between rounded-lg border p-4 no-underline transition hover:bg-muted"
                >
                  <div>
                    <p className="font-medium text-foreground">{link.title}</p>
                    <p className="text-sm text-muted-foreground">{link.description}</p>
                  </div>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                </Link>
              ))}
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}