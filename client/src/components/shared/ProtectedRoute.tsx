import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../contexts/useAuth';

type ProtectedRouteProps = {
  allowedRoles?: string[];
};

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated) {
    const target = allowedRoles?.length ? '/login?error=Admin%20access%20required' : '/login';
    return <Navigate to={target} replace />;
  }

  if (allowedRoles?.length) {
    const normalizedRole = user?.role?.toLowerCase() ?? '';
    const isAllowed = allowedRoles.some((role) => role.toLowerCase() === normalizedRole);
    if (!isAllowed) {
      return <Navigate to="/?error=Admin%20access%20required" replace />;
    }
  }

  return <Outlet />;
}
