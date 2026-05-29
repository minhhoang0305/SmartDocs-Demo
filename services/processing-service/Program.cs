using processing_service.Consumer;
using processing_service.Services;
using processing_service.gRPC;
using RabbitMQ.Client;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;
using StackExchange.Redis;
using processing_service.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    var serviceName = "processing-service";
    var environmentName = context.HostingEnvironment.EnvironmentName;
    var lokiUri = context.Configuration["Loki:Uri"] ?? "http://loki:3100";

    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("service", serviceName)
        .Enrich.WithProperty("environment", environmentName)
        .WriteTo.Console()
        .WriteTo.GrafanaLoki(
            lokiUri,
            labels:
            [
                new LokiLabel { Key = "service", Value = serviceName },
                new LokiLabel { Key = "environment", Value = environmentName }
            ]);
});

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = "rabbitmq",
        DispatchConsumersAsync = true
    };

    return factory.CreateConnection();
});

builder.Services.Configure<RedisOption>(builder.Configuration.GetSection(RedisOption.SectionName));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration;
    var connectionString = configuration["Redis:ConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Redis connection string is not configured");
    }
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddOpenApi();
builder.Services.AddHostedService<DocumentUploadConsumer>();
builder.Services.AddScoped<RedisService>();
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGrpcService<DocumentGrpcService>();

app.Run();
