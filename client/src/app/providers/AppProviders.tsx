import type { ReactNode } from 'react';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from '../../contexts/AuthContext';
import { CartProvider } from '../../contexts/CartContext';
import { router } from '../../routes';

type Props = { children?: ReactNode };

export function AppProviders({ children }: Props) {
  return <AuthProvider><CartProvider>{children}<RouterProvider router={router} /></CartProvider></AuthProvider>;
}
