import { useNavigate } from 'react-router-dom';
import { ShoppingCart } from 'lucide-react';
import { useAuth } from '../../contexts/useAuth';
import { useCart } from '../../contexts/useCart';

export function SearchHeaderActions() {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuth();
  const { totalQuantity } = useCart();

  const cartButton = (
    <button
      type="button"
      onClick={() => navigate('/cart')}
      className="relative inline-flex h-10 w-10 items-center justify-center rounded-full border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-100"
      aria-label="Open cart"
    >
      <ShoppingCart className="h-4 w-4" />
      {totalQuantity > 0 ? (
        <span className="absolute -right-1 -top-1 inline-flex min-h-5 min-w-5 items-center justify-center rounded-full bg-orange-500 px-1 text-[10px] font-semibold text-slate-950">
          {totalQuantity > 99 ? '99+' : totalQuantity}
        </span>
      ) : null}
    </button>
  );

  if (!isAuthenticated) {
    return (
      <div className="flex items-center gap-2">
        {cartButton}
        <button
          type="button"
          onClick={() => navigate('/register')}
          className="rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100"
        >
          Register
        </button>
        <button
          type="button"
          onClick={() => navigate('/login')}
          className="rounded-full bg-slate-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-700"
        >
          Sign In
        </button>
      </div>
    );
  }

  const isAdmin = user?.role?.toLowerCase() === 'admin';

  return (
    <div className="flex flex-wrap items-center justify-end gap-2">
      {cartButton}
      <span className="rounded-full border border-slate-300 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-700 sm:text-sm">
        {user?.userName ?? 'user@invenflowai.com'} - {user?.role ?? 'User'}
      </span>
      {isAdmin ? (
        <button
          type="button"
          onClick={() => navigate('/dashboard')}
          className="rounded-full bg-orange-500 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-orange-400"
        >
          Admin Operations Console
        </button>
      ) : null}
      <button
        type="button"
        onClick={() => {
          logout();
          navigate('/');
        }}
        className="rounded-full border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100"
      >
        Logout
      </button>
    </div>
  );
}
