using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PB_Clientes.Application.Commands.CreateCliente;
using PB_Clientes.Application.Interfaces;
using PB_Clientes.Application.Repositories;
using PB_Clientes.Application.Services;
using PB_Clientes.Domain.Repositories;
using PB_Clientes.Infrastructure.Data;
using PB_Clientes.Infrastructure.Outbox;
using PB_Clientes.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "PB_Clientes.Api";
var serviceVersion = "1.0.0";
var configuration = builder.Configuration;

// OpenTelemetry - Tracing and Metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("PB_Clientes.Api")
            .AddSource("PB_Clientes.Api.Outbox")
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1.0))) 
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(configuration["OpenTelemetry:OtlpEndpoint"]?? "");
            });
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

// OpenTelemetry logs
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(configuration["OpenTelemetry:OtlpEndpoint"] ?? "");
        otlpOptions.Protocol = OtlpExportProtocol.Grpc;
    });
});

// DbContext
builder.Services.AddDbContext<ClientesDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("ClientesDb")));

// Dependency Injection - Repositories
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

//MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateClienteHandler).Assembly));

// MassTransit (RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["RabbitMQ:Host"], 5672, "/", h =>
        {
            h.Username(configuration["RabbitMQ:Username"] ?? "");
            h.Password(configuration["RabbitMQ:Password"] ?? "");
        });
    });
});

// Outbox dispatcher hosted service
builder.Services.AddHostedService<OutboxDispatcher>();

// Swagger e Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
