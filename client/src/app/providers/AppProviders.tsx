import type { ReactNode } from 'react';
import { RouterProvider } from 'react-router-dom';
import { router } from '../../routes';

type Props = { children?: ReactNode };

export function AppProviders({ children }: Props) {
  return <>{children}<RouterProvider router={router} /></>;
}
