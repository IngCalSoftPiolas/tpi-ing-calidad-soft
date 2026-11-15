using System.Net.Http.Headers;
using Dsw2025Tpi.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Siembra usuarios de Identity con el rol indicado y genera un JWT usando la
/// misma configuración (clave, issuer y audience) con la que arranca la API.
/// </summary>
public static class AuthenticationTestHelper
{
    public const string Password = "Integration123!";

    public static async Task<string> CreateTokenAsync(this CustomWebApplicationFactory factory, string role)
    {
        // Fuerza el arranque de la API para que Program.cs cree los roles.
        factory.CreateClient();

        using var scope = factory.CreateDbScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            EnsureSucceeded(roleResult, $"crear el rol '{role}'");
        }

        var suffix = Guid.NewGuid().ToString("N");
        var user = new IdentityUser
        {
            UserName = $"integration-{role.ToLowerInvariant()}-{suffix}",
            Email = $"integration-{role.ToLowerInvariant()}-{suffix}@example.com"
        };

        var createResult = await userManager.CreateAsync(user, Password);
        EnsureSucceeded(createResult, $"crear el usuario de prueba con rol '{role}'");

        var assignRoleResult = await userManager.AddToRoleAsync(user, role);
        EnsureSucceeded(assignRoleResult, $"asignar el rol '{role}' al usuario de prueba");

        return await jwtTokenService.GenerateToken(user);
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory,
        string role)
    {
        var token = await factory.CreateTokenAsync(role);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"No se pudo {action}: {errors}");
        }
    }
}
