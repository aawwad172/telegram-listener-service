using Service.Template.Application;
using Service.Template.Domain;
using Service.Template.Infrastructure;
using Service.Template.Service;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddDomainServices()
                .AddInfrastructureServices()
                .AddApplicationServices()
                .AddServiceServices(builder.Configuration);

IHost host = builder.Build();
host.Run();
