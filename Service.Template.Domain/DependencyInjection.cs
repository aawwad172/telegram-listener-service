using Microsoft.Extensions.DependencyInjection;

namespace Service.Template.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register domain services here
        // Example: services.AddTransient<IMyDomainService, MyDomainService>();
        return services;
    }
}
