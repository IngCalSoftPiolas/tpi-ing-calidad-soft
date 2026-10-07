import { instance } from "../../shared/api/axiosInstance";

export const login = async (username, password) => {
  try {
    const response = await instance.post("/api/auth/login", { username, password });
    return {
        token : response.data.token,
        role: response.data.role,
        customerId: response.data.id,
        error: null
    }
  } catch (error) {
    return {
        token: null,
        role: null,
        customerId: null,
        error: error.response?.data?.error || "Ocurrió un error inesperado"
    };
  }
};
