import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import type { UniversalPriceTier } from '../services/api';
import { CartContext, type AddCartItemInput, type CartItem } from './cartContextInstance';

const CART_STORAGE_KEY = 'invenflow-cart';

function normalizeTiers(priceBreaks: UniversalPriceTier[] | undefined) {
  return [...(priceBreaks ?? [])]
    .filter((tier) => Number.isFinite(tier.qty) && tier.qty > 0 && Number.isFinite(tier.unitPrice) && tier.unitPrice >= 0)
    .sort((a, b) => a.qty - b.qty);
}

function getUnitPriceForQuantity(priceBreaks: UniversalPriceTier[] | undefined, quantity: number, fallbackPrice: number) {
  const tiers = normalizeTiers(priceBreaks);
  if (tiers.length === 0) {
    return fallbackPrice;
  }

  return tiers.reduce((active, tier) => (quantity >= tier.qty ? tier.unitPrice : active), tiers[0].unitPrice);
}

function normalizeMpn(mpn: string) {
  return mpn.trim().toUpperCase();
}

function parseStoredCartItems() {
  if (typeof window === 'undefined') {
    return [] as CartItem[];
  }

  try {
    const raw = window.localStorage.getItem(CART_STORAGE_KEY);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw) as CartItem[];
    if (!Array.isArray(parsed)) {
      return [];
    }

    return parsed.filter((item) => item && typeof item === 'object' && typeof item.id === 'string' && typeof item.mpn === 'string');
  } catch {
    window.localStorage.removeItem(CART_STORAGE_KEY);
    return [];
  }
}

function persistCartItems(items: CartItem[]) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(parseStoredCartItems);

  const value = useMemo(() => {
    const addToCart = (input: AddCartItemInput) => {
      const selectedQty = Number.isFinite(input.quantity) ? Math.floor(input.quantity) : 0;
      if (selectedQty <= 0) {
        return;
      }

      setItems((prevItems) => {
        const normalizedMpn = normalizeMpn(input.mpn);
        const index = prevItems.findIndex((item) => normalizeMpn(item.mpn) === normalizedMpn);

        if (index === -1) {
          const baseQuantity = selectedQty;
          const fallbackPrice = input.unitPrice ?? 0;
          const unitPrice = getUnitPriceForQuantity(input.priceBreaks, baseQuantity, fallbackPrice);
          const nextItems = [
            ...prevItems,
            {
              ...input,
              id: normalizedMpn,
              quantity: baseQuantity,
              unitPrice,
              priceBreaks: normalizeTiers(input.priceBreaks),
            },
          ];
          persistCartItems(nextItems);
          return nextItems;
        }

        const current = prevItems[index];
        const nextQuantity = current.quantity + selectedQty;
        const fallbackPrice = input.unitPrice ?? current.unitPrice;
        const nextItem: CartItem = {
          ...current,
          quantity: nextQuantity,
          unitPrice: getUnitPriceForQuantity(current.priceBreaks, nextQuantity, fallbackPrice),
        };

        const nextItems = [...prevItems];
        nextItems[index] = nextItem;
        persistCartItems(nextItems);
        return nextItems;
      });
    };

    const updateCartItemQuantity = (mpn: string, quantity: number) => {
      const exactQuantity = Number.isFinite(quantity) ? Math.floor(quantity) : 0;
      const normalizedMpn = normalizeMpn(mpn);

      setItems((prevItems) => {
        const nextItems = prevItems
          .map((item) => {
            if (normalizeMpn(item.mpn) !== normalizedMpn) {
              return item;
            }

            if (exactQuantity <= 0) {
              return null;
            }

            return {
              ...item,
              quantity: exactQuantity,
              unitPrice: getUnitPriceForQuantity(item.priceBreaks, exactQuantity, item.unitPrice),
            };
          })
          .filter((item): item is CartItem => item !== null);

        persistCartItems(nextItems);
        return nextItems;
      });
    };

    const removeFromCart = (mpn: string) => {
      const normalizedMpn = normalizeMpn(mpn);
      setItems((prevItems) => {
        const nextItems = prevItems.filter((item) => normalizeMpn(item.mpn) !== normalizedMpn);
        persistCartItems(nextItems);
        return nextItems;
      });
    };

    const clearCart = () => {
      setItems([]);
      persistCartItems([]);
    };

    const totalQuantity = items.reduce((sum, item) => sum + item.quantity, 0);
    const totalPrice = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);

    return {
      items,
      totalQuantity,
      totalPrice,
      addToCart,
      updateCartItemQuantity,
      removeFromCart,
      addItem: addToCart,
      updateQuantity: updateCartItemQuantity,
      removeItem: removeFromCart,
      clearCart,
    };
  }, [items]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}
