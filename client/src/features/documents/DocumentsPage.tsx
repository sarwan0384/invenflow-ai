import { UploadCloud } from 'lucide-react';
import { useEffect, useState } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { getDocuments } from '../../services/api';
import type { InboundDocument } from '../../types';
import { formatDate } from '../../lib/utils';

const toneMap: Record<string, 'default' | 'success' | 'warning' | 'danger' | 'info'> = {
  Uploaded: 'default',
  Processing: 'warning',
  ReviewRequired: 'danger',
  Processed: 'success',
  Failed: 'danger',
};

export function DocumentsPage() {
  const [documents, setDocuments] = useState<InboundDocument[]>([]);

  useEffect(() => {
    getDocuments().then((data) => setDocuments(data as InboundDocument[])).catch(() => setDocuments([]));
  }, []);

  return (
    <AppLayout active="/documents">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Inbound document automation</p>
            <h2 className="text-2xl font-semibold text-white">Document Review Queue</h2>
          </div>
          <Button><UploadCloud className="mr-2 h-4 w-4" />Upload Document</Button>
        </div>

        <div className="space-y-3">
          {documents.map((document) => (
            <article key={document.id} className="flex flex-col gap-4 rounded-[24px] border border-white/10 bg-slate-900/70 p-5 shadow-lg shadow-black/10 backdrop-blur-xl md:flex-row md:items-center md:justify-between">
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <h3 className="text-lg font-semibold text-white">{document.fileName}</h3>
                  <Badge tone={toneMap[document.status] ?? 'default'}>{document.status}</Badge>
                </div>
                <p className="mt-2 text-sm text-slate-400">Vendor: {document.vendor?.name ?? 'Unassigned'} • Confidence: {(document.confidenceScore * 100).toFixed(0)}%</p>
              </div>
              <div className="text-sm text-slate-400">
                <p>{formatDate(document.uploadedAt)}</p>
                <p className="mt-1">{document.fileUrl}</p>
              </div>
            </article>
          ))}
        </div>
      </div>
    </AppLayout>
  );
}
