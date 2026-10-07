import { createContext, useState } from 'react';
import { login } from '../services/login';
import toast from 'react-hot-toast';

const AuthContext = createContext();

function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(() => {
    const token = localStorage.getItem('token');
    return Boolean(token);
  });

  const signout = () => {
    localStorage.clear();
    setIsAuthenticated(false);
    toast.success('Sesión cerrada exitosamente');
  };

  const signin = async (username, password) => {
    const { token, role, customerId, error } = await login(username, password);

    if (error) {
      return { error };
    }

    localStorage.setItem('token', token);
    localStorage.setItem('customerId', customerId);
    setIsAuthenticated(true);

    return { error: null, role };
  };

  return (
    <AuthContext.Provider
      value={ {
        isAuthenticated,
        signin,
        signout,
      } }
    >
      {children}
    </AuthContext.Provider>
  );
};

export {
  AuthProvider,
  AuthContext,
};
