import { UserPlus2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { getVendors } from '../../services/api';
import type { Vendor } from '../../types';
import { formatDate } from '../../lib/utils';

export function VendorsPage() {
  const [vendors, setVendors] = useState<Vendor[]>([]);

  useEffect(() => {
    getVendors().then((data) => setVendors(data as Vendor[])).catch(() => setVendors([]));
  }, []);

  return (
    <AppLayout active="/vendors">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Supplier network</p>
            <h2 className="text-2xl font-semibold text-white">Vendor Intelligence</h2>
          </div>
          <Button><UserPlus2 className="mr-2 h-4 w-4" />Add Vendor</Button>
        </div>

        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {vendors.map((vendor) => (
            <article key={vendor.id} className="rounded-[24px] border border-white/10 bg-slate-900/70 p-5 shadow-lg shadow-black/10 backdrop-blur-xl">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-lg font-semibold text-white">{vendor.name}</p>
                  <p className="text-sm text-slate-400">{vendor.code}</p>
                </div>
                <Badge tone="info">Active</Badge>
              </div>
              <div className="mt-5 space-y-2 text-sm text-slate-400">
                <p>Email: {vendor.email}</p>
                <p>Onboarded: {formatDate(vendor.createdAt)}</p>
              </div>
            </article>
          ))}
        </div>
      </div>
    </AppLayout>
  );
}
