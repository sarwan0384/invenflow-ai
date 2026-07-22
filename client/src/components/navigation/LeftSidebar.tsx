import { BarChart3, Boxes, FileText, LayoutGrid, Package2, Truck, ChevronLeft, ChevronRight } from 'lucide-react';
import { useState } from 'react';
import { cn } from '../../lib/utils';

const navItems = [
  { label: 'Overview', href: '/', icon: LayoutGrid },
  { label: 'Inventory', href: '/inventory', icon: Boxes },
  { label: 'Vendors', href: '/vendors', icon: Truck },
  { label: 'Documents', href: '/documents', icon: FileText },
  { label: 'Insights', href: '/insights', icon: BarChart3 },
];

type Props = { active?: string };

export function LeftSidebar({ active = '/' }: Props) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <aside className={cn('hidden border-r border-white/10 bg-slate-950/80 backdrop-blur-xl lg:flex lg:flex-col', collapsed ? 'w-20' : 'w-72')}>
      <div className="flex items-center justify-between border-b border-white/10 px-4 py-4">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-gradient-to-br from-violet-500 to-indigo-500 text-white shadow-lg shadow-violet-500/20">
            <Package2 className="h-5 w-5" />
          </div>
          {!collapsed ? <div><p className="text-sm font-semibold text-white">InvenFlow AI</p><p className="text-xs text-slate-400">Command Center</p></div> : null}
        </div>
        <button type="button" onClick={() => setCollapsed((v) => !v)} className="rounded-lg p-2 text-slate-400 hover:bg-white/10 hover:text-white">
          {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
        </button>
      </div>

      <nav className="flex-1 space-y-1 p-3">
        {navItems.map((item) => {
          const Icon = item.icon;
          const isActive = active === item.href;
          return (
            <a key={item.href} href={item.href} className={cn('flex items-center gap-3 rounded-2xl px-3 py-3 text-sm transition-all', isActive ? 'bg-violet-500/15 text-white shadow-inner shadow-violet-500/10' : 'text-slate-400 hover:bg-white/10 hover:text-white')}>
              <Icon className="h-4 w-4" />
              {!collapsed ? <span>{item.label}</span> : null}
            </a>
          );
        })}
      </nav>

      <div className="border-t border-white/10 p-4">
        <div className="rounded-2xl border border-violet-500/20 bg-violet-500/10 p-3 text-sm text-slate-200">
          <p className="font-medium">AI sync online</p>
          <p className="mt-1 text-xs text-slate-400">31 docs processed today</p>
        </div>
      </div>
    </aside>
  );
}
