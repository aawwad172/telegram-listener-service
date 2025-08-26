using Microsoft.Extensions.DependencyInjection;
using Telegram.Listener.Domain.Interfaces.Infrastructure;
using Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;
using Telegram.Listener.Infrastructure.Persistence;
using Telegram.Listener.Infrastructure.Persistence.Repositories;

namespace Telegram.Listener.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register infrastructure services here
        // Example: services.AddTransient<IMyInfrastructureService, MyInfrastructureService>();
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        services.AddTransient<IDropFolderRepository, DropFolderRepository>();
        services.AddTransient<IMessageRepository, MessageRepository>();
        return services;
    }
}
