using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Listener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register infrastructure services here
        // Example: services.AddTransient<IMyInfrastructureService, MyInfrastructureService>();
        return services;
    }
}
