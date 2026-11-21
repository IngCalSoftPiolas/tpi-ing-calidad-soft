using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Application.Services.Interfaces;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Dsw2025Tpi.UnitTests.OrderServiceTests;

public class CreateOrderTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly OrderService _service;

    public CreateOrderTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _productServiceMock = new Mock<IProductService>();
        _service = new OrderService(_repositoryMock.Object, _productServiceMock.Object);
    }


    private static Customer BuildCustomer() =>
        new Customer("cliente@test.com", "Cliente Test", "381-5551234");

    private static Product BuildProduct(string sku, string name, decimal price, int stock, bool isActive = true) =>
        new Product(sku, name, price, stock)
        {
            Description = $"Descripción de {name}",
            IsActive = isActive
        };

    private static OrderModelRequest BuildRequest(Guid customerId, params OrderItemModelRequest[] items) =>
        new OrderModelRequest(
            CustomerId: customerId,
            ShippingAddress: "Calle Falsa 123",
            BillingAdress: "Calle Falsa 123",
            OrderItems: items,
            Notes: "Entregar por la tarde");

    private void SetupCustomer(Customer? customer, Guid id) =>
        _repositoryMock
            .Setup(r => r.GetById<Customer>(id, It.IsAny<string[]>()))
            .ReturnsAsync(customer);

    private void SetupProduct(Product? product, Guid id) =>
        _repositoryMock
            .Setup(r => r.GetById<Product>(id, It.IsAny<string[]>()))
            .ReturnsAsync(product);

    private void SetupPersistence()
    {
        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);

        _repositoryMock
            .Setup(r => r.Update(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);
    }


    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        var customer = BuildCustomer();
        var product1 = BuildProduct("SKU-001", "Mouse Gamer", 100m, 10);
        var product2 = BuildProduct("SKU-002", "Teclado Mecánico", 50.50m, 5);

        SetupCustomer(customer, customer.Id);
        SetupProduct(product1, product1.Id);
        SetupProduct(product2, product2.Id);
        SetupPersistence();

        Order? savedOrder = null;
        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => savedOrder = o)
            .ReturnsAsync((Order o) => o);

        var request = BuildRequest(
            customer.Id,
            new OrderItemModelRequest(product1.Id, 3),   // 3 x 100    = 300
            new OrderItemModelRequest(product2.Id, 2));  // 2 x 50.50  = 101

        // Act
        var result = await _service.CreateOrder(request);

        // Assert - respuesta
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(402m); // suma exacta de subtotales
        result.CustomerId.Should().Be(customer.Id);
        result.CustomerName.Should().Be(customer.Name);
        result.Status.Should().Be("PENDING");
        result.OrderItems.Should().HaveCount(2);
        result.OrderItems.Select(i => i.Subtotal).Should().BeEquivalentTo(new decimal?[] { 300m, 101m });

        // Assert - stock descontado
        product1.StockQuantity.Should().Be(7);
        product2.StockQuantity.Should().Be(3);

        // Assert - orden enviada al repositorio con el total correcto
        savedOrder.Should().NotBeNull();
        savedOrder!.TotalAmount.Should().Be(401m);

        // Assert - interacciones con el repositorio
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _repositoryMock.Verify(r => r.Update(product1), Times.Once);
        _repositoryMock.Verify(r => r.Update(product2), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // Arrange
        var customer = BuildCustomer();
        var product = BuildProduct("SKU-001", "Mouse Gamer", 100m, 2); // stock = 2

        SetupCustomer(customer, customer.Id);
        SetupProduct(product, product.Id);
        SetupPersistence();

        var request = BuildRequest(customer.Id, new OrderItemModelRequest(product.Id, 5)); // pide 5

        // Act
        Func<Task> act = () => _service.CreateOrder(request);

        // Assert
        await act.Should().ThrowAsync<InsufficientStockException>();

        product.StockQuantity.Should().Be(2); // el producto no se altera
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupCustomer(null, customerId); // cliente inexistente
        SetupPersistence();

        var request = BuildRequest(customerId, new OrderItemModelRequest(productId, 1));

        // Act
        Func<Task> act = () => _service.CreateOrder(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Cliente inexistente.");

        // Falla de inmediato: no consulta productos, no actualiza ni crea orden
        _repositoryMock.Verify(r => r.GetById<Product>(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    
}
