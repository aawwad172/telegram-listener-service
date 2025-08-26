using Microsoft.Extensions.DependencyInjection;
using Telegram.Listener.Application.Services;
using Telegram.Listener.Application.Utilities;
using Telegram.Listener.Domain.Interfaces.Application;

namespace Telegram.Listener.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        // Example: services.AddTransient<IMyService, MyService>();
        services.AddTransient<IQueuedMessagesService, QueuedMessagesService>();
        MapsterConfigurations.RegisterMappings();
        return services;
    }
}
