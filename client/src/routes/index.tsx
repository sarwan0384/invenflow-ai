import { createBrowserRouter } from 'react-router-dom';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { InventoryPage } from '../features/inventory/InventoryPage';
import { VendorsPage } from '../features/vendors/VendorsPage';
import { DocumentsPage } from '../features/documents/DocumentsPage';
import { InsightsPage } from '../features/insights/InsightsPage';

export const router = createBrowserRouter([
  { path: '/', element: <DashboardPage /> },
  { path: '/inventory', element: <InventoryPage /> },
  { path: '/vendors', element: <VendorsPage /> },
  { path: '/documents', element: <DocumentsPage /> },
  { path: '/insights', element: <InsightsPage /> },
]);
