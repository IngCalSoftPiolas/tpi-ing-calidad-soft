using System.Net;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

/// <summary>
/// Verifica que la API levanta en memoria contra SQLite. Las pruebas de los
/// controladores van en TP4-05/06/07.
/// </summary>
[Collection("IntegrationTests")]
public class SmokeTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_WhenApiStarts_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthcheck");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Database_InTestingEnvironment_UsesSqliteWithRolesCreated()
    {
        // Arrange
        _factory.CreateClient(); // fuerza el arranque de la API
        using var scope = _factory.CreateDbScope();
        var business = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthenticateContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Act
        var roles = await roleManager.Roles.Select(r => r.Name).ToListAsync();

        // Assert
        business.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        auth.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        roles.Should().BeEquivalentTo(new[] { "Admin", "Client", "Master" });
        (await business.Set<Product>().AnyAsync()).Should().BeFalse("en Testing no corre el seed de products.json");
    }
}
