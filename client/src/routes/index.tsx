import { createBrowserRouter } from 'react-router-dom';
import { ProtectedRoute } from '../components/shared/ProtectedRoute';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { SearchLandingPage } from '../features/search/SearchLandingPage';
import { SearchResultsPage } from '../features/search/SearchResultsPage';
import { ProductDetailsPage } from '../features/search/ProductDetailsPage';
import { CartPage } from '../features/search/CartPage';
import { LoginPage } from '../features/auth/LoginPage';
import { RegisterPage } from '../features/auth/RegisterPage';
import { InventoryPage } from '../features/inventory/InventoryPage';
import { VendorsPage } from '../features/vendors/VendorsPage';
import { DocumentsPage } from '../features/documents/DocumentsPage';
import { InsightsPage } from '../features/insights/InsightsPage';

export const router = createBrowserRouter([
  { path: '/', element: <SearchLandingPage /> },
  { path: '/search', element: <SearchResultsPage /> },
  { path: '/product-details', element: <ProductDetailsPage /> },
  { path: '/cart', element: <CartPage /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  {
    element: <ProtectedRoute allowedRoles={['Admin']} />,
    children: [
      { path: '/operations', element: <DashboardPage /> },
      { path: '/dashboard', element: <DashboardPage /> },
      { path: '/inventory', element: <InventoryPage /> },
      { path: '/vendors', element: <VendorsPage /> },
      { path: '/documents', element: <DocumentsPage /> },
      { path: '/insights', element: <InsightsPage /> },
    ],
  },
]);
