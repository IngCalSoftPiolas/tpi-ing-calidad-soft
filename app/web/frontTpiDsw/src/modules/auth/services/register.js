import { instance } from "../../shared/api/axiosInstance";

export const registerService = async (username, email, password, role, nombre, phoneNumber) => {
  try {
    const response = await instance.post("/api/auth/register", { 
        username, 
        email, 
        password, 
        role,
        nombre,
        phoneNumber 
    });
    return { data: response.data, error: null };
  } catch (error) {
    return { 
        data: null, 
        error: error.response?.data || "Error desconocido al registrar" 
    };
  }
};