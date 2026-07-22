import type { ReactNode } from 'react';
import { Bell, Search, Sparkles } from 'lucide-react';
import { LeftSidebar } from '../navigation/LeftSidebar';

type Props = {
  children: ReactNode;
  active?: string;
};

export function AppLayout({ children, active = '/' }: Props) {
  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(120,119,198,0.16),_transparent_30%),linear-gradient(135deg,_#020617,_#0f172a_45%,_#111827)] text-slate-100">
      <div className="flex min-h-screen">
        <LeftSidebar active={active} />
        <div className="flex-1">
          <header className="border-b border-white/10 bg-slate-950/70 px-6 py-4 backdrop-blur-xl">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <p className="text-sm font-medium uppercase tracking-[0.3em] text-violet-300">InvenFlow AI</p>
                <h1 className="text-xl font-semibold text-white">Operations Intelligence Console</h1>
              </div>
              <div className="flex items-center gap-3">
                <label className="flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-400">
                  <Search className="h-4 w-4" />
                  <input className="w-32 bg-transparent outline-none sm:w-48" placeholder="Search" />
                </label>
                <button type="button" className="rounded-2xl border border-white/10 bg-white/5 p-2.5 text-slate-300 hover:bg-white/10 hover:text-white">
                  <Bell className="h-4 w-4" />
                </button>
                <div className="flex items-center gap-2 rounded-2xl border border-violet-500/20 bg-violet-500/10 px-3 py-2">
                  <Sparkles className="h-4 w-4 text-violet-300" />
                  <span className="text-sm text-slate-200">AI Ready</span>
                </div>
              </div>
            </div>
          </header>

          <main className="p-6 lg:p-8">{children}</main>
        </div>
      </div>
    </div>
  );
}
