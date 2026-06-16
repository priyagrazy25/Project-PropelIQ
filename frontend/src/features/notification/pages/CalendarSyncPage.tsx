import { useLocation, useNavigate } from 'react-router-dom';
import { CalendarSyncChooser } from '../components/CalendarSyncChooser';

export function CalendarSyncPage() {
  const location = useLocation();
  const navigate = useNavigate();

  const state = location.state as { appointmentId?: string } | null;
  const appointmentId = state?.appointmentId ?? '';

  return (
    <main className="space-y-6 max-w-2xl" role="main">
      <nav
        className="flex items-center gap-1 text-sm text-muted-foreground"
        aria-label="Breadcrumb"
      >
        <a href="/dashboard" className="hover:text-foreground">
          Dashboard
        </a>
        <span aria-hidden="true">›</span>
        <a
          href="/booking/confirm"
          className="hover:text-foreground"
          onClick={(e) => {
            e.preventDefault();
            void navigate(-1);
          }}
        >
          Booking
        </a>
        <span aria-hidden="true">›</span>
        <span aria-current="page" className="text-foreground">
          Calendar Sync
        </span>
      </nav>

      <CalendarSyncChooser
        appointmentId={appointmentId}
        onSkip={() => {
          void navigate(-1);
        }}
      />
    </main>
  );
}
