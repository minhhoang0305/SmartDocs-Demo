using processing_service.Consumer;
using processing_service.Services;
using processing_service.gRPC;
using RabbitMQ.Client;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

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

builder.Services.AddOpenApi();
builder.Services.AddHostedService<DocumentUploadConsumer>();
builder.Services.AddScoped<RedisService>();
builder.Services.AddGrpc();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = "rabbitmq",
        DispatchConsumersAsync = true
    };

    return factory.CreateConnection();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};
app.MapGrpcService<DocumentGrpcService>();

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
