import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Search, SlidersHorizontal } from 'lucide-react';
import { SearchHeaderActions } from '../../components/navigation/SearchHeaderActions';
import { useCart } from '../../contexts/useCart';
import { searchUniversalProducts, type ProviderResultGroup, type UniversalProduct } from '../../services/api';

type Currency = 'USD' | 'INR' | 'EUR';

const currencySymbol: Record<Currency, string> = {
  USD: '$',
  INR: '₹',
  EUR: '€',
};

const toUsdRate: Record<Currency, number> = {
  USD: 1,
  INR: 0.012,
  EUR: 1.09,
};

const MAX_RESULTS_PER_QUERY = 20;
const PROVIDER_LAYOUT_ORDER = ['Fetchchips', 'Arrow Electronics', 'DigiKey'];

type ProviderCard = {
  providerName: string;
  title: string;
  results: UniversalProduct[];
};

function formatStockDisplay(item: UniversalProduct) {
  const isCapped = item.availableStock >= 10000 || item.availabilityStatus === '10,000+ in stock';
  return isCapped ? '10,000+' : item.availableStock.toLocaleString();
}

function normalizeProviderName(name: string) {
  return name.trim().toLowerCase();
}

function getCardTitle(providerName: string) {
  if (normalizeProviderName(providerName) === 'fetchchips') {
    return 'Fetchchips (In-House Stock)';
  }

  return providerName;
}

function getRowPartNumber(item: UniversalProduct) {
  return item.partNumber || item.sku || item.title;
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

function getUnitPriceForQuantity(item: UniversalProduct, quantity: number) {
  const tiers = [...(item.priceBreaks ?? [])]
    .filter((tier) => tier.qty > 0)
    .sort((a, b) => a.qty - b.qty);

  if (tiers.length === 0) {
    return 0;
  }

  return tiers.reduce((active, tier) => (quantity >= tier.qty ? tier.unitPrice : active), tiers[0].unitPrice);
}

function convertCurrency(amount: number, sourceCurrencyCode: string, targetCurrency: Currency) {
  const normalizedSource = sourceCurrencyCode.toUpperCase();
  const sourceCurrency = (normalizedSource === 'USD' || normalizedSource === 'INR' || normalizedSource === 'EUR')
    ? normalizedSource as Currency
    : 'USD';

  const usdValue = amount * toUsdRate[sourceCurrency];
  return usdValue / toUsdRate[targetCurrency];
}

function formatConvertedPrice(amount: number, sourceCurrencyCode: string, targetCurrency: Currency) {
  const converted = convertCurrency(amount, sourceCurrencyCode, targetCurrency);
  const precision = targetCurrency === 'INR' ? 2 : 3;
  return `${currencySymbol[targetCurrency]}${converted.toFixed(precision)}`;
}

export function SearchResultsPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { addToCart } = useCart();

  const query = searchParams.get('q')?.trim() || 'BAV99';
  const category = searchParams.get('category')?.trim() || 'Electronics';
  const errorMessage = searchParams.get('error');

  const [topQuery, setTopQuery] = useState(query);
  const [desiredStock, setDesiredStock] = useState('');
  const [inStockOnly, setInStockOnly] = useState(true);
  const [currency, setCurrency] = useState<Currency>('INR');
  const [manufacturer, setManufacturer] = useState('All Manufacturers');
  const [providerGroups, setProviderGroups] = useState<ProviderResultGroup[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [apiError, setApiError] = useState('');
  const [rowQuantities, setRowQuantities] = useState<Record<string, number>>({});

  useEffect(() => {
    let isMounted = true;
    const loadResults = async () => {
      setIsLoading(true);
      setApiError('');

      try {
        const liveProducts = await searchUniversalProducts(query, category);
        if (!isMounted) {
          return;
        }

        setProviderGroups(liveProducts);
      } catch (err) {
        if (!isMounted) {
          return;
        }

        const message = err instanceof Error ? err.message : 'Unable to load marketplace results.';
        setApiError(message);
        setProviderGroups([]);
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void loadResults();

    return () => {
      isMounted = false;
    };
  }, [query, category]);

  const submitSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const nextQuery = topQuery.trim() || 'BAV99';
    setTopQuery(nextQuery);
    navigate(`/search?q=${encodeURIComponent(nextQuery)}&category=${encodeURIComponent(category)}`);
  };

  const manufacturers = useMemo(() => {
    const all = providerGroups
      .flatMap((group) => group.results)
      .map((item) => item.brandOrManufacturer)
      .filter(Boolean);

    return ['All Manufacturers', ...Array.from(new Set(all))];
  }, [providerGroups]);

  const filteredCards = useMemo(() => {
    const requiredStock = Number.parseInt(desiredStock, 10);
    const cards: ProviderCard[] = [];

    const providerNames = providerGroups.length > 0
      ? PROVIDER_LAYOUT_ORDER.filter((providerName) => providerGroups.some((group) => normalizeProviderName(group.providerName) === normalizeProviderName(providerName)))
          .concat(providerGroups.map((group) => group.providerName).filter((providerName) => !PROVIDER_LAYOUT_ORDER.some((known) => normalizeProviderName(known) === normalizeProviderName(providerName))))
      : PROVIDER_LAYOUT_ORDER;

    for (const providerName of providerNames) {
      const sourceGroup = providerGroups.find((group) => normalizeProviderName(group.providerName) === normalizeProviderName(providerName));
      const items = sourceGroup?.results ?? [];

      const filteredItems = items
        .filter((item) => {
          const stockMatch = Number.isNaN(requiredStock) ? true : item.availableStock >= requiredStock;
          const inStockMatch = inStockOnly ? item.availableStock > 0 : true;
          const manufacturerMatch = manufacturer === 'All Manufacturers' ? true : item.brandOrManufacturer === manufacturer;
          return stockMatch && inStockMatch && manufacturerMatch;
        })
        .sort((a, b) => b.availableStock - a.availableStock)
        .slice(0, MAX_RESULTS_PER_QUERY);

      cards.push({
        providerName,
        title: getCardTitle(providerName),
        results: filteredItems,
      });
    }

    return cards;
  }, [providerGroups, desiredStock, inStockOnly, manufacturer]);

  const getRowKey = (item: UniversalProduct) => `${item.providerName}-${item.itemId}-${item.supplierRealId}`;

  const getRowQuantity = (item: UniversalProduct) => {
    const key = getRowKey(item);
    const minQty = item.minQty > 0 ? item.minQty : 1;
    const stepQty = minQty;
    return rowQuantities[key] ?? clampQuantity(minQty, minQty, stepQty);
  };

  const updateRowQuantity = (item: UniversalProduct, nextQuantity: number) => {
    const key = getRowKey(item);
    const minQty = item.minQty > 0 ? item.minQty : 1;
    const stepQty = minQty;
    setRowQuantities((prev) => ({
      ...prev,
      [key]: clampQuantity(nextQuantity, minQty, stepQty),
    }));
  };

  const handleAddToCart = (item: UniversalProduct, navigateToCart: boolean) => {
    const selectedQuantity = getRowQuantity(item);
    const unitPrice = getUnitPriceForQuantity(item, selectedQuantity);

    addToCart({
      providerName: item.providerName,
      supplierRealId: item.supplierRealId,
      vendorCartId: item.vendorCartId || item.supplierRealId,
      mpn: getRowPartNumber(item),
      manufacturer: item.brandOrManufacturer || 'Unknown',
      quantity: selectedQuantity,
      minQty: item.minQty > 0 ? item.minQty : 1,
      orderMultiple: item.minQty > 0 ? item.minQty : 1,
      unitPrice,
      currency: item.currency || 'USD',
      packagingContainer: item.packagingContainer,
      priceBreaks: item.priceBreaks ?? [],
    });

    if (navigateToCart) {
      navigate('/cart');
    }
  };

  const totalVisibleRows = useMemo(
    () => filteredCards.reduce((acc, card) => acc + card.results.length, 0),
    [filteredCards],
  );

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex w-full max-w-[1400px] flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
          <Link to="/" className="whitespace-nowrap text-sm font-semibold uppercase tracking-[0.22em] text-slate-800">
            ProChips
          </Link>

          <form onSubmit={submitSearch} className="flex min-w-0 flex-1 items-center gap-2 rounded-full border border-slate-300 bg-white px-3 py-2">
            <Search className="h-4 w-4 text-slate-500" />
            <input
              value={topQuery}
              onChange={(event) => setTopQuery(event.target.value)}
              className="w-full bg-transparent text-sm outline-none"
              placeholder="Search parts, products, or manufacturers"
            />
            <button type="submit" className="rounded-full bg-orange-500 px-4 py-1.5 text-sm font-medium text-slate-950 hover:bg-orange-400">
              Find
            </button>
          </form>

          <div className="ml-auto">
            <SearchHeaderActions />
          </div>
        </div>
      </header>

      <div className="sticky top-[61px] z-30 border-b border-slate-200 bg-white">
        <div className="mx-auto flex w-full max-w-[1400px] flex-wrap items-center gap-3 px-4 py-3 sm:px-6">
          <div className="flex flex-wrap gap-2">
            {['Part Search', 'Parametric Search', 'Product Details'].map((pill, index) => (
              <button
                key={pill}
                type="button"
                className={`rounded-full px-3 py-1.5 text-xs font-medium ${index === 0 ? 'bg-slate-900 text-white' : 'border border-slate-300 text-slate-700 hover:bg-slate-100'}`}
              >
                {pill}
              </button>
            ))}
          </div>

          <div className="ml-auto flex flex-wrap items-center gap-2 text-sm">
            <div className="flex items-center gap-2 rounded-full border border-slate-300 bg-white px-3 py-1.5">
              <SlidersHorizontal className="h-4 w-4 text-slate-500" />
              <label htmlFor="desired-stock" className="text-slate-600">Desired Stock</label>
              <input
                id="desired-stock"
                type="number"
                min={0}
                value={desiredStock}
                onChange={(event) => setDesiredStock(event.target.value)}
                className="w-20 bg-transparent text-right outline-none"
                placeholder="Any"
              />
            </div>

            <label className="flex items-center gap-2 rounded-full border border-slate-300 bg-white px-3 py-1.5">
              <input
                type="checkbox"
                checked={inStockOnly}
                onChange={(event) => setInStockOnly(event.target.checked)}
                className="h-4 w-4 rounded border-slate-300"
              />
              In-Stock Only
            </label>

            <label className="rounded-full border border-slate-300 bg-white px-3 py-1.5">
              <span className="mr-2 text-slate-600">Currency</span>
              <select value={currency} onChange={(event) => setCurrency(event.target.value as Currency)} className="bg-transparent outline-none">
                <option value="USD">USD $</option>
                <option value="INR">INR ₹</option>
                <option value="EUR">EUR €</option>
              </select>
            </label>

            <label className="rounded-full border border-slate-300 bg-white px-3 py-1.5">
              <span className="mr-2 text-slate-600">Manufacturer</span>
              <select value={manufacturer} onChange={(event) => setManufacturer(event.target.value)} className="bg-transparent outline-none">
                {manufacturers.map((item) => (
                  <option key={item} value={item}>{item}</option>
                ))}
              </select>
            </label>
          </div>
        </div>
      </div>

      <main className="mx-auto w-full max-w-[1400px] space-y-5 px-4 py-6 sm:px-6">
        {errorMessage ? <section className="rounded-2xl border border-rose-300 bg-rose-50 p-4 text-sm text-rose-700">{errorMessage}</section> : null}
        {apiError ? <section className="rounded-2xl border border-rose-300 bg-rose-50 p-4 text-sm text-rose-700">{apiError}</section> : null}

        <section className="space-y-4">
          <div className="rounded-2xl border border-slate-200 bg-white p-4">
            <p className="text-sm text-slate-600">
              Results for <span className="font-semibold text-slate-900">{query}</span> in <span className="font-semibold text-slate-900">{category}</span>
            </p>
            {isLoading ? <p className="mt-2 text-sm text-slate-500">Loading live marketplace data...</p> : null}
          </div>

          {!isLoading && totalVisibleRows === 0 ? (
            <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-slate-500">No matching suppliers found for the selected filters.</div>
          ) : (
            <div className="space-y-4">
              {filteredCards.map((card) => (
                <article key={card.providerName} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                  <div className="mb-4 flex items-center justify-between gap-3">
                    <h3 className="text-lg font-semibold text-slate-900">{card.title}</h3>
                    <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-700">
                      {card.results.length} item{card.results.length === 1 ? '' : 's'}
                    </span>
                  </div>

                  {card.results.length === 0 ? (
                    <p className="rounded-xl bg-slate-50 p-4 text-sm text-slate-500">No matching parts for this distributor.</p>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="min-w-full text-sm">
                        <thead className="bg-slate-100 text-slate-700">
                          <tr>
                              <th className="px-3 py-2 text-left font-medium">Part #</th>
                              <th className="px-3 py-2 text-left font-medium">Manufacturer</th>
                              <th className="px-3 py-2 text-left font-medium">Description</th>
                              <th className="px-3 py-2 text-left font-medium">Stock</th>
                              <th className="px-3 py-2 text-left font-medium">Price</th>
                              <th className="px-3 py-2 text-right font-medium">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {card.results.map((item) => (
                            <tr key={`${card.providerName}-${item.itemId}-${item.supplierRealId}`} className="border-t border-slate-200 align-top">
                              <td className="px-3 py-3">
                                <Link
                                  to={`/product-details?supplierRealId=${encodeURIComponent(item.supplierRealId)}&mpn=${encodeURIComponent(getRowPartNumber(item))}`}
                                  className="font-semibold text-slate-900 underline-offset-2 hover:underline"
                                >
                                  {getRowPartNumber(item)}
                                </Link>
                                <p className="mt-1 text-xs text-slate-500">{item.distiSku || `DISTI # ${item.supplierRealId}`}</p>
                              </td>
                              <td className="px-3 py-3 text-slate-800">{item.brandOrManufacturer || 'Unknown'}</td>
                              <td className="px-3 py-3">
                                <p className="text-slate-800">{item.description || item.title}</p>
                                <div className="mt-2 space-y-1 text-xs text-slate-600">
                                  <p>Min Qty: {item.minQty > 0 ? item.minQty : 1}</p>
                                  <p>Container: {item.packagingContainer || 'N/A'}</p>
                                  <p>RoHS: {item.roHSStatus || 'Unknown'}</p>
                                  <p>Lead time: {item.leadTime || 'TBD'}</p>
                                </div>
                              </td>
                              <td className="px-3 py-3 font-semibold text-slate-900">
                                <p>{item.regionStock || 'Warehouse - N/A'}</p>
                                <p className="mt-1 text-xs font-normal text-slate-600">{formatStockDisplay(item)} in stock</p>
                              </td>
                              <td className="px-3 py-3">
                                <div className="space-y-1">
                                  {(item.priceBreaks ?? []).length === 0 ? (
                                    <span className="text-slate-600">No tier pricing published</span>
                                  ) : (
                                    (item.priceBreaks ?? []).map((tier) => (
                                      <div key={`${card.providerName}-${item.itemId}-${tier.qty}`} className="text-slate-700">
                                        {tier.qty}+ : {formatConvertedPrice(tier.unitPrice, item.currency || 'USD', currency)}
                                      </div>
                                    ))
                                  )}
                                </div>
                              </td>
                              <td className="px-3 py-3 text-right">
                                <div className="ml-auto flex w-[230px] flex-col gap-2">
                                  <div className="inline-flex items-center justify-end gap-1">
                                    <button
                                      type="button"
                                      onClick={() => updateRowQuantity(item, getRowQuantity(item) - (item.minQty > 0 ? item.minQty : 1))}
                                      className="h-8 w-8 rounded-lg border border-slate-300 bg-white text-sm font-semibold text-slate-700 hover:bg-slate-100"
                                    >
                                      -
                                    </button>
                                    <input
                                      type="number"
                                      min={item.minQty > 0 ? item.minQty : 1}
                                      step={item.minQty > 0 ? item.minQty : 1}
                                      value={getRowQuantity(item)}
                                      onChange={(event) => updateRowQuantity(item, Number.parseInt(event.target.value || '0', 10) || (item.minQty > 0 ? item.minQty : 1))}
                                      className="h-8 w-20 rounded-lg border border-slate-300 bg-white px-2 text-center text-sm outline-none ring-slate-200 focus:ring"
                                    />
                                    <button
                                      type="button"
                                      onClick={() => updateRowQuantity(item, getRowQuantity(item) + (item.minQty > 0 ? item.minQty : 1))}
                                      className="h-8 w-8 rounded-lg border border-slate-300 bg-white text-sm font-semibold text-slate-700 hover:bg-slate-100"
                                    >
                                      +
                                    </button>
                                  </div>

                                  <div className="grid grid-cols-2 gap-1.5">
                                    <button
                                      type="button"
                                      onClick={() => handleAddToCart(item, false)}
                                      className="h-8 rounded-lg border border-slate-400 bg-white px-2 text-xs font-semibold text-slate-800 hover:bg-slate-100"
                                    >
                                      Add to Cart
                                    </button>
                                    <button
                                      type="button"
                                      onClick={() => handleAddToCart(item, true)}
                                      className="h-8 rounded-lg bg-emerald-500 px-2 text-xs font-semibold text-emerald-950 transition hover:bg-emerald-400"
                                    >
                                      Buy Now
                                    </button>
                                  </div>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </article>
              ))}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
