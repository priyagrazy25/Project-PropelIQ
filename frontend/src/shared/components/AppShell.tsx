import {
  ClipboardList,
  Clock,
  FileText,
  HeartPulse,
  LayoutDashboard,
  Search,
} from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { logout } from '../../features/identity/identitySlice';
import { clearAccessToken } from '../api/authInterceptor';
import { ResponsiveNavShell } from './layout/ResponsiveNavShell';

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/search', icon: Search, label: 'Book Appointment' },
  { to: '/intake', icon: ClipboardList, label: 'Intake' },
  { to: '/documents', icon: FileText, label: 'Documents' },
  { to: '/health-profile', icon: HeartPulse, label: 'Health Profile' },
  { to: '/waitlist', icon: Clock, label: 'Waitlist' },
] as const;

export function AppShell() {
  const fullName = useAppSelector((state) => state.identity.fullName);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const initials = fullName
    ? fullName
        .split(' ')
        .map((n) => n.charAt(0))
        .join('')
        .toUpperCase()
        .slice(0, 2)
    : 'PA';

  const handleLogout = useCallback(() => {
    clearAccessToken();
    dispatch(logout());
    void navigate('/login');
  }, [dispatch, navigate]);

  return (
    <ResponsiveNavShell
      appTitle="Unified Patient Access"
      homeTo="/dashboard"
      navAriaLabel="Sidebar navigation"
      navItems={navItems.map((item) => ({ ...item, end: false }))}
      initials={initials}
      onLogout={handleLogout}
    />
  );
}
