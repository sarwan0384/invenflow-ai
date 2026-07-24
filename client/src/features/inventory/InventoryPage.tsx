import { PackagePlus, Pencil, Share2, Trash2, TriangleAlert } from 'lucide-react';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { DataTable } from '../../components/shared/DataTable';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { createInventoryItem, deleteInventoryItem, getInventory, syncExternalUrl, updateInventoryItem } from '../../services/api';
import type { InventoryItem } from '../../types';
import { cn, formatCurrency, formatDate } from '../../lib/utils';
import { useAuth } from '../../contexts/useAuth';

type InventoryFormState = {
  sku: string;
  name: string;
  category: string;
  quantityOnHand: string;
  unitPrice: string;
};

const initialFormState: InventoryFormState = {
  sku: '',
  name: '',
  category: '',
  quantityOnHand: '0',
  unitPrice: '0',
};

export function InventoryPage() {
  const { user } = useAuth();
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedItem, setSelectedItem] = useState<InventoryItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [formState, setFormState] = useState<InventoryFormState>(initialFormState);
  const [isSyncModalOpen, setIsSyncModalOpen] = useState(false);
  const [syncUrl, setSyncUrl] = useState('');
  const [isSyncing, setIsSyncing] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const loadItems = async () => {
    setIsLoading(true);
    try {
      const data = await getInventory();
      setItems(data);
    } catch {
      setItems([]);
      setFeedback({ type: 'error', message: 'Unable to load inventory right now.' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    let isMounted = true;

    async function init() {
      try {
        const data = await getInventory();
        if (!isMounted) return;
        setItems(data);
      } catch {
        if (!isMounted) return;
        setItems([]);
        setFeedback({ type: 'error', message: 'Unable to load inventory right now.' });
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

  const statusBadge = useMemo(
    () => (item: InventoryItem) => {
      if (item.quantityOnHand <= 0) return { tone: 'danger' as const, label: 'Out of stock' };
      if (item.quantityOnHand < 20) return { tone: 'warning' as const, label: 'Low stock' };
      return { tone: 'success' as const, label: 'Healthy' };
    },
    [],
  );

  const openCreateModal = () => {
    setSelectedItem(null);
    setFormState(initialFormState);
    setIsModalOpen(true);
  };

  const openSyncModal = () => {
    setSyncUrl('');
    setFeedback(null);
    setIsSyncModalOpen(true);
  };

  const openEditModal = (item: InventoryItem) => {
    setSelectedItem(item);
    setFormState({
      sku: item.sku,
      name: item.name,
      category: item.category,
      quantityOnHand: String(item.quantityOnHand),
      unitPrice: String(item.unitPrice),
    });
    setIsModalOpen(true);
  };

  const openDeleteModal = (item: InventoryItem) => {
    setSelectedItem(item);
    setIsDeleteModalOpen(true);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setFeedback(null);

    try {
      if (selectedItem) {
        await updateInventoryItem(selectedItem.id, {
          sku: formState.sku,
          name: formState.name,
          category: formState.category,
          quantityOnHand: Number(formState.quantityOnHand),
          unitPrice: Number(formState.unitPrice),
        });
        setFeedback({ type: 'success', message: 'Inventory item updated successfully.' });
      } else {
        await createInventoryItem({
          sku: formState.sku,
          name: formState.name,
          category: formState.category,
          quantityOnHand: Number(formState.quantityOnHand),
          unitPrice: Number(formState.unitPrice),
        });
        setFeedback({ type: 'success', message: 'Inventory item created successfully.' });
      }

      setIsModalOpen(false);
      setSelectedItem(null);
      setFormState(initialFormState);
      await loadItems();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'Unable to save inventory item.' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!selectedItem) return;

    setIsDeleting(true);
    setFeedback(null);

    try {
      await deleteInventoryItem(selectedItem.id);
      setIsDeleteModalOpen(false);
      setSelectedItem(null);
      setFeedback({ type: 'success', message: 'Inventory item deleted successfully.' });
      await loadItems();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'Unable to delete inventory item.' });
    } finally {
      setIsDeleting(false);
    }
  };

  const handleExternalSync = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSyncing(true);
    setFeedback(null);

    try {
      const result = await syncExternalUrl({ url: syncUrl.trim() });
      setIsSyncModalOpen(false);
      setFeedback({
        type: 'success',
        message: `External sync complete. ${result.updated} updated, ${result.added} added from ${result.sourceType}.`,
      });
      await loadItems();
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : 'Unable to sync external inventory.',
      });
    } finally {
      setIsSyncing(false);
    }
  };

  return (
    <AppLayout active="/inventory">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Inventory orchestration</p>
            <h2 className="text-2xl font-semibold text-white">Stock Overview</h2>
          </div>
          <div className="flex flex-wrap gap-3">
            <Button onClick={openSyncModal} variant="secondary">
              Sync External Link
            </Button>
            <Button variant="secondary">Export CSV</Button>
            <Button variant="secondary">Export Excel</Button>
            <Button variant="secondary">Export PDF</Button>
            {(user?.role === 'Admin' || user?.role === 'Manager') ? <Button onClick={openCreateModal}>
              <PackagePlus className="mr-2 h-4 w-4" />
              Add Stock
            </Button> : null}
          </div>
        </div>

        {feedback ? (
          <div className={cn('rounded-2xl border px-4 py-3 text-sm', feedback.type === 'success' ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300' : 'border-rose-500/30 bg-rose-500/10 text-rose-300')}>
            {feedback.message}
          </div>
        ) : null}

        {isLoading ? (
          <div className="p-8 text-center text-sm text-slate-400">Loading inventory…</div>
        ) : items.length === 0 ? (
          <div className="p-8 text-center text-sm text-slate-400">No inventory records yet.</div>
        ) : (
          <DataTable
            columns={[
              { key: 'sku', header: 'SKU' },
              { key: 'name', header: 'Name' },
              { key: 'category', header: 'Category' },
              { key: 'quantityOnHand', header: 'On Hand' },
              { key: 'unitPrice', header: 'Unit Price', render: (value) => formatCurrency(Number(value)) },
              { key: 'updatedAt', header: 'Updated', render: (value) => formatDate(String(value)) },
              { key: 'status', header: 'Status', render: (_value, row) => { const badge = statusBadge(row as unknown as InventoryItem); return <Badge tone={badge.tone}>{badge.label}</Badge>; } },
              {
                key: 'actions',
                header: 'Actions',
                render: (_value, row) => (
                  <div className="flex gap-2">
                    {(user?.role === 'Admin' || user?.role === 'Manager') ? (
                      <button type="button" onClick={() => openEditModal(row as unknown as InventoryItem)} className="rounded-lg border border-white/10 bg-white/5 p-2 text-slate-200">
                        <Pencil className="h-4 w-4" />
                      </button>
                    ) : null}
                    <button type="button" className="rounded-lg border border-white/10 bg-white/5 p-2 text-slate-200">
                      <Share2 className="h-4 w-4" />
                    </button>
                    {user?.role === 'Admin' ? (
                      <button type="button" onClick={() => openDeleteModal(row as unknown as InventoryItem)} className="rounded-lg border border-rose-500/20 bg-rose-500/10 p-2 text-rose-300">
                        <Trash2 className="h-4 w-4" />
                      </button>
                    ) : null}
                  </div>
                ),
              },
            ]}
            rows={items as unknown as Array<Record<string, unknown>>}
          />
        )}
      </div>

      <Modal
        open={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setSelectedItem(null);
          setFormState(initialFormState);
        }}
        title={selectedItem ? 'Edit Inventory Item' : 'Add Inventory Stock'}
        description={selectedItem ? 'Update the selected inventory item details.' : 'Create a new inventory record and make it available to downstream workflows.'}
        size="md"
      >
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <label className="space-y-2 text-sm text-slate-300">
              <span>SKU</span>
              <input
                required
                readOnly={Boolean(selectedItem)}
                value={formState.sku}
                onChange={(event) => setFormState((prev) => ({ ...prev, sku: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Name</span>
              <input
                required
                value={formState.name}
                onChange={(event) => setFormState((prev) => ({ ...prev, name: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Category</span>
              <input
                required
                value={formState.category}
                onChange={(event) => setFormState((prev) => ({ ...prev, category: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Quantity On Hand</span>
              <input
                required
                type="number"
                min="0"
                value={formState.quantityOnHand}
                onChange={(event) => setFormState((prev) => ({ ...prev, quantityOnHand: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300 md:col-span-2">
              <span>Unit Price</span>
              <input
                required
                type="number"
                min="0"
                step="0.01"
                value={formState.unitPrice}
                onChange={(event) => setFormState((prev) => ({ ...prev, unitPrice: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
          </div>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => { setIsModalOpen(false); setSelectedItem(null); setFormState(initialFormState); }}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {selectedItem ? 'Save Changes' : 'Save Stock'}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        open={isSyncModalOpen}
        onClose={() => setIsSyncModalOpen(false)}
        title="Sync External Link"
        description="Enter a public API or product page URL to synchronize external inventory data."
        size="md"
      >
        <form className="space-y-4" onSubmit={handleExternalSync}>
          <label className="space-y-2 text-sm text-slate-300">
            <span>URL</span>
            <input
              required
              type="url"
              value={syncUrl}
              onChange={(event) => setSyncUrl(event.target.value)}
              placeholder="https://supplier.example.com/products"
              className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
            />
          </label>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsSyncModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={isSyncing}>
              Sync Now
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        open={isDeleteModalOpen}
        onClose={() => {
          setIsDeleteModalOpen(false);
          setSelectedItem(null);
        }}
        title="Delete Inventory Item?"
        description="This action cannot be undone."
        size="sm"
      >
        <div className="space-y-5">
          <div className="flex items-start gap-3 rounded-2xl border border-rose-500/20 bg-rose-500/10 p-4 text-sm text-rose-200">
            <TriangleAlert className="mt-0.5 h-5 w-5 shrink-0" />
            <p>
              Are you sure you want to delete <span className="font-semibold text-white">{selectedItem?.name}</span> (SKU: <span className="font-semibold text-white">{selectedItem?.sku}</span>)?
            </p>
          </div>
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => { setIsDeleteModalOpen(false); setSelectedItem(null); }}>
              Cancel
            </Button>
            <Button type="button" variant="secondary" loading={isDeleting} onClick={handleDelete} className="border-rose-500/20 bg-rose-500/10 text-rose-200 hover:bg-rose-500/20 hover:text-white">
              Delete
            </Button>
          </div>
        </div>
      </Modal>
    </AppLayout>
  );
}
