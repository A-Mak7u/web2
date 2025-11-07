using Microsoft.EntityFrameworkCore;
using Monolith.Application;
using Monolith.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// инфраструктурные зависимости (EF Core, Redis и т.п.)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    // открытый CORS для локальной отладки
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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

// создаём БД при старте (для демо)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonolithDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
