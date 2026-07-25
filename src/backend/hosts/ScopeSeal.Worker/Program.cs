using Microsoft.Extensions.DependencyInjection;
using ScopeSeal.Extraction.Services;
using ScopeSeal.Infrastructure.DependencyInjection;
using ScopeSeal.Infrastructure.Services;
using ScopeSeal.Shared.DependencyInjection;
using ScopeSeal.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScopeSealShared(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is required for the worker host.");
}

builder.Services.AddScopeSealInfrastructure(connectionString, builder.Environment);
builder.Services.AddHostedService<ProcessingJobWorker>();

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

var host = builder.Build();
host.Run();
