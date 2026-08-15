import { Link } from 'react-router-dom';
import { SearchHeaderActions } from '../../components/navigation/SearchHeaderActions';
import { useCart } from '../../contexts/useCart';

type Currency = 'USD' | 'INR' | 'EUR';

const currencySymbol: Record<Currency, string> = {
  USD: '$',
  INR: 'Rs ',
  EUR: 'EUR ',
};

function formatPrice(amount: number, currencyCode: string) {
  const normalized = currencyCode.toUpperCase();
  const code = normalized === 'USD' || normalized === 'INR' || normalized === 'EUR'
    ? normalized as Currency
    : 'USD';

  return `${currencySymbol[code]}${amount.toFixed(code === 'INR' ? 2 : 4)}`;
}

export function CartPage() {
  const { items, totalPrice, updateCartItemQuantity, removeFromCart, clearCart } = useCart();

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex w-full max-w-[1400px] flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
          <Link to="/" className="whitespace-nowrap text-sm font-semibold uppercase tracking-[0.22em] text-slate-800">
            InvenFlow
          </Link>
          <div className="ml-auto">
            <SearchHeaderActions />
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[1400px] space-y-5 px-4 py-6 sm:px-6">
        <section className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-white p-4">
          <div>
            <h1 className="text-xl font-semibold text-slate-900">Cart</h1>
            <p className="text-sm text-slate-600">Review selected parts and adjust quantities before checkout.</p>
          </div>
          <button
            type="button"
            onClick={clearCart}
            className="rounded-xl border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
          >
            Clear Cart
          </button>
        </section>

        {items.length === 0 ? (
          <section className="rounded-2xl border border-slate-200 bg-white p-8 text-center">
            <p className="text-slate-700">Your cart is empty.</p>
            <Link to="/search" className="mt-3 inline-flex rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-700">
              Back to Search
            </Link>
          </section>
        ) : (
          <section className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
            <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white">
              <table className="min-w-full text-sm">
                <thead className="bg-slate-100 text-slate-700">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">MPN</th>
                    <th className="px-3 py-2 text-left font-medium">Manufacturer</th>
                    <th className="px-3 py-2 text-left font-medium">Quantity</th>
                    <th className="px-3 py-2 text-left font-medium">Unit Price</th>
                    <th className="px-3 py-2 text-left font-medium">Line Total</th>
                    <th className="px-3 py-2 text-right font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id} className="border-t border-slate-200">
                      <td className="px-3 py-3">
                        <p className="font-semibold text-slate-900">{item.mpn}</p>
                        <p className="mt-1 text-xs text-slate-500">{item.providerName}</p>
                      </td>
                      <td className="px-3 py-3 text-slate-800">{item.manufacturer || 'Unknown'}</td>
                      <td className="px-3 py-3">
                        <input
                          type="number"
                          min={item.minQty || 1}
                          step={item.orderMultiple || item.minQty || 1}
                          value={item.quantity}
                          onChange={(event) => {
                            const parsedValue = Number.parseInt(event.target.value, 10);
                            updateCartItemQuantity(item.mpn, Number.isNaN(parsedValue) ? 0 : parsedValue);
                          }}
                          className="h-9 w-24 rounded-lg border border-slate-300 bg-white px-2 text-sm outline-none ring-slate-200 focus:ring"
                        />
                      </td>
                      <td className="px-3 py-3 text-slate-800">{formatPrice(item.unitPrice, item.currency || 'USD')}</td>
                      <td className="px-3 py-3 font-semibold text-slate-900">{formatPrice(item.quantity * item.unitPrice, item.currency || 'USD')}</td>
                      <td className="px-3 py-3 text-right">
                        <button
                          type="button"
                          onClick={() => removeFromCart(item.mpn)}
                          className="rounded-lg border border-rose-300 bg-rose-50 px-3 py-1.5 text-xs font-semibold text-rose-700 hover:bg-rose-100"
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <aside className="rounded-2xl border border-slate-200 bg-white p-4">
              <h2 className="text-base font-semibold text-slate-900">Order Summary</h2>
              <div className="mt-3 space-y-2 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-slate-600">Items</span>
                  <span className="font-medium text-slate-900">{items.length}</span>
                </div>
                <div className="flex items-center justify-between border-t border-slate-200 pt-2">
                  <span className="text-slate-600">Total Price</span>
                  <span className="text-lg font-semibold text-slate-900">{formatPrice(totalPrice, 'USD')}</span>
                </div>
              </div>
              <button
                type="button"
                className="mt-4 inline-flex h-10 w-full items-center justify-center rounded-xl bg-blue-600 px-4 text-sm font-semibold text-white hover:bg-blue-500"
              >
                Proceed to Checkout
              </button>
            </aside>
          </section>
        )}
      </main>
    </div>
  );
}
