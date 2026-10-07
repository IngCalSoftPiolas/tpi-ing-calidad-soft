import { Menu } from "lucide-react";
import { useState } from "react";
import './DashboardPageAdminStyles.css'
import Sidebar from "../components/Sidebar";
import useAuth from "../../auth/hooks/useAuth.js";
import { NavLink, useNavigate, Outlet } from "react-router-dom";
function DashboardPageAdmin(){

    const getLinkStyles = ({ isActive }) => (
    `
       pl-4 w-full block  pt-4 pb-4 rounded-4xl transition hover:bg-gray-100 text-left
      ${isActive
      ? 'bg-purple-200 hover:bg-purple-100 '
      : ''
    }
    `
  );

    const navigate = useNavigate();

    const { signout } = useAuth();

    const logout = () => {
    signout();
    navigate('/login');
  };

    const [showNav, setShowNav] = useState(false);

    const toggleSidebar = () => setShowNav(prev => !prev);

    return(
        <div
        className="
            h-dvh
            bg-neutral-100 
            grid
            grid-cols-[0.15fr_1fr]
            grid-rows-[0.06fr_1fr]
            grid-areas-mobile
            sm:grid-areas-desktop 
            gap-4
            p-3
            text-base
            overflow-hidden
            min-h-0
            "
        >
            
            <Sidebar show={showNav} onToggle={toggleSidebar}>
                <ul className="w-full">
                    <li>
                        <NavLink to="/admin/home" className={getLinkStyles}>
                            Inicio
                        </NavLink>
                    </li>
                    <li>
                        <NavLink to="/admin/products" className={getLinkStyles}>
                            Productos
                        </NavLink>
                    </li>
                    <li>
                        <NavLink to="/admin/orders" className={getLinkStyles}>
                            Ordenes
                        </NavLink>
                    </li>
                </ul>
                <div className="w-full sm:hidden">
                    <button onClick={logout} className="w-full text-left p-2  bg-gray-100 border-gray-200 rounded-lg hover:bg-gray-200">
                        Cerrar sesión
                    </button>
                </div>
            </Sidebar>

            <header className="[grid-area:header]
                bg-white
                flex
                border-gray-200
                rounded-md
                shadow
                justify-between
                p-4
                items-center
                text-xl
                sticky">
                    <h1 className="pl-10">Administrador</h1>
                    <div className="hidden sm:block">
                        <button onClick={logout} className="sm:text-sm p-2 bg-gray-100 border-gray-200 rounded-lg hover:bg-gray-200">
                            Cerrar sesión
                        </button>
                    </div>
                    <div className="sm:hidden cursor-pointer">
                    <Menu onClick={()=>setShowNav(!showNav)}/>
                    </div>
                    
            </header>
            <main className="[grid-area:content]
                bg-neutral-100
                pl-2
                overflow-hidden
                min-h-0">
                    <Outlet/>
            </main>
        </div>
    );
}

export default DashboardPageAdmin;