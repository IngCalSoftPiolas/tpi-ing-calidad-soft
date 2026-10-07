using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services.Interfaces;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IRepository _repository;
        public ProductService(IRepository repository)
        {
            _repository = repository;
        }  
        //Creacion de productos
        private Product ProductGenerator(ProductModelRequest r)
        {
            var product = new Product(r.Sku, r.Name, r.CurrentUnitPrice, r.StockQuantity);
            //Completamos los datos del producto de ser necesario
            if (!string.IsNullOrEmpty(r.InternalCode)) product.InternalCode = r.InternalCode;
            if (!string.IsNullOrEmpty(r.Description)) product.Description = r.Description;
            //Retorno el producto completo
            return product;
        }
        private ProductModelResponse ResponseGenerator(Product p)
        {
            var status = "disabled";
            if (p.IsActive)
            {
                status = "active";
            }

            return new ProductModelResponse(
                p.Id,
                p.Sku,
                p.Name,
                p.InternalCode,
                p.Description,
                p.CurrentUnitPrice,
                p.StockQuantity,
                status
                );
        }

        //Adicion de productos
        public async Task<ProductModelResponse> CreateProductAsync(ProductModelRequest r)
        {
            var product = ProductGenerator(r);
            //Revisamos que no haya un producto con el mismo sku antes
            if (! (await _repository.First<Product>(p =>p.Sku == product.Sku) is null))
                throw new DuplicatedItemException("Un producto con ese SKU ya existe.");
            //Añadimos el producto a la base de datos
            var response = await _repository.Add(product);

            //Hacemos el retorno del response
            return ResponseGenerator(response);
        }

        //Obtencion de productos disponibles
        public async Task<IEnumerable<ProductModelResponse>> GetAllProductsAsync()
        {
            //Hago el return de todos los productos activos

            var productos = await _repository.
                GetFiltered<Product>(p => p.IsActive)
                ?? throw new NotFoundException("No hay productos activos.");
            return productos.Select(p => ResponseGenerator(p));
        }
        public async Task<ProductModelResponse> UpdateProductsAsync(Guid id, ProductModelUpdateRequest r)
        {
            //Obtengo el producto a modificar de la base de datos
            var product = await _repository.GetById<Product>(id)
                ?? throw new NotFoundException("No existe el producto.");

            if (!string.IsNullOrEmpty(r.Sku)) product.Sku = r.Sku;
            if (!string.IsNullOrEmpty(r.Name)) product.Name = r.Name;
            if (!string.IsNullOrEmpty(r.InternalCode)) product.InternalCode = r.InternalCode;
            if (!string.IsNullOrEmpty(r.Description)) product.Description = r.Description;
            if (r.CurrentUnitPrice.HasValue) product.CurrentUnitPrice = r.CurrentUnitPrice;
            if (r.StockQuantity.HasValue) product.StockQuantity = r.StockQuantity;

            var response = await _repository.Update(product);
            return ResponseGenerator(response);
        }
        public async Task<ProductModelResponse> GetProductByIdAsync(Guid id)
        {
            var product = await _repository.GetById<Product>(id) ?? throw new NotFoundException("Producto Inexistente.");
            return ResponseGenerator(product);
        }
        public async Task DisableProductById(Guid id)
        {
            var product = await _repository.GetById<Product>(id) ?? throw new NotFoundException("Producto Inexistente.");
            product.IsActive = false;
            await _repository.Update(product);
        }

        public async Task<ProductModelResponsePagination?> GetProducts(ProductFilterProduct request)
        {
            var isActive = request.Status == "enabled"
                ? (bool?)true
                : request.Status == "disabled"
                    ? (bool?)false
                    : null;

            var activeProducts = await _repository.GetFiltered<Product>(
                p => (isActive == null || p.IsActive == isActive)
                && (string.IsNullOrEmpty(request.Search) || p.Name.Contains(request.Search))
            );
                if (activeProducts is null || !activeProducts.Any()) {
                throw new NotFoundException("No hay productos que coincidan con el filtro.");
            }
                var products = activeProducts.Select(p => ResponseGenerator(p))
                .OrderBy(p => p.Sku)
                .Skip((request.PageNumber - 1) * request.PageSize ?? 0)
                .Take(request.PageSize ?? activeProducts.Count())
                ;
            return new ProductModelResponsePagination(products.ToList(), activeProducts.Count());
        }

    }
}
