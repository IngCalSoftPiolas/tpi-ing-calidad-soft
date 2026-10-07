import React, { createContext, useEffect, useState } from 'react';

export const CartContext = createContext();

function getProductKey(p) {
  if (!p) return undefined;
  return p.id ?? p._id ?? p.sku ?? p.code ?? p.name ?? JSON.stringify({ name: p.name, price: p.currentUnitPrice ?? p.price });
}

export function CartProvider({ children }) {
  const [items, setItems] = useState(() => {
    const itemsCart = JSON.parse(localStorage.getItem('cartItems')) || [];
    return itemsCart;
  });

  // // load from localStorage on mount
  // useEffect(() => {
  //   try {
  //     const raw = localStorage.getItem('cartItems');
  //     console.log('Loaded cart from localStorage', raw);
  //     if (raw) setItems(JSON.parse(raw));
  //   } catch (e) {
  //     console.error('Failed to load cart from localStorage', e);
  //   }
  // }, []);

  // // persist changes
  // useEffect(() => {
  //   try {
  //     const raw = localStorage.getItem('cartItems');
  //     if (!raw) {
  //       localStorage.setItem('cartItems', JSON.stringify(items));
  //     }
  //     console.log('Saved cart to localStorage', items);
  //     console.log('cartItems', localStorage.getItem('cartItems'));
  //   } catch (e) {
  //     console.error('Failed to save cart to localStorage', e);
  //   }
  // }, [items]);

  // useEffect(() => {
  //   const itemsCart = JSON.parse(localStorage.getItem('cartItems')) || [];
    
  //   if (itemsCart.length && items.length === 0) 
  //   {
  //     setItems(itemsCart);
  //   }
  // }, []);

  function addToCart(product, qty = 1) {
    if (!product || qty <= 0) return;

    console.debug('[Cart] addToCart called', { key: getProductKey(product), qty });

    setItems(prev => {
      console.debug('[Cart] prev items', prev);

      const stock = Number(product.stockQuantity ?? product.stock ?? Infinity);
      const hasFiniteStock = Number.isFinite(stock);

      const key = getProductKey(product);
      const index = prev.findIndex(i => getProductKey(i) === key);
      const copy = [...prev];

      if (index >= 0) {
        const existing = { ...copy[index] };
        const newQty = (Number(existing.quantity) || 0) + qty;
        existing.quantity = hasFiniteStock ? Math.min(newQty, Math.max(0, Math.floor(stock))) : newQty;
        copy[index] = existing;
      } else {
        const initialQty = hasFiniteStock ? Math.min(qty, Math.max(0, Math.floor(stock))) : qty;
        // keep original product fields and add `quantity`
        copy.push({ ...product, quantity: initialQty });
      }

      console.debug('[Cart] new items', copy);
      return copy;
    });
  }

  function removeFromCart(productKey) {
    const newItems = items.filter(i => getProductKey(i) !== productKey);
    setItems(newItems);
    localStorage.setItem(JSON.stringify(newItems))
  }

  function updateQuantity(productKey, quantity) {
    setItems(prev => prev.map(i => getProductKey(i) === productKey ? { ...i, quantity: Math.max(0, Math.floor(quantity)) } : i));
  }

  function clearCart() {
    setItems([]);
    localStorage.removeItem('cartItems');
  }

  const totalItems = items.reduce((s, it) => s + (Number(it.quantity) || 0), 0);
  const totalPrice = items.reduce((s, it) => s + ((Number(it.currentUnitPrice ?? it.price ?? 0) || 0) * (Number(it.quantity) || 0)), 0);

  return (
    <CartContext.Provider value={{ items, addToCart, removeFromCart, updateQuantity, clearCart, totalItems, totalPrice }}>
      {children}
    </CartContext.Provider>
  );
}
