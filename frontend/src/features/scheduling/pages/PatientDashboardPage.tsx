import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Bot,
  CalendarDays,
  ClipboardList,
  Clock,
  Download,
  FileText,
  HeartPulse,
  Plus,
  Upload,
} from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { toast } from 'sonner';
import { useAppSelector } from '../../../app/hooks';
import { fetchPatient360View } from '../../clinical/api/patient360Api';
import { downloadPdfConfirmation } from '../../notification/api/notificationApi';
import type { MyAppointment, WaitlistEntry } from '../api/schedulingApi';
import {
  fetchMyAppointments,
  fetchWaitlistEntries,
} from '../api/schedulingApi';
import { QueueStatusSection } from '../components/QueueStatusSection';

type Tab = 'upcoming' | 'past';

const ACTIVE_STATUSES = new Set([
  'Scheduled',
  'Confirmed',
  'Arrived',
  'InProgress',
]);

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return (
    d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }) +
    ' · ' +
    d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })
  );
}

function badgeVariant(
  status: string,
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status.toLowerCase()) {
    case 'scheduled':
    case 'confirmed':
      return 'default';
    case 'cancelled':
    case 'noshow':
      return 'destructive';
    case 'completed':
      return 'secondary';
    default:
      return 'secondary';
  }
}

function resolveDisplayStatus(appt: MyAppointment, isInPastTab: boolean): string {
  if (isInPastTab && ACTIVE_STATUSES.has(appt.status)) {
    return 'Completed';
  }
  return appt.status;
}

export function PatientDashboardPage() {
  const location = useLocation();
  const fullName = useAppSelector((state) => state.identity.fullName);
  const firstName = fullName?.split(' ')[0] ?? 'Patient';

  const [appointments, setAppointments] = useState<MyAppointment[]>([]);
  const [waitlistEntries, setWaitlistEntries] = useState<WaitlistEntry[]>([]);
  const [documentCount, setDocumentCount] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<Tab>('upcoming');

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);

    const [apptResult, wlResult, view360Result] = await Promise.all([
      fetchMyAppointments(),
      fetchWaitlistEntries(),
      fetchPatient360View(),
    ]);

    if (apptResult.success) {
      setAppointments(apptResult.data);
    } else {
      setError(apptResult.error.message);
    }

    if (wlResult.success) {
      setWaitlistEntries(wlResult.data);
    }

    if (view360Result.success) {
      setDocumentCount(view360Result.data.documentCount);
    }

    setLoading(false);
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useEffect(() => {
    const state = location.state as
      | { refreshDashboard?: number }
      | null
      | undefined;
    if (typeof state?.refreshDashboard === 'number') {
      void loadData();
    }
  }, [location.state, loadData]);

  const upcoming = useMemo(() => {
    const now = new Date();
    return appointments.filter(
      (a) =>
        ACTIVE_STATUSES.has(a.status) &&
        new Date(a.appointmentDateTime) >= now,
    );
  }, [appointments]);

  const past = useMemo(() => {
    const now = new Date();
    return appointments.filter(
      (a) =>
        !ACTIVE_STATUSES.has(a.status) ||
        new Date(a.appointmentDateTime) < now,
    );
  }, [appointments]);

  const activeWaitlist = useMemo(
    () =>
      waitlistEntries.filter(
        (w) => w.status === 'Active' || w.status === 'Notified',
      ),
    [waitlistEntries],
  );

  const displayed = activeTab === 'upcoming' ? upcoming : past;

  return (
    <div className="space-y-6">
      {/* Header */}
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">
            Welcome back, {firstName}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Here&apos;s an overview of your health activity.
          </p>
        </div>
        <Button
          asChild
          size="lg"
          className="bg-[#1E6F9F] hover:bg-[#175F87] text-white shadow-md hover:shadow-lg transition-all duration-200"
        >
          <Link
            to="/search"
            className="inline-flex items-center gap-2 whitespace-nowrap"
          >
            <Plus className="h-5 w-5" />
            Book Appointment
          </Link>
        </Button>
      </header>

      {/* Queue Status (shows if patient is currently waiting) */}
      <QueueStatusSection />

      {/* Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card role="article">
          <CardContent className="flex flex-col items-center py-6">
            <CalendarDays className="h-8 w-8 text-primary mb-2" />
            <div className="text-2xl font-bold">{upcoming.length}</div>
            <div className="text-sm text-muted-foreground">
              Upcoming Appointments
            </div>
          </CardContent>
        </Card>
        <Card role="article">
          <CardContent className="flex flex-col items-center py-6">
            <ClipboardList className="h-8 w-8 text-primary mb-2" />
            <Badge
              variant="outline"
              className="bg-amber-50 text-amber-800 border-amber-200 mb-1"
            >
              Pending
            </Badge>
            <div className="text-sm text-muted-foreground">Intake Status</div>
          </CardContent>
        </Card>
        <Card role="article">
          <CardContent className="flex flex-col items-center py-6">
            <FileText className="h-8 w-8 text-primary mb-2" />
            <div className="text-2xl font-bold">{documentCount}</div>
            <div className="text-sm text-muted-foreground">
              Documents Uploaded
            </div>
          </CardContent>
        </Card>
        <Card role="article">
          <CardContent className="flex flex-col items-center py-6">
            <Clock className="h-8 w-8 text-primary mb-2" />
            <div className="text-2xl font-bold">{activeWaitlist.length}</div>
            <div className="text-sm text-muted-foreground">
              Waitlist Entries
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Error */}
      {error !== null && (
        <div
          className="flex items-center justify-between rounded-lg border border-destructive/50 bg-destructive/10 p-4"
          role="alert"
        >
          <p className="text-sm text-destructive">{error}</p>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              void loadData();
            }}
          >
            Retry
          </Button>
        </div>
      )}

      {/* Appointments Section */}
      <Card>
        <CardContent className="p-6">
          <h2 className="text-lg font-semibold mb-4">Appointments</h2>
          <Tabs
            defaultValue="upcoming"
            value={activeTab}
            onValueChange={(v) => setActiveTab(v as Tab)}
          >
            <TabsList>
              <TabsTrigger value="upcoming">
                Upcoming ({upcoming.length})
              </TabsTrigger>
              <TabsTrigger value="past">Past ({past.length})</TabsTrigger>
            </TabsList>

            <TabsContent value={activeTab} className="mt-4">
              {loading && (
                <div className="space-y-3" aria-busy="true" role="status">
                  {Array.from({ length: 3 }, (_, i) => (
                    <Skeleton key={i} className="h-12 w-full" />
                  ))}
                </div>
              )}

              {!loading && displayed.length === 0 && (
                <div
                  className="flex flex-col items-center py-12 text-center"
                  role="status"
                >
                  <CalendarDays className="h-12 w-12 text-muted-foreground mb-3" />
                  <p className="text-sm text-muted-foreground">
                    {activeTab === 'upcoming'
                      ? 'No upcoming appointments.'
                      : 'No past appointments.'}
                  </p>
                  {activeTab === 'upcoming' && (
                    <Button asChild className="mt-3">
                      <Link to="/search">Book Your First Appointment</Link>
                    </Button>
                  )}
                </div>
              )}

              {!loading && displayed.length > 0 && (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Provider</TableHead>
                      <TableHead>Specialty</TableHead>
                      <TableHead>Date &amp; Time</TableHead>
                      <TableHead>Status</TableHead>
                      {activeTab === 'upcoming' && (
                        <TableHead>Actions</TableHead>
                      )}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {displayed.map((appt) => (
                      <TableRow key={appt.appointmentId}>
                        <TableCell className="font-medium">
                          {appt.providerName}
                        </TableCell>
                        <TableCell>{appt.specialty}</TableCell>
                        <TableCell>
                          {formatDateTime(appt.appointmentDateTime)}
                        </TableCell>
                        <TableCell>
                          <Badge variant={badgeVariant(resolveDisplayStatus(appt, activeTab === 'past'))}>
                            {resolveDisplayStatus(appt, activeTab === 'past')}
                          </Badge>
                        </TableCell>
                        {activeTab === 'upcoming' && (
                          <TableCell>
                            <div className="flex items-center gap-1">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  downloadPdfConfirmation(
                                    appt.appointmentId,
                                  ).then((result) => {
                                    if (!result.success) {
                                      toast.error(
                                        result.error ??
                                          'Failed to download PDF',
                                      );
                                    }
                                  });
                                }}
                                aria-label={`Download PDF confirmation for ${appt.providerName}`}
                              >
                                <Download className="h-4 w-4 mr-1" />
                                PDF
                              </Button>
                              <Button variant="ghost" size="sm" asChild>
                                <Link
                                  to="/reschedule"
                                  state={{ appointment: appt }}
                                >
                                  Reschedule
                                </Link>
                              </Button>
                            </div>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card>
        <CardContent className="p-6">
          <h2 className="text-lg font-semibold mb-4">Quick Actions</h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Link
              to="/intake"
              className="flex flex-col items-center gap-2 rounded-lg border border-border p-6 hover:bg-muted/50 transition-colors"
              aria-label="Start AI-assisted intake"
            >
              <Bot className="h-8 w-8 text-primary" />
              <div className="font-medium text-sm">Complete Intake</div>
              <div className="text-xs text-muted-foreground">
                AI-assisted or manual entry
              </div>
            </Link>
            <Link
              to="/documents"
              className="flex flex-col items-center gap-2 rounded-lg border border-border p-6 hover:bg-muted/50 transition-colors"
              aria-label="Upload clinical documents"
            >
              <Upload className="h-8 w-8 text-primary" />
              <div className="font-medium text-sm">Upload Documents</div>
              <div className="text-xs text-muted-foreground">
                Upload clinical records
              </div>
            </Link>
            <Link
              to="/health-profile"
              className="flex flex-col items-center gap-2 rounded-lg border border-border p-6 hover:bg-muted/50 transition-colors"
              aria-label="View your health profile"
            >
              <HeartPulse className="h-8 w-8 text-primary" />
              <div className="font-medium text-sm">Health Profile</div>
              <div className="text-xs text-muted-foreground">
                View your 360° health data
              </div>
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
