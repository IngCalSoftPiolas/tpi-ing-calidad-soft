import Card from "../../shared/components/Card";
import { getProducts } from "../../products/services/products"; 
import { useEffect, useState } from "react";
import { getOrders } from "../../orders/services/listOrders";
import { useCallback } from "react";

export default function AdminPage() {

    const [totalProducts, setTotalProducts] = useState(0);
    const [totalOrders, setTotalOrders] = useState(0);
    const [loading, setLoading] = useState(false);
    const [search, setSearch] = useState('');
    const [status, setStatus] = useState('all');

    const fetchProducts = useCallback(async (search, status) => {
          try {
            setLoading(true);
            const { data, error } = await getProducts(search, status);
            if (error) throw error;
            setTotalProducts(data.totalCount);
          } catch (error) {
            console.error(error);
          } finally {
            setLoading(false);
          }
        }, []);

        const fetchOrders = useCallback(async () => {
          try {
            setLoading(true);
            const { data, error } = await getOrders();
            if (error) throw error;
            setTotalOrders(data.length);
          } catch (error) {
            console.error(error);
          } finally {
            setLoading(false);
          }
        }, []);

    useEffect(() => {
        fetchProducts(search, status);
        fetchOrders();
    }, []);

  return (
    <div className="flex flex-col h-full justify-start gap-4">
        <Card>
            <div className="flex flex-col gap-2">
            <span className="text-2xl">Productos</span>
            <span>Cantidad de productos: {totalProducts}</span>
            </div>
        </Card>
        <Card>
            <div className="flex flex-col gap-2">
            <span className="text-2xl">Órdenes</span>
            <span>Cantidad de órdenes: {totalOrders}</span>
            </div>
        </Card>
    </div>
  );
}
