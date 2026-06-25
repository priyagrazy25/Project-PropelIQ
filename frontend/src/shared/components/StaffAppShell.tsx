import {
  AlertTriangle,
  ClipboardList,
  Hash,
  LayoutDashboard,
  UserSearch,
  Users,
  Zap,
} from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { logout } from '../../features/identity/identitySlice';
import { clearAccessToken } from '../api/authInterceptor';
import { ResponsiveNavShell } from './layout/ResponsiveNavShell';

const staffNavItems = [
  { to: '/staff/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/staff/walk-in', icon: Users, label: 'Walk-In' },
  { to: '/staff/queue', icon: ClipboardList, label: 'Queue' },
  { to: '/staff/conflicts', icon: Zap, label: 'Conflicts' },
  { to: '/staff/codes', icon: Hash, label: 'Codes' },
  { to: '/staff/risk', icon: AlertTriangle, label: 'Risk' },
  { to: '/staff/patient-view', icon: UserSearch, label: 'Patient View' },
] as const;

export function StaffAppShell() {
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
    : 'ST';

  const handleLogout = useCallback(() => {
    clearAccessToken();
    dispatch(logout());
    void navigate('/login');
  }, [dispatch, navigate]);

  return (
    <ResponsiveNavShell
      appTitle="Unified Patient Access"
      homeTo="/staff/dashboard"
      navAriaLabel="Staff navigation"
      navItems={staffNavItems.map((item) => ({ ...item, end: false }))}
      initials={initials}
      onLogout={handleLogout}
    />
  );
}
