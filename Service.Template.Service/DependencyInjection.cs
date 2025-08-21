using A2ASerilog;
using Microsoft.Extensions.Configuration;
using Service.Template.Domain.Settings;

namespace Service.Template.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register your service dependencies here
        // Example: services.AddScoped<IMyService, MyService>();

        services.Configure<DbSettings>(configuration.GetSection(nameof(DbSettings)));
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));

        AppSettings? appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();

        LoggerService._logPath = appSettings!.LogPath;
        LoggerService._flushPeriod = appSettings.LogFlushInterval;

        return services;
    }
}
