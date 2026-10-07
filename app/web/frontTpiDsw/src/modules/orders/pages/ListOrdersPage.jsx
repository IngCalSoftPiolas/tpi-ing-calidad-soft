import Card from "../../shared/components/Card";
import { useForm, Controller, useWatch } from "react-hook-form";
import { useEffect, useCallback } from "react";
import { getOrders } from "../services/listOrders.js";
import { useState } from "react";
import OrderCard from "../components/OrderCard";

function ListOrdersPage() {

    //const [ total, setTotal ] = useState(0);
    const [ orders, setOrders ] = useState([]);

    const [loading, setLoading] = useState(false);

    const {
        register,
        control,
    } = useForm({mode: 'onChange', defaultValues: { search: '', status: 'all' }});

    // observar campos concretos (no todo el form)
    const searchValue = useWatch({ control, name: 'search' });
    const statusValue = useWatch({ control, name: 'status' });

    const fetchOrders = useCallback(async () => {
          try {
            setLoading(true);
            const { data, error } = await getOrders();
            if (error) throw error;
            setOrders(data);
          } catch (error) {
            console.error(error);
          } finally {
            setLoading(false);
          }
        }, []);

    useEffect(() => {
        const id = setTimeout(() => {
        fetchOrders();
      }, 300);
      return () => clearTimeout(id);
    }, [searchValue, statusValue, fetchOrders]);

    const statusMap = {
      active: 'Activo',
      disabled: 'Inactivo',
      all: 'Todos'
    };

    // filtrar orders según el input 'search' y el select 'status'. Si search está vacío, devolver todas. Si status es 'all', no filtrar por estado.
    const filteredOrders = orders.filter(order => {
      const q = (searchValue || '').toString().trim().toLowerCase();
      if (q) {
        if (!String(order.orderId).toLowerCase().includes(q)) return false;
      }

      const s = (statusValue || 'all').toString().trim().toLowerCase();
      if (s && s !== 'all') {
        const orderStatus = (order.status || '').toString().trim().toLowerCase();
        if (orderStatus !== s) return false;
      }

      return true;
    });

    return (
        <div className="flex flex-col h-full min-h-0 justify-start gap-4"> 
             <Card className="flex flex-col gap-4 min-h-0"> 
                <form className="flex flex-col gap-4">
                    <div className="flex justify-between items-center">
                        <span className="font-bold">Ordenes</span>
                    </div>
                    <div className="flex gap-2 w-full">
                        <input type="text" placeholder="Buscar ordenes por id..." className="input-default w-full" {...register('search')} />
                        <div className="hidden sm:block">
                          <Controller
                            name="status"
                            control={control}
                            render={({ field }) => (
                              <select {...field} className="input-default w-50">
                                <option value="all">Filtrar por estado</option>
                                <option value="pending">Pendiente</option>
                                <option value="processing">En Proceso</option>
                                <option value="shipped">Enviado</option>
                                <option value="delivered">Entregado</option>
                                <option value="canceled">Cancelado</option>
                                <option value="all">Todos</option>
                              </select>
                            )}
                          />
                        </div>
                    </div>
                    <div className="sm:hidden w-full gap-2">
                        <Controller
                          name="status"
                          control={control}
                          render={({ field }) => (
                            <select {...field} className="input-default w-full">
                                <option value="all">Filtrar por estado</option>
                                <option value="pending">Pendiente</option>
                                <option value="processing">En Proceso</option>
                                <option value="shipped">Enviado</option>
                                <option value="delivered">Entregado</option>
                                <option value="canceled">Cancelado</option>
                                <option value="all">Todos</option>
                            </select>
                          )}
                        />
                    </div>
                </form>
            </Card>

            <div className="flex-1 min-h-0 overflow-auto space-y-4 bg-transparent">
                 {filteredOrders.map(order => (
                      <OrderCard
                          key={order.orderId}
                          order={order}
                          statusMap={statusMap}
                      />
                  ))}
              </div>
        </div>
    );
}

export default ListOrdersPage;