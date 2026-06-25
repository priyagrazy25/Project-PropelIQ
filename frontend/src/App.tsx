import { useCallback } from 'react';
import {
  createBrowserRouter,
  isRouteErrorResponse,
  Link,
  Navigate,
  Outlet,
  RouterProvider,
  useNavigate,
  useRouteError,
} from 'react-router-dom';
import { useAppDispatch, useAppSelector } from './app/hooks';
import { InsurancePreCheckForm } from './features/clinical/components/InsurancePreCheckForm';
import { AIIntakePage } from './features/clinical/pages/AIIntakePage';
import { ConflictResolutionPage } from './features/clinical/pages/ConflictResolutionPage';
import { DocumentUploadPage } from './features/clinical/pages/DocumentUploadPage';
import { MedicalCodingPage } from './features/clinical/pages/MedicalCodingPage';
import { IntakeSummaryPage } from './features/clinical/pages/IntakeSummaryPage';
import { ManualIntakePage } from './features/clinical/pages/ManualIntakePage';
import { PatientView360Page } from './features/clinical/pages/PatientView360Page';
import { ProcessingStatusPage } from './features/clinical/pages/ProcessingStatusPage';
import { logout } from './features/identity/identitySlice';
import { AdminDashboardPage } from './features/identity/pages/AdminDashboardPage.tsx';
import { AdminUserManagementPage } from './features/identity/pages/AdminUserManagementPage';
import { AuditLogViewerPage } from './features/identity/pages/AuditLogViewerPage';
import { LoginPage } from './features/identity/pages/LoginPage';
import { RegistrationPage } from './features/identity/pages/RegistrationPage';
import { CalendarSyncPage } from './features/notification/pages/CalendarSyncPage';
import { BookingConfirmationPage } from './features/scheduling/pages/BookingConfirmationPage';
import { NoShowRiskDashboardPage } from './features/scheduling/pages/NoShowRiskDashboardPage';
import { PatientDashboardPage } from './features/scheduling/pages/PatientDashboardPage';
import { ProviderSearchPage } from './features/scheduling/pages/ProviderSearchPage';
import { QueueManagementPage } from './features/scheduling/pages/QueueManagementPage';
import { RescheduleAppointmentPage } from './features/scheduling/pages/RescheduleAppointmentPage';
import { WaitlistPage } from './features/scheduling/pages/WaitlistPage';
import { StaffDashboardPage } from './features/scheduling/pages/StaffDashboardPage';
import { WalkInBookingPage } from './features/scheduling/pages/WalkInBookingPage';
import { clearAccessToken } from './shared/api/authInterceptor';
import { AdminAppShell } from './shared/components/AdminAppShell';
import { AppShell } from './shared/components/AppShell';
import { ProtectedRoute } from './shared/components/ProtectedRoute';
import { SessionTimeoutModal } from './shared/components/SessionTimeoutModal';
import { StaffAppShell } from './shared/components/StaffAppShell';
import { useInactivityTimer } from './shared/hooks/useInactivityTimer';
import { useSessionRestore } from './shared/hooks/useSessionRestore';

function SessionTimeoutWrapper({ children }: { children: React.ReactNode }) {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const isAuthenticated = useAppSelector(
    (state) => state.identity.isAuthenticated,
  );

  useSessionRestore();

  const handleLogout = useCallback(() => {
    clearAccessToken();
    dispatch(logout());
    void navigate('/login', { state: { sessionExpired: true } });
  }, [dispatch, navigate]);

  const { showModal, secondsLeft, extendSession, logoutNow } =
    useInactivityTimer(isAuthenticated, handleLogout);

  return (
    <>
      {children}
      {showModal && (
        <SessionTimeoutModal
          secondsLeft={secondsLeft}
          onExtendSession={extendSession}
          onLogout={logoutNow}
        />
      )}
    </>
  );
}

function RootLayout() {
  return (
    <SessionTimeoutWrapper>
      <Outlet />
    </SessionTimeoutWrapper>
  );
}

function RouteMessagePage({
  title,
  message,
}: {
  title: string;
  message: string;
}) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4" role="main">
      <div className="w-full max-w-lg rounded-xl border bg-card p-8 text-center shadow-[var(--shadow-3)]">
        <p className="text-sm font-semibold uppercase tracking-[0.2em] text-primary">
          Unified Patient Access
        </p>
        <h1 className="mt-4 text-3xl font-bold text-foreground">{title}</h1>
        <p className="mt-3 text-sm text-muted-foreground">{message}</p>
        <div className="mt-6 flex items-center justify-center gap-3">
          <Link
            to="/login"
            className="inline-flex items-center rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground no-underline transition hover:opacity-90"
          >
            Go to Login
          </Link>
          <Link
            to="/"
            className="inline-flex items-center rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground no-underline transition hover:bg-muted"
          >
            Back to Home
          </Link>
        </div>
      </div>
    </main>
  );
}

function UnauthorizedPage() {
  return (
    <RouteMessagePage
      title="Access denied"
      message="Your account does not have permission to open that module. Sign in with the correct role or return to an allowed page."
    />
  );
}

function NotFoundPage() {
  return (
    <RouteMessagePage
      title="Page not found"
      message="The page you requested does not exist or may have moved. Use the links below to return to a supported route."
    />
  );
}

function RouteErrorPage() {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    if (error.status === 404) {
      return <NotFoundPage />;
    }

    return (
      <RouteMessagePage
        title={`${error.status} ${error.statusText}`}
        message="The application could not complete this navigation. Please return to a known page and try again."
      />
    );
  }

  return (
    <RouteMessagePage
      title="Something went wrong"
      message="An unexpected application error occurred. Please return to login or refresh the page."
    />
  );
}

const router = createBrowserRouter([
  {
    element: <RootLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      { path: '/', element: <Navigate to="/login" replace /> },
      { path: '/register', element: <RegistrationPage /> },
      { path: '/login', element: <LoginPage /> },
      { path: '/unauthorized', element: <UnauthorizedPage /> },
      {
        element: (
          <ProtectedRoute allowedRoles={['Patient']}>
            <AppShell />
          </ProtectedRoute>
        ),
        children: [
          { path: '/dashboard', element: <PatientDashboardPage /> },
          { path: '/search', element: <ProviderSearchPage /> },
          {
            path: '/booking/confirm',
            element: <BookingConfirmationPage />,
          },
          { path: '/reschedule', element: <RescheduleAppointmentPage /> },
          { path: '/calendar-sync', element: <CalendarSyncPage /> },
          { path: '/waitlist', element: <WaitlistPage /> },
          { path: '/intake', element: <AIIntakePage /> },
          { path: '/intake/ai', element: <AIIntakePage /> },
          { path: '/intake/manual', element: <ManualIntakePage /> },
          { path: '/intake/summary', element: <IntakeSummaryPage /> },
          { path: '/intake/insurance', element: <InsurancePreCheckForm /> },
          { path: '/documents', element: <DocumentUploadPage /> },
          { path: '/documents/processing', element: <ProcessingStatusPage /> },
          { path: '/health-profile', element: <PatientView360Page /> },
          { path: '/clinical/conflicts/:conflictId', element: <ConflictResolutionPage /> },
        ],
      },
      {
        path: '/queue',
        element: (
          <ProtectedRoute allowedRoles={['Provider', 'FrontDesk', 'Admin']}>
            <Navigate to="/staff/dashboard" replace />
          </ProtectedRoute>
        ),
      },
      {
        element: (
          <ProtectedRoute allowedRoles={['FrontDesk', 'Provider', 'Admin']}>
            <StaffAppShell />
          </ProtectedRoute>
        ),
        children: [
          {
            path: '/staff/dashboard',
            element: <StaffDashboardPage />,
          },
          { path: '/staff/walk-in', element: <WalkInBookingPage /> },
          { path: '/staff/queue', element: <QueueManagementPage /> },
          {
            path: '/staff/conflicts',
            element: (
              <div className="placeholder-page">
                Conflicts &mdash; Coming Soon
              </div>
            ),
          },
          {
            path: '/staff/codes',
            element: <MedicalCodingPage />,
          },
          {
            path: '/staff/risk',
            element: <NoShowRiskDashboardPage />,
          },
          {
            path: '/staff/patient-view',
            element: <PatientView360Page />,
          },
          {
            path: '/staff/patient-view/:patientId',
            element: <PatientView360Page />,
          },
        ],
      },
      {
        element: (
          <ProtectedRoute allowedRoles={['Admin']}>
            <AdminAppShell />
          </ProtectedRoute>
        ),
        children: [
          {
            path: '/management',
            element: <AdminUserManagementPage />,
          },
          {
            path: '/management/dashboard',
            element: <AdminDashboardPage />,
          },
          {
            path: '/management/queue',
            element: <QueueManagementPage />,
          },
          {
            path: '/management/conflicts',
            element: (
              <div className="placeholder-page">
                Conflicts &mdash; Coming Soon
              </div>
            ),
          },
          {
            path: '/management/codes',
            element: <MedicalCodingPage />,
          },
          {
            path: '/management/risk',
            element: <NoShowRiskDashboardPage />,
          },
          {
            path: '/management/walk-in',
            element: <WalkInBookingPage />,
          },
          {
            path: '/management/audit',
            element: <AuditLogViewerPage />,
          },
          {
            path: '/management/patient-view',
            element: (
              <div className="placeholder-page">
                Patient View &mdash; Coming Soon
              </div>
            ),
          },
          {
            path: '/management/settings',
            element: (
              <div className="placeholder-page">
                Settings &mdash; Coming Soon
              </div>
            ),
          },
        ],
      },
      // Development demo route - bypasses auth for UI testing
      {
        path: '/demo/risk',
        element: <NoShowRiskDashboardPage />,
      },
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
]);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
