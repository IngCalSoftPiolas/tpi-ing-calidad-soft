using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Enums;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository _repository;
        private readonly IProductService _productService;
        public OrderService(IRepository repository, IProductService productService)
        {
            _repository = repository;
            _productService = productService;
        }

        private async Task<OrderModelResponse> OrderResponseGenerator(Order r)
        {
            OrderItemModelResponse[] orderItemsResponse = r.OrderItems.Select(
                p => new OrderItemModelResponse(
                    p.Product.Name,
                    p.Product.Description,
                    p.Quantity,
                    p.Subtotal
                    )
                ).ToArray();

            var customerName = await _repository.First<Customer>(c => r.CustomerId == c.Id);

            return new OrderModelResponse(
                r.Id,
                r.TotalAmount,
                r.CustomerId,
                customerName?.Name,
                r.ShippingAddress,
                r.BillingAddress,
                r.Notes,
                r.Status.ToString(),
                orderItemsResponse);
        }

        public async Task<OrderModelResponse> CreateOrder(OrderModelRequest r)
        {
            //Verifico que exista el cliente
            var customer = await _repository.GetById<Customer>(r.CustomerId)
                ?? throw new NotFoundException("Cliente inexistente.");

            //Creo una lista de ordenes para añadir las ordenes individualmente
            var orderItems = new List<OrderItem>();

            //Creo una lista de productos para que no haya incongruencias en el stock
            var productsInItems = new List<Product>();
            foreach (var item in r.OrderItems)
            {
                var product = productsInItems.Find(p => p.Id == item.ProductCode);   //Si el elemento existe anteriormente lo saco de productsInItems
                if (product is null)
                {
                    product = await _repository.GetById<Product>(item.ProductCode)        //Si no existe en productsInItems lo obtengo de la database
                    ?? throw new NotFoundException($"Producto Id:{item.ProductCode} no encontrado");
                    if (!product.IsActive) throw new InactiveProductException($"Producto Id:{item.ProductCode} esta inactivo");
                    productsInItems.Add(product);   //Por ultimo añado el item a productsInItems
                }
                //Hacemos validaciones
                if (product.StockQuantity < item.Quantity)
                    throw new InsufficientStockException($"El stock del producto Id:{product.Id} Nombre:{product.Name} no es suficiente.");

                //Disminuyo el stock del producto

                product.StockQuantity -= item.Quantity;
                //Añado el OrderItem
                orderItems.Add(new OrderItem(product, item.Quantity));
            }
            //Creo la orden y la guardo en la database
            var order = new Order(customer, r.ShippingAddress, r.BillingAdress, orderItems);
            order.Notes = r.Notes;
            await _repository.Add<Order>(order);

            //Actualizo el stock de los productos en la database
            foreach (var p in productsInItems)
            {
                await _repository.Update<Product>(p);
            }

            //Hago el return

            OrderItemModelResponse[] orderItemsResponse = order.OrderItems.Select(
                p => new OrderItemModelResponse(
                    p.Product.Name,
                    p.Product.Description,
                    p.Quantity,
                    p.Subtotal
                    )
                ).ToArray();

            return new OrderModelResponse(order.Id,
                order.TotalAmount,
                order.CustomerId,
                customer.Name,
                order.ShippingAddress,
                order.BillingAddress,
                order.Notes,
                order.Status.ToString(),
                orderItemsResponse);
        }

        public async Task<OrderModelResponse> GetOrderById(Guid id)
        {
            var order = await _repository.First<Order>(p => p.Id == id, "OrderItems", "OrderItems.Product")
            ?? throw new NotFoundException("La orden no existe.");

            return await OrderResponseGenerator(order);

        }

        public async Task<IEnumerable<OrderModelResponse>> GetAllOrders()
        {
            var orders = await _repository.GetAll<Order>("OrderItems", "OrderItems.Product")
                ?? throw new NotFoundException("La orden no existe.");

            var responses = new List<OrderModelResponse>();

            foreach (var o in orders)
            {
                var response = await OrderResponseGenerator(o); // 🔥 secuencial → sin error
                responses.Add(response);
            }

            return responses;
        }


        public async Task<OrderModelResponse> UpdateOrderStatus(Guid id, NewOrderStatusModel r)
        {
            if (r is null) throw new ArgumentException("Request vacío.");

            // Parsear y validar nuevo estado
            if (!Enum.TryParse<OrderStatus>(r.NewOrderStatus, true, out var newStatus))
            {
                throw new ArgumentException($"Estado inválido: '{r.NewOrderStatus}'.");
            }

            // Obtener la orden con sus relaciones para devolver el DTO completo
            var order = await _repository.First<Order>(p => p.Id == id, "OrderItems", "OrderItems.Product")
                ?? throw new NotFoundException("La orden no existe.");

            // Validar transición
            if (!order.CanTransitionTo(newStatus))
            {
                throw new ArgumentException($"Transición no permitida de {order.Status} a {newStatus}.");
            }

            // Aplicar cambio y persistir
            order.ChangeStatus(newStatus);
            await _repository.Update<Order>(order);

            return await OrderResponseGenerator(order);
        }

    }
}
