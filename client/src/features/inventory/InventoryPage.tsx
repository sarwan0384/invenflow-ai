import { PackagePlus } from 'lucide-react';
import { useEffect, useState } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { getInventory } from '../../services/api';
import type { InventoryItem } from '../../types';
import { formatCurrency, formatDate } from '../../lib/utils';

export function InventoryPage() {
  const [items, setItems] = useState<InventoryItem[]>([]);

  useEffect(() => {
    getInventory().then((data) => setItems(data as InventoryItem[])).catch(() => setItems([]));
  }, []);

  return (
    <AppLayout active="/inventory">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Inventory orchestration</p>
            <h2 className="text-2xl font-semibold text-white">Stock Overview</h2>
          </div>
          <Button><PackagePlus className="mr-2 h-4 w-4" />Add Stock</Button>
        </div>

        <div className="overflow-hidden rounded-[24px] border border-white/10 bg-slate-900/70 shadow-lg shadow-black/10 backdrop-blur-xl">
          <table className="min-w-full divide-y divide-white/10 text-sm">
            <thead className="bg-white/5 text-left text-slate-400">
              <tr>
                <th className="px-4 py-3">SKU</th>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">On Hand</th>
                <th className="px-4 py-3">Unit Price</th>
                <th className="px-4 py-3">Updated</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {items.map((item) => (
                <tr key={item.id} className="text-slate-200">
                  <td className="px-4 py-3 font-medium text-white">{item.sku}</td>
                  <td className="px-4 py-3">{item.name}</td>
                  <td className="px-4 py-3">{item.category}</td>
                  <td className="px-4 py-3">{item.quantityOnHand}</td>
                  <td className="px-4 py-3">{formatCurrency(item.unitPrice)}</td>
                  <td className="px-4 py-3">{formatDate(item.updatedAt)}</td>
                  <td className="px-4 py-3"><Badge tone={item.quantityOnHand < 20 ? 'warning' : 'success'}>{item.quantityOnHand < 20 ? 'Low stock' : 'Healthy'}</Badge></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AppLayout>
  );
}
