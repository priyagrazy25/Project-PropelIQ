import { Skeleton } from '@/components/ui/skeleton';
import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAppSelector } from '../../app/hooks';

interface ProtectedRouteProps {
  children: ReactNode;
  allowedRoles?: Array<'Patient' | 'Provider' | 'Admin' | 'FrontDesk'>;
}

export function ProtectedRoute({
  children,
  allowedRoles,
}: ProtectedRouteProps) {
  const { isAuthenticated, sessionChecked, role } = useAppSelector(
    (state) => state.identity,
  );
  const location = useLocation();

  if (!sessionChecked) {
    return (
      <div
        className="flex items-center justify-center min-h-screen"
        aria-busy="true"
      >
        <div className="w-full max-w-md space-y-4 p-8">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles && role && !allowedRoles.includes(role)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}
