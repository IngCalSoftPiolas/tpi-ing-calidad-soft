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
    public async Task AddProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
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
    public async Task AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException()
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
    public async Task AddProduct_WithEmptySku_CreatesProductWithEmptySku()
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

    [Theory]
    [InlineData(0, 100.0)]   // stock en cero
    [InlineData(-5, 100.0)]  // stock negativo
    [InlineData(10, -10.0)]  // precio negativo
    public async Task AddProduct_WithInvalidStockOrPrice_ThrowsInvalidDataException(int stock, double price)
    {
        // Arrange
        var request = new ProductModelRequest(
            Sku: "TEST-INV",
            InternalCode: "INT-INV",
            Name: "Producto Invalido",
            Description: "Descripción",
            CurrentUnitPrice: (decimal)price,
            StockQuantity: stock
        );

        // Act
        var act = async () => await _service.CreateProductAsync(request);

        // Assert: se lanza el error con su mensaje antes de usar el repositorio
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.Which.Message.Should().NotBeNullOrWhiteSpace();

        _repositoryMock.Verify(r => r.First<Product>(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<string[]>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _repositoryMock.VerifyNoOtherCalls();
    }
}
