import Card from "../../shared/components/Card";
import { useForm, Controller, useWatch } from "react-hook-form";
import { useEffect, useCallback } from "react";
import { getProducts } from "../services/products.js";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import ProductCard from "../components/ProductCard.jsx";

function ListProductsPage() {

    const [ total, setTotal ] = useState(0);
    const [ products, setProducts ] = useState([]);

    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const {
        register,
        control,
    } = useForm({mode: 'onChange', defaultValues: { search: '', status: 'all' }});

    // observar campos concretos (no todo el form)
    const searchValue = useWatch({ control, name: 'search' });
    const statusValue = useWatch({ control, name: 'status' });

    const fetchProducts = useCallback(async (search, status) => {
      try {
        setLoading(true);
        const { data, error } = await getProducts(search, status);
        if (error) throw error;
        setTotal(data.totalCount);
        setProducts(data.products);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }, []);

    
    useEffect(() => {
      const id = setTimeout(() => {
        fetchProducts(searchValue, statusValue);
      }, 300);

      return () => clearTimeout(id);
    }, [searchValue, statusValue, fetchProducts]);

    const statusMap = {
      active: 'Activo',
      disabled: 'Inactivo',
      all: 'Todos'
    };

    return (
        <div className="flex flex-col h-full min-h-0 justify-start gap-4">
            <Card className="flex flex-col gap-4"> {/* header / filtros queda fijo */}
                <form className="flex flex-col gap-4">
                    <div className="flex justify-between items-center">
                        <span className="font-bold">Productos</span>
                        <button className="hidden sm:block text-sm p-2" onClick={() => navigate('/admin/products/create')}>Crear Producto</button>
                        <button className="sm:hidden text-sm" onClick={() => navigate('/admin/products/create')}>
                            <img
                                src="https://www.svgrepo.com/show/521942/add-ellipse.svg"
                                alt="add"
                                className="w-5 h-5"
                            />
                        </button>
                    </div>
                    <div className="flex gap-2 w-full">
                        <input type="text" placeholder="Buscar producto por nombre..." className="input-default w-full" {...register('search')} />
                        <div className="hidden sm:block">
                          <Controller
                            name="status"
                            control={control}
                            render={({ field }) => (
                              <select {...field} className="input-default w-50">
                                <option value="all">Filtrar por estado</option>
                                <option value="enabled">Activo</option>
                                <option value="disabled">Inactivo</option>
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
                                <option value="enabled">Activo</option>
                                <option value="disabled">Inactivo</option>
                                <option value="all">Todos</option>
                            </select>
                          )}
                        />
                    </div>
                </form>
            </Card>
 
            <div className="flex-1 min-h-0 overflow-auto space-y-4 bg-transparent">
                 {products.map(product => (
                     <ProductCard
                         key={product.productId}
                         product={product}
                         statusMap={statusMap}
                     />
                 ))}
             </div>
         </div>
     );
 }
 
 export default ListProductsPage;