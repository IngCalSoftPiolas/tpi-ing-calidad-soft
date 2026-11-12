using System.Net;
using System.Text.Json;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

/// <summary>
/// Pruebas de integración de los listados de productos: contrato JSON público,
/// filtros por query string y acceso administrativo a productos inactivos.
/// </summary>
public class ProductsGetTests
{
    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOkWithPaginatedJsonStructure()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await SeedProductsAsync(factory,
            NewProduct("IT-PUB-001", "Mouse público", isActive: true),
            NewProduct("IT-PUB-002", "Teclado público", isActive: true),
            NewProduct("IT-PUB-003", "Producto inactivo", isActive: false));

        // Act
        var response = await client.GetAsync("/api/products?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ReadPagedProductsAsync(response);
        page.TotalItems.Should().Be(2, "el catálogo público solo expone productos activos");
        page.Skus.Should().BeEquivalentTo(new[] { "IT-PUB-001", "IT-PUB-002" });
        page.Statuses.Should().OnlyContain(status => status == "active");
    }

    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        await SeedProductsAsync(factory,
            NewProduct("IT-SEARCH-001", "Mouse Gamer", isActive: true),
            NewProduct("IT-SEARCH-002", "Mouse Office", isActive: true),
            NewProduct("IT-SEARCH-003", "Teclado Mecánico", isActive: true));

        // Act
        var response = await client.GetAsync("/api/products?search=mouse&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ReadPagedProductsAsync(response);
        page.TotalItems.Should().Be(2);
        page.Skus.Should().BeEquivalentTo(new[] { "IT-SEARCH-001", "IT-SEARCH-002" });
        page.Skus.Should().NotContain("IT-SEARCH-003");
    }

    [Fact]
    public async Task GetAuthProducts_AdminEndpoint_WhenCalledByAdmin_IncludesInactiveProducts()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory();
        var anonymousClient = factory.CreateClient();
        await SeedProductsAsync(factory,
            NewProduct("IT-ADMIN-001", "Producto activo", isActive: true),
            NewProduct("IT-ADMIN-002", "Producto inactivo", isActive: false));

        // Act: sin credenciales el endpoint administrativo debe rechazar el acceso.
        var anonymousResponse = await anonymousClient.GetAsync("/api/products/admin");

        // Assert
        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act: el catálogo público excluye el inactivo.
        var publicResponse = await anonymousClient.GetAsync("/api/products?pageNumber=1&pageSize=10");
        var publicPage = await ReadPagedProductsAsync(publicResponse);

        // Assert
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        publicPage.TotalItems.Should().Be(1);
        publicPage.Skus.Should().BeEquivalentTo(new[] { "IT-ADMIN-001" });

        // Act: un Admin obtiene también los inactivos.
        var adminClient = await factory.CreateAuthenticatedClientAsync("Admin");
        var adminResponse = await adminClient.GetAsync("/api/products/admin?pageNumber=1&pageSize=10");

        // Assert
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminPage = await ReadPagedProductsAsync(adminResponse);
        adminPage.TotalItems.Should().Be(2);
        adminPage.Skus.Should().BeEquivalentTo(new[] { "IT-ADMIN-001", "IT-ADMIN-002" });
        adminPage.Statuses.Should().Contain("disabled");
    }

    private static async Task SeedProductsAsync(
        CustomWebApplicationFactory factory,
        params Product[] products)
    {
        using var scope = factory.CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Set<Product>().AddRange(products);
        await db.SaveChangesAsync();
    }

    private static Product NewProduct(string sku, string name, bool isActive) =>
        new(sku, name, 123.45m, 10)
        {
            InternalCode = $"INT-{sku}",
            Description = $"{name} para pruebas de integración",
            IsActive = isActive
        };

    /// <summary>
    /// Valida el contrato JSON paginado y devuelve los datos necesarios para
    /// verificar cada escenario.
    /// </summary>
    private static async Task<PagedProducts> ReadPagedProductsAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        root.TryGetProperty("productItems", out var items)
            .Should().BeTrue("la respuesta debe usar el campo 'productItems'");
        root.TryGetProperty("totalItems", out var totalItems)
            .Should().BeTrue("la respuesta debe usar el campo 'totalItems'");
        root.TryGetProperty("products", out _)
            .Should().BeFalse("el contrato anterior con 'products' ya no debe exponerse");
        root.TryGetProperty("totalCount", out _)
            .Should().BeFalse("el contrato anterior con 'totalCount' ya no debe exponerse");

        items.ValueKind.Should().Be(JsonValueKind.Array);
        totalItems.ValueKind.Should().Be(JsonValueKind.Number);

        var productElements = items.EnumerateArray().ToList();

        return new PagedProducts(
            Skus: productElements.Select(p => p.GetProperty("sku").GetString()!).ToList(),
            Statuses: productElements.Select(p => p.GetProperty("status").GetString()).ToList(),
            TotalItems: totalItems.GetInt32());
    }

    private sealed record PagedProducts(
        IReadOnlyList<string> Skus,
        IReadOnlyList<string?> Statuses,
        int TotalItems);
}
