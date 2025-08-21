using Microsoft.Extensions.DependencyInjection;

namespace Service.Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register infrastructure services here
        // Example: services.AddTransient<IMyInfrastructureService, MyInfrastructureService>();
        return services;
    }
}
