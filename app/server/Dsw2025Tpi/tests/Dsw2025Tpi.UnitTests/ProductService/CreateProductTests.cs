using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Dsw2025Tpi.UnitTests.ProductServiceTests;

public class CreateProductTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ProductService _service;

    public CreateProductTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ProductService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateProductAsync_ValidRequest_ReturnsProductModelResponse()
    {
        // Arrange
        var request = new ProductModelRequest(
            Sku: "TEST-001",
            InternalCode: "INT-TEST001",
            Name: "Producto Test",
            Description: "Descripción de prueba",
            CurrentUnitPrice: 100.50m,
            StockQuantity: 10
        );

        var createdProduct = new Product(request.Sku, request.Name, request.CurrentUnitPrice, request.StockQuantity)
        {
            InternalCode = request.InternalCode,
            Description = request.Description,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Sku.Should().Be(request.Sku);
        result.Name.Should().Be(request.Name);
        result.InternalCode.Should().Be(request.InternalCode);
        result.Description.Should().Be(request.Description);
        result.CurrentUnitPrice.Should().Be(request.CurrentUnitPrice);
        result.StockQuantity.Should().Be(request.StockQuantity);
        result.status.Should().Be("active");

        _repositoryMock.Verify(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateSku_ThrowsDuplicatedItemException()
    {
        // Arrange
        var request = new ProductModelRequest(
            Sku: "EXISTING-001",
            InternalCode: "INT-EXIST001",
            Name: "Producto Existente",
            Description: "Descripción",
            CurrentUnitPrice: 50.00m,
            StockQuantity: 5
        );

        var existingProduct = new Product(request.Sku, "Otro Nombre", 10m, 1);

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(existingProduct);

        // Act
        var act = async () => await _service.CreateProductAsync(request);

        // Assert
        await act.Should().ThrowAsync<DuplicatedItemException>()
            .WithMessage("Un producto con ese SKU ya existe.");

        _repositoryMock.Verify(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Once);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_EmptySku_CreatesProductWithEmptySku()
    {
        // Arrange: El service no valida el SKU, eso lo hace el controller
        var request = new ProductModelRequest(
            Sku: "",
            InternalCode: "INT-TEST001",
            Name: "Producto Test",
            Description: "Descripción",
            CurrentUnitPrice: 100.50m,
            StockQuantity: 10
        );

        var createdProduct = new Product(request.Sku, request.Name, request.CurrentUnitPrice, request.StockQuantity)
        {
            InternalCode = request.InternalCode,
            Description = request.Description,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert: El service crea el producto sin validar (la validación es en el controller)
        result.Should().NotBeNull();
        result.Sku.Should().Be("");
    }

    [Fact]
    public async Task CreateProductAsync_NegativePrice_CreatesProductWithNegativePrice()
    {
        // Arrange: El service no valida el precio, eso lo hace el controller
        var request = new ProductModelRequest(
            Sku: "TEST-002",
            InternalCode: "INT-TEST002",
            Name: "Producto Test",
            Description: "Descripción",
            CurrentUnitPrice: -10m,
            StockQuantity: 10
        );

        var createdProduct = new Product(request.Sku, request.Name, request.CurrentUnitPrice, request.StockQuantity)
        {
            InternalCode = request.InternalCode,
            Description = request.Description,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert: El service crea el producto sin validar (la validación es en el controller)
        result.Should().NotBeNull();
        result.CurrentUnitPrice.Should().Be(-10m);
    }

    [Fact]
    public async Task CreateProductAsync_NegativeStock_CreatesProductWithNegativeStock()
    {
        // Arrange: El service no valida el stock, eso lo hace el controller
        var request = new ProductModelRequest(
            Sku: "TEST-003",
            InternalCode: "INT-TEST003",
            Name: "Producto Test",
            Description: "Descripción",
            CurrentUnitPrice: 100.50m,
            StockQuantity: -5
        );

        var createdProduct = new Product(request.Sku, request.Name, request.CurrentUnitPrice, request.StockQuantity)
        {
            InternalCode = request.InternalCode,
            Description = request.Description,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert: El service crea el producto sin validar (la validación es en el controller)
        result.Should().NotBeNull();
        result.StockQuantity.Should().Be(-5);
    }

    [Fact]
    public async Task CreateProductAsync_MissingOptionalFields_ProductIsInactive()
    {
        // Arrange
        var request = new ProductModelRequest(
            Sku: "TEST-004",
            InternalCode: null,
            Name: null,
            Description: null,
            CurrentUnitPrice: null,
            StockQuantity: null
        );

        var createdProduct = new Product(request.Sku, request.Name, request.CurrentUnitPrice, request.StockQuantity)
        {
            InternalCode = request.InternalCode,
            Description = request.Description,
            IsActive = false
        };

        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.status.Should().Be("disabled");
    }
}