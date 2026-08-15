import { useMemo, useState } from 'react';
import { Search } from 'lucide-react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';

type Category = 'Electronics' | 'Watches' | 'Perfumes';

type MarketplaceRow = {
  id: string;
  partNumber: string;
  manufacturer: string;
  description: string;
  stock: number;
  priceBreaks: string[];
  supplierTag: string;
};

const electronicsRows: MarketplaceRow[] = [
  {
    id: 'bav99-7-f',
    partNumber: 'BAV99-7-F',
    manufacturer: 'Diodes Incorporated',
    description: 'Dual switching diode, SOT-23, 85V',
    stock: 18420,
    priceBreaks: ['1: $0.09', '100: $0.06', '1000: $0.04'],
    supplierTag: 'Verified Distributor #104',
  },
  {
    id: 'bav99wt1g',
    partNumber: 'BAV99WT1G',
    manufacturer: 'onsemi',
    description: 'General-purpose dual diode, SC-70',
    stock: 9620,
    priceBreaks: ['1: $0.11', '100: $0.08', '1000: $0.05'],
    supplierTag: 'Verified Distributor #238',
  },
  {
    id: 'bav99lt1g',
    partNumber: 'BAV99LT1G',
    manufacturer: 'onsemi',
    description: 'Small signal dual diode, SOT-23',
    stock: 27100,
    priceBreaks: ['1: $0.10', '100: $0.07', '1000: $0.045'],
    supplierTag: 'Verified Distributor #512',
  },
];

const quickPills = ['BAV99', '1N4148W'];

export function UniversalSearchPage() {
  const [query, setQuery] = useState('BAV99');
  const [category, setCategory] = useState<Category>('Electronics');

  const rows = useMemo(() => {
    const normalized = query.trim().toUpperCase();
    if (!normalized) {
      return electronicsRows;
    }

    return electronicsRows.filter((row) => {
      return (
        row.partNumber.toUpperCase().includes(normalized) ||
        row.manufacturer.toUpperCase().includes(normalized) ||
        row.description.toUpperCase().includes(normalized)
      );
    });
  }, [query]);

  return (
    <AppLayout active="/">
      <div className="space-y-6">
        <section className="rounded-[28px] border border-white/10 bg-slate-900/70 p-6 shadow-2xl shadow-black/20 backdrop-blur-xl">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-sm text-emerald-200">
            <Search className="h-4 w-4" />
            Universal Cross-Platform Part Search
          </div>
          <h2 className="text-3xl font-semibold text-white">Find the best source for every part, instantly.</h2>
          <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-400">Search across internal warehouse inventory and external supplier marketplaces while preserving blind procurement workflows.</p>

          <div className="mt-6 grid gap-3 lg:grid-cols-[220px_minmax(0,1fr)_auto]">
            <label className="flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-3 text-sm text-slate-200">
              <span className="text-slate-400">Category</span>
              <select
                value={category}
                onChange={(event) => setCategory(event.target.value as Category)}
                className="w-full bg-transparent text-white outline-none"
              >
                <option value="Electronics" className="bg-slate-900">Electronics</option>
                <option value="Watches" className="bg-slate-900" disabled>Watches (Upcoming)</option>
                <option value="Perfumes" className="bg-slate-900" disabled>Perfumes (Upcoming)</option>
              </select>
            </label>

            <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-slate-200">
              <Search className="h-4 w-4 text-slate-400" />
              <input
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                className="w-full bg-transparent text-white outline-none"
                placeholder="Search by part number, MPN, or manufacturer"
              />
            </label>

            <Button type="button">Search</Button>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-2">
            <span className="text-sm text-slate-400">Try searching:</span>
            {quickPills.map((pill) => (
              <button
                key={pill}
                type="button"
                onClick={() => setQuery(pill)}
                className="rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-sm text-emerald-200 transition hover:bg-emerald-500/20"
              >
                {pill}
              </button>
            ))}
          </div>
        </section>

        <section className="rounded-[24px] border border-emerald-500/30 bg-emerald-500/10 p-5">
          <div className="flex items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-semibold text-white">Our Warehouse Inventory</h3>
              <p className="text-sm text-emerald-100/80">Prioritized internal stock for same-day dispatch.</p>
            </div>
            <Badge tone="success">In-House Stock</Badge>
          </div>
        </section>

        <section className="overflow-hidden rounded-[24px] border border-white/10 bg-slate-900/70 shadow-lg shadow-black/10 backdrop-blur-xl">
          <div className="border-b border-white/10 px-5 py-4">
            <h3 className="text-lg font-semibold text-white">Marketplace Comparison</h3>
            <p className="text-sm text-slate-400">Live supplier rows grouped for blind procurement.</p>
          </div>

          <div className="overflow-x-auto">
            <table className="min-w-full text-left text-sm">
              <thead className="bg-slate-900/90 text-slate-300">
                <tr>
                  <th className="px-4 py-3 font-medium">Part #</th>
                  <th className="px-4 py-3 font-medium">Manufacturer</th>
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 font-medium">Live Stock</th>
                  <th className="px-4 py-3 font-medium">Price Breaks</th>
                  <th className="px-4 py-3 font-medium">Supplier</th>
                  <th className="px-4 py-3 font-medium text-right">Action</th>
                </tr>
              </thead>
              <tbody>
                {rows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-slate-400">No matches found for this query.</td>
                  </tr>
                ) : (
                  rows.map((row) => (
                    <tr key={row.id} className="border-t border-white/10 text-slate-200">
                      <td className="px-4 py-3 font-medium text-white">{row.partNumber}</td>
                      <td className="px-4 py-3">{row.manufacturer}</td>
                      <td className="px-4 py-3">{row.description}</td>
                      <td className="px-4 py-3">{row.stock.toLocaleString()}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1">
                          {row.priceBreaks.map((tier) => (
                            <span key={tier} className="rounded-full border border-white/15 bg-white/5 px-2 py-0.5 text-xs">{tier}</span>
                          ))}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-slate-300">{row.supplierTag}</td>
                      <td className="px-4 py-3 text-right">
                        <button type="button" className="rounded-xl bg-emerald-500 px-3 py-1.5 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400">
                          Buy Now
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </AppLayout>
  );
}
