import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ResponsiveTable, type ResponsiveTableColumn } from '@/components/common/ResponsiveTable';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { AlertTriangle, RefreshCw, Send } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { toast } from 'sonner';
import type {
  AppointmentRiskAssessment,
  RiskAssessmentParams,
  RiskLevel,
} from '../api/riskApi';
import {
  fetchRiskAssessments,
  getDateRangeOptions,
  sendRiskReminder,
} from '../api/riskApi';
import { RiskBadge, RiskIndicator } from '../components/RiskIndicator';

type DateRangeOption = 'today' | 'next7' | 'next30';

/**
 * No-Show Risk Dashboard (SCR-022).
 * Displays appointment risk assessments with filtering and sorting.
 * Implements AC-2 (visual indicators) and AC-5 (sort/filter).
 */
export function NoShowRiskDashboardPage() {
  const [assessments, setAssessments] = useState<AppointmentRiskAssessment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isDemo, setIsDemo] = useState(false);
  const [riskLevelFilter, setRiskLevelFilter] = useState<RiskLevel | 'all'>('all');
  const [dateRangeOption, setDateRangeOption] = useState<DateRangeOption>('today');
  const [sendingReminder, setSendingReminder] = useState<string | null>(null);

  const dateRanges = useMemo(() => getDateRangeOptions(), []);

  const loadAssessments = useCallback(
    async (showSkeleton = false) => {
      if (showSkeleton) setLoading(true);
      setError(null);

      const rangeIndex = dateRangeOption === 'today' ? 0 : dateRangeOption === 'next7' ? 1 : 2;
      const range = dateRanges[rangeIndex]!; // Always 0, 1, or 2 - guaranteed to exist

      const params: RiskAssessmentParams = {
        startDate: range.startDate,
        endDate: range.endDate,
        riskLevel: riskLevelFilter !== 'all' ? riskLevelFilter : undefined,
      };

      const result = await fetchRiskAssessments(params);

      if (result.success) {
        // Sort by risk score descending (AC-5: highest first)
        const sorted = [...result.data].sort((a, b) => b.riskScore - a.riskScore);
        setAssessments(sorted);
        setIsDemo(result.isDemo ?? false);
      } else {
        setError(result.error.message);
      }

      setLoading(false);
    },
    [dateRangeOption, riskLevelFilter, dateRanges],
  );

  useEffect(() => {
    void loadAssessments(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dateRangeOption, riskLevelFilter]);

  const handleSendReminder = useCallback(async (appointmentId: string) => {
    setSendingReminder(appointmentId);
    const result = await sendRiskReminder(appointmentId);
    setSendingReminder(null);

    if (result.success) {
      toast.success('Reminder sent successfully');
    } else {
      toast.error(result.error?.message ?? 'Failed to send reminder');
    }
  }, []);

  const highRiskCount = useMemo(
    () => assessments.filter((a) => a.riskLevel === 'High').length,
    [assessments],
  );

  const tableColumns = useMemo<ResponsiveTableColumn<AppointmentRiskAssessment>[]>(
    () => [
      {
        key: 'patient',
        header: 'Patient',
        render: (assessment) =>
          assessment.patientName ? (
            <span className="text-gray-900 font-medium">{assessment.patientName}</span>
          ) : (
            <span className="text-gray-400 italic">Unknown Patient</span>
          ),
      },
      {
        key: 'appointment',
        header: 'Appointment',
        className: 'text-sm text-gray-700',
        render: (assessment) => formatAppointmentTime(assessment.appointmentDateTime),
      },
      {
        key: 'provider',
        header: 'Provider',
        className: 'text-sm text-gray-700',
        render: (assessment) => assessment.providerName,
      },
      {
        key: 'risk-score',
        header: 'Risk Score',
        render: (assessment) => (
          <RiskIndicator score={assessment.riskScore} level={assessment.riskLevel} showBar />
        ),
      },
      {
        key: 'risk-level',
        header: 'Risk Level',
        render: (assessment) => <RiskBadge level={assessment.riskLevel} />,
      },
      {
        key: 'factors',
        header: 'Contributing Factors',
        render: (assessment) => (
          <span className="text-xs text-gray-500 max-w-[200px] line-clamp-2">
            {assessment.contributingFactors.join(', ') || 'None identified'}
          </span>
        ),
      },
      {
        key: 'action',
        header: 'Action',
        render: (assessment) => (
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleSendReminder(assessment.appointmentId)}
            disabled={sendingReminder === assessment.appointmentId}
            className={cn(
              'text-xs min-h-11',
              sendingReminder === assessment.appointmentId && 'opacity-50',
            )}
          >
            {sendingReminder === assessment.appointmentId ? (
              <RefreshCw className="h-3 w-3 mr-1 animate-spin" />
            ) : (
              <Send className="h-3 w-3 mr-1" />
            )}
            Send Reminder
          </Button>
        ),
      },
    ],
    [handleSendReminder, sendingReminder],
  );

  const formatAppointmentTime = (dateTimeStr: string): string => {
    const date = new Date(dateTimeStr);
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const isToday = date.toDateString() === today.toDateString();
    const isTomorrow = date.toDateString() === tomorrow.toDateString();

    const timeStr = date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });

    if (isToday) return `Today ${timeStr}`;
    if (isTomorrow) return `Tomorrow ${timeStr}`;
    return `${date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} ${timeStr}`;
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-10 w-64" />
        </div>
        <Skeleton className="h-14 w-full" />
        <div className="flex gap-3">
          <Skeleton className="h-9 w-36" />
          <Skeleton className="h-9 w-36" />
        </div>
        <Card className="p-0">
          <div className="space-y-1">
            {[...Array(6)].map((_, i) => (
              <Skeleton key={i} className="h-14 w-full rounded-none" />
            ))}
          </div>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center">
        <AlertTriangle className="h-12 w-12 text-red-500 mb-4" />
        <h2 className="text-lg font-semibold text-gray-900 mb-2">Failed to load risk assessments</h2>
        <p className="text-gray-600 mb-4">{error}</p>
        <Button onClick={() => loadAssessments(true)} variant="outline">
          <RefreshCw className="h-4 w-4 mr-2" />
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-sm" aria-label="Breadcrumb">
        <NavLink to="/staff/dashboard" className="text-primary hover:underline">
          Dashboard
        </NavLink>
        <span className="text-gray-400">›</span>
        <span className="text-gray-600">No-Show Risk</span>
      </nav>

      {/* Page Header */}
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold text-gray-900">No-Show Risk Assessment</h1>
        <Button variant="outline" size="sm" onClick={() => loadAssessments()}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      </div>

      {/* Demo Mode Banner */}
      {isDemo && (
        <div
          className="flex items-center gap-3 px-5 py-3 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800"
          role="status"
        >
          <span className="text-lg" aria-hidden="true">⚠️</span>
          <span>
            <strong>Demo Mode</strong> — Using sample data. Log in as FrontDesk or Provider to view real assessments.
          </span>
        </div>
      )}

      {/* Alert Banner */}
      {highRiskCount > 0 && (
        <div
          className="flex items-center gap-3 px-5 py-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-800"
          role="alert"
        >
          <span className="text-lg" aria-hidden="true">🚨</span>
          <span>
            <strong>{highRiskCount} patient{highRiskCount !== 1 ? 's' : ''}</strong> with high no-show risk
            {dateRangeOption === 'today' ? " in today's schedule" : ''} — consider proactive outreach.
          </span>
        </div>
      )}

      {/* Filter Bar */}
      <div className="flex gap-3 flex-wrap">
        <select
          className="h-9 px-3 border border-gray-300 rounded-md text-sm text-gray-700 bg-white focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          value={riskLevelFilter}
          onChange={(e) => setRiskLevelFilter(e.target.value as RiskLevel | 'all')}
          aria-label="Risk level filter"
        >
          <option value="all">All Risk Levels</option>
          <option value="High">High</option>
          <option value="Medium">Medium</option>
          <option value="Low">Low</option>
        </select>

        <select
          className="h-9 px-3 border border-gray-300 rounded-md text-sm text-gray-700 bg-white focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          value={dateRangeOption}
          onChange={(e) => setDateRangeOption(e.target.value as DateRangeOption)}
          aria-label="Time range"
        >
          <option value="today">Today</option>
          <option value="next7">Next 7 Days</option>
          <option value="next30">Next 30 Days</option>
        </select>
      </div>

      {/* Risk Table */}
      <Card className="overflow-hidden">
        {assessments.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <div className="text-4xl mb-4">📊</div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">No appointments found</h3>
            <p className="text-gray-600 text-sm">
              There are no scheduled appointments for the selected filters.
            </p>
          </div>
        ) : (
          <ResponsiveTable
            data={assessments}
            columns={tableColumns}
            getRowKey={(assessment) => assessment.appointmentId}
            ariaLabel="No-show risk patients"
          />
        )}
      </Card>
    </div>
  );
}
