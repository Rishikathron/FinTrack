using FinTrack.AI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.AI.Extensions;

/// <summary>
/// Extension methods to register FinTrack AI services into the DI container.
/// Called from the main FinTrack API project's Program.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ChatService (Semantic Kernel + plugins) as a scoped service.
    /// Requires SemanticKernel:ApiKey and SemanticKernel:ModelId in configuration.
    /// </summary>
    public static IServiceCollection AddFinTrackAI(this IServiceCollection services)
    {
        services.AddScoped<ChatService>();
        return services;
    }
}
