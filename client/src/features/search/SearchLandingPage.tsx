import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Search } from 'lucide-react';
import { SearchHeaderActions } from '../../components/navigation/SearchHeaderActions';

type Category = 'Electronics' | 'Watches';

const quickExamples = ['BAV99', '1N4148W'];

export function SearchLandingPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState<Category>('Electronics');
  const errorMessage = searchParams.get('error');

  const submitSearch = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const normalizedQuery = query.trim() || 'BAV99';
    navigate(`/search?q=${encodeURIComponent(normalizedQuery)}&category=${encodeURIComponent(category)}`);
  };

  const useQuickExample = (example: string) => {
    navigate(`/search?q=${encodeURIComponent(example)}&category=${encodeURIComponent(category)}`);
  };

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_20%_20%,_rgba(254,154,46,0.18),_transparent_35%),radial-gradient(circle_at_80%_15%,_rgba(45,212,191,0.12),_transparent_35%),linear-gradient(160deg,_#05070f_0%,_#0b1220_50%,_#12161f_100%)] px-4 py-10 text-slate-100">
      <header className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 py-2">
        <p className="text-sm font-semibold uppercase tracking-[0.24em] text-slate-300">ProChips</p>
        <SearchHeaderActions />
      </header>

      <div className="mx-auto flex min-h-[80vh] w-full max-w-6xl items-center justify-center">
        <section className="w-full max-w-4xl text-center">
          <p className="mb-4 text-xs font-semibold uppercase tracking-[0.35em] text-orange-300/90">ProChips Marketplace</p>
          <h1 className="text-balance text-4xl font-semibold leading-tight text-white sm:text-5xl lg:text-6xl">
            Universal Part & Product Search
          </h1>
          {errorMessage ? <div className="mx-auto mt-5 max-w-xl rounded-2xl border border-rose-400/35 bg-rose-500/15 px-4 py-3 text-sm text-rose-100">{errorMessage}</div> : null}

          <form onSubmit={submitSearch} className="mx-auto mt-8 w-full max-w-5xl rounded-[999px] border border-white/20 bg-slate-900/60 p-2 shadow-[0_28px_80px_rgba(0,0,0,0.45)] backdrop-blur-2xl">
            <div className="flex flex-col items-stretch gap-2 md:flex-row md:items-center">
              <div className="flex items-center rounded-[999px] border border-white/15 bg-slate-950/70 px-4 py-3 md:w-56 md:py-4">
                <label className="sr-only" htmlFor="search-category">Category</label>
                <select
                  id="search-category"
                  value={category}
                  onChange={(event) => setCategory(event.target.value as Category)}
                  className="w-full bg-transparent text-sm text-slate-200 outline-none"
                >
                  <option value="Electronics" className="bg-slate-900">Electronics</option>
                  <option value="Watches" className="bg-slate-900" disabled>Watches (Coming Soon)</option>
                </select>
              </div>

              <div className="flex min-w-0 flex-1 items-center gap-3 rounded-[999px] border border-white/10 bg-slate-950/65 px-5 py-3 md:py-4">
                <Search className="h-5 w-5 flex-none text-slate-400" />
                <label className="sr-only" htmlFor="search-query">Search query</label>
                <input
                  id="search-query"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Search part numbers, manufacturer SKUs, or product names"
                  className="w-full min-w-0 bg-transparent text-base text-white outline-none placeholder:text-slate-500"
                />
              </div>

              <button
                type="submit"
                className="rounded-[999px] bg-orange-500 px-8 py-3 text-sm font-semibold text-slate-950 transition hover:bg-orange-400 md:py-4"
              >
                Find
              </button>
            </div>
          </form>

          <p className="mt-5 text-sm text-slate-300/80">
            Try searching{' '}
            <button type="button" onClick={() => useQuickExample(quickExamples[0])} className="font-medium text-orange-300 hover:text-orange-200 hover:underline">
              {quickExamples[0]}
            </button>
            {' '}or{' '}
            <button type="button" onClick={() => useQuickExample(quickExamples[1])} className="font-medium text-orange-300 hover:text-orange-200 hover:underline">
              {quickExamples[1]}
            </button>
            .
          </p>
        </section>
      </div>
    </div>
  );
}
