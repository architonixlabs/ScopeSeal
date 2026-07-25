using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ScopeSeal.Shared.Configuration;

namespace ScopeSeal.Api.DependencyInjection;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddScopeSealOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var scopeSealOptions = configuration.GetSection(ScopeSealOptions.SectionName).Get<ScopeSealOptions>()
            ?? new ScopeSealOptions();
        var otelOptions = scopeSealOptions.Security.OpenTelemetry;

        if (!otelOptions.Enabled)
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(otelOptions.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("ScopeSeal.Api");

                if (otelOptions.ExportToConsole)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(otelOptions.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelOptions.OtlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (otelOptions.ExportToConsole)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(otelOptions.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otelOptions.OtlpEndpoint);
                    });
                }
            });

        return services;
    }
}
