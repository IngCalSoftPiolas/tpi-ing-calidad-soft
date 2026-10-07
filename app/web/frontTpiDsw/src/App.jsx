import LoginPage from './modules/auth/pages/LoginPage.jsx'
import RegisterPage from './modules/auth/pages/RegisterPage.jsx'
import DashboardPageAdmin from './modules/templates/pages/DashboardPageAdmin.jsx';
import ProtectedRoute from './modules/auth/components/ProtectedRoute.jsx';
import { AuthProvider } from './modules/auth/context/AuthProvider.jsx';
import ListProductsPage from './modules/products/pages/ListProductsPage.jsx';
import ListOrdersPage from './modules/orders/pages/ListOrdersPage.jsx';
import CreateProductsPage from './modules/products/pages/CreateProductsPage.jsx';
import { RouterProvider, createBrowserRouter } from "react-router-dom";
import {Toaster} from 'react-hot-toast';
import HomePage from './modules/home/pages/HomePage.jsx';
import AdminHomePage from './modules/home/pages/AdminHomePage.jsx';
import CartPage from './modules/cart/pages/CartPage.jsx';
import './App.css'
import { CartProvider } from './modules/cart/context/CartProvider.jsx';

function App() {
  const router = createBrowserRouter([
    {
      path: "/",
      element: <HomePage />
    },
    {
      path: "/cart",
      element: <CartPage />
    },
    {
      path: "/login",
      element: <LoginPage />
    },
    {
      path: "/register",
      element: <RegisterPage />
    },
    {
      path: "/admin",
      element: 
              <ProtectedRoute>
                <DashboardPageAdmin />
              </ProtectedRoute>,
      children: [
        {
          path: "products",
          element: <ListProductsPage />,
        },
        {
          path: "products/create",
          element: <CreateProductsPage />
        },
        {
          path: "orders",
          element: <ListOrdersPage />
        },
        {
          path: "home",
          element: <AdminHomePage/>
        }
      ],
    },
  ]);

  return (
    <>
      <AuthProvider>
        <CartProvider>
          <Toaster position="top-right" />
          <RouterProvider router={router} />
        </CartProvider>
      </AuthProvider>
    </>
  );
}

export default App




