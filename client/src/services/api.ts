const API_BASE_URL = 'http://localhost:5000/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export async function getInventory() {
  return request<any[]>('/inventory');
}

export async function getVendors() {
  return request<any[]>('/vendors');
}

export async function getDocuments() {
  return request<any[]>('/inbounddocuments');
}
