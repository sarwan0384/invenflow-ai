import { Activity, Boxes, FileCheck2, TrendingUp, Truck } from 'lucide-react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';

const metrics = [
  { label: 'Inventory Value', value: '$1.24M', detail: '+12.4% vs last month', icon: TrendingUp },
  { label: 'Active SKUs', value: '384', detail: '18 newly added', icon: Boxes },
  { label: 'Docs Processed', value: '126', detail: '92% confidence', icon: FileCheck2 },
  { label: 'Vendors Synced', value: '47', detail: '3 pending review', icon: Truck },
];

const recentActivity = [
  { title: 'Invoice #4821 reviewed', meta: 'High confidence • 2 mins ago', tone: 'success' as const },
  { title: 'Stock threshold alert', meta: 'SKU-1042 • 14 mins ago', tone: 'warning' as const },
  { title: 'Vendor onboarding completed', meta: 'Northwind • 32 mins ago', tone: 'info' as const },
];

export function DashboardPage() {
  return (
    <AppLayout active="/">
      <div className="space-y-6">
        <section className="rounded-[28px] border border-white/10 bg-slate-900/70 p-6 shadow-2xl shadow-black/20 backdrop-blur-xl">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
            <div className="max-w-2xl">
              <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-violet-500/20 bg-violet-500/10 px-3 py-1 text-sm text-violet-200">
                <Activity className="h-4 w-4" />
                AI inventory operations are running smoothly
              </div>
              <h2 className="text-3xl font-semibold text-white">A calm, intelligent command center for every inbound flow.</h2>
              <p className="mt-3 text-sm leading-7 text-slate-400">Monitor inventory, vendors, and inbound documents from one elegant control surface with automated extraction and exception handling.</p>
            </div>
            <div className="flex gap-3">
              <Button variant="secondary">Export Brief</Button>
              <Button>New Review</Button>
            </div>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {metrics.map((metric) => {
            const Icon = metric.icon;
            return (
              <article key={metric.label} className="rounded-[24px] border border-white/10 bg-slate-900/70 p-5 shadow-lg shadow-black/10 backdrop-blur-xl">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="text-sm text-slate-400">{metric.label}</p>
                    <p className="mt-2 text-2xl font-semibold text-white">{metric.value}</p>
                  </div>
                  <div className="rounded-2xl bg-violet-500/10 p-2 text-violet-300">
                    <Icon className="h-5 w-5" />
                  </div>
                </div>
                <p className="mt-4 text-sm text-slate-500">{metric.detail}</p>
              </article>
            );
          })}
        </section>

        <section className="grid gap-6 xl:grid-cols-[1.5fr_0.9fr]">
          <article className="rounded-[24px] border border-white/10 bg-slate-900/70 p-6 shadow-lg shadow-black/10 backdrop-blur-xl">
            <div className="mb-5 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold text-white">Weekly throughput</h3>
                <p className="text-sm text-slate-400">Document automation velocity across the last 7 days</p>
              </div>
              <Badge tone="success">+18% throughput</Badge>
            </div>
            <div className="grid grid-cols-7 gap-3">
              {[44, 61, 58, 77, 69, 85, 92].map((value, index) => (
                <div key={index} className="flex flex-col items-center gap-2">
                  <div className="flex h-28 w-full items-end rounded-2xl border border-white/10 bg-gradient-to-t from-violet-600/40 to-slate-800 p-2">
                    <div className="w-full rounded-xl bg-gradient-to-t from-violet-500 to-indigo-400" style={{ height: `${value}%` }} />
                  </div>
                  <span className="text-xs text-slate-500">D{index + 1}</span>
                </div>
              ))}
            </div>
          </article>

          <article className="rounded-[24px] border border-white/10 bg-slate-900/70 p-6 shadow-lg shadow-black/10 backdrop-blur-xl">
            <div className="mb-5 flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold text-white">Recent activity</h3>
                <p className="text-sm text-slate-400">Signal from the system</p>
              </div>
            </div>
            <div className="space-y-3">
              {recentActivity.map((item) => (
                <div key={item.title} className="rounded-2xl border border-white/10 bg-white/5 p-3">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-medium text-white">{item.title}</p>
                    <Badge tone={item.tone}>{item.tone === 'warning' ? 'Needs attention' : item.tone === 'success' ? 'Healthy' : 'Stable'}</Badge>
                  </div>
                  <p className="mt-2 text-sm text-slate-400">{item.meta}</p>
                </div>
              ))}
            </div>
          </article>
        </section>
      </div>
    </AppLayout>
  );
}
