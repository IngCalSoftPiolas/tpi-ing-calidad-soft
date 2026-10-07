import { useContext, useState } from "react";
import { CartContext } from "../context/CartProvider";
import { createOrder } from "../../orders/services/createOrder"; 
import { useNavigate, NavLink } from "react-router-dom";
import toast from "react-hot-toast";
import Card from "../../shared/components/Card";
import useAuth from "../../auth/hooks/useAuth";

export default function CartPage() {
  const { items, removeFromCart, updateQuantity, clearCart, totalPrice, totalItems } = useContext(CartContext);
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [address, setAddress] = useState("");

  const getLinkStyles = ({ isActive }) => (
    `inline-flex items-center justify-center px-4 py-2 rounded-full transition hover:bg-gray-100 ${isActive ? 'bg-purple-200 hover:bg-purple-100' : ''}`
  );

  const fmtPrice = (val) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(val);


    // Manejar creacion de pedido
  const handleCheckout = async () => {
    if (items.length === 0) return;

    // 1. Verificar sesión
    const token = localStorage.getItem('token');
    if (!token) {
        // Requisito: Usuario no logueado -> Login
        // Guardamos la intención de compra para redirigir después (opcional) o simplemente mandamos al login
        toast.error("Debes iniciar sesión para finalizar la compra");
        navigate("/login");
        return;
    }

    setLoading(true);

    try {
        // 2. Creamos una plantilla de payload según la API
      const payload = {
        customerId: localStorage.getItem('customerId'),
        shippingAddress: address,
        billingAdress: address,
        notes: "Compra desde Web",
        orderItems: items.map(i => ({
          productCode: i.id || i.productId,
          quantity: i.quantity
        }))
      };

        // 3. Llamar al servicio de creación de orden
      const { data, error } = await createOrder(payload);

      if (error) {
        toast.error(typeof error === 'string' ? error : "Error al procesar la orden");
      } else {
        toast.success(`¡Pedido creado con éxito! ID: ${data.orderId.substring(0,8)}`);
        clearCart();
        navigate("/"); // Volver al home o a mis órdenes
      }

    } catch (err) {
      console.error(err);
      toast.error("Ocurrió un error inesperado");
    } finally {
      setLoading(false);
    }
  };

  const { isAuthenticated, signout } = useAuth();

  const logout = () => {
    signout();
    navigate('/');
  };

  return (
    <div className="min-h-screen h-dvh bg-gray-50 flex flex-col">
        
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
            <div className="flex-1 max-w-md mx-4 hidden sm:block">
               <div className="relative w-64">
              <input
                type="text"
                className="border border-gray-300 bg-gray-100 rounded-2xl p-2 pr-10 w-full"
                placeholder="Search"
                disabled
              />
              <img
                src="https://www.svgrepo.com/show/532555/search.svg"
                alt="search"
                className="absolute right-3 top-1/2 transform -translate-y-1/2 w-5 h-5 pointer-events-none"
              />
            </div>
            </div>
            <div className="flex items-center justify-end sm:w-72 gap-4">
                    <button onClick={() => navigate('/')} aria-label="Volver Atras" className="bg-gray-200 ml-4 p-2 border-none  shadow-stone-300 hover:bg-gray-300">
                        <img src="https://www.svgrepo.com/show/458545/back.svg" alt="previous" className="w-6 h-6" />
                    </button> 
                    {isAuthenticated ? (
                        <button onClick={logout} aria-label="Cerrar Sesión" className="bg-gray-200 ml-2 p-2 border-none shadow-stone-300 hover:bg-gray-300">
                            <img src="https://www.svgrepo.com/show/526589/logout-2.svg" alt="logout" className="w-6 h-6" />
                        </button> 
                    ) : <button onClick={() => navigate('/login')} aria-label="Logearse" className="bg-gray-200 ml-2 p-2 border-none  shadow-stone-300 hover:bg-gray-300">
                        <img src="https://www.svgrepo.com/show/535711/user.svg" alt="user" className="w-6 h-6" />
                    </button>
                    }
            </div>
        </header>

        {/* CONTENIDO PRINCIPAL */}
        <main className="flex-1 p-4 sm:p-8 max-w-7xl mx-auto w-full h-80 overflow-auto sm:overflow-hidden">
            
            {items.length === 0 ? (
                <div className="text-center py-20">
                    <h2 className="text-2xl font-bold text-gray-400">El carrito está vacío</h2>
                    <button onClick={() => navigate('/')} className="mt-4 gap-4 p-2 bg-gray-300 text-gray-700 border-none shadow-stone-300 text-sm sm:text-base font-semibold hover:underline">Volver a la tienda</button>
                </div>
            ) : (
                // LAYOUT: Grid para separar Lista (izq) y Resumen (der)
                <div className="grid grid-cols-1 lg:grid-cols-[1fr_350px] gap-6 items-start">
                    
                    {/* LISTA DE PRODUCTOS (Izquierda) */}
                    <div className="flex flex-col gap-4">
                        {items.map((item) => (
                            <Card key={item.id ?? item.sku} className="flex flex-col sm:flex-row items-center justify-between p-6 shadow-sm border border-gray-100">
                                
                                {/* Info Producto */}
                                <div className="flex-1 w-full sm:w-auto mb-4 sm:mb-0">
                                    <h3 className="text-lg font-bold text-gray-800">{item.name}</h3>
                                    <div className="flex flex-col sm:flex-row sm:gap-8 mt-1 text-sm text-gray-500">
                                        <span>Cantidad de productos: {item.quantity}</span>
                                        <span>Sub Total: {fmtPrice(item.currentUnitPrice * item.quantity)}</span>
                                    </div>
                                </div>

                                {/* Controles */}
                                <div className="flex items-center gap-3">
                                    {/* Contador - 0 + */}
                                    <div className="flex items-center gap-2">
                                        <button 
                                            onClick={() => updateQuantity(item.id ?? item.sku, item.quantity - 1)}
                                            className="text-xl font-bold text-gray-800 px-2"
                                            disabled={item.quantity <= 1}
                                        >−</button>
                                        
                                        <span className="border border-gray-300 rounded px-3 py-1 text-gray-700 bg-white min-w-[40px] text-center">
                                            {item.quantity}
                                        </span>

                                        <button 
                                            onClick={() => updateQuantity(item.id ?? item.sku, item.quantity + 1)}
                                            className="text-xl font-bold text-gray-800 px-2"
                                        >+</button>
                                    </div>

                                    {/* Botón Borrar (Lila) */}
                                    <button 
                                        onClick={() => removeFromCart(item.id ?? item.sku)}
                                        className="bg-purple-200 text-purple-800 hover:bg-purple-300 px-4 py-2 rounded-md font-medium text-sm flex items-center gap-1 transition-colors"
                                    >
                                        Borrar
                                    </button>
                                </div>
                            </Card>
                        ))}
                    </div>

                    {/* RESUMEN DE PEDIDO (Derecha) */}
                    <Card className="bg-white p-6 shadow-sm border border-gray-100 sticky top-24">
                        <h2 className="text-xl font-bold mb-6">Detalle de pedido</h2>
                        
                        <div className="space-y-4 mb-8">
                            <div className="flex justify-between text-gray-600">
                                <span>Cantidad de en total:</span>
                                <span>{totalItems}</span>
                            </div>
                            <div className="flex justify-between text-lg font-medium text-gray-800 pt-4 border-t border-gray-100">
                                <span>Total a pagar:</span>
                                <span>{fmtPrice(totalPrice)}</span>
                            </div>
                        </div>

                        {/* Input opcional para dirección si quieres simularlo aquí */}
                        <div className="mb-4">
                            <label className="text-xs text-gray-500 block mb-1">Dirección de envío</label>
                            <input 
                                type="text" 
                                value={address}
                                onChange={(e) => setAddress(e.target.value)}
                                className="w-full bg-gray-50 border border-gray-200 rounded p-2 text-sm"
                                placeholder="Ingresa tu dirección de envío"
                            />
                        </div>

                        <button 
                            onClick={handleCheckout}
                            disabled={loading}
                            className={`
                                w-full py-3 rounded-lg font-semibold text-center transition-colors
                                ${loading 
                                    ? 'bg-gray-300 text-gray-500 cursor-not-allowed' 
                                    : 'bg-purple-200 text-purple-800 hover:bg-purple-300'}
                            `}
                        >
                            {loading ? "Procesando..." : "Finalizar Compra"}
                        </button>
                    </Card>

                </div>
            )}
        </main>
    </div>
  );
}