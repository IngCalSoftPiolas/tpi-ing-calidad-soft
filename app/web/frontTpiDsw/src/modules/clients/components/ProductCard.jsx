import { useState } from "react";
import Card from "../../shared/components/Card";

export default function ProductCard({ product, statusMap = {} }) {
    const [expanded, setExpanded] = useState(false);

    return (
        <Card>
            <div className="flex flex-col gap-2">
                <div className="flex justify-between items-start">
                    <div>
                        <span className="font-bold">{product.sku} - {product.name}</span>
                        <div className="text-sm text-gray-600">
                            {product.stockQuantity} - {statusMap[product.status] ?? product.status}
                        </div>
                    </div>
                    <div className="ml-4">
                        <button
                            type="button"
                            className="text-sm px-3 py-1 border rounded bg-white"
                            onClick={() => setExpanded(v => !v)}
                        >
                            {expanded ? 'Ocultar' : 'Ver'}
                        </button>
                    </div>
                </div>

                {expanded && (
                    <div className="mt-2 p-3 border border-gray-200  rounded text-sm space-y-1">
                        <div><strong>Código interno:</strong> {product.internalCode ?? '-'}</div>
                        <div><strong>Descripción:</strong> {product.description ?? '-'}</div>
                        <div>
                            <strong>Precio:</strong>{' '}
                            {product.currentUnitPrice != null
                                ? new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(product.currentUnitPrice)
                                : '-'}
                        </div>
                        <div><strong>Estado:</strong> {statusMap[product.status] ?? product.status}</div>
                    </div>
                )}
            </div>
        </Card>
    );
}