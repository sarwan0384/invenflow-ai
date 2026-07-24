import { createBrowserRouter } from 'react-router-dom';
import { ProtectedRoute } from '../components/shared/ProtectedRoute';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { LoginPage } from '../features/auth/LoginPage';
import { RegisterPage } from '../features/auth/RegisterPage';
import { InventoryPage } from '../features/inventory/InventoryPage';
import { VendorsPage } from '../features/vendors/VendorsPage';
import { DocumentsPage } from '../features/documents/DocumentsPage';
import { InsightsPage } from '../features/insights/InsightsPage';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { path: '/', element: <DashboardPage /> },
      { path: '/inventory', element: <InventoryPage /> },
      { path: '/vendors', element: <VendorsPage /> },
      { path: '/documents', element: <DocumentsPage /> },
      { path: '/insights', element: <InsightsPage /> },
    ],
  },
]);
