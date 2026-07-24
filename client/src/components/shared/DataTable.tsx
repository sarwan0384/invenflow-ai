import { useMemo, useState } from 'react';
import { ChevronDown, ChevronUp, Search, SlidersHorizontal } from 'lucide-react';

type Column<T> = {
  key: keyof T;
  header: string;
  render?: (value: T[keyof T], row: T) => React.ReactNode;
};

type DataTableProps<T extends Record<string, unknown>> = {
  columns: Column<T>[];
  rows: T[];
  pageSize?: number;
};

export function DataTable<T extends Record<string, unknown>>({ columns, rows, pageSize = 8 }: DataTableProps<T>) {
  const [query, setQuery] = useState('');
  const [sortKey, setSortKey] = useState<keyof T | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [page, setPage] = useState(1);
  const [density, setDensity] = useState<'comfortable' | 'compact'>('comfortable');

  const filteredRows = useMemo(() => {
    const lowered = query.toLowerCase();
    return rows.filter((row) => columns.some((column) => String(row[column.key] ?? '').toLowerCase().includes(lowered)));
  }, [columns, query, rows]);

  const sortedRows = useMemo(() => {
    if (!sortKey) return filteredRows;
    const direction = sortDirection === 'asc' ? 1 : -1;
    return [...filteredRows].sort((a, b) => {
      const left = a[sortKey];
      const right = b[sortKey];
      if (typeof left === 'number' && typeof right === 'number') return (left - right) * direction;
      return String(left).localeCompare(String(right)) * direction;
    });
  }, [filteredRows, sortDirection, sortKey]);

  const pageCount = Math.max(1, Math.ceil(sortedRows.length / pageSize));
  const pagedRows = sortedRows.slice((page - 1) * pageSize, page * pageSize);

  const toggleSort = (key: keyof T) => {
    if (sortKey === key) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
      return;
    }
    setSortKey(key);
    setSortDirection('asc');
  };

  return (
    <div className="overflow-hidden rounded-[24px] border border-white/10 bg-slate-900/70 shadow-lg shadow-black/10 backdrop-blur-xl">
      <div className="flex flex-col gap-3 border-b border-white/10 p-4 md:flex-row md:items-center md:justify-between">
        <label className="flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-400">
          <Search className="h-4 w-4" />
          <input value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} placeholder="Filter columns" className="bg-transparent outline-none" />
        </label>
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => setDensity((current) => (current === 'comfortable' ? 'compact' : 'comfortable'))} className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-300">
            <SlidersHorizontal className="mr-2 inline h-4 w-4" />
            {density === 'comfortable' ? 'Comfortable' : 'Compact'}
          </button>
        </div>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-white/10 text-sm">
          <thead className="bg-white/5 text-left text-slate-400">
            <tr>
              {columns.map((column) => (
                <th key={String(column.key)} className="cursor-pointer px-4 py-3" onClick={() => toggleSort(column.key)}>
                  <div className="flex items-center gap-2">
                    <span>{column.header}</span>
                    {sortKey === column.key ? (sortDirection === 'asc' ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />) : null}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {pagedRows.map((row, index) => (
              <tr key={index} className={density === 'compact' ? 'text-sm' : 'text-base'}>
                {columns.map((column) => (
                  <td key={String(column.key)} className="px-4 py-3 text-slate-200">{column.render ? column.render(row[column.key], row) : String(row[column.key] ?? '')}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex items-center justify-between border-t border-white/10 px-4 py-3 text-sm text-slate-400">
        <span>Showing {pagedRows.length} of {sortedRows.length}</span>
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} className="rounded-xl border border-white/10 bg-white/5 px-3 py-2" disabled={page === 1}>Prev</button>
          <span>{page}/{pageCount}</span>
          <button type="button" onClick={() => setPage((current) => Math.min(pageCount, current + 1))} className="rounded-xl border border-white/10 bg-white/5 px-3 py-2" disabled={page === pageCount}>Next</button>
        </div>
      </div>
    </div>
  );
}
