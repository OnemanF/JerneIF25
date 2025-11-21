// server/api/Etc/OpenApiClientGenExtensions.cs
namespace api.Etc;

public static class OpenApiClientGenExtensions
{
    /// <summary>
    /// Starter calls this. For exam, we no-op (generate later if needed).
    /// </summary>
    public static Task GenerateApiClientsFromOpenApi(this WebApplication app, string outputPath)
        => Task.CompletedTask;
}