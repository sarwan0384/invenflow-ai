import { createContext } from 'react';
import type { UniversalPriceTier } from '../services/api';

export type CartItem = {
  id: string;
  providerName: string;
  supplierRealId: string;
  vendorCartId: string;
  mpn: string;
  manufacturer: string;
  quantity: number;
  minQty: number;
  orderMultiple: number;
  unitPrice: number;
  currency: string;
  packagingContainer?: string;
  priceBreaks: UniversalPriceTier[];
};

export type AddCartItemInput = Omit<CartItem, 'id' | 'unitPrice'> & {
  unitPrice?: number;
};

export type CartContextValue = {
  items: CartItem[];
  totalQuantity: number;
  totalPrice: number;
  addToCart: (item: AddCartItemInput) => void;
  updateCartItemQuantity: (mpn: string, quantity: number) => void;
  removeFromCart: (mpn: string) => void;
  addItem: (item: AddCartItemInput) => void;
  updateQuantity: (mpn: string, quantity: number) => void;
  removeItem: (mpn: string) => void;
  clearCart: () => void;
};

export const CartContext = createContext<CartContextValue | undefined>(undefined);
