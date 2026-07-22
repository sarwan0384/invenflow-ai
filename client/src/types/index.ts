export type NavItem = {
  label: string;
  href: string;
  icon: string;
};

export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  category: string;
  quantityOnHand: number;
  unitPrice: number;
  updatedAt: string;
}

export interface Vendor {
  id: string;
  name: string;
  code: string;
  email: string;
  createdAt: string;
}

export interface InboundDocument {
  id: string;
  fileName: string;
  fileUrl: string;
  status: 'Uploaded' | 'Processing' | 'ReviewRequired' | 'Processed' | 'Failed';
  vendorId?: string;
  vendor?: Vendor;
  confidenceScore: number;
  uploadedAt: string;
}
