import Card from "../../shared/components/Card";
import ProductCardHome from "../components/ProductCardHome";
import { NavLink } from "react-router-dom";
import { useEffect } from "react";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { useCallback } from "react";
import { getProducts } from "../../products/services/products";
import useAuth from "../../auth/hooks/useAuth";
import Pagination from "../components/Pagination";
import Sidebar from "../../templates/components/Sidebar";
import { Menu } from "lucide-react";
import "./HomePage.css";

export default function HomePage() {
    const [ total, setTotal ] = useState(0);
    const [ products, setProducts ] = useState([]);
    const [ pageNumber, setPageNumber ] = useState(1);
    const [loading, setLoading] = useState(false);

    const [showNav, setShowNav] = useState(false);

    const toggleSidebar = () => setShowNav(prev => !prev);
    const { signout, isAuthenticated } = useAuth();

    const logout = () => {
        signout();
        navigate('/');
    };



    const navigate = useNavigate();
    const {
        register,
        control,
    } = useForm({mode: 'onChange', defaultValues: { search: '', status: 'all' }});

    const getLinkStyles = ({ isActive }) => (
  `
    inline-flex items-center justify-center
    px-4 py-2
    rounded-full transition hover:bg-gray-100
    ${isActive ? 'bg-purple-200 hover:bg-purple-100' : ''}
  `
);

    // observar campos concretos (no todo el form)
    const searchValue = useWatch({ control, name: 'search' });
    const statusValue = useWatch({ control, name: 'status' });

    // pageSize ahora es state para poder cambiarlo en mobile
    const [pageSize, setPageSize] = useState(6);
    // detectar mobile según breakpoint sm (max-width: 639px)
    const [isMobile, setIsMobile] = useState(() => typeof window !== 'undefined' && window.matchMedia ? window.matchMedia('(max-width: 639px)').matches : false);

    useEffect(() => {
      if (typeof window === 'undefined' || !window.matchMedia) return;
      const mq = window.matchMedia('(max-width: 639px)');
      const handler = (e) => setIsMobile(e.matches);
      // addEventListener preferred
      if (mq.addEventListener) mq.addEventListener('change', handler);
      else mq.addListener(handler);
      return () => {
        if (mq.removeEventListener) mq.removeEventListener('change', handler);
        else mq.removeListener(handler);
      };
    }, []);

    // fetchProducts should depend on pageNumber so changing page fetches new data
    const fetchProducts = useCallback(async (search, status) => {
      try {
        setLoading(true);
        const { data, error } = await getProducts(search, status, pageNumber, pageSize);
        if (error) throw error;
        setTotal(data.totalCount);
        setProducts(data.products);
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }, [pageNumber, pageSize]);

    useEffect(() => {
      const id = setTimeout(() => {
        fetchProducts(searchValue, statusValue);
      }, 300);

      return () => clearTimeout(id);
    }, [searchValue, statusValue, fetchProducts]);

    // cuando estamos en mobile, ajustar pageSize al totalCount para mostrar todo en una página
    useEffect(() => {
      if (isMobile) {
        if (total > 0 && pageSize !== total) {
          setPageSize(total);
          setPageNumber(1);
        }
      } else {
        // volver al pageSize por defecto cuando no está en mobile
        if (pageSize !== 6) {
          setPageSize(6);
          setPageNumber(1);
        }
      }
    }, [isMobile, total]);

    // cuando cambia la búsqueda o el filtro, volver a la página 1
    useEffect(() => {
      setPageNumber(1);
    }, [searchValue, statusValue]);

    const statusMap = {
      active: 'Activo',
      disabled: 'Inactivo',
      all: 'Todos'
    };

    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    const goToPage = (p) => {
      if (p < 1 || p > totalPages) return;
      setPageNumber(p);
    };

  return (
    <div className="flex flex-col sm:h-full sm:home-page-desktop w-full">
        <header className="bg-white
                border-gray-200
                rounded-md
                flex
                justify-between
                items-center
                shadow
                p-4
                text-base
                font-semibold
                text-gray-700
                w-full">
            <div className="hidden sm:block flex items-center justify-center text-center sm:w-120 h-full pl-5 gap-10">
            <NavLink to="/" className={getLinkStyles}>Productos</NavLink>
            <NavLink to="/cart" className={getLinkStyles}>Carrito</NavLink>
            </div>
            <div className="relative w-64">
              <input
                type="text"
                className="border border-gray-300 rounded-2xl p-2 pr-10 w-full"
                placeholder="Search"
                {...register('search')}
              />
              <img
                src="https://www.svgrepo.com/show/532555/search.svg"
                alt="search"
                className="absolute right-3 top-1/2 transform -translate-y-1/2 w-5 h-5 pointer-events-none"
              />
            </div>
            <div className="flex items-center gap-4">
              <div className="sm:hidden">
                <button onClick={() => navigate('/cart')} aria-label="Ir al carrito" className="bg-gray-200 ml-4 p-2  shadow-stone-300 border-none hover:bg-gray-300">
                  <img src="https://www.svgrepo.com/show/533043/cart-shopping.svg" alt="cart" className="w-6 h-6" />
                </button>
              </div>
              <div className="flex items-center justify-end sm:w-120 gap-4">
                {isAuthenticated ? (
                    <button onClick={logout} aria-label="Cerrar Sesión" className="bg-gray-200 p-2 border-none shadow-stone-300 hover:bg-gray-300">
                      <img src="https://www.svgrepo.com/show/526589/logout-2.svg" alt="logout" className="w-6 h-6" />
                    </button>
                  ) :
                  <button onClick={() => navigate('/login')} aria-label="Logearse" className="bg-gray-200 p-2 border-none shadow-stone-300 hover:bg-gray-300">
                    <img src="https://www.svgrepo.com/show/535711/user.svg" alt="user" className="w-6 h-6" />
                  </button>
                }
              </div>
            </div>
        </header>
        <main className="p-4 w-full h-full min-h-0 grid grid-cols-1 md:grid-cols-3 grid-rows-2 gap-4 overflow-y-auto">
            {products.map((product) => (
              <ProductCardHome key={product.productId} product={product} />
            ))}
        </main>
        <Pagination total={total} pageNumber={pageNumber} totalPages={totalPages} onPageChange={goToPage} />
    </div>
  );
}
