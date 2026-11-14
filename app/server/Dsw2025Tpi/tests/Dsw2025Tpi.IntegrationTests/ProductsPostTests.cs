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
/// Pruebas de integración de POST /api/products: autorización por rol,
/// persistencia real en SQLite y conflicto por SKU duplicado.
/// </summary>
public class ProductsPostTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Endpoint = "/api/products";
    private readonly CustomWebApplicationFactory _factory;

    public ProductsPostTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddProduct_WhenUnauthenticatedOrNotAdmin_ReturnsUnauthorizedOrForbidden()
    {
        // Arrange
        var anonymousClient = _factory.CreateClient();
        var clientClient = await _factory.CreateAuthenticatedClientAsync("Client");
        var request = NewValidRequest();

        // Act
        var anonymousResponse = await anonymousClient.PostAsJsonAsync(Endpoint, request);
        var clientResponse = await clientClient.PostAsJsonAsync(Endpoint, request);

        // Assert
        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        clientResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        (await db.Set<Product>().AnyAsync(p => p.Sku == request.Sku))
            .Should().BeFalse("las peticiones rechazadas no deben persistir el producto");
    }

    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        // Arrange
        var adminClient = await _factory.CreateAuthenticatedClientAsync("Admin");
        var request = new ProductModelRequest(
            Sku: NewSku(),
            InternalCode: "INT-IT-001",
            Name: "Producto de integración",
            Description: "Creado por una prueba de integración",
            CurrentUnitPrice: 1234.56m,
            StockQuantity: 42);

        // Act
        var response = await adminClient.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ProductModelResponse>();
        created.Should().NotBeNull();
        created!.ProductId.Should().NotBe(Guid.Empty);
        created.Sku.Should().Be(request.Sku);
        created.Name.Should().Be(request.Name);
        created.InternalCode.Should().Be(request.InternalCode);
        created.Description.Should().Be(request.Description);
        created.CurrentUnitPrice.Should().Be(request.CurrentUnitPrice);
        created.StockQuantity.Should().Be(request.StockQuantity);
        created.status.Should().Be("active");

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().EndWith(created.ProductId.ToString(), "CreatedAtAction debe apuntar al producto creado");

        using var scope = _factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");

        var persisted = await db.Set<Product>().SingleOrDefaultAsync(p => p.Sku == request.Sku);
        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(created.ProductId);
        persisted.Name.Should().Be(request.Name);
        persisted.InternalCode.Should().Be(request.InternalCode);
        persisted.Description.Should().Be(request.Description);
        persisted.CurrentUnitPrice.Should().Be(request.CurrentUnitPrice);
        persisted.StockQuantity.Should().Be(request.StockQuantity);
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExistsInDatabase_ReturnsConflictStatusCode()
    {
        // Arrange: el producto existente se inserta directamente en SQLite.
        var sku = NewSku();
        using (var seedScope = _factory.CreateDbScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
            db.Set<Product>().Add(new Product(sku, "Producto existente", 999.99m, 10)
            {
                InternalCode = "INT-EXIST",
                Description = "Insertado antes de la petición"
            });
            await db.SaveChangesAsync();
        }

        var adminClient = await _factory.CreateAuthenticatedClientAsync("Admin");
        var request = NewValidRequest(sku);

        // Act
        var response = await adminClient.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("error").GetString().Should().Contain("SKU");

        using var scope = _factory.CreateDbScope();
        var verificationDb = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        (await verificationDb.Set<Product>().CountAsync(p => p.Sku == sku))
            .Should().Be(1, "el intento duplicado no debe insertar otra fila");
    }

    private static ProductModelRequest NewValidRequest(string? sku = null) =>
        new(
            Sku: sku ?? NewSku(),
            InternalCode: "INT-IT-002",
            Name: "Producto de integración",
            Description: "Payload válido para pruebas de integración",
            CurrentUnitPrice: 250.75m,
            StockQuantity: 8);

    private static string NewSku() => $"IT-{Guid.NewGuid():N}";
}
