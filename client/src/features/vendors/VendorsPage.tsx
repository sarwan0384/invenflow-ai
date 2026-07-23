import { Edit3, Trash2, UserPlus2 } from 'lucide-react';
import { useEffect, useState, type FormEvent } from 'react';
import { AppLayout } from '../../components/shared/AppLayout';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { createVendor, deleteVendor, getVendors, updateVendor } from '../../services/api';
import type { Vendor } from '../../types';
import { cn, formatDate } from '../../lib/utils';

type VendorFormState = {
  name: string;
  code: string;
  email: string;
  contactPerson: string;
  phoneNumber: string;
  address: string;
};

const initialFormState: VendorFormState = {
  name: '',
  code: '',
  email: '',
  contactPerson: '',
  phoneNumber: '',
  address: '',
};

export function VendorsPage() {
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingVendor, setEditingVendor] = useState<Vendor | null>(null);
  const [formState, setFormState] = useState<VendorFormState>(initialFormState);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const loadVendors = async () => {
    setIsLoading(true);
    try {
      const data = await getVendors();
      setVendors(data as Vendor[]);
    } catch {
      setVendors([]);
      setFeedback({ type: 'error', message: 'Unable to load vendors right now.' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadVendors();
  }, []);

  const openCreateModal = () => {
    setEditingVendor(null);
    setFormState(initialFormState);
    setIsModalOpen(true);
  };

  const openEditModal = (vendor: Vendor) => {
    setEditingVendor(vendor);
    setFormState({
      name: vendor.name,
      code: vendor.code,
      email: vendor.email,
      contactPerson: vendor.contactPerson ?? '',
      phoneNumber: vendor.phoneNumber ?? '',
      address: vendor.address ?? '',
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setFeedback(null);

    try {
      if (editingVendor) {
        await updateVendor(editingVendor.id, {
          name: formState.name,
          code: formState.code,
          email: formState.email,
          contactPerson: formState.contactPerson,
          phoneNumber: formState.phoneNumber,
          address: formState.address,
        });
      } else {
        await createVendor({
          name: formState.name,
          code: formState.code,
          email: formState.email,
          contactPerson: formState.contactPerson,
          phoneNumber: formState.phoneNumber,
          address: formState.address,
        });
      }

      setIsModalOpen(false);
      setFormState(initialFormState);
      setFeedback({ type: 'success', message: editingVendor ? 'Vendor updated successfully.' : 'Vendor added successfully.' });
      await loadVendors();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'Unable to save vendor.' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (vendor: Vendor) => {
    if (!window.confirm(`Delete ${vendor.name}?`)) return;
    try {
      await deleteVendor(vendor.id);
      setFeedback({ type: 'success', message: 'Vendor deleted successfully.' });
      await loadVendors();
    } catch (error) {
      setFeedback({ type: 'error', message: error instanceof Error ? error.message : 'Unable to delete vendor.' });
    }
  };

  return (
    <AppLayout active="/vendors">
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-sm text-slate-400">Supplier network</p>
            <h2 className="text-2xl font-semibold text-white">Vendor Intelligence</h2>
          </div>
          <Button onClick={openCreateModal}>
            <UserPlus2 className="mr-2 h-4 w-4" />
            Add Vendor
          </Button>
        </div>

        {feedback ? (
          <div className={cn('rounded-2xl border px-4 py-3 text-sm', feedback.type === 'success' ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300' : 'border-rose-500/30 bg-rose-500/10 text-rose-300')}>
            {feedback.message}
          </div>
        ) : null}

        {isLoading ? (
          <div className="rounded-[24px] border border-white/10 bg-slate-900/70 p-8 text-center text-sm text-slate-400">Loading vendors…</div>
        ) : vendors.length === 0 ? (
          <div className="rounded-[24px] border border-dashed border-white/10 bg-slate-900/60 p-10 text-center text-sm text-slate-400">
            No vendors yet. Add your first supplier to start the pipeline.
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {vendors.map((vendor) => (
              <article key={vendor.id} className="rounded-[24px] border border-white/10 bg-slate-900/70 p-5 shadow-lg shadow-black/10 backdrop-blur-xl">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-lg font-semibold text-white">{vendor.name}</p>
                    <p className="text-sm text-slate-400">{vendor.code || 'No code provided'}</p>
                  </div>
                  <Badge tone="info">Active</Badge>
                </div>
                <div className="mt-5 space-y-2 text-sm text-slate-400">
                  <p>Email: {vendor.email}</p>
                  <p>Phone: {vendor.phoneNumber || '—'}</p>
                  <p>Onboarded: {formatDate(vendor.createdAt)}</p>
                </div>
                <div className="mt-5 flex gap-2">
                  <Button type="button" variant="secondary" onClick={() => openEditModal(vendor)}>
                    <Edit3 className="mr-2 h-4 w-4" />
                    Edit
                  </Button>
                  <Button type="button" variant="ghost" onClick={() => void handleDelete(vendor)}>
                    <Trash2 className="mr-2 h-4 w-4" />
                    Delete
                  </Button>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      <Modal
        open={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingVendor ? 'Edit Vendor' : 'Add Vendor'}
        description="Capture supplier contact details and keep the procurement workflow current."
        size="lg"
      >
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <label className="space-y-2 text-sm text-slate-300">
              <span>Vendor Name</span>
              <input
                required
                value={formState.name}
                onChange={(event) => setFormState((prev) => ({ ...prev, name: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Vendor Code</span>
              <input
                required
                value={formState.code}
                onChange={(event) => setFormState((prev) => ({ ...prev, code: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Contact Person</span>
              <input
                value={formState.contactPerson}
                onChange={(event) => setFormState((prev) => ({ ...prev, contactPerson: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Email</span>
              <input
                type="email"
                required
                value={formState.email}
                onChange={(event) => setFormState((prev) => ({ ...prev, email: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300">
              <span>Phone Number</span>
              <input
                value={formState.phoneNumber}
                onChange={(event) => setFormState((prev) => ({ ...prev, phoneNumber: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
            <label className="space-y-2 text-sm text-slate-300 md:col-span-2">
              <span>Address</span>
              <textarea
                rows={3}
                value={formState.address}
                onChange={(event) => setFormState((prev) => ({ ...prev, address: event.target.value }))}
                className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-3 py-2.5 text-white outline-none ring-0"
              />
            </label>
          </div>

          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {editingVendor ? 'Save Changes' : 'Add Vendor'}
            </Button>
          </div>
        </form>
      </Modal>
    </AppLayout>
  );
}
