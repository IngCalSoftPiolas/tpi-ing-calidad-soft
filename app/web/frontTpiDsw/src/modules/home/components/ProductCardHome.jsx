import React, { useState, useContext } from 'react';
import Card from '../../shared/components/Card';
import { CartContext } from '../../cart/context/CartProvider';

export default function ProductCard({ product = {}, onAdd }) {
  const [qty, setQty] = useState(0);
  const { addToCart } = useContext(CartContext);

  // normalize stock value: if undefined => unlimited (Infinity)
  const rawStock = product.stockQuantity ?? null;
  const stockNum = Number(rawStock);
  const hasFiniteStock = Number.isFinite(stockNum);
  const maxQty = hasFiniteStock ? Math.max(0, Math.floor(stockNum)) : Infinity;
  const isSoldOut = hasFiniteStock && maxQty === 0;

  const increase = () => setQty((q) => Math.min(q + 1, maxQty));
  const decrease = () => setQty((q) => Math.max(0, q - 1));

  // Guard against missing or invalid price values
  const rawPrice = product.currentUnitPrice ?? product.price ?? 0;
  const price = Number(rawPrice);
  const safePrice = Number.isFinite(price) ? price : 0;
  const priceLabel = safePrice % 1 === 0 ? safePrice.toFixed(0) : safePrice.toFixed(2);

  const handleAdd = () => {
    if (qty <= 0) return; // nothing to add
    if (isSoldOut) return; // cannot add
    addToCart(product, qty);
    if (onAdd) onAdd(product, qty);
    setQty(0);
  };

  return (
    <Card className="flex flex-col p-0 overflow-hidden">
      <div className="bg-gray-100 w-full h-48 md:h-56 flex items-center justify-center">
        {product.imageUrl ? (
          <img src={product.imageUrl} alt={product.name} className="w-full h-full object-cover" />
        ) : (
          <div className="w-full h-full bg-gray-200" />
        )}
      </div>

      <div className="p-4 flex flex-col justify-between flex-1">
        <div>
          <h3 className="text-sm font-medium text-gray-800 mb-1">{product.name ?? 'Text'}</h3>
          {isSoldOut ? (
            <div className="text-xs text-red-500">Agotado</div>
          ) : hasFiniteStock ? (
            <div className="text-xs text-gray-500">Stock: {maxQty}</div>
          ) : null}
        </div>

        <div className="flex items-center justify-between mt-2">
          <div>
            <div className="text-sm text-gray-800 font-bold">${priceLabel}</div>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2 bg-white border-none rounded-md px-2 py-1">
              <button onClick={decrease} className="bg-white text-gray-600 border-none shadow-none" aria-label="decrease">−</button>
              <div className="text-sm text-gray-700 text-center border border-gray-200 px-3 py-1.5 rounded-md">{qty}</div>
              <button
                onClick={increase}
                className={`text-gray-600 bg-white border-none shadow-none ${qty >= maxQty ? 'opacity-50 cursor-not-allowed' : ''}`}
                aria-label="increase"
                disabled={qty >= maxQty}
              >
                +
              </button>
            </div>

            <button
              onClick={handleAdd}
              className={`px-3 py-1 rounded-md text-sm ${qty > 0 && !isSoldOut ? 'bg-purple-200 text-purple-800' : 'bg-gray-200 text-gray-500 cursor-not-allowed'}`}
              disabled={qty <= 0 || isSoldOut}
            >Agregar</button>
          </div>
        </div>
      </div>
    </Card>
  );
}
