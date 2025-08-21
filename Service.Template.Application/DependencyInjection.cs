using Microsoft.Extensions.DependencyInjection;

namespace Service.Template.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        // Example: services.AddTransient<IMyService, MyService>();
        return services;
    }
}
