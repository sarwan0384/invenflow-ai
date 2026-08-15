import { useContext } from 'react';
import { CartContext, type CartContextValue } from './cartContextInstance';

export type { CartItem, AddCartItemInput, CartContextValue } from './cartContextInstance';

export function useCart(): CartContextValue {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within CartProvider');
  }

  return context;
}
