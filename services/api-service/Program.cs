using api_service.Data;
using Microsoft.EntityFrameworkCore;
using api_service.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.OpenApi;
using api_service.Interface;
using api_service.Middleware;
using api_service.Options;
using RabbitMQ.Client;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;
using System.Diagnostics;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    var serviceName = "api-service";
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

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration;
    var connectionString = configuration["Redis:ConnectionString"];
    if(string.IsNullOrWhiteSpace(connectionString)) 
    {
        throw new InvalidOperationException("Redis connection string is not configured");
    }
    return ConnectionMultiplexer.Connect(connectionString);
});

var vaultAddress = builder.Configuration["Vault:Address"];
var vaultToken = builder.Configuration["Vault:Token"];

if (!string.IsNullOrWhiteSpace(vaultAddress) && !string.IsNullOrWhiteSpace(vaultToken))
{
    try
    {
        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        var jwtSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync("smartdocs/jwt", mountPoint: "secret");
        var minioSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync("smartdocs/minio", mountPoint: "secret");
        var dbSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync("smartdocs/database", mountPoint: "secret");

        string? GetJwtSecretValue(string key) =>
            jwtSecret.Data.Data.TryGetValue(key, out var value) ? value?.ToString() : null;

        string? GetMinioSecretValue(string key) =>
            minioSecret.Data.Data.TryGetValue(key, out var value) ? value?.ToString() : null;

        string? GetDbSecretValue(string key) =>
            dbSecret.Data.Data.TryGetValue(key, out var value) ? value?.ToString() : null;

        var minioAccessKey = GetMinioSecretValue("AccessKey");
        if (!string.IsNullOrWhiteSpace(minioAccessKey))
            builder.Configuration["Minio:AccessKey"] = minioAccessKey;

        var minioSecretKey = GetMinioSecretValue("SecretKey");
        if (!string.IsNullOrWhiteSpace(minioSecretKey))
            builder.Configuration["Minio:SecretKey"] = minioSecretKey;

        var dbPassword = GetDbSecretValue("Password");
        if (!string.IsNullOrWhiteSpace(dbPassword))
        {
            builder.Configuration["ConnectionStrings:DefaultConnection"] =
                $"Host=postgres;Port=5432;Database=smartdocs;Username=postgres;Password={dbPassword}";
        }

        var jwtPrivateKeyPem = GetJwtSecretValue("PrivateKeyPem");
        if (!string.IsNullOrWhiteSpace(jwtPrivateKeyPem))
            builder.Configuration["Jwt:PrivateKeyPem"] = jwtPrivateKeyPem;

        var jwtPublicKeyPem = GetJwtSecretValue("PublicKeyPem");
        if (!string.IsNullOrWhiteSpace(jwtPublicKeyPem))
            builder.Configuration["Jwt:PublicKeyPem"] = jwtPublicKeyPem;

        var jwtPrivateKeyBase64 = GetJwtSecretValue("PrivateKeyBase64");
        if (!string.IsNullOrWhiteSpace(jwtPrivateKeyBase64))
            builder.Configuration["Jwt:PrivateKeyBase64"] = jwtPrivateKeyBase64;

        var jwtPublicKeyBase64 = GetJwtSecretValue("PublicKeyBase64");
        if (!string.IsNullOrWhiteSpace(jwtPublicKeyBase64))
            builder.Configuration["Jwt:PublicKeyBase64"] = jwtPublicKeyBase64;
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Vault secrets could not be loaded. Falling back to configured values. Error: {ErrorMessage}", ex.Message);
    }
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options =>
            JwtRsaKeyReader.HasPrivateKey(options)
            && JwtRsaKeyReader.HasPublicKey(options)
            && JwtRsaKeyReader.CanCreatePrivateKey(options)
            && JwtRsaKeyReader.CanCreatePublicKey(options)
            && !string.IsNullOrWhiteSpace(options.Issuer)
            && !string.IsNullOrWhiteSpace(options.Audience)
            && options.Expireminutes > 0
            && options.RefreshTokenExpireDays > 0,
        "Jwt configuration is invalid")
    .ValidateOnStart();
builder.Services.AddSingleton<RsaSecurityKey>(sp =>
{
    var options = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
    return JwtRsaKeyReader.CreatePublicKey(options);
});

builder.Services
    .AddOptions<MinioOptions>()
    .Bind(builder.Configuration.GetSection(MinioOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Endpoint)
            && !string.IsNullOrWhiteSpace(options.AccessKey)
            && !string.IsNullOrWhiteSpace(options.SecretKey)
            && !string.IsNullOrWhiteSpace(options.BucketName),
        "Minio configuration is invalid")
    .ValidateOnStart();
builder.Services
    .AddOptions<ChunkUploadOptions>()
    .Bind(builder.Configuration.GetSection(ChunkUploadOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory(){HostName = "rabbitmq"};
    return factory.CreateConnection();
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập 'Bearer' token của bạn."
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();
builder.Services.ConfigureOptions<JwtBearerOptionsSetup>();

builder.Services.AddAuthorization(
    options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    }
);

builder.Services.AddScoped<MinioService>();
builder.Services.AddScoped<IMessagePublisher, RabbitmqPublish>();
builder.Services.AddScoped<ChunkUploadService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshService, RefreshService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(
    options =>
    {
        options.MultipartBodyLengthLimit =
            1024L * 1024L * 1024L * 5L;
    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(), 
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");

        return new ValueTask();
    };
});

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.ConfigureExceptionHandler();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<TraceIdMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();   
app.Run();
