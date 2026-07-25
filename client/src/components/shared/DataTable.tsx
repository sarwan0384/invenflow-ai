import { useMemo, useState, type ReactNode } from 'react';
import { ArrowDown, ArrowUp, ChevronDown, Search } from 'lucide-react';
import { utils, writeFile } from 'xlsx';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';

type Column<T> = {
  key: keyof T | string;
  header: string;
  render?: (value: T[keyof T], row: T) => ReactNode;
};

type DataTableProps<T extends Record<string, unknown>> = {
  columns: Column<T>[];
  data?: T[];
  rows?: T[];
  fileName?: string;
  initialPageSize?: number;
};

export function DataTable<T extends Record<string, unknown>>({ columns, data, rows, fileName = 'invenflow-table', initialPageSize = 10 }: DataTableProps<T>) {
  const [query, setQuery] = useState('');
  const [sortKey, setSortKey] = useState<keyof T | string | null>(() => (columns.length ? columns[0].key : null));
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [isExportOpen, setIsExportOpen] = useState(false);

  const sourceRows = useMemo(() => (Array.isArray(data) ? data : Array.isArray(rows) ? rows : []) as T[], [data, rows]);

  const filteredRows = useMemo(() => {
    const lowered = query.trim().toLowerCase();
    if (!lowered) return sourceRows;

    return sourceRows.filter((row) => columns.some((column) => String(row[column.key as keyof T] ?? '').toLowerCase().includes(lowered)));
  }, [columns, query, sourceRows]);

  const sortedRows = useMemo(() => {
    if (!sortKey) return filteredRows;
    const direction = sortDirection === 'asc' ? 1 : -1;
    return [...filteredRows].sort((a, b) => {
      const left = a[sortKey as keyof T];
      const right = b[sortKey as keyof T];
      if (typeof left === 'number' && typeof right === 'number') return (left - right) * direction;
      return String(left).localeCompare(String(right)) * direction;
    });
  }, [filteredRows, sortDirection, sortKey]);

  const pageCount = Math.max(1, Math.ceil(sortedRows.length / pageSize));
  const pagedRows = sortedRows.slice((page - 1) * pageSize, page * pageSize);
  const totalItems = sortedRows.length;
  const startItem = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = totalItems === 0 ? 0 : Math.min(page * pageSize, totalItems);

  const toggleSort = (key: keyof T | string) => {
    if (sortKey === key) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'));
      return;
    }
    setSortKey(key);
    setSortDirection('asc');
  };

  const formatExportValue = (value: unknown) => {
    if (value === null || value === undefined || value === '') return '';
    if (value instanceof Date) return value.toISOString();
    if (typeof value === 'number') return Number.isFinite(value) ? String(value) : '';
    if (typeof value === 'boolean') return value ? 'true' : 'false';
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
  };

  const downloadBlob = (blob: Blob, name: string) => {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = name;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    window.URL.revokeObjectURL(url);
  };

  const exportData = (format: 'csv' | 'excel' | 'pdf') => {
    const exportRows = sortedRows.map((row) => columns.reduce<Record<string, string>>((accumulator, column) => {
      accumulator[column.header] = formatExportValue(row[column.key as keyof T]);
      return accumulator;
    }, {}));

    const headers = columns.map((column) => column.header);

    if (format === 'csv') {
      const csvRows = [headers, ...exportRows.map((row) => headers.map((header) => row[header]).map((value) => {
        const text = String(value ?? '');
        return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
      }))];
      const csvContent = csvRows.map((row) => row.join(',')).join('\n');
      downloadBlob(new Blob([csvContent], { type: 'text/csv;charset=utf-8;' }), `${fileName}.csv`);
      return;
    }

    if (format === 'excel') {
      const sheet = utils.json_to_sheet(exportRows.map((row) => Object.fromEntries(headers.map((header) => [header, row[header]]))));
      const workbook = utils.book_new();
      utils.book_append_sheet(workbook, sheet, 'Sheet1');
      writeFile(workbook, `${fileName}.xlsx`);
      return;
    }

    const doc = new jsPDF();
    const tableData = exportRows.map((row) => headers.map((header) => row[header]));
    autoTable(doc, { head: [headers], body: tableData });
    doc.save(`${fileName}.pdf`);
  };

  return (
    <div className="relative flex max-h-[calc(100vh-14rem)] flex-col overflow-hidden rounded-[24px] border border-white/10 bg-slate-900/70 shadow-lg shadow-black/10 backdrop-blur-xl">
      <div className="flex flex-col gap-3 border-b border-white/10 p-4 md:flex-row md:items-center md:justify-between">
        <label className="flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-400">
          <Search className="h-4 w-4" />
          <input value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} placeholder="Filter rows" className="bg-transparent outline-none" />
        </label>
        <div className="flex items-center gap-3">
          <div className="relative overflow-visible">
            <button type="button" onClick={() => setIsExportOpen((current) => !current)} className="flex items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-300">
              Export
              <ChevronDown className="h-4 w-4" />
            </button>
            {isExportOpen ? (
              <div className="absolute right-0 top-full z-50 mt-2 w-40 rounded-xl border border-white/10 bg-slate-950/95 p-2 shadow-lg shadow-black/20">
                <button type="button" onClick={() => { setIsExportOpen(false); exportData('csv'); }} className="flex w-full items-center rounded-lg px-3 py-2 text-left text-sm text-slate-300 hover:bg-white/5">CSV</button>
                <button type="button" onClick={() => { setIsExportOpen(false); exportData('excel'); }} className="flex w-full items-center rounded-lg px-3 py-2 text-left text-sm text-slate-300 hover:bg-white/5">Excel</button>
                <button type="button" onClick={() => { setIsExportOpen(false); exportData('pdf'); }} className="flex w-full items-center rounded-lg px-3 py-2 text-left text-sm text-slate-300 hover:bg-white/5">PDF</button>
              </div>
            ) : null}
          </div>
        </div>
      </div>
      <div className="flex-1 min-h-0 overflow-y-auto">
        <table className="min-w-full table-fixed divide-y divide-white/10 text-sm">
          <thead className="sticky top-0 z-10 bg-slate-900/95 text-left text-slate-400">
            <tr>
              {columns.map((column) => (
                <th key={String(column.key)} className="cursor-pointer px-4 py-3" onClick={() => toggleSort(column.key)}>
                  <div className="flex items-center gap-2">
                    <span>{column.header}</span>
                    {sortKey === column.key ? (sortDirection === 'asc' ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />) : null}
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {pagedRows.map((row, index) => (
              <tr key={index} className="text-base">
                {columns.map((column) => (
                  <td key={String(column.key)} className="px-4 py-3 text-slate-200">{column.render ? column.render(row[column.key as keyof T], row) : String(row[column.key as keyof T] ?? '')}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex flex-col gap-3 border-t border-white/10 px-4 py-3 text-sm text-slate-400 md:flex-row md:items-center md:justify-between">
        <span>Showing {startItem}–{endItem} of {totalItems}</span>
        <div className="flex flex-col gap-2 md:flex-row md:items-center md:gap-3">
          <div className="flex items-center gap-2">
            <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 disabled:opacity-50" disabled={page === 1}>Prev</button>
            <span>{page}/{pageCount}</span>
            <button type="button" onClick={() => setPage((current) => Math.min(pageCount, current + 1))} className="rounded-xl border border-white/10 bg-white/5 px-3 py-2 disabled:opacity-50" disabled={page === pageCount}>Next</button>
          </div>
          <label className="flex items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-400">
            <span>Rows</span>
            <select value={pageSize} onChange={(event) => { setPageSize(Number(event.target.value)); setPage(1); }} className="bg-transparent outline-none">
              <option value={5}>5</option>
              <option value={10}>10</option>
              <option value={20}>20</option>
            </select>
          </label>
        </div>
      </div>
    </div>
  );
}
