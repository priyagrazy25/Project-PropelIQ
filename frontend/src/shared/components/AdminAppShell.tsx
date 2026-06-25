import { LayoutDashboard, Shield, Users } from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { logout } from '../../features/identity/identitySlice';
import { clearAccessToken } from '../api/authInterceptor';
import { ResponsiveNavShell } from './layout/ResponsiveNavShell';

const adminNavItems = [
  { to: '/management/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/management', icon: Users, label: 'Users' },
  { to: '/management/audit', icon: Shield, label: 'Audit Logs' },
] as const;

export function AdminAppShell() {
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
    : 'AD';

  const handleLogout = useCallback(() => {
    clearAccessToken();
    dispatch(logout());
    void navigate('/login');
  }, [dispatch, navigate]);

  return (
    <ResponsiveNavShell
      appTitle="Admin Portal"
      homeTo="/management"
      navAriaLabel="Admin navigation"
      navItems={adminNavItems.map((item) => ({
        ...item,
        end: item.to === '/management',
      }))}
      initials={initials}
      onLogout={handleLogout}
    />
  );
}
