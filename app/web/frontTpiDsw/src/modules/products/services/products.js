import { instance } from '../../shared/api/axiosInstance';

export const getProducts = async (search = null, status = null, pageNumber = 1, pageSize = 20 ) => {
  const params = {
    pageNumber,
    pageSize
  };

  if (search) params.search = search;
  if (status && status !== 'all') params.status = status;

  try {
    const response = await instance.get('/api/products/admin', { params });
    return { data: response.data, error: null };
  } catch (error) {
    if (error.response && error.response.status === 404) {
        return { 
            data: { products: [], totalCount: 0 }, 
            error: null 
        };
    }

    return { data: null, error: error.response?.data };
  }
};

export const createProduct = async (productData) => {
  try {
    const response = await instance.post('/api/products', productData);
    return { data: response.data, error: null };
  } catch (error) {
    console.error(error);
    return { data: null, error: error.response?.data};
  }
};