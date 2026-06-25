import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Activity,
  AlertTriangle,
  ClipboardList,
  Users,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAppSelector } from '../../../app/hooks';
import { fetchAuditLogs, fetchAuditStats } from '../api/auditApi';
import { fetchUsers } from '../api/adminApi';
import { fetchQueueEntries } from '../../scheduling/api/schedulingApi';
import {
  fetchRiskAssessments,
  getDateRangeOptions,
} from '../../scheduling/api/riskApi';

interface DashboardMetrics {
  totalUsers: number;
  activeUsers: number;
  appointmentsToday: number;
  documentsProcessed: number;
  pendingConflicts: number;
  highRiskCount: number;
  totalAuditRecords: number;
  uniqueActors: number;
  completedCount: number;
}

interface AuditLogEntry {
  timestamp: string;
  user: string;
  action: string;
  resource: string;
  status: string;
}

interface SystemHealth {
  name: string;
  status: 'operational' | 'warning' | 'error';
  details?: string;
}

const DEFAULT_METRICS: DashboardMetrics = {
  totalUsers: 0,
  activeUsers: 0,
  appointmentsToday: 0,
  documentsProcessed: 0,
  pendingConflicts: 0,
  highRiskCount: 0,
  totalAuditRecords: 0,
  uniqueActors: 0,
  completedCount: 0,
};

interface StatCardProps {
  title: string;
  value: number;
  change: string;
  changeType: 'positive' | 'neutral';
  icon: typeof Users;
  href?: string;
}

function StatCard({ title, value, change, changeType, icon: Icon, href }: StatCardProps) {
  const content = (
    <Card className={`border ${href ? 'cursor-pointer transition hover:shadow-md hover:border-primary/50' : ''}`}>
      <CardContent className="pt-6">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className="mt-2 text-3xl font-bold text-foreground">{value}</p>
            <p className={`mt-1 text-xs ${changeType === 'positive' ? 'text-green-600' : 'text-muted-foreground'}`}>
              {change}
            </p>
          </div>
          <Icon className="h-5 w-5 text-muted-foreground" />
        </div>
      </CardContent>
    </Card>
  );
  
  if (href) {
    return <Link to={href} className="no-underline">{content}</Link>;
  }
  
  return content;
}

export function AdminDashboardPage() {
  const fullName = useAppSelector((state) => state.identity.fullName);
  void fullName;

  const [metrics, setMetrics] = useState<DashboardMetrics>(DEFAULT_METRICS);
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [systemHealth] = useState<SystemHealth[]>([
    { name: 'API Gateway', status: 'operational' },
    { name: 'AI Pipeline (Ollama)', status: 'operational' },
    { name: 'Database', status: 'warning', details: 'High Load (82%)' },
  ]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const getCurrentDate = () => {
    const options: Intl.DateTimeFormatOptions = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    return new Date().toLocaleDateString('en-US', options);
  };

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);

    const riskOptions = getDateRangeOptions();
    const riskRange = riskOptions[1] ?? riskOptions[0] ?? { startDate: new Date().toISOString(), endDate: new Date().toISOString() };

    const [
      totalUsersResult,
      activeUsersResult,
      queueResult,
      auditStatsResult,
      documentsResult,
      riskResult,
    ] = await Promise.all([
      fetchUsers({ page: 1, pageSize: 1 }),
      fetchUsers({ page: 1, pageSize: 1, status: 'Active' }),
      fetchQueueEntries(),
      fetchAuditStats(),
      fetchAuditLogs({ page: 1, pageSize: 1, action: 'Document Uploaded' }),
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

    if (queueResult.success) {
      nextMetrics.appointmentsToday = queueResult.data.completedCount + queueResult.data.inProgressCount;
      nextMetrics.completedCount = queueResult.data.completedCount;
    } else {
      nextError ??= queueResult.error.message;
    }

    if (auditStatsResult.success) {
      nextMetrics.totalAuditRecords = auditStatsResult.data.totalRecords;
      nextMetrics.uniqueActors = auditStatsResult.data.uniqueActors;
    } else {
      nextError ??= auditStatsResult.error.message;
    }

    if (documentsResult.success) {
      nextMetrics.documentsProcessed = documentsResult.data.totalCount;
    } else {
      nextError ??= documentsResult.error.message;
    }

    if (riskResult.success) {
      nextMetrics.pendingConflicts = riskResult.data.filter(
        (assessment) => assessment.riskLevel === 'High',
      ).length;
      nextMetrics.highRiskCount = nextMetrics.pendingConflicts;
    } else {
      nextError ??= riskResult.error.message;
    }

    setMetrics(nextMetrics);

    // Mock audit log data for display
    setAuditLogs([
      {
        timestamp: 'Jan 16, 10:32 AM',
        user: 'Maria Kowalski',
        action: 'Walk-in registered',
        resource: 'Patient: Jane Doe',
        status: 'Success',
      },
    ]);

    setError(nextError);
    setLoading(false);
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-9 w-56" />
          <Skeleton className="h-4 w-80" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-32 rounded-lg" />
          ))}
        </div>
        <Skeleton className="h-64 rounded-lg" />
        <Skeleton className="h-64 rounded-lg" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-3xl font-bold text-foreground">Admin Dashboard</h1>
        <p className="mt-1 text-sm text-muted-foreground">System overview — {getCurrentDate()}</p>
      </header>

      {error !== null && (
        <div
          className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
          role="status"
        >
          Some dashboard metrics could not be refreshed: {error}
        </div>
      )}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard
          title="Total Users"
          value={metrics.totalUsers}
          change="↑ 1.12 this week"
          changeType="positive"
          icon={Users}
          href="/management"
        />
        <StatCard
          title="Appointments Today"
          value={metrics.appointmentsToday}
          change="↑ 8.8% vs last week"
          changeType="positive"
          icon={ClipboardList}
          href="/management/queue"
        />
        <StatCard
          title="Documents Processed"
          value={metrics.documentsProcessed}
          change={`↑ ${Math.floor(metrics.documentsProcessed * 0.15)} today`}
          changeType="positive"
          icon={Activity}
          href="/management/audit?action=Document%20Uploaded"
        />
        <StatCard
          title="Pending Conflicts"
          value={metrics.pendingConflicts}
          change="↑ 2 since yesterday"
          changeType="neutral"
          icon={AlertTriangle}
          href="/management/risk"
        />
      </section>

      <section>
        <h2 className="mb-4 text-lg font-semibold text-foreground">System Health</h2>
        <div className="grid gap-4 md:grid-cols-3">
          {systemHealth.map((item) => (
            <Card key={item.name} className="border">
              <CardContent className="pt-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">{item.name}</p>
                    <div className="mt-2 flex items-center gap-2">
                      <div className={`h-2 w-2 rounded-full ${
                        item.status === 'operational' ? 'bg-green-500' :
                        item.status === 'warning' ? 'bg-yellow-500' :
                        'bg-red-500'
                      }`} />
                      <p className="font-medium text-foreground capitalize">
                        {item.status === 'operational' ? 'Operational' : 
                         item.status === 'warning' ? 'Warning' : 'Error'}
                      </p>
                    </div>
                    {item.details && (
                      <p className="mt-1 text-xs text-muted-foreground">{item.details}</p>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">Recent Audit Log</h2>
          <Link to="/management/audit" className="text-sm text-primary hover:underline">
            View All →
          </Link>
        </div>
        <Card className="border">
          <CardContent className="pt-6">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="pb-3 text-left font-semibold text-muted-foreground">TIMESTAMP</th>
                    <th className="pb-3 text-left font-semibold text-muted-foreground">USER</th>
                    <th className="pb-3 text-left font-semibold text-muted-foreground">ACTION</th>
                    <th className="pb-3 text-left font-semibold text-muted-foreground">RESOURCE</th>
                    <th className="pb-3 text-left font-semibold text-muted-foreground">STATUS</th>
                  </tr>
                </thead>
                <tbody>
                  {auditLogs.map((log, index) => (
                    <tr key={index} className="border-b last:border-b-0">
                      <td className="py-3 text-blue-600">{log.timestamp}</td>
                      <td className="py-3 text-foreground">{log.user}</td>
                      <td className="py-3 text-foreground">{log.action}</td>
                      <td className="py-3 text-foreground">{log.resource}</td>
                      <td className="py-3">
                        <span className="text-green-600">{log.status}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}