import { useState } from "react";
import Card from "../../shared/components/Card";

export default function OrderCard({ order, statusMap = {} }) {
    const [expanded, setExpanded] = useState(false);

    const fmt = (value) =>
        value != null
            ? new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" }).format(value)
            : "-";

    return (
        <Card>
            <div className="flex flex-col gap-2">
                <div className="flex justify-between items-start">
                    <div>
                        <span className="font-bold">{order.orderId} - {order.customerName}</span>
                        <div className="text-sm text-gray-600">
                            {fmt(order.totalAmount)} · {statusMap[order.status] ?? order.status}
                        </div>
                    </div>

                    <div className="ml-4">
                        <button
                            type="button"
                            className="text-sm px-3 py-1 border rounded bg-white"
                            onClick={() => setExpanded(v => !v)}
                        >
                            {expanded ? "Ocultar" : "Ver"}
                        </button>
                    </div>
                </div>

                {expanded && (
                    <div className="mt-2 p-3 border border-gray-200 rounded text-sm space-y-3 bg-gray-50">
                        <div><strong>Cliente:</strong> {order.customerName} ({order.customerId})</div>
                        <div><strong>Dirección envío:</strong> {order.shippingAddress ?? "-"}</div>
                        <div><strong>Dirección facturación:</strong> {order.billingAddress ?? "-"}</div>
                        <div><strong>Notas:</strong> {order.notes ?? "-"}</div>
                        <div><strong>Estado:</strong> {statusMap[order.status] ?? order.status}</div>
                        <div><strong>Total:</strong> {fmt(order.totalAmount)}</div>

                        <div>
                            <strong>Items:</strong>
                            <div className="mt-2 space-y-2">
                                {Array.isArray(order.orderItems) && order.orderItems.length > 0 ? (
                                    order.orderItems.map((it, idx) => (
                                        <div key={idx} className="p-2 border border-gray-100 rounded bg-white">
                                            <div className="font-medium">{it.name}</div>
                                            <div className="text-xs text-gray-600">{it.description}</div>
                                            <div className="text-sm mt-1">
                                                Cantidad: {it.quantity} · Subtotal: {fmt(it.subtotal)}
                                            </div>
                                        </div>
                                    ))
                                ) : (
                                    <div className="text-gray-600">Sin items</div>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </Card>
    );
}