import { useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Bell, Moon, Search, Sparkles, Sun, UserCircle2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { LeftSidebar } from '../navigation/LeftSidebar';
import { useAuth } from '../../contexts/useAuth';
import { searchContent } from '../../services/api';

type Props = {
  children: ReactNode;
  active?: string;
};

export function AppLayout({ children, active = '/' }: Props) {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [theme, setTheme] = useState<'dark' | 'light'>(() => {
    const storedTheme = localStorage.getItem('invenflow-theme') as 'dark' | 'light' | null;
    if (storedTheme === 'light' || storedTheme === 'dark') {
      return storedTheme;
    }
    return 'dark';
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [results, setResults] = useState<{ inventory: Array<{ title: string; subtitle: string }>; vendors: Array<{ title: string; subtitle: string }>; documents: Array<{ title: string; subtitle: string }> }>({ inventory: [], vendors: [], documents: [] });

  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
  }, [theme]);

  useEffect(() => {
    const timeout = window.setTimeout(async () => {
      if (!searchQuery.trim()) {
        setResults({ inventory: [], vendors: [], documents: [] });
        return;
      }

      try {
        const payload = await searchContent(searchQuery);
        setResults(payload);
      } catch {
        setResults({ inventory: [], vendors: [], documents: [] });
      }
    }, 250);
    return () => window.clearTimeout(timeout);
  }, [searchQuery]);

  const toggleTheme = () => {
    const nextTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(nextTheme);
    localStorage.setItem('invenflow-theme', nextTheme);
  };

  const profileLabel = useMemo(() => user ? `${user.userName} • ${user.role}` : 'Guest', [user]);

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(120,119,198,0.16),_transparent_30%),linear-gradient(135deg,_#020617,_#0f172a_45%,_#111827)] text-slate-100 dark:bg-slate-950">
      <div className="flex min-h-screen">
        <LeftSidebar active={active} />
        <div className="flex-1">
          <header className="border-b border-white/10 bg-slate-950/70 px-6 py-4 backdrop-blur-xl">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <p className="text-sm font-medium uppercase tracking-[0.3em] text-violet-300">InvenFlow AI</p>
                <h1 className="text-xl font-semibold text-white">Operations Intelligence Console</h1>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <label className="relative flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-400">
                  <Search className="h-4 w-4" />
                  <input value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} className="w-32 bg-transparent outline-none sm:w-48" placeholder="Search inventory, vendors, docs" />
                  {searchQuery ? <div className="absolute left-0 top-full z-20 mt-2 w-80 rounded-2xl border border-white/10 bg-slate-900/95 p-3 shadow-2xl">
                    {results.inventory.length === 0 && results.vendors.length === 0 && results.documents.length === 0 ? <p className="text-sm text-slate-400">No matches yet.</p> : <div className="space-y-2 text-sm">{results.inventory.map((item) => <div key={item.title} className="rounded-xl bg-white/5 p-2"><p className="text-white">{item.title}</p><p className="text-xs text-slate-400">{item.subtitle}</p></div>)}{results.vendors.map((item) => <div key={item.title} className="rounded-xl bg-white/5 p-2"><p className="text-white">{item.title}</p><p className="text-xs text-slate-400">{item.subtitle}</p></div>)}{results.documents.map((item) => <div key={item.title} className="rounded-xl bg-white/5 p-2"><p className="text-white">{item.title}</p><p className="text-xs text-slate-400">{item.subtitle}</p></div>)}</div>}
                  </div> : null}
                </label>
                <button type="button" className="rounded-2xl border border-white/10 bg-white/5 p-2.5 text-slate-300 hover:bg-white/10 hover:text-white">
                  <Bell className="h-4 w-4" />
                </button>
                <button type="button" onClick={toggleTheme} className="rounded-2xl border border-white/10 bg-white/5 p-2.5 text-slate-300 hover:bg-white/10 hover:text-white">
                  {theme === 'dark' ? <Moon className="h-4 w-4" /> : <Sun className="h-4 w-4" />}
                </button>
                <div className="flex items-center gap-2 rounded-2xl border border-violet-500/20 bg-violet-500/10 px-3 py-2">
                  <Sparkles className="h-4 w-4 text-violet-300" />
                  <span className="text-sm text-slate-200">{profileLabel}</span>
                </div>
                <button type="button" onClick={() => { logout(); navigate('/login'); }} className="rounded-2xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-300">
                  <UserCircle2 className="mr-2 inline h-4 w-4" />Logout
                </button>
              </div>
            </div>
          </header>

          <main className="p-6 lg:p-8">{children}</main>
        </div>
      </div>
    </div>
  );
}
