import {Navigate} from 'react-router-dom';
import useAuth from '../hooks/useAuth';

function ProtectedRoute({ children }) {
    const { isAuthenticated } = useAuth();

    console.log('ProtectedRoute - isAuthenticated:', isAuthenticated);

    if (!isAuthenticated) {
        return <Navigate to="/login" />;
    }

    return children;
}

export default ProtectedRoute;