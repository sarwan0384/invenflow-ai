import type { InventoryItem, InboundDocument, Vendor } from '../types';

const API_BASE_URL = 'http://localhost:5206/api';

export function getStoredAuthToken() {
  if (typeof window === 'undefined') return '';

  try {
    const stored = window.localStorage.getItem('invenflow-auth');
    if (!stored) return '';
    const parsed = JSON.parse(stored) as { token?: string };
    return parsed.token ?? '';
  } catch {
    return '';
  }
}

function buildHeaders(init?: RequestInit) {
  const headers = new Headers(init?.headers || {});
  const token = getStoredAuthToken();

  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  return headers;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: buildHeaders(init),
  });

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

export interface SyncExternalUrlResult {
  added: number;
  updated: number;
  sourceType: 'API' | 'Web' | 'AI';
}

export async function syncExternalUrl(payload: { url: string }) {
  return request<SyncExternalUrlResult>('/sync/external-url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
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

export interface SearchResults {
  inventory: Array<{ title: string; subtitle: string }>;
  vendors: Array<{ title: string; subtitle: string }>;
  documents: Array<{ title: string; subtitle: string }>;
}

export async function searchContent(query: string) {
  return request<SearchResults>(`/search?q=${encodeURIComponent(query)}`);
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
