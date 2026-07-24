import type { ReactNode } from 'react';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from '../../contexts/AuthContext';
import { router } from '../../routes';

type Props = { children?: ReactNode };

export function AppProviders({ children }: Props) {
  return <AuthProvider>{children}<RouterProvider router={router} /></AuthProvider>;
}
