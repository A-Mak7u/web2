using System.Text.Json;
using System.Text.Json.Serialization;
using CatalogService.Api.Data;
using CatalogService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using CatalogService.Api.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    // открытый CORS для локальной отладки
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // избегаем циклических ссылок при сериализации
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<TraceStore>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    // кэш каталога хранится в Redis
    options.Configuration = builder.Configuration["Redis:Configuration"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.MapControllers();

// создаём БД и наполняем демо-данными
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.EnsureCreated();
    if (!await db.Categories.AnyAsync())
    {
        var electronics = new Category { Name = "Electronics" };
        db.Categories.Add(electronics);
        db.Products.AddRange(
            new Product { Name = "Phone", Price = 500m, Category = electronics },
            new Product { Name = "Laptop", Price = 1200m, Category = electronics }
        );
        await db.SaveChangesAsync();
    }
}

app.Run();
