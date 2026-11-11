namespace Dsw2025Tpi.IntegrationTests.Infrastructure;

/// <summary>
/// Las clases marcadas con [Collection("IntegrationTests")] comparten una sola
/// instancia de la fábrica y no se ejecutan en paralelo entre sí.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
