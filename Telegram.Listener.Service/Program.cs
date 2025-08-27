using Telegram.Listener.Application;
using Telegram.Listener.Domain;
using Telegram.Listener.Infrastructure;
using Telegram.Listener.Service;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDomainServices()
                .AddInfrastructureServices()
                .AddApplicationServices()
                .AddServiceServices(builder.Configuration);

IHost host = builder.Build();
host.Run();
