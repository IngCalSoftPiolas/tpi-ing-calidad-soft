import { useState } from "react";
import Card from "../../shared/components/Card";

export default function ProductCard({ product, statusMap = {} }) {
    const [expanded, setExpanded] = useState(false);

    const fmt = (value) =>
        value != null
            ? new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" }).format(value)
            : "-";

    const rawPrice = product?.currentUnitPrice ?? product?.price ?? null;
    const priceLabel = rawPrice != null && Number.isFinite(Number(rawPrice)) ? fmt(Number(rawPrice)) : "-";

    const rawStock = product?.stockQuantity ?? product?.stock ?? null;
    const stockNum = Number(rawStock);
    const hasFiniteStock = Number.isFinite(stockNum);
    const stockLabel = rawStock == null ? "Ilimitado" : hasFiniteStock ? Math.max(0, Math.floor(stockNum)) : "-";

    const imgSrc = product?.imageUrl ?? product?.image ?? product?.pictureUrl ?? null;

    return (
        <Card>
            <div className="flex flex-col gap-2">
                <div className="flex justify-between items-start">
                    <div>
                        <span className="font-bold">{product?.name ?? product?.title ?? "Sin nombre"}</span>
                        <div className="text-sm text-gray-600">
                            {priceLabel} · {statusMap[product?.status] ?? product?.status ?? "-"}
                        </div>
                        <div className="text-xs text-gray-500">ID: {product?.productId ?? product?.id ?? "-"}</div>
                    </div>

                    <div className="ml-4 flex items-center gap-2">
                        <button
                            type="button"
                            className="text-sm px-3 py-1 border rounded bg-white"
                            onClick={() => setExpanded((v) => !v)}
                        >
                            {expanded ? "Ocultar" : "Ver"}
                        </button>
                    </div>
                </div>

                {expanded && (
                    <div className="mt-2 p-3 border border-gray-200 rounded text-sm space-y-3 bg-gray-50">
                        <div className="flex gap-4">
                            {imgSrc ? (
                                <img src={imgSrc} alt={product?.name ?? "product"} className="w-24 h-24 object-cover rounded" />
                            ) : (
                                <div className="w-24 h-24 bg-gray-100 rounded flex items-center justify-center text-xs text-gray-500">Sin imagen</div>
                            )}

                            <div className="flex-1">
                                <div><strong>Nombre:</strong> {product?.name ?? "-"}</div>
                                <div><strong>SKU:</strong> {product?.sku ?? product?.code ?? "-"}</div>
                                <div><strong>Precio:</strong> {priceLabel}</div>
                                <div><strong>Stock:</strong> {stockLabel}</div>
                                <div><strong>Estado:</strong> {statusMap[product?.status] ?? product?.status ?? "-"}</div>
                            </div>
                        </div>

                        <div>
                            <strong>Descripción:</strong>
                            <div className="mt-1 text-gray-700">{product?.description ?? product?.shortDescription ?? "-"}</div>
                        </div>

                        {product?.attributes && typeof product.attributes === 'object' && Object.keys(product.attributes).length > 0 && (
                            <div>
                                <strong>Atributos:</strong>
                                <div className="mt-2 grid grid-cols-2 gap-2 text-xs text-gray-700">
                                    {Object.entries(product.attributes).map(([k, v]) => (
                                        <div key={k} className="p-2 border border-gray-100 rounded bg-white">
                                            <div className="font-medium">{k}</div>
                                            <div className="text-gray-600">{String(v)}</div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}

                        <div className="text-xs text-gray-500">Product raw data: {JSON.stringify({ id: product?.productId ?? product?.id ?? null })}</div>
                    </div>
                )}
            </div>
        </Card>
    );
}
