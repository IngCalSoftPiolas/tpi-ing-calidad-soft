using Dsw2025Tpi.Application.Dtos;

namespace Dsw2025Tpi.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductModelResponse> CreateProductAsync(ProductModelRequest r);
        Task DisableProductById(Guid id);
        Task<ProductModelResponse> GetProductByIdAsync(Guid id);
        Task<IEnumerable<ProductModelResponse>> GetAllProductsAsync();
        Task<ProductModelResponse> UpdateProductsAsync(Guid id, ProductModelUpdateRequest r);
        Task<ProductModelResponsePagination?> GetProducts(ProductFilterProduct request);
    }
}