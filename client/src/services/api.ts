import type { InventoryItem, InboundDocument, Vendor } from '../types';

const API_BASE_URL = 'http://localhost:5206/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, init);

  if (!response.ok) {
    const errorText = await response.text();
    let message = `Request failed: ${response.status}`;
    try {
      const parsed = JSON.parse(errorText);
      message = parsed?.message || parsed?.error || message;
    } catch {
      if (errorText) {
        message = errorText;
      }
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return response.json() as Promise<T>;
  }

  return response.text() as unknown as Promise<T>;
}

export async function getInventory() {
  return request<InventoryItem[]>('/inventoryitems');
}

export async function createInventoryItem(payload: Record<string, unknown>) {
  return request<InventoryItem>('/inventoryitems', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

export async function updateInventoryItem(id: string, payload: Record<string, unknown>) {
  return request<InventoryItem>(`/inventoryitems/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

export async function deleteInventoryItem(id: string) {
  return request<void>(`/inventoryitems/${id}`, {
    method: 'DELETE',
  });
}

export async function getVendors() {
  return request<Vendor[]>('/vendors');
}

export async function createVendor(payload: Record<string, unknown>) {
  return request<Vendor>('/vendors', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

export async function updateVendor(id: string, payload: Record<string, unknown>) {
  return request<Vendor>(`/vendors/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
}

export async function deleteVendor(id: string) {
  return request<void>(`/vendors/${id}`, {
    method: 'DELETE',
  });
}

export async function getDocuments() {
  return request<InboundDocument[]>('/inbounddocuments');
}

export async function uploadDocument(formData: FormData) {
  return request<InboundDocument>('/inbounddocuments/upload', {
    method: 'POST',
    body: formData,
  });
}

export async function processDocument(id: string) {
  return request<InboundDocument>(`/inbounddocuments/${id}/process-ai`, {
    method: 'POST',
  });
}

export async function deleteDocument(id: string) {
  return request<void>(`/inbounddocuments/${id}`, {
    method: 'DELETE',
  });
}
