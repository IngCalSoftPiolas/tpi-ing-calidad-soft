import { instance } from "../../shared/api/axiosInstance";

export const createOrder = async (orderData) => {
    try {
        const response = await instance.post("/api/orders", orderData);
        return { data: response.data, error: null };
    } catch (error) {
        return { 
            data: null, 
            error: error.response?.data || "Error al crear la orden" 
        };
    }
};