import { instance } from "../../shared/api/axiosInstance";

export const getOrders = async () => {
    try {
        const response = await instance.get("/api/orders");
        return { data: response.data };
    } catch (error) {
        return { error: error.response.data };
    }
};
