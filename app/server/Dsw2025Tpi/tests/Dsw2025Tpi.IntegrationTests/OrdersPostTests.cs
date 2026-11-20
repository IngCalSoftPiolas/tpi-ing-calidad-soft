using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

/// <summary>
/// Pruebas de integración de POST /api/orders contra SQLite: autorización por rol,
/// persistencia de la orden con descuento de stock y error JSON por stock insuficiente.
/// </summary>
public class OrdersPostTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Endpoint = "/api/orders";
    private const string ClientRole = "Client";
    private readonly CustomWebApplicationFactory _factory;

    public OrdersPostTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = NewRequest(Guid.NewGuid(), Guid.NewGuid(), 1);

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WhenAuthenticatedAsAdmin_ReturnsForbidden()
    {
        // Arrange: el endpoint admite Master y Client, pero no Admin.
        var customer = NewCustomer();
        var product = NewProduct(10);
        await SeedAsync(customer, product);

        var adminClient = await _factory.CreateAuthenticatedClientAsync("Admin");
        var request = NewRequest(customer.Id, product.Id, 1);

        // Act
        var response = await adminClient.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        (await db.Set<Order>().CountAsync(o => o.CustomerId == customer.Id))
            .Should().Be(0, "una petición rechazada no debe persistir la orden");
    }

    [Fact]
    public async Task CreateOrder_WhenAuthenticatedWithValidData_ReturnsCreatedAndPersistsOrderAndStockInSqlite()
    {
        // Arrange: el entorno Testing no ejecuta el seed de JSON, así que se siembra a mano.
        const int initialStock = 10;
        const int quantity = 3;
        const decimal unitPrice = 123.45m;

        var customer = NewCustomer();
        var product = NewProduct(initialStock, unitPrice);
        await SeedAsync(customer, product);

        var client = await _factory.CreateAuthenticatedClientAsync(ClientRole);
        var request = NewRequest(customer.Id, product.Id, quantity);

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert: contrato de la respuesta 201.
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<OrderModelResponse>();
        created.Should().NotBeNull();
        created!.OrderId.Should().NotBe(Guid.Empty);
        created.CustomerId.Should().Be(customer.Id);
        created.CustomerName.Should().Be(customer.Name);
        created.ShippingAddress.Should().Be(request.ShippingAddress);
        created.BillingAddress.Should().Be(request.BillingAdress);
        created.Notes.Should().Be(request.Notes);
        created.Status.Should().Be("PENDING");
        created.TotalAmount.Should().Be(unitPrice * quantity);
        created.OrderItems.Should().ContainSingle();
        created.OrderItems[0].Quantity.Should().Be(quantity);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().EndWith(created.OrderId.ToString(), "CreatedAtAction debe apuntar a la orden creada");

        // Assert: la orden y el descuento de stock quedan persistidos en SQLite.
        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");

        var persistedOrder = await db.Set<Order>().SingleOrDefaultAsync(o => o.Id == created.OrderId);
        persistedOrder.Should().NotBeNull();
        persistedOrder!.CustomerId.Should().Be(customer.Id);
        persistedOrder.ShippingAddress.Should().Be(request.ShippingAddress);
        (await db.Set<OrderItem>().CountAsync(i => i.OrderId == persistedOrder.Id))
            .Should().Be(1, "la orden debe conservar el item comprado");

        var persistedProduct = await db.Set<Product>().SingleAsync(p => p.Id == product.Id);
        persistedProduct.StockQuantity.Should().Be(
            initialStock - quantity,
            "el stock debe descontarse en la base de datos");
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequestWithErrorMessage()
    {
        // Arrange
        const int initialStock = 2;
        var customer = NewCustomer();
        var product = NewProduct(initialStock);
        await SeedAsync(customer, product);

        var client = await _factory.CreateAuthenticatedClientAsync(ClientRole);
        var request = NewRequest(customer.Id, product.Id, initialStock + 1);

        // Act
        var response = await client.PostAsJsonAsync(Endpoint, request);

        // Assert: el error de falta de stock se informa como JSON.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = body.RootElement.GetProperty("error").GetString();
        error.Should().NotBeNullOrWhiteSpace();
        error.Should().Contain("stock", "el mensaje debe indicar claramente la falta de stock");
        error.Should().Contain(product.Name!);

        // Assert: ante el error no se persiste nada ni se modifica el stock.
        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        (await db.Set<Order>().CountAsync(o => o.CustomerId == customer.Id))
            .Should().Be(0, "una orden con stock insuficiente no debe persistirse");
        (await db.Set<OrderItem>().CountAsync(i => i.ProductId == product.Id))
            .Should().Be(0, "no deben persistirse items de una orden rechazada");
        (await db.Set<Product>().SingleAsync(p => p.Id == product.Id)).StockQuantity
            .Should().Be(initialStock, "el stock no debe modificarse cuando la orden se rechaza");
    }

    private async Task SeedAsync(Customer customer, Product product)
    {
        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Set<Customer>().Add(customer);
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync();
    }

    private static Customer NewCustomer() =>
        new($"cliente-{Guid.NewGuid():N}@example.com", "Cliente de integración", "3815555555");

    private static Product NewProduct(int stock, decimal price = 99.99m) =>
        new($"ORD-{Guid.NewGuid():N}", "Producto de integración", price, stock)
        {
            InternalCode = "INT-ORD-001",
            Description = "Producto usado por las pruebas de integración de órdenes"
        };

    private static OrderModelRequest NewRequest(Guid customerId, Guid productId, int quantity) =>
        new(
            CustomerId: customerId,
            ShippingAddress: "Av. de envío 123",
            BillingAdress: "Av. de facturación 456",
            OrderItems: new[] { new OrderItemModelRequest(ProductCode: productId, Quantity: quantity) },
            Notes: "Orden creada por una prueba de integración");
}
