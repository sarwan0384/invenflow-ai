import { useEffect, useState, type FormEvent } from 'react';

type ExternalLink = {
  id: string;
  title: string;
  url: string;
  lastSynced: string | null;
};

type ExternalLinkManagerProps = {
  onSync?: (url: string) => Promise<unknown>;
};

const formatSynced = (timestamp: string | null) => {
  if (!timestamp) return 'Never';
  return new Date(timestamp).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const buildId = () => {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `link-${Date.now()}`;
};

export default function ExternalLinkManager({ onSync }: ExternalLinkManagerProps) {
  const [title, setTitle] = useState('');
  const [url, setUrl] = useState('');
  const [links, setLinks] = useState<ExternalLink[]>(() => {
    if (typeof window === 'undefined') return [];
    try {
      return JSON.parse(window.localStorage.getItem('externalLinks') || '[]');
    } catch {
      return [];
    }
  });
  const [loading, setLoading] = useState<Record<string, boolean>>({});

  useEffect(() => {
    window.localStorage.setItem('externalLinks', JSON.stringify(links));
  }, [links]);

  const handleAdd = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedTitle = title.trim();
    const trimmedUrl = url.trim();
    if (!trimmedTitle || !trimmedUrl) return;
    const normalizedUrl = trimmedUrl.match(/^https?:\/\//i)
      ? trimmedUrl
      : `https://${trimmedUrl}`;

    setLinks((current) => [
      {
        id: buildId(),
        title: trimmedTitle,
        url: normalizedUrl,
        lastSynced: null,
      },
      ...current,
    ]);
    setTitle('');
    setUrl('');
  };

  const handleSync = async (item: ExternalLink) => {
    if (!onSync) return;
    setLoading((current) => ({ ...current, [item.id]: true }));
    try {
      await onSync(item.url);
      setLinks((current) =>
        current.map((link) =>
          link.id === item.id ? { ...link, lastSynced: new Date().toISOString() } : link
        )
      );
    } finally {
      setLoading((current) => ({ ...current, [item.id]: false }));
    }
  };

  return (
    <section className="space-y-6 rounded-3xl border border-slate-700 bg-slate-950/90 p-6 shadow-xl shadow-black/20">
      <div className="space-y-3">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-sm uppercase tracking-[0.24em] text-slate-400">External Link Manager</p>
            <h2 className="text-2xl font-semibold text-white">Save and sync URLs</h2>
          </div>
        </div>

        <form className="grid gap-3 sm:grid-cols-[1.5fr_1fr_auto]" onSubmit={handleAdd}>
          <label className="block">
            <span className="text-sm text-slate-300">Link Title</span>
            <input
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              className="mt-2 h-12 w-full rounded-2xl border border-slate-700 bg-slate-900 px-4 text-slate-100 outline-none transition focus:border-sky-500"
              placeholder="e.g. Product docs"
            />
          </label>

          <label className="block">
            <span className="text-sm text-slate-300">URL</span>
            <input
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              className="mt-2 h-12 w-full rounded-2xl border border-slate-700 bg-slate-900 px-4 text-slate-100 outline-none transition focus:border-sky-500"
              placeholder="https://example.com"
            />
          </label>

          <button
            type="submit"
            className="mt-6 h-12 rounded-2xl bg-sky-500 px-6 text-sm font-semibold text-white transition hover:bg-sky-400 sm:mt-2"
          >
            Save Link
          </button>
        </form>
      </div>

      <div className="rounded-3xl border border-slate-800 bg-slate-900/80 p-4">
        <div className="mb-4 flex items-center justify-between text-sm text-slate-500">
          <span>{links.length} saved link{links.length === 1 ? '' : 's'}</span>
          <span>Last updated: {links[0]?.lastSynced ? formatSynced(links[0].lastSynced) : 'Never'}</span>
        </div>

        <div className="space-y-3">
          {links.length === 0 ? (
            <div className="rounded-3xl border border-dashed border-slate-700 bg-slate-950/80 px-5 py-8 text-center text-slate-500">
              Add a link to see it appear here.
            </div>
          ) : (
            links.map((item) => (
              <div
                key={item.id}
                className="grid gap-4 rounded-3xl border border-slate-800 bg-slate-950/90 p-4 sm:grid-cols-[1.5fr_1fr_auto] sm:items-center"
              >
                <div className="space-y-1">
                  <p className="font-semibold text-white">{item.title}</p>
                  <a
                    href={item.url}
                    target="_blank"
                    rel="noreferrer"
                    className="text-sm text-sky-300 transition hover:text-sky-200"
                  >
                    {item.url}
                  </a>
                </div>

                <div className="text-sm text-slate-400">
                  <p className="font-medium text-slate-200">Last Synced</p>
                  <p>{formatSynced(item.lastSynced)}</p>
                </div>

                <button
                  type="button"
                  onClick={() => handleSync(item)}
                  disabled={loading[item.id]}
                  className="inline-flex h-12 items-center justify-center rounded-2xl bg-slate-800 px-4 text-sm font-semibold text-slate-100 transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {loading[item.id] ? (
                    <span className="inline-flex h-5 w-5 animate-spin rounded-full border-2 border-slate-200 border-t-transparent" />
                  ) : (
                    'Sync Now'
                  )}
                </button>
              </div>
            ))
          )}
        </div>
      </div>
    </section>
  );
}
