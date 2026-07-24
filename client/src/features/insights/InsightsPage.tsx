import { Activity, ArrowUpRight, Package2, TrendingUp, Warehouse } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { getDocuments, getInventory } from '../../services/api';
import type { InboundDocument, InventoryItem } from '../../types';

const trendBars = [
  { label: 'Jan', value: 48 },
  { label: 'Feb', value: 62 },
  { label: 'Mar', value: 71 },
  { label: 'Apr', value: 84 },
  { label: 'May', value: 96 },
  { label: 'Jun', value: 110 },
];

const isProcessed = (status: number | string) => {
  const normalized = typeof status === 'number' ? status : String(status).toLowerCase();
  return normalized === 2 || normalized === 'processed' || normalized === 'processed';
};

const isFailed = (status: number | string) => {
  const normalized = typeof status === 'number' ? status : String(status).toLowerCase();
  return normalized === 3 || normalized === 'failed';
};

export function InsightsPage() {
  const [documents, setDocuments] = useState<InboundDocument[]>([]);
  const [inventory, setInventory] = useState<InventoryItem[]>([]);

  useEffect(() => {
    const loadData = async () => {
      try {
        const [documentsData, inventoryData] = await Promise.all([getDocuments(), getInventory()]);
        setDocuments(documentsData as InboundDocument[]);
        setInventory(inventoryData as InventoryItem[]);
      } catch {
        setDocuments([]);
        setInventory([]);
      }
    };

    void loadData();
    const interval = window.setInterval(() => {
      void loadData();
    }, 10000);

    return () => {
      window.clearInterval(interval);
    };
  }, []);

  const processedCount = useMemo(() => documents.filter((document) => isProcessed(document.status)).length, [documents]);
  const failedCount = useMemo(() => documents.filter((document) => isFailed(document.status)).length, [documents]);
  const accuracyRate = processedCount === 0 ? 0 : Math.max(0, Math.round((processedCount / Math.max(documents.length, 1)) * 100));
  const lowStockAlerts = useMemo(() => inventory.filter((item) => item.quantityOnHand <= 5).length, [inventory]);
  const summaryText = processedCount === 0
    ? 'No documents have been processed yet.'
    : `${processedCount} document${processedCount === 1 ? '' : 's'} processed with ${failedCount} critical failure${failedCount === 1 ? '' : 's'}.`;

  const inventoryValue = useMemo(
    () => inventory.reduce((sum, item) => sum + item.quantityOnHand * item.unitPrice, 0),
    [inventory]
  );

  const stats = [
    {
      label: 'Total Inbound Value Processed',
      value: processedCount === 0 ? '$0.0K' : `$${(inventoryValue / 1000).toFixed(1)}K`,
      detail: processedCount === 0 ? 'No invoices processed yet' : `${processedCount} invoices reviewed`,
    },
    {
      label: 'Extraction Accuracy Rate',
      value: `${accuracyRate}%`,
      detail: documents.length === 0 ? 'No extraction data yet' : `${documents.length} document${documents.length === 1 ? '' : 's'} analyzed`,
    },
    {
      label: 'Low Stock / Reorder Alerts',
      value: `${lowStockAlerts}`,
      detail: lowStockAlerts === 0 ? 'No items below reorder threshold' : `${lowStockAlerts} item${lowStockAlerts === 1 ? '' : 's'} need attention`,
    },
  ];

  return (
    <AppLayout active="/insights">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Operations intelligence</p>
            <h2 className="text-2xl font-semibold text-white">Insights & Forecasts</h2>
          </div>
          <div className="rounded-2xl border border-violet-500/20 bg-violet-500/10 px-4 py-2 text-sm text-violet-200">
            Live AI insights synced hourly
          </div>
        </div>

        <div className="grid gap-4 md:grid-cols-3">
          {stats.map((stat) => (
            <div key={stat.label} className="rounded-[24px] border border-white/10 bg-slate-900/70 p-5 shadow-lg shadow-black/10 backdrop-blur-xl">
              <p className="text-sm text-slate-400">{stat.label}</p>
              <div className="mt-3 flex items-end justify-between gap-3">
                <p className="text-3xl font-semibold text-white">{stat.value}</p>
                <div className="rounded-full border border-emerald-500/20 bg-emerald-500/10 p-2 text-emerald-300">
                  <ArrowUpRight className="h-4 w-4" />
                </div>
              </div>
              <p className="mt-2 text-sm text-slate-500">{stat.detail}</p>
            </div>
          ))}
        </div>

        <div className="grid gap-6 xl:grid-cols-[1.3fr_0.7fr]">
          <div className="rounded-[24px] border border-white/10 bg-slate-900/70 p-6 shadow-lg shadow-black/10 backdrop-blur-xl">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-slate-400">Vendor inflation & price trend</p>
                <h3 className="text-lg font-semibold text-white">Cost movement overview</h3>
              </div>
              <div className="rounded-2xl border border-amber-500/20 bg-amber-500/10 p-2 text-amber-300">
                <TrendingUp className="h-5 w-5" />
              </div>
            </div>

            <div className="mt-6 flex h-48 items-end gap-3">
              {trendBars.map((bar) => (
                <div key={bar.label} className="flex flex-1 flex-col items-center gap-2">
                  <div className="w-full rounded-t-2xl bg-gradient-to-t from-violet-600 to-cyan-400" style={{ height: `${bar.value}%` }} />
                  <span className="text-xs text-slate-400">{bar.label}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="space-y-6">
            <div className="rounded-[24px] border border-white/10 bg-slate-900/70 p-6 shadow-lg shadow-black/10 backdrop-blur-xl">
              <div className="flex items-center gap-3">
                <div className="rounded-2xl border border-cyan-500/20 bg-cyan-500/10 p-2 text-cyan-300">
                  <Warehouse className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-sm text-slate-400">Reorder watchlist</p>
                  <h3 className="text-lg font-semibold text-white">{lowStockAlerts} SKU{lowStockAlerts === 1 ? '' : 's'} flagged</h3>
                </div>
              </div>
              {lowStockAlerts === 0 ? (
                <p className="mt-4 text-sm text-slate-400">All stock levels are healthy right now.</p>
              ) : (
                <ul className="mt-4 space-y-3 text-sm text-slate-300">
                  {inventory
                    .filter((item) => item.quantityOnHand <= 5)
                    .map((item) => (
                      <li key={item.id} className="rounded-2xl border border-white/10 bg-white/5 px-3 py-2">
                        {item.sku} • {item.name} is low ({item.quantityOnHand} on hand)
                      </li>
                    ))}
                </ul>
              )}
            </div>

            <div className="rounded-[24px] border border-white/10 bg-slate-900/70 p-6 shadow-lg shadow-black/10 backdrop-blur-xl">
              <div className="flex items-center gap-3">
                <div className="rounded-2xl border border-emerald-500/20 bg-emerald-500/10 p-2 text-emerald-300">
                  <Activity className="h-5 w-5" />
                </div>
                <div>
                  <p className="text-sm text-slate-400">Processing health</p>
                  <h3 className="text-lg font-semibold text-white">System stable</h3>
                </div>
              </div>
              <div className="mt-4 flex items-center gap-2 text-sm text-slate-400">
                <Package2 className="h-4 w-4 text-violet-300" />
                {summaryText}
              </div>
            </div>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
