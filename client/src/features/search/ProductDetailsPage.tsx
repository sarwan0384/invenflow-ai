import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, ExternalLink, ShoppingCart, Zap } from 'lucide-react';
import { getProductDetails, type ProductDetail } from '../../services/api';
import { SearchHeaderActions } from '../../components/navigation/SearchHeaderActions';
import { useCart } from '../../contexts/useCart';

type Currency = 'USD' | 'INR' | 'EUR';

const currencySymbol: Record<Currency, string> = {
  USD: '$',
  INR: '₹',
  EUR: '€',
};

function formatPrice(amount: number, currencyCode: string) {
  const normalized = currencyCode.toUpperCase();
  const code = normalized === 'USD' || normalized === 'INR' || normalized === 'EUR'
    ? normalized as Currency
    : 'USD';

  return `${currencySymbol[code]}${amount.toFixed(code === 'INR' ? 2 : 4)}`;
}

function clampQuantity(nextQuantity: number, minQty: number, orderMultiple: number) {
  const safeMinQty = minQty > 0 ? minQty : 1;
  const safeOrderMultiple = orderMultiple > 0 ? orderMultiple : 1;

  if (nextQuantity <= safeMinQty) {
    return safeMinQty;
  }

  const remainder = (nextQuantity - safeMinQty) % safeOrderMultiple;
  if (remainder === 0) {
    return nextQuantity;
  }

  return nextQuantity - remainder;
}

function isInHouseProviderName(providerName: string) {
  return providerName.trim().toLowerCase() === 'fetchchips direct';
}

function getStockBadgeTone(stock: number) {
  if (stock > 1000) {
    return 'bg-emerald-100 text-emerald-800 border-emerald-200';
  }

  if (stock > 0) {
    return 'bg-amber-100 text-amber-800 border-amber-200';
  }

  return 'bg-rose-100 text-rose-800 border-rose-200';
}

function getStockBadgeLabel(stock: number) {
  if (stock > 1000) {
    return `In Stock · ${stock.toLocaleString()} units`;
  }

  if (stock > 0) {
    return `Low Stock · ${stock.toLocaleString()} units`;
  }

  return 'Out of Stock';
}

export function ProductDetailsPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { addToCart } = useCart();
  const supplierRealId = searchParams.get('supplierRealId') ?? '';
  const mpn = searchParams.get('mpn') ?? '';

  const [detail, setDetail] = useState<ProductDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [quantity, setQuantity] = useState(1);

  useEffect(() => {
    if (!mpn.trim()) {
      setError('No MPN provided for product details.');
      setDetail(null);
      return;
    }

    let isMounted = true;
    const load = async () => {
      setIsLoading(true);
      setError('');

      try {
        const response = await getProductDetails(supplierRealId, mpn);
        if (!isMounted) {
          return;
        }

        setDetail(response);
        const minQty = response.minQty > 0 ? response.minQty : 1;
        const orderMultiple = response.orderMultiple > 0 ? response.orderMultiple : minQty;
        setQuantity(clampQuantity(minQty, minQty, orderMultiple));
      } catch (err) {
        if (!isMounted) {
          return;
        }

        setError(err instanceof Error ? err.message : 'Unable to load product details.');
        setDetail(null);
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void load();

    return () => {
      isMounted = false;
    };
  }, [supplierRealId, mpn]);

  const primaryBestPrice = useMemo(() => {
    if (!detail || !detail.priceBreaks || detail.priceBreaks.length === 0) {
      return 0;
    }

    return detail.priceBreaks
      .filter((tier) => tier.qty > 0)
      .reduce((min, tier) => Math.min(min, tier.unitPrice), detail.priceBreaks[0].unitPrice);
  }, [detail]);

  const activePriceTier = useMemo(() => {
    if (!detail || !detail.priceBreaks || detail.priceBreaks.length === 0) {
      return null;
    }

    const sorted = [...detail.priceBreaks].sort((a, b) => a.qty - b.qty);
    return sorted.reduce((active, tier) => (quantity >= tier.qty ? tier : active), sorted[0]);
  }, [detail, quantity]);

  const totalCost = useMemo(() => {
    if (!activePriceTier) {
      return 0;
    }

    return quantity * activePriceTier.unitPrice;
  }, [activePriceTier, quantity]);

  const specs = useMemo(() => {
    if (!detail) {
      return [] as Array<{ key: string; value: string }>;
    }

    return Object.entries(detail.specifications ?? {}).map(([key, value]) => ({ key, value }));
  }, [detail]);

  const handleQuantityChange = (nextQuantity: number) => {
    if (!detail) {
      return;
    }

    setQuantity(clampQuantity(nextQuantity, detail.minQty || 1, detail.orderMultiple || detail.minQty || 1));
  };

  const handleAddToCart = () => {
    if (!detail) {
      return;
    }

    addToCart({
      providerName: detail.providerName,
      supplierRealId: detail.supplierRealId,
      vendorCartId: detail.vendorCartId,
      mpn: detail.mpn,
      manufacturer: detail.manufacturer || 'Unknown',
      quantity,
      minQty: detail.minQty || 1,
      orderMultiple: detail.orderMultiple || detail.minQty || 1,
      unitPrice: activePriceTier?.unitPrice ?? primaryBestPrice,
      currency: detail.currency || 'USD',
      packagingContainer: detail.packagingContainer,
      priceBreaks: detail.priceBreaks ?? [],
    });
  };

  const handleAlternateAddToCart = (
    providerName: string,
    supplierId: string,
    vendorCartId: string,
    partNumber: string,
    manufacturer: string,
    minQty: number,
    orderMultiple: number,
    unitPrice: number,
    currency: string,
  ) => {
    addToCart({
      providerName,
      supplierRealId: supplierId,
      vendorCartId,
      mpn: partNumber,
      manufacturer,
      quantity,
      minQty,
      orderMultiple,
      unitPrice,
      currency,
      priceBreaks: [{ qty: 1, unitPrice }],
    });
  };

  const handleBuyNow = () => {
    if (!detail) {
      return;
    }

    handleAddToCart();
    navigate('/cart');
  };

  const handleAlternateBuyNow = (
    providerName: string,
    supplierId: string,
    vendorCartId: string,
    partNumber: string,
    manufacturer: string,
    minQty: number,
    orderMultiple: number,
    unitPrice: number,
    currency: string,
  ) => {
    handleAlternateAddToCart(
      providerName,
      supplierId,
      vendorCartId,
      partNumber,
      manufacturer,
      minQty,
      orderMultiple,
      unitPrice,
      currency,
    );
    navigate('/cart');
  };

  const quickIncrement = (amount: number) => {
    if (!detail) {
      return;
    }

    handleQuantityChange(quantity + amount);
  };

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex w-full max-w-[1400px] flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
          <Link to="/" className="whitespace-nowrap text-sm font-semibold uppercase tracking-[0.22em] text-slate-800">
            ProChips
          </Link>

          <div className="ml-auto">
            <SearchHeaderActions />
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[1400px] space-y-5 px-4 py-6 sm:px-6">
        <section className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-white p-4">
          <div className="space-y-1">
            <p className="text-sm text-slate-600">
              <Link to="/" className="font-medium text-slate-700 hover:text-slate-900">Home</Link>
              <span className="mx-2 text-slate-400">&gt;</span>
              <Link to="/search" className="font-medium text-slate-700 hover:text-slate-900">Search Results</Link>
              <span className="mx-2 text-slate-400">&gt;</span>
              <span className="font-semibold text-slate-900">{detail?.mpn || mpn || 'Product'}</span>
            </p>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Product Details</p>
          </div>

          <Link
            to="/search"
            className="inline-flex items-center gap-2 rounded-xl border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Search Results
          </Link>
        </section>

        {error ? <section className="rounded-2xl border border-rose-300 bg-rose-50 p-4 text-sm text-rose-700">{error}</section> : null}

        {isLoading ? (
          <section className="rounded-2xl border border-slate-200 bg-white p-6 text-sm text-slate-600">Loading product details...</section>
        ) : null}

        {!isLoading && detail ? (
          <>
            <section className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
              <article className="space-y-5 rounded-2xl border border-slate-200 bg-white p-6">
                <div className="space-y-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded-full border border-slate-300 bg-slate-100 px-2.5 py-1 text-xs font-semibold uppercase tracking-wide text-slate-700">
                      MPN
                    </span>
                    <h1 className="text-2xl font-semibold text-slate-900">{detail.mpn || mpn}</h1>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded-full border border-indigo-200 bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-700">
                      {detail.manufacturer || 'Unknown Manufacturer'}
                    </span>
                    <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${getStockBadgeTone(detail.availableStock)}`}>
                      {getStockBadgeLabel(detail.availableStock)}
                    </span>
                    <span className="rounded-full border border-slate-300 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-700">
                      {detail.category || 'Uncategorized'}
                    </span>
                  </div>

                  <p className="text-sm leading-6 text-slate-700">{detail.description || 'No description available.'}</p>
                </div>

                <div>
                  <a
                    href={detail.datasheetUrl || '#'}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-2 rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
                  >
                    Open Datasheet
                    <ExternalLink className="h-4 w-4" />
                  </a>
                </div>

                <div className="overflow-x-auto rounded-xl border border-slate-200">
                  <table className="min-w-full text-sm">
                    <thead className="bg-slate-100 text-slate-700">
                      <tr>
                        <th className="px-3 py-2 text-left font-medium">Volume Qty</th>
                        <th className="px-3 py-2 text-left font-medium">Unit Price</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(detail.priceBreaks ?? []).length === 0 ? (
                        <tr>
                          <td className="px-3 py-3 text-slate-600" colSpan={2}>No price tiers available.</td>
                        </tr>
                      ) : (
                        detail.priceBreaks.map((tier) => (
                          <tr
                            key={`${tier.qty}-${tier.unitPrice}`}
                            className={`border-t border-slate-200 ${activePriceTier?.qty === tier.qty ? 'bg-sky-50' : ''}`}
                          >
                            <td className="px-3 py-2 text-slate-800">{tier.qty}+</td>
                            <td className="px-3 py-2 text-slate-800">{formatPrice(tier.unitPrice, detail.currency || 'USD')}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                <div>
                  <h2 className="text-base font-semibold text-slate-900">Key Technical Specifications</h2>
                  <div className="mt-3 grid gap-2 sm:grid-cols-2">
                    {specs.length === 0 ? (
                      <p className="text-sm text-slate-600">No technical specs available.</p>
                    ) : (
                      specs.map((spec) => (
                        <div key={spec.key} className="rounded-xl border border-slate-200 bg-slate-50 p-3">
                          <p className="text-xs uppercase tracking-wide text-slate-500">{spec.key}</p>
                          <p className="mt-1 text-sm font-medium text-slate-900">{spec.value || 'N/A'}</p>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </article>

              <aside className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 shadow-sm lg:sticky lg:top-[78px]">
                <h2 className="text-sm font-semibold uppercase tracking-[0.12em] text-slate-800">Purchase Options</h2>

                <div className="mt-2 grid grid-cols-2 gap-2 rounded-xl border border-slate-200 bg-white p-3">
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Stock</p>
                    <p className="text-sm font-semibold text-slate-900">{detail.availableStock.toLocaleString()}</p>
                  </div>
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Lead Time</p>
                    <p className="text-sm font-semibold text-slate-900">{detail.leadTime || 'TBD'}</p>
                  </div>
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Unit</p>
                    <p className="text-base font-semibold text-slate-900">{formatPrice(activePriceTier?.unitPrice ?? primaryBestPrice, detail.currency || 'USD')}</p>
                  </div>
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Total</p>
                    <p className="text-lg font-bold text-slate-900">{formatPrice(totalCost, detail.currency || 'USD')}</p>
                  </div>
                </div>

                <div className="mt-2 rounded-xl border border-slate-200 bg-white p-3">
                  <label className="mb-1 block text-[11px] font-semibold uppercase tracking-wide text-slate-500" htmlFor="qty-input">Quantity</label>
                  <input
                    id="qty-input"
                    type="number"
                    min={detail.minQty || 1}
                    step={detail.orderMultiple || detail.minQty || 1}
                    value={quantity}
                    onChange={(event) => handleQuantityChange(Number.parseInt(event.target.value || '0', 10) || (detail.minQty || 1))}
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm outline-none ring-slate-200 focus:ring"
                  />

                  <div className="mt-2 grid grid-cols-3 gap-1.5">
                    <button type="button" onClick={() => quickIncrement(100)} className="h-8 rounded-md border border-slate-300 bg-white px-2 text-xs font-medium text-slate-700 hover:bg-slate-100">+100</button>
                    <button type="button" onClick={() => quickIncrement(500)} className="h-8 rounded-md border border-slate-300 bg-white px-2 text-xs font-medium text-slate-700 hover:bg-slate-100">+500</button>
                    <button type="button" onClick={() => quickIncrement(1000)} className="h-8 rounded-md border border-slate-300 bg-white px-2 text-xs font-medium text-slate-700 hover:bg-slate-100">+1000</button>
                  </div>
                </div>

                <div className="mt-2 space-y-1.5">
                  <button
                    type="button"
                    onClick={handleAddToCart}
                    className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-slate-400 bg-white px-3 text-sm font-semibold text-slate-800 hover:bg-slate-100"
                  >
                    <ShoppingCart className="h-4 w-4" />
                    Add to Cart
                  </button>

                  <button
                    type="button"
                    onClick={handleBuyNow}
                    className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-3 text-sm font-semibold text-white hover:bg-blue-500"
                  >
                    <Zap className="h-4 w-4" />
                    Buy Now
                  </button>
                </div>

                <div className="mt-2 grid grid-cols-2 gap-1.5 border-t border-slate-200 pt-2">
                  <p className="rounded-md border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-600">
                    Min: {detail.minQty || 1}
                  </p>
                  <p className="rounded-md border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-600">
                    Multiple: {detail.orderMultiple || detail.minQty || 1}
                  </p>
                  <p className="rounded-md border border-emerald-200 bg-emerald-50 px-2 py-1 text-[11px] font-medium text-emerald-700">
                    Fast Shipping
                  </p>
                  <p className="rounded-md border border-sky-200 bg-sky-50 px-2 py-1 text-[11px] font-medium text-sky-700">
                    Genuine Parts
                  </p>
                </div>
              </aside>
            </section>

            <section className="rounded-2xl border border-slate-200 bg-white p-5">
              <h2 className="text-base font-semibold text-slate-900">Alternate Offers</h2>
              <p className="mt-1 text-sm text-slate-600">Cross-distributor comparison for the same MPN.</p>

              <div className="mt-4 overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead className="bg-slate-100 text-slate-700">
                    <tr>
                      <th className="px-3 py-2 text-left font-medium">Distributor</th>
                      <th className="px-3 py-2 text-left font-medium">Part #</th>
                      <th className="px-3 py-2 text-left font-medium">Stock</th>
                      <th className="px-3 py-2 text-left font-medium">Lead Time</th>
                      <th className="px-3 py-2 text-left font-medium">Best Price</th>
                      <th className="px-3 py-2 text-right font-medium">Purchase</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(detail.alternateOffers ?? []).length === 0 ? (
                      <tr>
                        <td className="px-3 py-3 text-slate-600" colSpan={6}>No alternate offers available.</td>
                      </tr>
                    ) : (
                      detail.alternateOffers.map((offer) => (
                        <tr key={offer.supplierRealId} className="border-t border-slate-200">
                          <td className="px-3 py-2 text-slate-900">{offer.providerName}</td>
                          <td className="px-3 py-2 text-slate-800">{offer.partNumber || detail.mpn}</td>
                          <td className="px-3 py-2 text-slate-800">{offer.availableStock.toLocaleString()}</td>
                          <td className="px-3 py-2 text-slate-800">{offer.leadTime || 'TBD'}</td>
                          <td className="px-3 py-2 text-slate-800">{formatPrice(offer.bestUnitPrice || 0, offer.currency || 'USD')}</td>
                          <td className="px-3 py-2 text-right">
                            {isInHouseProviderName(offer.providerName) ? (
                              <button
                                type="button"
                                onClick={() => handleAlternateAddToCart(
                                  offer.providerName,
                                  offer.supplierRealId,
                                  offer.supplierRealId,
                                  offer.partNumber || detail.mpn,
                                  detail.manufacturer || 'Unknown',
                                  offer.minQty || 1,
                                  offer.orderMultiple || offer.minQty || 1,
                                  offer.bestUnitPrice || 0,
                                  offer.currency || 'USD',
                                )}
                                className="rounded-lg bg-emerald-500 px-3 py-1.5 text-xs font-semibold text-emerald-950 hover:bg-emerald-400"
                              >
                                Add to Cart
                              </button>
                            ) : (
                              <button
                                type="button"
                                onClick={() => handleAlternateBuyNow(
                                  offer.providerName,
                                  offer.supplierRealId,
                                  offer.supplierRealId,
                                  offer.partNumber || detail.mpn,
                                  detail.manufacturer || 'Unknown',
                                  offer.minQty || 1,
                                  offer.orderMultiple || offer.minQty || 1,
                                  offer.bestUnitPrice || 0,
                                  offer.currency || 'USD',
                                )}
                                className="rounded-lg bg-emerald-500 px-3 py-1.5 text-xs font-semibold text-emerald-950 hover:bg-emerald-400"
                              >
                                Buy Now
                              </button>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </>
        ) : null}
      </main>

      <footer className="border-t border-slate-200 bg-white">
        <div className="mx-auto w-full max-w-[1400px] px-4 py-4 text-xs text-slate-500 sm:px-6">
          InvenFlow Marketplace
        </div>
      </footer>
    </div>
  );
}
