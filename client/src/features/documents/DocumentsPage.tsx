import { Share2, Sparkles, Trash2, UploadCloud } from 'lucide-react';
import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { DataTable } from '../../components/shared/DataTable';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { deleteDocument, getDocuments, getVendors, processDocument, uploadDocument } from '../../services/api';
import type { InboundDocument, Vendor } from '../../types';
import { cn, formatDate } from '../../lib/utils';
import { useAuth } from '../../contexts/useAuth';

const getStatusMeta = (status: number | string) => {
  const normalized = typeof status === 'number' ? status : String(status).toLowerCase();

  switch (normalized) {
    case 0:
    case 'pending':
      return { label: 'Pending', tone: 'warning' as const, canProcess: true };
    case 1:
    case 'processing':
      return { label: 'Processing', tone: 'info' as const, canProcess: false };
    case 2:
    case 'processed':
      return { label: 'Processed', tone: 'success' as const, canProcess: false };
    case 3:
    case 'failed':
      return { label: 'Failed', tone: 'danger' as const, canProcess: true };
    default:
      return { label: 'Unknown', tone: 'default' as const, canProcess: false };
  }
};

export function DocumentsPage() {
  const { user } = useAuth();
  const [documents, setDocuments] = useState<InboundDocument[]>([]);
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isProcessing, setIsProcessing] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [vendorId, setVendorId] = useState('');
  const [documentToDelete, setDocumentToDelete] = useState<InboundDocument | null>(null);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const loadDocuments = async () => {
    setIsLoading(true);
    try {
      const data = await getDocuments();
      setDocuments(data);
    } catch {
      setDocuments([]);
      setFeedback({ type: 'error', message: 'Unable to load document queue.' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    let isMounted = true;

    async function init() {
      try {
        const [documentsData, vendorsData] = await Promise.all([getDocuments(), getVendors()]);
        if (!isMounted) return;
        setDocuments(documentsData);
        setVendors(vendorsData);
      } catch {
        if (!isMounted) return;
        setDocuments([]);
        setVendors([]);
        setFeedback({ type: 'error', message: 'Unable to load document queue.' });
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    void init();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    setSelectedFile(file);
  };

  const handleUpload = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedFile) {
      setFeedback({ type: 'error', message: 'Please choose a file to upload.' });
      return;
    }

    setIsSubmitting(true);
    setFeedback(null);

    try {
      const formData = new FormData();
      formData.append('file', selectedFile);
      if (vendorId) {
        formData.append('vendorId', vendorId);
      }
      await uploadDocument(formData);
      setIsModalOpen(false);
      setSelectedFile(null);
      setVendorId('');
      setFeedback({ type: 'success', message: 'Document uploaded and processed successfully.' });
      await loadDocuments();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Upload failed.';
      setFeedback({
        type: 'error',
        message: /already exists/i.test(message) ? 'Duplicate document detected. This file has already been uploaded.' : message,
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!documentToDelete) {
      return;
    }

    setIsDeleting(true);
    setFeedback(null);

    try {
      await deleteDocument(documentToDelete.id);
      setFeedback({ type: 'success', message: 'Document deleted successfully.' });
      setIsDeleteModalOpen(false);
      setDocumentToDelete(null);
      await loadDocuments();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'Delete failed.' });
    } finally {
      setIsDeleting(false);
    }
  };

  const handleProcess = async (document: InboundDocument) => {
    setIsProcessing(document.id);
    setFeedback(null);

    try {
      await processDocument(document.id);
      setFeedback({ type: 'success', message: 'Invoice processed! Inventory stock updated.' });
      await loadDocuments();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'AI processing failed.' });
    } finally {
      setIsProcessing(null);
    }
  };

  return (
    <AppLayout active="/documents">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Inbound document automation</p>
            <h2 className="text-2xl font-semibold text-white">Document Review Queue</h2>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary">Export CSV</Button>
            <Button variant="secondary">Export Excel</Button>
            <Button variant="secondary">Export PDF</Button>
            {(user?.role === 'Admin' || user?.role === 'Manager') ? <Button onClick={() => setIsModalOpen(true)}>
              <UploadCloud className="mr-2 h-4 w-4" />
              Upload Document
            </Button> : null}
          </div>
        </div>

        {feedback ? (
          <div className={cn('rounded-2xl border px-4 py-3 text-sm', feedback.type === 'success' ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300' : 'border-rose-500/30 bg-rose-500/10 text-rose-300')}>
            {feedback.message}
          </div>
        ) : null}

        {isLoading ? (
          <div className="p-8 text-center text-sm text-slate-400">Loading document queue…</div>
        ) : documents.length === 0 ? (
          <div className="p-8 text-center text-sm text-slate-400">No documents queued yet.</div>
        ) : (
          <DataTable
            columns={[
              { key: 'fileName', header: 'File' },
              { key: 'status', header: 'Status', render: (value) => { const statusMeta = getStatusMeta(value as string | number); return <Badge tone={statusMeta.tone}>{statusMeta.label}</Badge>; } },
              { key: 'confidenceScore', header: 'Confidence', render: (value) => `${(Number(value) * 100).toFixed(0)}%` },
              { key: 'uploadedAt', header: 'Uploaded', render: (value) => formatDate(String(value)) },
              {
                key: 'actions',
                header: 'Actions',
                render: (_value, row) => (
                  <div className="flex gap-2">
                    {(user?.role === 'Admin' || user?.role === 'Manager') ? (
                      <button type="button" onClick={() => void handleProcess(row as unknown as InboundDocument)} className="rounded-lg border border-white/10 bg-white/5 p-2 text-slate-200" disabled={isProcessing === (row as unknown as InboundDocument).id}>
                        <Sparkles className="h-4 w-4" />
                      </button>
                    ) : null}
                    <button type="button" className="rounded-lg border border-white/10 bg-white/5 p-2 text-slate-200">
                      <Share2 className="h-4 w-4" />
                    </button>
                    {user?.role === 'Admin' ? (
                      <button type="button" onClick={() => { setDocumentToDelete(row as unknown as InboundDocument); setIsDeleteModalOpen(true); }} className="rounded-lg border border-rose-500/20 bg-rose-500/10 p-2 text-rose-300">
                        <Trash2 className="h-4 w-4" />
                      </button>
                    ) : null}
                  </div>
                ),
              },
            ]}
            rows={documents as unknown as Array<Record<string, unknown>>}
          />
        )}
      </div>

      <Modal
        open={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Upload Invoice"
        description="Drop a PDF or image invoice and link it to a vendor for AI review."
        size="md"
      >
        <form className="space-y-4" onSubmit={handleUpload}>
          <label className="flex cursor-pointer flex-col items-center justify-center rounded-2xl border border-dashed border-white/10 bg-slate-950/60 px-6 py-10 text-center text-sm text-slate-400">
            <UploadCloud className="mb-3 h-8 w-8 text-violet-400" />
            <span className="font-medium text-slate-200">{selectedFile ? selectedFile.name : 'Click to choose a file'}</span>
            <span className="mt-1">PDF, JPG, or PNG files are supported.</span>
            <input type="file" className="sr-only" accept="application/pdf,image/png,image/jpeg,image/jpg" onChange={handleFileChange} />
          </label>

          <label className="space-y-2 text-sm text-slate-300">
            <span>Vendor (optional)</span>
            <select
              value={vendorId}
              onChange={(event) => setVendorId(event.target.value)}
              className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
            >
              <option value="">Unassigned</option>
              {vendors.map((vendor) => (
                <option key={vendor.id} value={vendor.id}>{vendor.name}</option>
              ))}
            </select>
          </label>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              Upload
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        open={isDeleteModalOpen}
        onClose={() => {
          setIsDeleteModalOpen(false);
          setDocumentToDelete(null);
        }}
        title="Delete Document?"
        description={`Are you sure you want to delete ${documentToDelete?.fileName ?? 'this document'}? This action cannot be undone.`}
        size="md"
      >
        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              setIsDeleteModalOpen(false);
              setDocumentToDelete(null);
            }}
          >
            Cancel
          </Button>
          <Button type="button" loading={isDeleting} className="bg-rose-600 hover:bg-rose-500" onClick={() => void handleDelete()}>
            Delete
          </Button>
        </div>
      </Modal>
    </AppLayout>
  );
}
