using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Repositories.Interfaces;
using FluentAssertions;
using Moq;

namespace Dsw2025Tpi.UnitTests;

/// <summary>
/// Verifica que el proyecto de pruebas unitarias se descubre y que xUnit, Moq y
/// FluentAssertions están bien referenciados. Las pruebas de negocio van en TP4-02/03/04.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void ProductService_CanBeCreatedWithMockedRepository()
    {
        // Arrange
        var repositoryMock = new Mock<IRepository>();

        // Act
        var service = new ProductService(repositoryMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
