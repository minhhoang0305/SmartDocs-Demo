using api_service.Data;
using Microsoft.EntityFrameworkCore;
using api_service.Services;
using api_service.Controller;
using Microsoft.AspNetCore.Http.Features;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<MinioService>();
builder.Services.AddScoped<DocumentController>();
builder.Services.AddScoped<RabbitmqPublish>();
builder.Services.AddScoped<ChunkUploadService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(
    options =>
    {
        options.MultipartBodyLengthLimit =
            1024L * 1024L * 1024L * 5L;
    });


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();   
app.Run();
