import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import {
  AlertTriangle,
  Bell,
  ClipboardList,
  Hash,
  LayoutDashboard,
  LogOut,
  Plus,
  Users,
  Zap,
} from 'lucide-react';
import { useCallback } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { logout } from '../../features/identity/identitySlice';
import { clearAccessToken } from '../api/authInterceptor';

const staffNavItems = [
  { to: '/staff/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/staff/walk-in', icon: Users, label: 'Walk-In' },
  { to: '/staff/queue', icon: ClipboardList, label: 'Queue' },
  { to: '/staff/conflicts', icon: Zap, label: 'Conflicts' },
  { to: '/staff/codes', icon: Hash, label: 'Codes' },
  { to: '/staff/risk', icon: AlertTriangle, label: 'Risk' },
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
    <div className="min-h-screen">
      {/* Header */}
      <header
        className="fixed top-0 left-0 right-0 h-16 bg-card border-b border-border flex items-center justify-between px-6 z-50 shadow-(--shadow-1)"
        role="banner"
      >
        <NavLink
          to="/staff/dashboard"
          className="flex items-center gap-2 text-xl font-bold text-primary no-underline hover:text-primary/80 transition-colors"
        >
          <span className="w-8 h-8 bg-primary rounded-md flex items-center justify-center text-primary-foreground text-lg font-bold">
            <Plus className="h-5 w-5" />
          </span>
          Unified Patient Access
        </NavLink>
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            aria-label="Notifications"
            className="relative"
          >
            <Bell className="h-5 w-5 text-muted-foreground" />
          </Button>
          <Avatar className="h-9 w-9 cursor-pointer">
            <AvatarFallback className="bg-secondary text-secondary-foreground text-sm font-semibold">
              {initials}
            </AvatarFallback>
          </Avatar>
        </div>
      </header>

      {/* Sidebar */}
      <nav
        className="fixed top-16 left-0 bottom-0 w-64 bg-card border-r border-border py-4 overflow-y-auto z-40 flex flex-col md:flex max-md:hidden"
        role="navigation"
        aria-label="Staff navigation"
      >
        {staffNavItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 px-6 py-3 text-sm font-medium text-muted-foreground no-underline border-l-[3px] border-transparent transition-colors duration-(--duration-micro)',
                'hover:bg-muted',
                isActive &&
                  'text-primary bg-accent border-l-primary font-semibold',
              )
            }
          >
            <item.icon className="h-5 w-5" aria-hidden="true" />
            {item.label}
          </NavLink>
        ))}

        <Separator className="my-2" />

        <button
          type="button"
          className="mt-auto flex items-center gap-3 px-6 py-3 text-sm font-medium text-muted-foreground no-underline border-l-[3px] border-transparent bg-transparent cursor-pointer w-full text-left font-sans hover:bg-muted transition-colors duration-(--duration-micro)"
          onClick={handleLogout}
        >
          <LogOut className="h-5 w-5" aria-hidden="true" />
          Sign Out
        </button>
      </nav>

      {/* Main Content */}
      <main
        className="ml-64 mt-16 p-8 min-h-[calc(100vh-4rem)] bg-(--surface) max-md:ml-0"
        role="main"
      >
        <Outlet />
      </main>
    </div>
  );
}
