using ScopeSeal.Shared.DependencyInjection;
using ScopeSeal.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScopeSealShared(builder.Configuration);

builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

var host = builder.Build();
host.Run();
