using Dsw2025Tpi.Api;
using Dsw2025Tpi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta la API en memoria con el entorno "Testing" y reemplaza SQL Server
/// por una base SQLite en memoria compartida entre ambos contextos.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "clave-de-pruebas-de-integracion-con-mas-de-64-caracteres-0123456789";
    public const string JwtIssuer = "Dsw2025Tpi.Api";
    public const string JwtAudience = "Dsw2025Tpi.Api";

    // Cada fábrica usa su propia base en memoria; la conexión abierta la mantiene viva.
    private readonly SqliteConnection _connection =
        new($"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting hace que Program.cs vea estos valores antes de builder.Build()
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:ExpireInMinutes", "60");

        builder.ConfigureServices(services =>
        {
            ReplaceWithSqlite<Dsw2025TpiContext>(services);
            ReplaceWithSqlite<AuthenticateContext>(services);

            // El esquema tiene que existir antes de que Program.cs cree los roles al iniciar
            CreateSchema();
        });
    }

    private void ReplaceWithSqlite<TContext>(IServiceCollection services) where TContext : DbContext
    {
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();
        services.AddDbContext<TContext>(options => options.UseSqlite(_connection));
    }

    private void CreateSchema()
    {
        using var business = new Dsw2025TpiContext(
            new DbContextOptionsBuilder<Dsw2025TpiContext>().UseSqlite(_connection).Options);
        business.Database.EnsureCreated();

        // EnsureCreated no crea nada si la base ya tiene tablas, así que el segundo
        // contexto crea las suyas explícitamente.
        using var auth = new AuthenticateContext(
            new DbContextOptionsBuilder<AuthenticateContext>().UseSqlite(_connection).Options);
        var creator = auth.GetService<IRelationalDatabaseCreator>();
        try
        {
            creator.CreateTables();
        }
        catch (SqliteException ex) when (ex.Message.Contains("already exists"))
        {
            // Las tablas de Identity ya existen (la fábrica se configuró más de una vez)
        }
    }

    /// <summary>Crea un scope para consultar o sembrar la base SQLite desde las pruebas.</summary>
    public IServiceScope CreateDbScope() => Services.CreateScope();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
