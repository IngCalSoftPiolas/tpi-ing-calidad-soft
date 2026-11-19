using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Dsw2025Tpi.UnitTests.ProductServiceTests;

public class GetProductsTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ProductService _service;

    public GetProductsTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ProductService(_repositoryMock.Object);
    }

    private void SetupGetFiltered(IEnumerable<Product> products)
    {
        _repositoryMock
            .Setup(r => r.GetFiltered<Product>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(),
                It.IsAny<string[]>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Product, bool>> predicate, string[] _) =>
            {
                var compiled = predicate.Compile();
                return products.Where(compiled);
            });
    }

    private Product CreateProduct(string sku, string? name, string? internalCode, bool isActive, decimal? price = 100, int? stock = 10)
    {
        return new Product(sku, name, price, stock)
        {
            InternalCode = internalCode,
            IsActive = isActive
        };
    }

    [Fact]
    public async Task GetProducts_WhenStatusEnabled_ReturnsOnlyActiveProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Producto Activo 1", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Producto Activo 2", "INT-002", isActive: true),
            CreateProduct("SKU-003", "Producto Inactivo", "INT-003", isActive: false)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: null,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.ProductItems.Should().AllSatisfy(p => p.status.Should().Be("active"));
        result.ProductItems.Select(p => p.Sku).Should().BeEquivalentTo(new[] { "SKU-001", "SKU-002" });
    }

    [Fact]
    public async Task GetProducts_WhenStatusDisabled_ReturnsOnlyInactiveProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Producto Activo", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Producto Inactivo 1", "INT-002", isActive: false),
            CreateProduct("SKU-003", "Producto Inactivo 2", "INT-003", isActive: false)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "disabled",
            Search: null,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request, includeInactive: true);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.ProductItems.Should().AllSatisfy(p => p.status.Should().Be("disabled"));
    }

    [Fact]
    public async Task GetProducts_WhenStatusNull_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Producto Activo", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Producto Inactivo", "INT-002", isActive: false)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: null,
            Search: null,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request, includeInactive: true);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task GetProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        // Arrange: 15 productos activos
        var products = Enumerable.Range(1, 15)
            .Select(i => CreateProduct($"SKU-{i:D3}", $"Producto {i}", $"INT-{i:D3}", isActive: true))
            .ToList();

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: null,
            PageNumber: 2,
            PageSize: 5
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(5);
        result.TotalItems.Should().Be(15);
        result.ProductItems.Select(p => p.Sku).Should().BeEquivalentTo(new[] { "SKU-006", "SKU-007", "SKU-008", "SKU-009", "SKU-010" });
    }

    [Fact]
    public async Task GetProducts_WithPaginationFirstPage_ReturnsFirstPage()
    {
        // Arrange
        var products = Enumerable.Range(1, 10)
            .Select(i => CreateProduct($"SKU-{i:D3}", $"Producto {i}", $"INT-{i:D3}", isActive: true))
            .ToList();

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: null,
            PageNumber: 1,
            PageSize: 3
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(3);
        result.TotalItems.Should().Be(10);
        result.ProductItems.Select(p => p.Sku).Should().BeEquivalentTo(new[] { "SKU-001", "SKU-002", "SKU-003" });
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersByNameCaseInsensitive()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Mouse Gamer", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Teclado Mecánico", "INT-002", isActive: true),
            CreateProduct("SKU-003", "Mouse Inalámbrico", "INT-003", isActive: true),
            CreateProduct("SKU-004", "Monitor 24\"", "INT-004", isActive: true)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: "mouse",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.ProductItems.Select(p => p.Name).Should().Contain(new[] { "Mouse Gamer", "Mouse Inalámbrico" });
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersBySkuCaseInsensitive()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-MOUSE-01", "Producto A", "INT-001", isActive: true),
            CreateProduct("SKU-TECLADO-01", "Producto B", "INT-002", isActive: true),
            CreateProduct("MOUSE-SKU-02", "Producto C", "INT-003", isActive: true)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: "mouse",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.ProductItems.Select(p => p.Sku).Should().Contain(new[] { "SKU-MOUSE-01", "MOUSE-SKU-02" });
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersByInternalCodeCaseInsensitive()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Producto A", "INT-MOUSE-01", isActive: true),
            CreateProduct("SKU-002", "Producto B", "INT-TECLADO-01", isActive: true),
            CreateProduct("SKU-003", "Producto C", "MOUSE-INT-02", isActive: true)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: "mouse",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(2);
        result.ProductItems.Select(p => p.InternalCode).Should().Contain(new[] { "INT-MOUSE-01", "MOUSE-INT-02" });
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_CombinedWithStatusFilter()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Mouse Gamer", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Mouse Office", "INT-002", isActive: false),
            CreateProduct("SKU-003", "Teclado Mecánico", "INT-003", isActive: true)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: "mouse",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.ProductItems.First().Sku.Should().Be("SKU-001");
        result.ProductItems.First().status.Should().Be("active");
    }

    [Fact]
    public async Task GetProducts_NoMatches_ReturnsEmptyListNotException()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateProduct("SKU-001", "Teclado", "INT-001", isActive: true),
            CreateProduct("SKU-002", "Monitor", "INT-002", isActive: true)
        };

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: "mouse",
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        SetupGetFiltered(new List<Product>());

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: null,
            PageNumber: 1,
            PageSize: 10
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task GetProducts_DefaultPageNumberAndPageSize_UsesDefaults()
    {
        // Arrange
        var products = Enumerable.Range(1, 20)
            .Select(i => CreateProduct($"SKU-{i:D3}", $"Producto {i}", $"INT-{i:D3}", isActive: true))
            .ToList();

        SetupGetFiltered(products);

        var request = new ProductFilterProduct(
            Status: "enabled",
            Search: null,
            PageNumber: null,
            PageSize: null
        );

        // Act
        var result = await _service.GetProducts(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductItems.Should().HaveCount(20);
        result.TotalItems.Should().Be(20);
    }
}